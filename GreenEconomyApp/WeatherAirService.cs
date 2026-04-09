using System.Net.Http.Json;
using LiteDB;

namespace GreenEconomyApp;

/// <summary>
/// Servizio singleton per il recupero dati meteo (OpenWeatherMap) e qualità dell'aria (IQAir).
/// Implementa cache locale su LiteDB e analisi trend storico.
/// </summary>
public class WeatherAirService
{
    private static readonly WeatherAirService _instance = new();
    public static WeatherAirService Instance => _instance;

    /// <summary>Ultimo errore riscontrato durante una chiamata API.</summary>
    public string LastError { get; private set; } = "";

    private readonly HttpClient _http = new();

    // Meccanismo "parola magica" per demo in sede di presentazione
    private const string MAGIC_WORD = "GREEN_MAGIC";
    private const string HIDDEN_OWM_KEY = "f1644ee2c9e80abf89d7656e94512fd8";
    private const string HIDDEN_IQAIR_KEY = "07758eab-e117-4959-8b67-13a5081542eb";

    private static readonly Dictionary<string, string> _cityRegions = new()
    {
        { "Roma", "Europa" }, { "Parigi", "Europa" }, { "Londra", "Europa" }, { "Berlino", "Europa" },
        { "New York", "America" }, { "Los Angeles", "America" }, { "San Paolo", "America" }, { "Città del Messico", "America" },
        { "Tokyo", "Asia" }, { "Pechino", "Asia" }, { "Nuova Delhi", "Asia" }, { "Seoul", "Asia" }
    };

    private WeatherAirService()
    {
        _http.DefaultRequestHeaders.Add("User-Agent", "GreenEconomyApp-StudentProject");
    }

    // ──────────────────────────────────────────────
    //  API Pubblica
    // ──────────────────────────────────────────────

    /// <summary>Recupera dati combinati per nome città (con geocoding, cache e trend).</summary>
    public async Task<GreenEconomyRecord?> GetCombinedDataAsync(string city, string owmKeyInput, string iqAirKeyInput)
    {
        LastError = "";
        var (owmKey, iqAirKey) = ResolveApiKeys(owmKeyInput, iqAirKeyInput);

        try
        {
            // Cache: se abbiamo dati recenti, restituiamoli subito
            using var db = new LiteDatabase(AppConstants.DbPath);
            var col = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName);

            var cached = col.FindOne(x => x.City.ToLower() == city.ToLower()
                         && x.Timestamp > DateTime.Now.AddMinutes(-AppConstants.CacheMinutes));
            if (cached != null) return cached;

            // Geocoding: Città → Coordinate
            var geo = await GeocodeAsync(city, owmKey);
            if (geo == null) return null;

            // Dati meteo + aria in parallelo
            var (weather, air) = await FetchWeatherAndAirAsync(geo.Lat, geo.Lon, owmKey, iqAirKey);
            if (weather == null || air == null) return null;

            // Analisi trend rispetto allo storico
            string trend = AnalyzeTrend(col, geo.Name, air.Data.Current.Pollution.Aqi);

            // Salvataggio
            var record = BuildRecord(geo.Name, GetRegion(geo.Name), geo.Lat, geo.Lon, weather, air, trend);
            col.Insert(record);
            col.EnsureIndex(x => x.City);

            return record;
        }
        catch (Exception ex)
        {
            LastError = $"Errore imprevisto: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[WeatherAirService] {ex.Message}");
            return null;
        }
    }

    /// <summary>Recupera dati combinati per coordinate (senza geocoding).</summary>
    public async Task<GreenEconomyRecord?> GetCombinedDataByCoordsAsync(double lat, double lon, string owmKeyInput, string iqAirKeyInput)
    {
        LastError = "";
        var (owmKey, iqAirKey) = ResolveApiKeys(owmKeyInput, iqAirKeyInput);

        try
        {
            var (weather, air) = await FetchWeatherAndAirAsync(lat, lon, owmKey, iqAirKey);
            if (weather == null || air == null) return null;

            string cityName = string.IsNullOrEmpty(weather.Name)
                ? $"Lat {lat:F2}, Lon {lon:F2}"
                : weather.Name;

            var record = BuildRecord(cityName, GetRegion(cityName), lat, lon, weather, air, "Rilevazione da Mappa");

            using var db = new LiteDatabase(AppConstants.DbPath);
            var col = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName);
            col.Insert(record);
            col.EnsureIndex(x => x.City);

            return record;
        }
        catch (Exception ex)
        {
            LastError = $"Errore imprevisto: {ex.Message}";
            return null;
        }
    }

    /// <summary>Recupera dati per un set di città in parallelo.</summary>
    public async Task<List<GreenEconomyRecord>> GetRegionalComparisonAsync(string[] cities, string owmKey, string iqKey)
    {
        var tasks = cities.Select(c => GetCombinedDataAsync(c, owmKey, iqKey));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).Cast<GreenEconomyRecord>().ToList();
    }

    /// <summary>Recupera lo storico temperatura e qualità dell'aria per un set di città (ultimi 5 giorni).</summary>
    public async Task<List<GreenEconomyRecord>> GetHistoricalDataAsync(string[] cities, string owmKeyInput)
    {
        var (owmKey, _) = ResolveApiKeys(owmKeyInput, "");
        var historyRecords = new List<GreenEconomyRecord>();
        var today = DateTime.UtcNow.Date;

        foreach (var city in cities)
        {
            var geo = await GeocodeAsync(city, owmKey);
            if (geo == null) continue;

            for (int i = 1; i <= 5; i++)
            {
                var targetDate = today.AddDays(-i);
                long startUnix = new DateTimeOffset(targetDate).ToUnixTimeSeconds();
                long endUnix = startUnix + 3600;

                try
                {
                    // Weather from OneCall Timemachine
                    var urlW = $"https://api.openweathermap.org/data/3.0/onecall/timemachine?lat={geo.Lat}&lon={geo.Lon}&dt={startUnix}&appid={owmKey}&units=metric";
                    
                    // Air Pollution History from OpenWeather
                    var urlAqi = $"http://api.openweathermap.org/data/2.5/air_pollution/history?lat={geo.Lat}&lon={geo.Lon}&start={startUnix}&end={endUnix}&appid={owmKey}";

                    var wTask = _http.GetAsync(urlW);
                    var aTask = _http.GetAsync(urlAqi);
                    await Task.WhenAll(wTask, aTask);

                    var tResp = (await wTask).IsSuccessStatusCode ? await (await wTask).Content.ReadFromJsonAsync<OwmTimeMachineResponse>() : null;
                    var aResp = (await aTask).IsSuccessStatusCode ? await (await aTask).Content.ReadFromJsonAsync<OwmAirPollutionHistoryResponse>() : null;

                    double temp = tResp?.Data?.FirstOrDefault()?.Temp ?? 0;
                    int aqiRaw = aResp?.List?.FirstOrDefault()?.Main?.Aqi ?? 0;
                    int aqiUs = aqiRaw * 20; // Indicativo: map 1-5 scale to ~AQI US
                    
                    string mainPol = aResp?.List?.FirstOrDefault()?.Components?.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "N/A";

                    if (tResp != null || aResp != null)
                    {
                        historyRecords.Add(new GreenEconomyRecord
                        {
                            City = city,
                            Region = GetRegion(city),
                            Latitude = geo.Lat,
                            Longitude = geo.Lon,
                            Temperature = temp,
                            AQI = aqiUs,
                            MainPollutant = mainPol.ToUpper(),
                            TrendInfo = "Dato Storico",
                            Timestamp = targetDate
                        });
                    }
                }
                catch { }
            }
        }

        return historyRecords.OrderByDescending(x => x.Timestamp).ToList();
    }

    // ──────────────────────────────────────────────
    //  Metodi privati (logica estratta e riutilizzabile)
    // ──────────────────────────────────────────────

    /// <summary>Risolve le chiavi API: se una è la parola magica, usa quelle embedded.</summary>
    private (string owm, string iqair) ResolveApiKeys(string owmInput, string iqairInput)
    {
        bool isMagic = owmInput == MAGIC_WORD || iqairInput == MAGIC_WORD;
        return (
            isMagic ? HIDDEN_OWM_KEY : owmInput,
            isMagic ? HIDDEN_IQAIR_KEY : iqairInput
        );
    }

    /// <summary>Geocoding: converte nome città in coordinate via OpenWeatherMap.</summary>
    private async Task<CityCoordinates?> GeocodeAsync(string city, string owmKey)
    {
        var url = $"http://api.openweathermap.org/geo/1.0/direct?q={Uri.EscapeDataString(city)}&limit=1&appid={owmKey}";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            LastError = $"Geocoding fallito: {response.StatusCode} (Controlla API Key OpenWeatherMap)";
            return null;
        }

        var results = await response.Content.ReadFromJsonAsync<List<CityCoordinates>>();
        if (results == null || results.Count == 0)
        {
            LastError = "Città non trovata. Verifica l'ortografia.";
            return null;
        }

        return results[0];
    }

    /// <summary>Chiama le API meteo e aria in parallelo.</summary>
    private async Task<(WeatherData? weather, AirQualityData? air)> FetchWeatherAndAirAsync(
        double lat, double lon, string owmKey, string iqAirKey)
    {
        var weatherUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={owmKey}&units=metric";
        var airUrl = $"https://api.airvisual.com/v2/nearest_city?lat={lat}&lon={lon}&key={iqAirKey}";

        var wTask = _http.GetAsync(weatherUrl);
        var aTask = _http.GetAsync(airUrl);
        await Task.WhenAll(wTask, aTask);

        var wResp = await wTask;
        var aResp = await aTask;

        if (!wResp.IsSuccessStatusCode)
        {
            LastError = $"Errore OpenWeather: {wResp.StatusCode}";
            return (null, null);
        }
        if (!aResp.IsSuccessStatusCode)
        {
            LastError = $"Errore IQAir: {aResp.StatusCode}." +
                (aResp.StatusCode == System.Net.HttpStatusCode.Forbidden ? " Controlla la tua API Key IQAir." : "");
            return (null, null);
        }

        var weather = await wResp.Content.ReadFromJsonAsync<WeatherData>();
        var air = await aResp.Content.ReadFromJsonAsync<AirQualityData>();

        return (weather, air);
    }

    /// <summary>Confronta l'AQI attuale con lo storico per determinare il trend.</summary>
    private string AnalyzeTrend(ILiteCollection<GreenEconomyRecord> col, string cityName, int currentAqi)
    {
        var lastRecord = col.Find(x => x.City.ToLower() == cityName.ToLower())
                            .OrderByDescending(x => x.Timestamp)
                            .FirstOrDefault();

        if (lastRecord == null) return "Dati Iniziali";

        return currentAqi < lastRecord.AQI ? "Aria in miglioramento"
             : currentAqi > lastRecord.AQI ? "Aria in peggioramento"
             : "Qualità aria stabile";
    }

    /// <summary>Costruisce un GreenEconomyRecord dai dati raccolti.</summary>
    private GreenEconomyRecord BuildRecord(string city, string region, double lat, double lon,
        WeatherData weather, AirQualityData air, string trend)
    {
        return new GreenEconomyRecord
        {
            City = city,
            Region = region,
            Latitude = lat,
            Longitude = lon,
            Temperature = weather.Main.Temp,
            AQI = air.Data.Current.Pollution.Aqi,
            MainPollutant = air.Data.Current.Pollution.MainPollutant,
            TrendInfo = trend,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>Determina la macro-regione di una città.</summary>
    private string GetRegion(string cityName)
        => _cityRegions.TryGetValue(cityName, out var region) ? region : "Internazionale";
}

using System.Net.Http.Json;
using System.Linq;
using LiteDB;

namespace GreenEconomyApp;

// classe singleton per il recupero dati meteo e qualità dell'aria da OpenWeatherMap e IQAir
// un singleton e una classe istanziata che permette di avere solo una istanza della classe
// cosi si evitano errori di duplicazione di istanze
public class WeatherAirService
{
    // Singleton: assicura che in tutta l'applicazione esista una sola istanza di questa classe.
    // Questo serve a non sprecare risorse (es. una sola connessione HTTP) e gestire in modo sicuro il database.
    private static readonly WeatherAirService _instance = new();
    public static WeatherAirService Instance => _instance; // metodo per accedere all'istanza singleton

    public string LastError { get; private set; } = "";

    private readonly HttpClient _http = new(); // client http per effettuare le richieste API

    // Meccanismo "parola magica" per demo in sede di presentazione
    private const string MAGIC_WORD = "GREEN_MAGIC";
    private const string HIDDEN_OWM_KEY = "f1644ee2c9e80abf89d7656e94512fd8";
    private const string HIDDEN_IQAIR_KEY = "07758eab-e117-4959-8b67-13a5081542eb";

    // uso il client http per fare una DefaultRequestHeaders ovvero impostare un header che verrà inviato con tutte le richieste
    // in questo caso user-agent serve per identificare la nostra applicazione
    // in questo modo non devo specificare l'user-agent in ogni richiesta
    private WeatherAirService()
    {
        _http.DefaultRequestHeaders.Add("User-Agent", "GreenEconomyApp-StudentProject");
    }

    // recupera dati combinati per nome città (con geocoding, cache e trend).
    public async Task<GreenEconomyRecord?> GetCombinedDataAsync(string city, string owmKeyInput, string iqAirKeyInput)
    {
        LastError = "";
        
        // 1. Risolvo le chiavi API (controllo se ha usato la parola magica)
        var keys = ResolveApiKeys(owmKeyInput, iqAirKeyInput);
        string owmKey = keys.owm;
        string iqKey = keys.iqair;

        try
        {
            // apro il database locale
            using var db = new LiteDatabase(AppConstants.DbPath);
            var col = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName);

            // 2. Cerco nel database se ho già dati recenti (Cache)
            var cached = col.FindOne(x => x.City.ToLower() == city.ToLower());
            if (cached != null)
            {
                // Se sono passati meno di 15 minuti, restituisco il dato salvato
                if (cached.Timestamp > DateTime.Now.AddMinutes(-AppConstants.CacheMinutes))
                {
                    return cached;
                }
            }

            // 3. Recupero le coordinate geografiche della città
            var geo = await GeocodeAsync(city, owmKey);
            if (geo == null) return null;

            // ASINCRO: Mentre questa chiamata parla con internet, il programma non si blocca.
            // Le API vengono chiamate in parallelo per risparmiare tempo.
            var data = await FetchWeatherAndAirAsync(geo.Lat, geo.Lon, owmKey, iqKey);
            if (data.weather == null || data.air == null) return null;

            // 5. Analizzo il trend (aria in miglioramento/peggioramento)
            string trend = AnalyzeTrend(col, geo.Name, data.air.Data.Current.Pollution.Aqi);

            // 6. Costruisco il record finale e lo salvo nel DB
            var record = BuildRecord(geo.Name, GetRegion(geo.Country), geo.Lat, geo.Lon, data.weather, data.air, trend);
            col.Insert(record);

            return record;
        }
        catch (Exception ex)
        {
            LastError = "Errore durante il recupero dei dati.";
            System.Diagnostics.Debug.WriteLine($"[WeatherAirService] {ex.Message}");
            return null;
        }
    }

    // recupera dati combinati per coordinate (senza geocoding)
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

            var record = BuildRecord(cityName, GetRegion(weather.Sys.Country), lat, lon, weather, air, "Rilevazione da Mappa");

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

    // recupera dati per un set di città in parallelo
    public async Task<List<GreenEconomyRecord>> GetRegionalComparisonAsync(string[] cities, string owmKey, string iqKey)
    {
        var tasks = new List<Task<GreenEconomyRecord?>>();
        foreach (var city in cities)
        {
            tasks.Add(GetCombinedDataAsync(city, owmKey, iqKey));
        }

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).Cast<GreenEconomyRecord>().ToList();
    }


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
                    // API CALL: Usiamo gli endpoint 'history' e 'timemachine' per recuperare i record del passato.
                    // Nota: questi dati non sono 'live', ma vengono estratti dagli archivi storici dei server.
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
                            Region = GetRegion(geo.Country),
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

    // questo serve per risolvere le api key, se una e la parola magica, usa quelle embedded.
    private (string owm, string iqair) ResolveApiKeys(string owmInput, string iqairInput)
    {
        bool isMagic = owmInput == MAGIC_WORD || iqairInput == MAGIC_WORD; // vardo se ha la parola magica
        return (
            isMagic ? HIDDEN_OWM_KEY : owmInput,
            isMagic ? HIDDEN_IQAIR_KEY : iqairInput
        );
    }

    // converte nome città in coordinate via OpenWeatherMap
    private async Task<CityCoordinates?> GeocodeAsync(string city, string owmKey)
    {
        // EscapeDataString e qualcosa di c# che trasforma le robe cosi vanno bene per gli URL
        var url = $"http://api.openweathermap.org/geo/1.0/direct?q={Uri.EscapeDataString(city)}&limit=1&appid={owmKey}";
        var response = await _http.GetAsync(url); // questa e la chiamata vera e propria

        if (!response.IsSuccessStatusCode)
        {
            LastError = $"Geocoding fallito: {response.StatusCode}";
            return null;
        }

        // prende la risposta e la trasforma in una lista di CityCoordinates
        var results = await response.Content.ReadFromJsonAsync<List<CityCoordinates>>();
        if (results == null || results.Count == 0)
        {
            LastError = "Città non trovata. Verifica l'ortografia.";
            return null;
        }

        return results[0];
    }

    // effettiva funzione che chiama le api e prende i dati
    private async Task<(WeatherData? weather, AirQualityData? air)> FetchWeatherAndAirAsync(
        double lat, double lon, string owmKey, string iqAirKey)
    {
        var weatherUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={owmKey}&units=metric";
        var airUrl = $"https://api.airvisual.com/v2/nearest_city?lat={lat}&lon={lon}&key={iqAirKey}";

        var wTask = _http.GetAsync(weatherUrl);
        var aTask = _http.GetAsync(airUrl);
        await Task.WhenAll(wTask, aTask); // qui le prendo entrambe

        var wResp = await wTask; // dati
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

        var weather = await wResp.Content.ReadFromJsonAsync<WeatherData>(); // prendo i dati
        var air = await aResp.Content.ReadFromJsonAsync<AirQualityData>();

        return (weather, air);
    }

    // QUESTA FUNZIONE È IL "CERVELLO" STATISTICO:
    // Confronta il dato corrente con l'ultimo salvato per dire se la situazione migliora.
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

    // costruisce un GreenEconomyRecord dai dati raccolti
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

    // determina la macro-regione in base al codice nazione ISO
    private string GetRegion(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode)) return "Internazionale";
        countryCode = countryCode.ToUpper();

        string[] eu = { "IT", "FR", "ES", "DE", "GB", "UK", "NL", "BE", "CH", "AT", "PT", "GR", "SE", "NO", "FI", "DK", "PL", "CZ", "HU", "RO", "BG", "IE" };
        string[] am = { "US", "CA", "MX", "BR", "AR", "CL", "CO", "PE", "VE", "CU" };
        string[] as_ = { "CN", "JP", "KR", "IN", "RU", "ID", "TH", "VN", "MY", "PH", "SG", "TR", "SA", "IR", "IL" };
        string[] af = { "EG", "ZA", "NG", "MA", "DZ", "KE", "ET" };
        string[] oc = { "AU", "NZ" };

        if (eu.Contains(countryCode)) return "Europa";
        if (am.Contains(countryCode)) return "America";
        if (as_.Contains(countryCode)) return "Asia";
        if (af.Contains(countryCode)) return "Africa";
        if (oc.Contains(countryCode)) return "Oceania";

        return "Internazionale";
    }
}

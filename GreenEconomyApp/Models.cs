using System.Text.Json.Serialization;
using LiteDB;

namespace GreenEconomyApp;

// OWM /geo/1.0/direct → converte nome città in lat/lon (serve a tutte le altre API che vogliono coordinate)
public class CityCoordinates
{
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("country")] public string Country { get; set; } = "";
}

// OWM /data/2.5/weather → meteo attuale per lat/lon
public class WeatherData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("main")] public MainInfo Main { get; set; } = new();
    [JsonPropertyName("sys")] public SysInfo Sys { get; set; } = new(); // info paese

    public class MainInfo
    {
        [JsonPropertyName("temp")] public double Temp { get; set; }
        [JsonPropertyName("humidity")] public int Humidity { get; set; }
    }

    public class SysInfo
    {
        [JsonPropertyName("country")] public string Country { get; set; } = ""; // codice nazione ISO
    }
}

// OWM /data/3.0/onecall/timemachine → temperatura storica per lat/lon + timestamp Unix
// L'API restituisce un array "data" con le rilevazioni orarie del giorno richiesto
public class OwmTimeMachineResponse
{
    [JsonPropertyName("data")] public List<TimeMachineData> Data { get; set; } = new();

    public class TimeMachineData
    {
        [JsonPropertyName("temp")] public double Temp { get; set; }
    }
}

// OWM /air_pollution/history → qualità dell'aria storica per lat/lon + range Unix (start/end)
// Components è un Dictionary perché OWM non restituisce sempre gli stessi inquinanti:
// dipende dalla zona geografica quali sono presenti (CO, NO, NO2, O3, PM2.5, ecc.)
public class OwmAirPollutionHistoryResponse
{
    [JsonPropertyName("list")] public List<AirPollutionData> List { get; set; } = new();

    public class AirPollutionData
    {
        [JsonPropertyName("main")] public AirPollutionMain Main { get; set; } = new();
        [JsonPropertyName("components")] public Dictionary<string, double> Components { get; set; } = new();
    }

    public class AirPollutionMain
    {
        [JsonPropertyName("aqi")] public int Aqi { get; set; } // scala 1 (ottimo) → 5 (pessimo)
    }
}

// IQAir /v2/nearest_city → AQI attuale secondo standard US (scala 0-500, diversa da OWM)
// Usata in coppia con OWM: IQAir per il dato presente, OWM per lo storico
// La risposta è molto annidiata: data → current → pollution → valori
public class AirQualityData
{
    [JsonPropertyName("data")] public DataContent Data { get; set; } = new();

    public class DataContent
    {
        [JsonPropertyName("current")] public CurrentInfo Current { get; set; } = new();
    }

    public class CurrentInfo
    {
        [JsonPropertyName("pollution")] public PollutionInfo Pollution { get; set; } = new();
    }

    public class PollutionInfo
    {
        [JsonPropertyName("aqius")] public int Aqi { get; set; }
        [JsonPropertyName("mainus")] public string MainPollutant { get; set; } = ""; // es. "p2" = PM2.5
    }
}

// Record locale LiteDB: unisce i dati di OWM + IQAir in un'unica riga persistente
public class GreenEconomyRecord
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string City { get; set; } = "";
    public string Region { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Temperature { get; set; }
    public int AQI { get; set; }
    public string MainPollutant { get; set; } = "";
    public string TrendInfo { get; set; } = "";
}

public class Impostazioni
{
    public string Lingua { get; set; } = "Italiano";
    public string UnitaMisura { get; set; } = "°C - Celsius";
    public string ApiKey { get; set; } = "";       // OpenWeatherMap
    public string ApiKeyIQAir { get; set; } = "";  // IQAir
    public string CittaDefault { get; set; } = "";
}

// Decodifica i messaggi JSON mandati dalla mappa Leaflet via WebView2
public class MapMessage
{
    public string type { get; set; } = "";
    public double lat { get; set; }
    public double lng { get; set; }
}

public record CityData(string Name, double Lat, double Lon);
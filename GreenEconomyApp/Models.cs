using System.Text.Json.Serialization;
using LiteDB;

namespace GreenEconomyApp;


// Coordinate delle citta restituite da openweather
public class CityCoordinates
{
    [JsonPropertyName("lat")] public double Lat { get; set; }
    
    [JsonPropertyName("lon")] public double Lon { get; set; }
    
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    
    [JsonPropertyName("country")] public string Country { get; set; } = "";
}

// Dati meteo attuali restituiti da OpenWeatherMap ( quando clicchi su una citta ti restituisce questo)
public class WeatherData
{
    /// <summary>Nome della città rilevata.</summary>
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    
    /// <summary>Informazioni principali (temperatura e umidità).</summary>
    [JsonPropertyName("main")] public MainInfo Main { get; set; } = new();

    /// <summary>
    /// Classe interna per i dettagli termici e igrometrici.
    /// </summary>
    public class MainInfo
    {
        /// <summary>Temperatura attuale.</summary>
        [JsonPropertyName("temp")] public double Temp { get; set; }
        
        /// <summary>Percentuale di umidità.</summary>
        [JsonPropertyName("humidity")] public int Humidity { get; set; }
    }
}

/// <summary>
/// Rappresenta la risposta delle API storiche di OpenWeatherMap ("Time Machine").
/// </summary>
public class OwmTimeMachineResponse
{
    /// <summary>Lista di rilevazioni storiche nel tempo.</summary>
    [JsonPropertyName("data")] public List<TimeMachineData> Data { get; set; } = new();

    /// <summary>
    /// Singolo dato storico di temperatura.
    /// </summary>
    public class TimeMachineData
    {
        /// <summary>Temperatura registrata in un determinato momento.</summary>
        [JsonPropertyName("temp")] public double Temp { get; set; }
    }
}

/// <summary>
/// Risposta del servizio Air Pollution History di OpenWeatherMap per i dati storici sull'inquinamento.
/// </summary>
public class OwmAirPollutionHistoryResponse
{
    /// <summary>Lista dei dati di inquinamento registrati per diversi timestamp.</summary>
    [JsonPropertyName("list")] public List<AirPollutionData> List { get; set; } = new();

    /// <summary>
    /// Contiene i dati specifici di una rilevazione dell'aria.
    /// </summary>
    public class AirPollutionData
    {
        /// <summary>Informazioni generali sulla qualità dell'aria (AQI).</summary>
        [JsonPropertyName("main")] public AirPollutionMain Main { get; set; } = new();
        
        /// <summary>Dizionario dei singoli componenti chimici (CO, NO, NO2, O3, ecc.).</summary>
        [JsonPropertyName("components")] public Dictionary<string, double> Components { get; set; } = new();
    }

    /// <summary>
    /// Contiene l'indice di qualità dell'aria.
    /// </summary>
    public class AirPollutionMain
    {
        /// <summary>Indice AQI (1=Ottimo, 5=Pessimo).</summary>
        [JsonPropertyName("aqi")] public int Aqi { get; set; }
    }
}

/// <summary>
/// Risposta della API Air Quality di IQAir, focalizzata sui dati attuali dell'inquinamento.
/// </summary>
public class AirQualityData
{
    /// <summary>Contenitore principale dei dati della risposta.</summary>
    [JsonPropertyName("data")] public DataContent Data { get; set; } = new();

    public class DataContent
    {
        /// <summary>Informazioni sulla situazione attuale.</summary>
        [JsonPropertyName("current")] public CurrentInfo Current { get; set; } = new();
    }

    public class CurrentInfo
    {
        /// <summary>Dati specifici sull'inquinamento attuale.</summary>
        [JsonPropertyName("pollution")] public PollutionInfo Pollution { get; set; } = new();
    }

    public class PollutionInfo
    {
        /// <summary>Indice AQI secondo lo standard statunitense.</summary>
        [JsonPropertyName("aqius")] public int Aqi { get; set; }
        
        /// <summary>Inquinante principale rilevato (es. p2 per polveri sottili).</summary>
        [JsonPropertyName("mainus")] public string MainPollutant { get; set; } = "";
    }
}

// ──────────────────────────────────────────────
//  Record persistente (LiteDB)
// ──────────────────────────────────────────────

/// <summary>
/// Rappresenta una singola rilevazione completa salvata nel database locale LiteDB.
/// Unisce dati meteo e dati di qualità dell'aria.
/// </summary>
public class GreenEconomyRecord
{
    /// <summary>Identificativo univoco generato da LiteDB.</summary>
    [BsonId] public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    
    /// <summary>Data e ora in cui è stata effettuata la rilevazione.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>Nome della città osservata.</summary>
    public string City { get; set; } = "";
    
    /// <summary>Regione o continente di appartenenza.</summary>
    public string Region { get; set; } = "";
    
    /// <summary>Latitudine geografica al momento del salvataggio.</summary>
    public double Latitude { get; set; }
    
    /// <summary>Longitudine geografica al momento del salvataggio.</summary>
    public double Longitude { get; set; }
    
    /// <summary>Temperatura registrata.</summary>
    public double Temperature { get; set; }
    
    /// <summary>Indice di qualità dell'aria salvato.</summary>
    public int AQI { get; set; }
    
    /// <summary>Inquinante principale identificato dall'API.</summary>
    public string MainPollutant { get; set; } = "";
    
    /// <summary>Informazioni opzionali sull'andamento (trend).</summary>
    public string TrendInfo { get; set; } = "";
}

// ──────────────────────────────────────────────
//  Impostazioni utente (persistite in JSON)
// ──────────────────────────────────────────────

/// <summary>
/// Gestisce la configurazione dell'utente che viene salvata su disco (file JSON).
/// </summary>
public class Impostazioni
{
    /// <summary>Lingua dell'applicazione (es. Italiano, English).</summary>
    public string Lingua { get; set; } = "Italiano";
    
    /// <summary>Unità per la visualizzazione della temperatura.</summary>
    public string UnitaMisura { get; set; } = "°C - Celsius";
    
    /// <summary>Chiave API per i servizi OpenWeatherMap.</summary>
    public string ApiKey { get; set; } = "";
    
    /// <summary>Chiave API per i servizi IQAir.</summary>
    public string ApiKeyIQAir { get; set; } = "";
    
    /// <summary>Città caricata automaticamente all'avvio se impostata.</summary>
    public string CittaDefault { get; set; } = "";
}

// ──────────────────────────────────────────────
//  Modelli interni dell'applicazione
// ──────────────────────────────────────────────

/// <summary>
/// Struttura utilizzata per decodificare i messaggi JSON inviati dalla mappa JavaScript (WebView2).
/// </summary>
public class MapMessage
{
    /// <summary>Tipo di evento (es. 'click', 'move').</summary>
    public string type { get; set; } = "";
    
    /// <summary>Latitudine cliccata sulla mappa.</summary>
    public double lat { get; set; }
    
    /// <summary>Longitudine cliccata sulla mappa.</summary>
    public double lng { get; set; }
}

/// <summary>
/// Record compatto per definire i dati statici di una città (Nome e Coordinate).
/// Utilizzato principalmente per il campionamento dei dati continentali.
/// </summary>
/// <param name="Name">Nome della città.</param>
/// <param name="Lat">Latitudine.</param>
/// <param name="Lon">Longitudine.</param>
public record CityData(string Name, double Lat, double Lon);


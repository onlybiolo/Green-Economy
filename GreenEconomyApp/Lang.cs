using System.Text.Json;

namespace GreenEconomyApp;

public static class Lang
{
    private static Dictionary<string, string> _dizionario = new(); // qui dentro ci sono tutte le parole tradotte

    public static event Action? OnLanguageChanged; // uso un evento che viene scatenato ogni volta che cambio lingua
    public static string LinguaCorrente { get; private set; } = "it"; // qui dentro c'è la lingua corrente

    // "traduttore" per convertire il nome della lingua in iso (serve per capire da quale file json prendere le traduzioni)
    private static readonly Dictionary<string, string> _displayToCodice = new()
    {
        { "Italiano", "it" }, { "English", "en" }, { "Español", "es" },
        { "中文", "zh" }, { "Hindi", "hi" }
    };

    // "traduttore" per convertire l'iso in nome display (serve per la combobox impostazioni)
    private static readonly Dictionary<string, string> _codiceToDisplay = new()
    {
        { "it", "Italiano" }, { "en", "English" }, { "es", "Español" },
        { "zh", "中文" }, { "hi", "Hindi" }
    };

    // metodo per convertire il nome display della lingua nel codice ISO
    public static string CodiceFromDisplay(string displayName)
        => _displayToCodice.TryGetValue(displayName, out var codice) ? codice : "it";
    //TryGetValue cerca la chiave displayName e se la trova tira fuori il codice, ovvero l'iso
    //it come fallback
    
    // metodo inverso
    public static string DisplayFromCodice(string codice)
        => _codiceToDisplay.TryGetValue(codice, out var display) ? display : "Italiano";


    // uso un task per cambiare la lingua in modo da non bloccare l'interfaccia utente
    public static async Task CambiaLingua(string codice)
    {
        LinguaCorrente = codice;
        string percorso = Path.Combine(AppConstants.AssetsPath, $"lang_{codice}.json"); // qua per codice si intende it, en come i nomi dei json

        if (!File.Exists(percorso)) //se non esiste il file json, svuota il dizionario e ritorna
        {
            _dizionario = new Dictionary<string, string>();
            return;
        }

        string json = await File.ReadAllTextAsync(percorso);
        _dizionario = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                      ?? new Dictionary<string, string>();

        // il dizionaro prende tutto il file json interessato
        // il file e formattato apposta per essere
        // benvenuto , "Benvenuti...etc"
        // cosi la UI dovra semplicemente cercare la sua chiave e poi prendere il valore
        // in questo modo non devo scrivere if/else per ogni lingua

        OnLanguageChanged?.Invoke(); // scateno l'evento per notificare che la lingua e cambiata
    }

    public static string Get(string chiave)
        => _dizionario.TryGetValue(chiave, out string? valore) ? valore : $"[{chiave}]";
    
    //il get serve proprio per cercare quel benvenuto e ritornare il valore della chiave 
}
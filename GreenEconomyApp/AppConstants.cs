namespace GreenEconomyApp;

public static class AppConstants //costanti globali per tutta l'app
{
    public static readonly string DbPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.db"); // database che contiene i dati storici

    public const string DbCollectionName = "History"; // collezione che contiene le citta campionate

    public static readonly string AssetsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"); // cartella che contiene i file json e html 

    public static readonly string SettingsFolder =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp"); // cartella che contiene le impostazioni dell'app

    public static readonly string SettingsFile =
        Path.Combine(SettingsFolder, "impostazioni_utente.json"); // file che contiene le impostazioni dell'app

    public const int CacheMinutes = 15; // durata cache in minuti per i dati meteo/aria
                                        // praticamente ogni 15 minuti vengono aggiornati i dati meteo/aria
                                        // cosi si evita di fare troppe chiamate alle api (siccome e free ci sono tot chiamate)
}

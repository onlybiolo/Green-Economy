using System.Globalization;
using System.Text;
using LiteDB;

namespace GreenEconomyApp;

public static class DataExporter // gestisce l'esportazione dei dati raccolti in vari formati
{
    public static string ExportCsv(string? customPath = null) //funzione per esportare i dati in formato csv
    {
        string fileName = $"dataset_greeneconomy_{DateTime.Now:yyyyMMdd_HHmmss}.csv"; //nome del file csv
        string fullPath = customPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName); //percorso del file csv
 
        try
        {
            if (!File.Exists(AppConstants.DbPath))
                return Lang.Get("export_no_db"); //se non esiste il database, restituisce un messaggio di errore

            using var db = new LiteDatabase(AppConstants.DbPath); // apre il database
            var records = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName) //GreenEconomyRecord e la classe che contiene i dati 
                            //ne crea una lista con tutti i dati del database
                            .FindAll()
                            .OrderBy(r => r.Timestamp) // ordino per timestamp
                            .ToList(); 

            if (records.Count == 0)
                return Lang.Get("export_no_data");  //se non ci sono dati, restituisce un messaggio di errore

            var sb = new StringBuilder(); // uso string builder per costruire il file csv

            //header
            sb.AppendLine("Timestamp,City,Region,Latitude,Longitude,Temperature_C,AQI,MainPollutant,Trend");

            // costruisco tutte le righe del csv
            foreach (var r in records)
            {
                sb.AppendLine(string.Join(",",
                    r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    CsvEscape(r.City), // CsvEscape serve per evitare problemi con le virgole (automaticamente aggiunge le virgolette se necessario)
                    CsvEscape(r.Region),
                    r.Latitude.ToString(CultureInfo.InvariantCulture), //Invariant culture serve per evitare problemi con le virgole (automaticamente aggiunge le virgolette se necessario)
                    r.Longitude.ToString(CultureInfo.InvariantCulture),
                    r.Temperature.ToString("F1", CultureInfo.InvariantCulture),
                    r.AQI,
                    CsvEscape(r.MainPollutant),
                    CsvEscape(r.TrendInfo)
                ));
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8); //scrive il file csv
            return $"{Lang.Get("export_csv_ok")}\n{Lang.Get("export_records")}: {records.Count}\n{Lang.Get("export_path")}: {fullPath}";
            //ritorna un messaggio di successo con il numero di record e il percorso del file
            //Lang e la classe che gestisce la lingua
            //li in base al messaggio preimpostato sappiamo cosa ritornare all'utente
            //scriverebbe: export_csv_ok \n export_records: 100 \n export_path: C:\Users\Biolo\Documents\greeneconomy\GreenEconomyApp\dataset_greeneconomy_20260409_165428.csv
        }
        catch (Exception ex)
        {
            return $"{Lang.Get("export_error")}: {ex.Message}"; // qui invece manderebbe solo : export_error\n e il messaggio di errore
        }
    }

    public static string ExportDatabase(string? customPath = null) // questa gestisce l'esportazione del database in formato .db
    {
        string fileName = $"dataset_greeneconomy_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        string fullPath = customPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        try
        {
            if (!File.Exists(AppConstants.DbPath))
                return Lang.Get("export_no_db"); // stessa cosa di prima, cambia solo il nome del file e il tipo di file

            File.Copy(AppConstants.DbPath, fullPath, true); // se esiste fa il Copy, in modo da avere sempre una copia di backup
            return $"{Lang.Get("export_db_ok")}\n{Lang.Get("export_path")}: {fullPath}";
        }
        catch (Exception ex)
        {
            return $"{Lang.Get("export_error")}: {ex.Message}";
        }
    }

    private static string CsvEscape(string value) // escape personalizzato
    {
        if (string.IsNullOrEmpty(value)) return ""; // prima controllo se e nullo o vuoto

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n')) // se contiene virgole o apici o newline
            return $"\"{value.Replace("\"", "\"\"")}\""; //se li contiene lo mette tra virgolette, cosi non interferisce con il csv
        return value;
    }
}

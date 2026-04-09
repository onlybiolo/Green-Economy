namespace GreenEconomyApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        string lingua = "it";
        try
        {
            Impostazioni imp = FormImpostazioni.CaricaImpostazioni();
            lingua = Lang.CodiceFromDisplay(imp.Lingua);
        }
        catch { /* prima esecuzione: usa italiano di default */ }

        Lang.CambiaLingua(lingua).GetAwaiter().GetResult();
        Application.Run(new Form1());
    }
}
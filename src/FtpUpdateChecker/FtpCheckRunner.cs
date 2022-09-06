namespace FtpUpdateChecker;

public static class FtpCheckRunner
{
    public const int DefaultYear = 2021;
    public const int DefaultMonth = 9;
    public const int DefaultDay = 1;

    public const string DefaultHostname = "mcrai.vshosting.cz";
    public const string DefaultBaseFolder = "/McRAI";

    public static void Run(string webName, string baseFolder) => Run(null, null, null, webName, baseFolder);

    public static void Run(string? username, string? password,
                           string? path, string? webName,
                           string baseFolder = DefaultBaseFolder,
                           string host = DefaultHostname,
                           int day = DefaultDay, int month = DefaultMonth, int year = DefaultYear)
    {
        //Načíst přihlašovací údaje k FTP. Uložené v souboru ftp_logins.txt nebo zadané.
        FtpLoginParser login;
        try
        {
            login = new FtpLoginParser(webName, password, username, baseFolder);
        }
        catch (Exception exception)
        {
            Output.WriteError(exception.Message);
            return;
        }
        //Načtení data, odkud hlásit aktualizaci.
        var from = LoadFromDateTime(webName, baseFolder, day, month, year);

        //Spustit prohledávání FTP.
        using var checker = new FtpChecker(login.Username, login.Password, host, from);
        if (path is not null)
        {
            //Kontrola jedné složky.
            checker.Run(path, baseFolder, webName);
        }
        else for (var i = 0; i < login.Paths?.Length; i++)
        {
            //Kontrola více složek (více webů pod jednom FTP).
            checker.Run(login.Paths[i], baseFolder, webName);
        }
    }

    /// <summary>
    /// Načte datum, po kterém hlásit soubory jako aktualizované.
    /// </summary>
    /// <remarks>
    /// Pokud se parametry <paramref name="day"/>, <paramref name="month"/> a <paramref name="year"/>
    /// neliší od výchozích hodnot (<see cref="DefaultDay"/>, <see cref="DefaultMonth"/> a <see cref="DefaultYear"/>),
    /// pokusí se toto datum načíst ze souboru ".phplogs/date-<paramref name="webName"/>.txt",
    /// který existuje pokud již byly tyto kontroly dříve provedeny (soubor obsahuje datum, kdy se naposledy kontrolovala aktualita).
    /// Pokud tento soubor neexistuje, pokusí se datum načíst z <paramref name="webName"/> složky,
    /// (to odpovídá datu, kdy byly soubory webu poprvé staženy z FTP) jinak používá výchozí hodnoty.
    /// </remarks>
    private static DateTime LoadFromDateTime(string webName, string baseFolder, int day, int month, int year)
    {
        var modifiedDate = year != DefaultYear || month != DefaultMonth || day != DefaultDay;
        var webPath = Path.Join(baseFolder, "weby", webName);

        var dateFile = Path.Join(".phplogs", $"date-{webName}.txt");
        var date = File.Exists(dateFile)

            ? DateTime.Parse(File.ReadAllText(dateFile))

            : webName switch
            {
                not null and _ when !modifiedDate && Directory.Exists(webPath)
                    => Directory.GetCreationTime(webPath),
                _ => new(year, month, day)
            };

        File.WriteAllText(dateFile, DateTime.Now.ToString());
        return date;
    }
}

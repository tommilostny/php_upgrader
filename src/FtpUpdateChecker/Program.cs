namespace FtpUpdateChecker;

class Program
{
    const int _defaultYear = 2021;
    const int _defaultMonth = 9;
    const int _defaultDay = 1;

    const string _defaultHostname = "mcrai.vshosting.cz";
    const string _defaultBaseFolder = "/McRAI";

    /// <summary>
    /// Nástroj pro kontrolu nových souborů na FTP serveru po určitém datu.
    /// </summary>
    /// <param name="username">Uživatelské jméno na FTP. Není nutné zadávat, pokud je zadán argument --web-name (načte se dle názvu složky s prefixem 'tom-'). </param>
    /// <param name="password">Heslo pro přístup na FTP. Není-li zadáno, načte se z ftp_logins.txt dle zadaného --username nebo načteného jména z --web-name. </param>
    /// <param name="host">Url serveru.</param>
    /// <param name="path">Root složka skenovaného webu. Není nutné zadávat, pokud je zadán argument --web-name (načte se dle uživatelského jména v ftp_logins.txz). </param>
    /// <param name="year">Soubory nad tímto rokem se zobrazí jako aktualizované.</param>
    /// <param name="month">Soubory nad tímto měsícem se zobrazí jako aktualizované.</param>
    /// <param name="day">Soubory nad tímto dnem se zobrazí jako aktualizované.</param>
    /// <param name="baseFolder">Kde je soubor ftp_logins.txt?</param>
    /// <param name="webName">Název složky v '{baseFolder}\weby'. Získá datum vytvoření, přepisuje argumenty --year, --month a --day.</param>
    static void Main(string? username = null, string? password = null, string host = _defaultHostname,
        string? path = null, int year = _defaultYear, int month = _defaultMonth, int day = _defaultDay,
        string baseFolder = _defaultBaseFolder, string? webName = null)
    {
        FtpLoginParser login;
        try
        {
            login = new FtpLoginParser(webName, password, username, baseFolder);
        }
        catch (System.Exception exception)
        {
            ConsoleOutput.WriteError(exception.Message);
            return;
        }

        bool modifiedDate = year != _defaultYear || month != _defaultMonth || day != _defaultDay;
        var webPath = Path.Join(baseFolder, "weby", webName);
        var date = webName switch
        {
            not null and _ when !modifiedDate && Directory.Exists(webPath)
                => Directory.GetCreationTime(webPath),
            _ => new(year, month, day)
        };

        using var checker = new FtpChecker(login.Username, login.Password, host, date);
        if (path is not null)
        {
            checker.Run(path);
            return;
        }
        for (int i = 0; i < login.Paths?.Length; i++)
        {
            checker.Run(login.Paths[i]);
        }
    }
}

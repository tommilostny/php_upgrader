namespace FtpUpdateChecker;

public class McraiFtp
{
    public const int DefaultYear = 2021;
    public const int DefaultMonth = 9;
    public const int DefaultDay = 1;

    public const string DefaultHostname1 = "mcrai.vshosting.cz";
    public const string DefaultHostname2 = "mcrai2.vshosting.cz";
    public const string DefaultHostnameUpgrade = "mcrai-upgrade.vshosting.cz";
    public const string DefaultBaseFolder = "/McRAI";
    
    private readonly string? _path;
    private readonly string? _webName;
    private readonly string _baseFolder;
    private readonly string _host;
    private readonly int _day;
    private readonly int _month;
    private readonly int _year;
    private readonly FtpLoginParser _login;

    public McraiFtp(string? username, string? password,
                    string? path, string? webName,
                    string baseFolder = DefaultBaseFolder,
                    string host = DefaultHostname1,
                    int day = DefaultDay, int month = DefaultMonth, int year = DefaultYear)
    {
        //Načíst přihlašovací údaje k FTP. Uložené v souboru ftp_logins.txt nebo zadané.
        try
        {
            _login = new FtpLoginParser(webName, password, username, baseFolder);
        }
        catch (Exception exception)
        {
            Output.WriteError(exception.Message);
            throw;
        }
        _path = path;
        _webName = webName;
        _baseFolder = baseFolder;
        _host = host;
        _day = day;
        _month = month;
        _year = year;
    }

    public McraiFtp(string webName, string baseFolder, string host = DefaultHostname1)
        : this(null, null, null, webName, baseFolder, host)
    {
    }

    public void CheckForUpdates()
    {
        //Načtení data, odkud hlásit aktualizaci.
        var from = LoadFromDateTime(_webName, _baseFolder, _day, _month, _year);

        //Spustit prohledávání FTP.
        using var checker = new FtpChecker(_login.Username, _login.Password, _host, from);

        checker.Run(_path is not null ? _path : _login.Path, _baseFolder, _webName);
    }

    public void UploadFiles(ISet<string> localFiles, string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        using var uploader = new FtpUploader(_login.Username, _login.Password, upgradeServerHostname, localFiles);
        uploader.Run(_login.Path, _baseFolder, _webName);
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

        var dateFile = Path.Join(FtpOperation.PhpLogsDir, $"date-{webName}.txt");
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

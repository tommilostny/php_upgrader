namespace FtpUpdateChecker;

/// <summary>
/// Třída poskytující McRAI FTP funkcionalitu pro vnější použití.
/// </summary>
public sealed class McraiFtp
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
    private readonly LoginParser _login;

    public McraiFtp(string? username, string? password,
                    string? path, string? webName,
                    string baseFolder = DefaultBaseFolder,
                    string host = DefaultHostname1,
                    int day = DefaultDay, int month = DefaultMonth, int year = DefaultYear)
    {
        //Načíst přihlašovací údaje k FTP. Uložené v souboru ftp_logins.txt nebo zadané.
        try
        {
            _login = new LoginParser(webName, password, username, baseFolder);
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

    public void Update(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        //kontrola všech souborů na serveru mcrai1 a získání seznamu ne-PHP souborů
        using var fc1 = new FtpChecker(_login.Username, _login.Password, _host, _webName, _baseFolder, _day, _month, _year);
        fc1.Run(GetRemotePath(), _baseFolder, _webName);
        if (fc1.NonPhpFiles.Count == 0)
        {
            return;
        }
        //kontrola (mcrai-upgrade) a získání seznamu souborů, které je potřeba stáhnout z mcrai1
        //(neexistují na mcrai-upgrade nebo je na mcrai1 novější verze)
        using var fc2 = new FtpChecker(_login.Username, _login.Password, upgradeServerHostname, _webName, _baseFolder, _day, _month, _year)
        {
            KnownNewNonPhpFiles = fc1.KnownNewNonPhpFiles,
        };
        fc2.Run(fc1.NonPhpFiles);
        if (fc2.KnownNewNonPhpFiles.Count == 0)
        {
            return;
        }
        //stažení (z mcrai1) seznamu souborů do dočasné složky
        using var fd1 = new FtpDownloader(_login.Username, _login.Password, _host);
        var tempDir = fd1.Run(fc2.KnownNewNonPhpFiles, _baseFolder, _webName, _login.Path);
        if (!tempDir.Exists)
        {
            return;
        }
        //nahrání souborů z dočasné složky (synchronizace složky) na mcrai-upgrade
        //a smazání dočasné složky a souborů v ní
        using var fu2 = new FtpUploader(_login.Username, _login.Password, upgradeServerHostname);
        fu2.Run(tempDir, _login.Path);
    }

    public void Upload(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        using var uploader = new FtpUploader(_login.Username, _login.Password, upgradeServerHostname);
        uploader.Run(GetRemotePath(), _baseFolder, _webName);
    }

    public void Download()
    {
        using var downloader = new FtpDownloader(_login.Username, _login.Password, _host);
        downloader.Run(GetRemotePath(), _baseFolder, _webName);
    }

    private string GetRemotePath() => _path is not null ? _path : _login.Path;
}

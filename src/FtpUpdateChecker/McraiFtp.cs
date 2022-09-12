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

    public void GetUpdatesFromServer()
    {
        using var checker = new FtpChecker(_login.Username, _login.Password, _host, _webName, _baseFolder, _day, _month, _year);
        checker.Run(GetRemotePath(), _baseFolder, _webName);
    }

    public void UploadToServer(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        using var uploader = new FtpUploader(_login.Username, _login.Password, upgradeServerHostname);
        uploader.Run(GetRemotePath(), _baseFolder, _webName);
    }

    public void DownloadFromServer()
    {
        using var downloader = new FtpDownloader(_login.Username, _login.Password, _host);
        downloader.Run(GetRemotePath(), _baseFolder, _webName);
    }

    private string GetRemotePath() => _path is not null ? _path : _login.Path;
}

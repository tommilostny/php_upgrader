namespace FtpSync;

/// <summary>
/// Třída poskytující McRAI FTP funkcionalitu pro vnější použití.
/// </summary>
public sealed class McraiFtp
{
    public const string DefaultHostname1 = "mcrai.vshosting.cz";
    public const string DefaultHostname2 = "mcrai2.vshosting.cz";
    public const string DefaultHostnameUpgrade = "mcrai-upgrade.vshosting.cz";
    public const string DefaultBaseFolder = "/McRAI";

    private readonly string? _path;
    private readonly string? _webName;
    private readonly string _baseFolder;
    private readonly string _host;
    private readonly LoginParser _login;

    private string RemotePath => _path is not null ? _path : _login?.Path ?? throw new InvalidDataException("No remote path provided.");

    public McraiFtp(string? username, string? password,
                    string? path, string? webName,
                    string baseFolder = DefaultBaseFolder,
                    string host = DefaultHostname1)
    {
        try //Načíst přihlašovací údaje k FTP. Uložené v souboru ftp_logins.txt nebo zadané.
        {
            _login = new LoginParser(webName, password, username, baseFolder);
        }
        catch (Exception ex)
        {
            ColoredConsole.SetColor(ConsoleColor.Red).WriteLineError(ex.Message).ResetColor();
            throw;
        }
        _path = path;
        _webName = webName;
        _baseFolder = baseFolder;
        _host = host;
    }

    public McraiFtp(string webName, string baseFolder, string host = DefaultHostname1)
        : this(username: null, password: null, path: null, webName, baseFolder, host)
    {
    }

    public async Task UpdateAsync(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        await CheckCredsAndRunTask(async () =>
        {
            using var synchronizer = new FtpSynchronizer(RemotePath, _baseFolder, _webName!, _host,
                                                         upgradeServerHostname, _login.Username!, _login.Password!);
            await synchronizer.SynchronizeAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task UploadAsync(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        await CheckCredsAndRunTask(async () =>
        {
            using var uploader = new FtpUploader(RemotePath, _baseFolder, _webName!, upgradeServerHostname, _login.Username!, _login.Password!);
            await uploader.UploadPhpsAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task DownloadAsync()
    {
        await CheckCredsAndRunTask(async () =>
        {
            using var downloader = new FtpDownloader(RemotePath, _baseFolder, _webName!, _host, _login.Username!, _login.Password!);
            await downloader.DownloadPhpsAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async Task CheckCredsAndRunTask(Func<Task> task)
    {
        if (_webName is not null && _login is { Username: not null, Password: not null })
        {
            await task().ConfigureAwait(false);
        }
    }
}

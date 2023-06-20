using FtpSync;

namespace FtpUpdateChecker;

/// <summary>
/// Třída poskytující McRAI FTP funkcionalitu pro vnější použití.
/// </summary>
public sealed class McraiFtp : IDisposable
{
    public const int DefaultYear = 2021;
    public const int DefaultMonth = 9;
    public const int DefaultDay = 1;

    public const string DefaultHostname1 = "mcrai.vshosting.cz";
    public const string DefaultHostname2 = "mcrai2.vshosting.cz";
    public const string DefaultHostnameUpgrade = "mcrai-upgrade.vshosting.cz";
    public const string DefaultBaseFolder = "/McRAI";
    public const string PhpLogsDir = ".phplogs";

    private readonly string? _path;
    private readonly string? _webName;
    private readonly string _baseFolder;
    private readonly string _host;
    private readonly int _day;
    private readonly int _month;
    private readonly int _year;
    private readonly LoginParser _login;
    private readonly Output _output;
    private readonly string[]? _ignoreFolders;

    public McraiFtp(string? username, string? password,
                    string? path, string? webName,
                    string baseFolder = DefaultBaseFolder,
                    string host = DefaultHostname1,
                    int day = DefaultDay, int month = DefaultMonth, int year = DefaultYear)
    {
        try //Načíst přihlašovací údaje k FTP. Uložené v souboru ftp_logins.txt nebo zadané.
        {
            _login = new LoginParser(webName, password, username, baseFolder);
        }
        catch (Exception exception)
        {
            _output.WriteErrorAsync(null, exception.Message).RunSynchronously();
            throw;
        }
        var phpLogFilePath = Path.Join(Environment.GetEnvironmentVariable("OneDriveConsumer"),
                                       PhpLogsDir,
                                       $"{webName}-{DateTime.UtcNow.Ticks}.txt");
        if (File.Exists(phpLogFilePath))
        {
            File.Delete(phpLogFilePath);
        }
        else
        {
            Directory.CreateDirectory(PhpLogsDir);
        }
        _output = new Output(phpLogFilePath);
        _path = path;
        _webName = webName;
        _baseFolder = baseFolder;
        _host = host;
        _day = day;
        _month = month;
        _year = year;
    }

    public McraiFtp(string webName, string baseFolder, string[]? ignoreFolders, string host = DefaultHostname1)
        : this(null, null, null, webName, baseFolder, host)
    {
        _ignoreFolders = ignoreFolders;
    }

    public void Dispose()
    {
        _output.Dispose();
    }

    public async Task UpdateAsync(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        var synchronizer = new FtpSynchronizer(GetRemotePath(), _baseFolder, _webName);
        await synchronizer.NewSync(_host, upgradeServerHostname, _login.Username, _login.Password);
        /*
        //kontrola všech souborů na serveru mcrai1 a získání seznamu ne-PHP souborů
        using var fc1 = new FtpChecker(_output, _login.Username, _login.Password, _host, _webName, _baseFolder, _day, _month, _year, _ignoreFolders)
        {
            Name = "FC1",
            Color = ConsoleColor.Blue,
        };
        //kontrola (mcrai-upgrade) a získání seznamu souborů, které je potřeba stáhnout z mcrai1
        //(neexistují na mcrai-upgrade nebo je na mcrai1 novější verze)
        using var fc2 = new FtpChecker(_output, _login.Username, _login.Password, upgradeServerHostname, _webName, _baseFolder, _day, _month, _year)
        {
            Name = "FC2",
            Color = ConsoleColor.DarkCyan,
        };
        //stažení (z mcrai1) seznamu souborů do dočasné složky
        using var fd1 = new FtpDownloader(_output, _login.Username, _login.Password, _host)
        {
            Name = "FD1",
            Color = ConsoleColor.DarkGreen,
        };
        //nahrání souborů z dočasné složky (synchronizace složky) na mcrai-upgrade
        //a smazání dočasné složky a souborů v ní
        using var fu2 = new FtpUploader(_output, _login.Username, _login.Password, upgradeServerHostname)
        {
            Name = "FU2",
            Color = ConsoleColor.Magenta,
        };

        //spuštění ve více vláknech paralelně
        var q1 = new ConcurrentQueue<RemoteFileInfo?>();
        var q2 = new ConcurrentQueue<RemoteFileInfo?>();
        var q3 = new ConcurrentQueue<string?>();
        var remPath = GetRemotePath();
        DirectoryInfo? tempDir = null;
        var tasks = new List<Func<Task>>
        {
            async () => await fc1.RunAsync(q1, q2, remPath, _baseFolder, _webName),
            async () => await fc2.RunAsync(q1, q2),
            async () => tempDir = await fd1.RunAsync(q2, q3, _baseFolder, _webName, remPath),
            async () => await fu2.RunAsync(q3, _baseFolder, _webName, remPath),
        };
        try
        {
            await Task.WhenAll(tasks.AsParallel().Select(async task => await task()));
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
                Console.WriteLine($"Exception type {innerEx.GetType()} from {innerEx.Source}.");
        }
        finally
        {
            if (tempDir?.Exists is true)
                tempDir.Delete(recursive: true);
        }*/
    }

    public void Upload(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        using var uploader = new FtpUploader(_output, _login.Username, _login.Password, upgradeServerHostname);
        uploader.Run(GetRemotePath(), _baseFolder, _webName);
    }

    public void Download()
    {
        using var downloader = new FtpDownloader(_output, _login.Username, _login.Password, _host);
        downloader.Run(GetRemotePath(), _baseFolder, _webName);
    }

    private string GetRemotePath() => _path is not null ? _path : _login.Path;
}

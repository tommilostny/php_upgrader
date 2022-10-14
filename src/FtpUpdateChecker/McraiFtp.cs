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

    public McraiFtp(string webName, string baseFolder, string host = DefaultHostname1)
        : this(null, null, null, webName, baseFolder, host)
    {
    }

    public void Dispose()
    {
        _output.Dispose();
    }

    public async Task UpdateAsync(string upgradeServerHostname = DefaultHostnameUpgrade)
    {
        var q1 = new Queue<RemoteFileInfo?>();
        var q2 = new Queue<RemoteFileInfo?>();
        var q3 = new Queue<string?>();

        //kontrola všech souborů na serveru mcrai1 a získání seznamu ne-PHP souborů
        using var fc1 = new FtpChecker(_output, _login.Username, _login.Password, _host, _webName, _baseFolder, _day, _month, _year)
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
        await fc1.PrintNameAsync(_output);
        var fc1Message = $"{_host} FTP checker.";
        Console.WriteLine(fc1Message);
        await _output.WriteLineToFileAsync(fc1Message);

        await fc2.PrintNameAsync(_output);
        var fc2Message = $"{upgradeServerHostname} FTP checker.";
        Console.WriteLine(fc2Message);
        await _output.WriteLineToFileAsync(fc2Message);

        await fd1.PrintNameAsync(_output);
        var fd1Message = $"{_host} FTP downloader.";
        Console.WriteLine(fd1Message);
        await _output.WriteLineToFileAsync(fd1Message);

        await fu2.PrintNameAsync(_output);
        var fu2Message = $"{upgradeServerHostname} FTP uploader.";
        Console.WriteLine(fu2Message);
        await _output.WriteLineToFileAsync(fu2Message);
        Console.WriteLine();

        //spuštění ve více vláknech
        var uploadTask = fu2.RunAsync(q3, _baseFolder, _webName, GetRemotePath());
        var downloadTask = fd1.RunAsync(q2, q3, _baseFolder, _webName, GetRemotePath());
        var checkTask2 = fc2.RunAsync(q1, q2);
        var checkTask1 = fc1.RunAsync(q1, q2, GetRemotePath(), _baseFolder, _webName);

        await Task.WhenAll(checkTask1, checkTask2, uploadTask);
        var tempDir = await downloadTask;
        if (tempDir.Exists)
        {
            tempDir.Delete(recursive: true);
        }
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

using EO = WinSCP.EnumerationOptions;

namespace FtpUpdateChecker;

/// <summary> Třída nad knihovnou WinSCP kontrolující soubory na FTP po určitém datu. </summary>
public class FtpChecker : IDisposable
{
    private const string _phpLogsDir = ".phplogs";

    private readonly SessionOptions _sessionOptions;
    private readonly Session _session = new();

    /// <summary> Datum, od kterého hlásit změnu. </summary>
    public DateTime FromDate { get; }

    /// <summary> Celkový počet souborů. </summary>
    public uint FileCount { get; private set; }

    /// <summary> Celkový počet složek. </summary>
    public uint FolderCount { get; private set; }

    /// <summary> Počet souborů přidaných po datu <see cref="FromDate"/>. </summary>
    public uint FoundCount { get; private set; }

    /// <summary> Počet PHP souborů přidaných po datu <see cref="FromDate"/>. </summary>
    public uint PhpFoundCount { get; private set; }

    /// <summary> Inicializace sezení spojení WinSCP, nastavení data. </summary>
    public FtpChecker(string username, string password, string hostname, DateTime fromDate)
    {
        _sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = hostname,
            UserName = username,
            Password = password,
            FtpSecure = FtpSecure.Explicit
        };
        FromDate = fromDate;
    }

    /// <summary> Spustit procházení všech souborů na FTP serveru v zadané cestě. </summary>
    public void Run(string path)
    {
        if (!_session.Opened)
        {
            Console.WriteLine($"Připojování k FTP {_sessionOptions.UserName}@{_sessionOptions.HostName} ...");
            try //Connect
            {
                _session.Open(_sessionOptions);
                Console.WriteLine("Připojení proběhlo úspěšně!\n");
            }
            catch (SessionRemoteException)
            {
                Output.WriteError("Připojení k FTP serveru selhalo pro zadané uživatelské jméno a heslo.");
                return;
            }
        }
        var phpLogFilePath = $"{_phpLogsDir}/{_sessionOptions.UserName}-{path}.txt";
        if (File.Exists(phpLogFilePath))
        {
            File.Delete(phpLogFilePath);
        }
        else
        {
            Directory.CreateDirectory(_phpLogsDir);
        }
        Console.WriteLine($"Probíhá kontrola '{path}'...");
        var enumerationOptions = EO.EnumerateDirectories | EO.AllDirectories;
        var fileInfos = _session.EnumerateRemoteFiles(path, null, enumerationOptions);

        FileCount = FolderCount = PhpFoundCount = FoundCount = 0;
        int messageLength = Output.WriteStatus(this);
        try //Enumerate files
        {
            foreach (var fileInfo in fileInfos)
            {
                Console.Write('\r');

                if (fileInfo.IsDirectory)
                {
                    FolderCount++;
                    messageLength = Output.WriteStatus(this);
                    continue;
                }
                if (fileInfo.LastWriteTime >= FromDate)
                {
                    FoundCount++;
                    bool isPhp;
                    if (isPhp = fileInfo.FullName.EndsWith(".php"))
                    {
                        PhpFoundCount++;
                    }
                    Output.WriteFoundFile(this, fileInfo, messageLength, isPhp, phpLogFilePath);
                }
                FileCount++;
                messageLength = Output.WriteStatus(this);
            }
        }
        catch (SessionRemoteException)
        {
            Output.WriteError($"Zadaná cesta '{path}' na serveru neexistuje.");
        }
        Output.WriteCompleted(phpLogFilePath, PhpFoundCount);
    }

    /// <summary> Uzavřít spojení k FTP serveru. </summary>
    public void Dispose()
    {
        if (_session.Opened)
        {
            _session.Close();
        }
        GC.SuppressFinalize(this);
    }
}

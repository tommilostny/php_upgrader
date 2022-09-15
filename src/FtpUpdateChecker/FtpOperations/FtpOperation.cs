namespace FtpUpdateChecker.FtpOperations;

internal abstract class FtpOperation : IDisposable
{
    public const string PhpLogsDir = ".phplogs";

    protected readonly SessionOptions _sessionOptions;
    protected readonly Session _session = new();

    /// <summary> Inicializace sezení spojení WinSCP. </summary>
    public FtpOperation(string username, string password, string hostname)
    {
        _sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = hostname,
            UserName = username,
            Password = password,
            FtpSecure = FtpSecure.Explicit
        };
    }

    /// <summary> Spustit FTP operaci. </summary>
    public abstract void Run(string path, string baseFolder, string webName);

    /// <summary> Uzavřít spojení k FTP serveru. </summary>
    public void Dispose()
    {
        _session.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void TryOpenSession(bool verbose = true)
    {
        if (!_session.Opened)
        {
            if (verbose) Console.WriteLine($"Připojování k FTP {_sessionOptions.UserName}@{_sessionOptions.HostName} ...");
            try //Connect
            {
                _session.Open(_sessionOptions);
                if (verbose) Console.WriteLine("Připojení proběhlo úspěšně!\n");
            }
            catch (SessionRemoteException)
            {
                Output.WriteError($"Připojení k FTP {_sessionOptions.HostName} selhalo.");
                throw;
            }
        }
    }
}

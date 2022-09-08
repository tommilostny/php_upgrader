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
        if (_session.Opened)
        {
            _session.Close();
        }
        GC.SuppressFinalize(this);
    }

    protected void TryOpenSession()
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
                throw;
            }
        }
    }
}

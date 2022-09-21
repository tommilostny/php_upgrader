namespace FtpUpdateChecker.FtpOperations;

internal abstract class FtpOperation : IDisposable
{
    public const string PhpLogsDir = ".phplogs";

    protected readonly SessionOptions _sessionOptions;
    protected readonly Session _session = new();

    public string Name { private get; init; }

    public ConsoleColor Color { private get; init; }

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

    public override string ToString() => Name;

    public void PrintName()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            Console.ForegroundColor = Color;
            Console.Write(Name);
            Console.ResetColor();
            Console.Write(": ");
        }
    }

    protected void TryOpenSession(bool verbose = true)
    {
        if (!_session.Opened)
        {
            if (verbose)
            {
                PrintName();
                Console.WriteLine($"Připojování k FTP {_sessionOptions.UserName}@{_sessionOptions.HostName}...");
            }
            int retries = 4;
            do
            {
                try //Connect
                {
                    _session.Open(_sessionOptions);
                    if (verbose)
                    {
                        PrintName();
                        Console.WriteLine("Připojení proběhlo úspěšně!");
                    }
                    retries = 0;
                }
                catch (SessionRemoteException err)
                {
                    if (--retries == 0)
                    {
                        Output.WriteError($"{Name}: Připojení k FTP {_sessionOptions.HostName} selhalo.");
                        Output.WriteError(err.Message);
                        throw;
                    }
                }
            }
            while (retries > 0);
        }
    }

    protected void SafeSessionAction(Action action, int retries = 3)
    {
        try
        {
            action();
        }
        catch //Chyba při komunikaci se serverem, znovu připojit a zkusit načíst informace o souboru.
        {
            _session.Close();
            TryOpenSession(verbose: false);
            if (--retries > 0)
            {
                SafeSessionAction(action, retries);
            }
        }
    }
}

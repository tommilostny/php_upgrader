namespace FtpUpdateChecker.FtpOperations;

internal abstract class FtpOperation : IDisposable
{
    protected readonly SessionOptions _sessionOptions;
    protected readonly Session _session = new();
    protected readonly Output _output;
    private readonly Random _random = new();

    public string Name { private get; init; }

    public ConsoleColor Color { private get; init; }

    /// <summary> Inicializace sezení spojení WinSCP. </summary>
    public FtpOperation(Output output, string username, string password, string hostname)
    {
        _sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = hostname,
            UserName = username,
            Password = password,
            FtpSecure = FtpSecure.Explicit
        };
        _output = output;
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

    public async Task PrintNameAsync(Output output)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            Console.ForegroundColor = Color;
            Console.Write(Name);
            Console.ResetColor();
            Console.Write(": ");
        }
        await output.WriteToFileAsync($"{Name}: ");
    }

    protected async Task TryOpenSessionAsync(bool verbose = true)
    {
        if (!_session.Opened)
        {
            if (verbose)
            {
                await PrintNameAsync(_output);
                var connectingMessage = $"Připojování k FTP {_sessionOptions.UserName}@{_sessionOptions.HostName}...";
                Console.WriteLine(connectingMessage);
                await _output.WriteLineToFileAsync(connectingMessage);
            }
            int retries = 4;
            do
            {
                try //Connect
                {
                    _session.Open(_sessionOptions);
                    if (verbose)
                    {
                        await PrintNameAsync(_output);
                        const string connectionSuccess = "Připojení proběhlo úspěšně!";
                        Console.WriteLine(connectionSuccess);
                        await _output.WriteLineToFileAsync(connectionSuccess);
                    }
                    retries = 0;
                }
                catch (SessionRemoteException err)
                {
                    if (--retries == 0)
                    {
                        await _output.WriteErrorAsync(this, $"Připojení k FTP {_sessionOptions.HostName} selhalo.");
                        await _output.WriteErrorAsync(this, err.Message);
                        throw;
                    }
                }
            }
            while (retries > 0);
        }
    }

    protected async Task SafeSessionActionAsync(Action action, int retries = 4)
    {
        try
        {
            action();
        }
        catch //Chyba při komunikaci se serverem, znovu připojit a zkusit načíst informace o souboru.
        {
            _session.Close();
            await Task.Delay((retries << 1) * _random.Next(1, 100));
            await TryOpenSessionAsync(verbose: false);
            if (--retries > 0)
            {
                await SafeSessionActionAsync(action, retries);
            }
        }
    }
}

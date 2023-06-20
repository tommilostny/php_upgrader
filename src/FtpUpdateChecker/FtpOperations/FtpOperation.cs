namespace FtpUpdateChecker.FtpOperations;

internal abstract class FtpOperation : IDisposable
{
    protected readonly SessionOptions _sessionOptions;
    protected readonly Session _session = new();
    protected readonly Output _output;
    private readonly Random _random = new();

    public string Name { private get; init; }

    public ConsoleColor Color { private get; init; }

    protected abstract string Type { get; }

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
        //PrintMessageAsync(, $"{hostname} {Type}.");
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

    public async Task PrintMessageAsync(Output output, string message)
    {
        Thread.BeginCriticalRegion();
        if (!string.IsNullOrEmpty(Name))
        {
            ColoredConsole.SetColor(Color)
                .Write(Name)
                .ResetColor()
                .Write(": ");
            if (!string.IsNullOrEmpty(message))
                ColoredConsole.WriteLine(message);
        }
        Thread.EndCriticalRegion();
        await output.WriteToFileAsync($"{Name}: {message}");
    }

    protected async Task TryOpenSessionAsync(bool verbose = true)
    {
        if (!_session.Opened)
        {
            if (verbose)
            {
                await PrintMessageAsync(_output, $"Připojování k FTP {_sessionOptions.UserName}@{_sessionOptions.HostName}...");
            }
            int retries = 4;
            do
            {
                try //Connect
                {
                    _session.Open(_sessionOptions);
                    if (verbose)
                    {
                        await PrintMessageAsync(_output, "Připojení proběhlo úspěšně!");
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

    protected async Task SafeSessionActionAsync(Func<Task> task, int retries = 3)
    {
        try
        {
            await task();
        }
        catch //Chyba při komunikaci se serverem, znovu připojit a zkusit načíst informace o souboru.
        {
            _session.Close();
            await Task.Delay(retries * _random.Next(1, 100));
            await TryOpenSessionAsync(verbose: false);
            if (--retries > 0)
            {
                await SafeSessionActionAsync(task, retries);
            }
        }
    }
}

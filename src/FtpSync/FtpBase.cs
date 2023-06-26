namespace FtpSync;

internal abstract class FtpBase : IDisposable
{
    protected const int _defaultRetries = 3;
    protected readonly object _writeLock = new();
    protected static readonly Random _random = new();
    protected readonly string _path;
    protected readonly string _baseFolder;
    protected readonly string _webName;

    protected AsyncFtpClient Client1 { get; private set; }

    protected List<FtpRule> PhpRules { get; } = new() { new PhpFtpRule() };

    protected FtpBase(string path, string baseFolder, string webName, string server, string username, string password)
    {
        _path = path;
        _baseFolder = baseFolder;
        _webName = webName;

        var creds = new NetworkCredential(username, password);
        var config = new FtpConfig()
        {
            EncryptionMode = FtpEncryptionMode.Explicit,
        };
        Client1 = new AsyncFtpClient(server, creds, port: 21, config);
    }

    public virtual void Dispose()
    {
        Client1.Dispose();
        GC.SuppressFinalize(this);
    }

    protected async Task ConnectClientAsync(AsyncFtpClient client)
    {
        lock (_writeLock)
            ColoredConsole.WriteLine($"{ConsoleColor.DarkYellow}{client.Credentials.UserName}@{client.Host}{Symbols.PREVIOUS_COLOR}: připojování...");

        await client.Connect().ConfigureAwait(false);

        lock (_writeLock)
            ColoredConsole.WriteLine($"{ConsoleColor.DarkYellow}{client.Credentials.UserName}@{client.Host}{Symbols.PREVIOUS_COLOR}: připojeno.");
    }

    private sealed class PhpFtpRule : FtpRule
    {
        public override bool IsAllowed(FtpListItem result)
        {
            return result.Name.EndsWith(".php", StringComparison.OrdinalIgnoreCase);
        }
    }
        
    internal enum FtpOp { Upload , Download }

    protected sealed class FtpProgressReport : IProgress<FtpProgress>
    {
        private readonly string? _lineStartStr;
        private readonly FO _operation;

        public FtpProgressReport(FO operation)
        {
            _operation = operation;
            _lineStartStr = _operation == FO.Upload ? "\r🔼 Probíhá upload\t" : "\r🔽 Probíhá download\t";
        }

        public void Report(FtpProgress value)
        {
            ColoredConsole.Write(_lineStartStr)
                          .SetColor(ConsoleColor.DarkGray)
                          .Write(_operation == FO.Download ? value.RemotePath : value.LocalPath)
                          .ResetColor()
                          .Write($" ({value.Progress:f2} %)...");

            if (value.Progress >= 100.0)
                ColoredConsole.WriteLine();
        }
    }
}

﻿namespace FtpSync;

internal abstract class FtpBase : IDisposable
{
    protected const byte _nStreams = 8;
    protected const int _defaultRetries = 3;
    protected readonly object _writeLock = new();
    protected static readonly Random _random = new();
    protected readonly string _path;
    protected readonly string _baseFolder;
    protected readonly string _webName;

    protected AsyncFtpClient Client1 { get; private set; }

    protected List<FtpRule> PhpRules { get; } = [new PhpFtpRule()];

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
        Client1.ValidateCertificate += ValidateVshostingCert;
    }

    public virtual void Dispose()
    {
        Client1.Dispose();
        GC.SuppressFinalize(this);
    }

    protected async Task ConnectClientVerboseAsync(AsyncFtpClient client)
    {
        lock (_writeLock)
            ColoredConsole.WriteLine($"{ConsoleColor.DarkYellow}{client.Credentials.UserName}@{client.Host}{Symbols.PREVIOUS_COLOR}: připojování...");

        int retries = _defaultRetries * 3;
        do try
        {
            await client.Connect().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (--retries == 0)
            {
                lock (_writeLock)
                    ColoredConsole.WriteLine($"{ConsoleColor.Red}{client.Credentials.UserName}@{client.Host}{Symbols.PREVIOUS_COLOR}: chyba připojení: {ex.Message}");
                throw;
            }
            lock (_writeLock)
                ColoredConsole.WriteLine($"{ConsoleColor.Yellow}{client.Credentials.UserName}@{client.Host}{Symbols.PREVIOUS_COLOR}: chyba připojení: {ex.Message}, opakování...");
            await Task.Delay(_random.Next(500, 3000)).ConfigureAwait(false);
        }
        while (!client.IsConnected);

        lock (_writeLock)
            ColoredConsole.WriteLine($"{ConsoleColor.DarkYellow}{client.Credentials.UserName}@{client.Host}{Symbols.PREVIOUS_COLOR}: připojeno.");
    }

    protected static async Task<ConcurrentQueue<AsyncFtpClient>> InitClientsQueueAsync(AsyncFtpClient first)
    {
        var clients = new ConcurrentQueue<AsyncFtpClient>();
        clients.Enqueue(first);
        for (byte i = 1; i < _nStreams; i++)
        {
            var dc = new AsyncFtpClient(first.Host, first.Credentials, config: first.Config);
            dc.ValidateCertificate += ValidateVshostingCert;

            await dc.Connect().ConfigureAwait(false);
            clients.Enqueue(dc);
        }
        return clients;
    }

    protected static async Task<AsyncFtpClient> ExtractClientAsync(ConcurrentQueue<AsyncFtpClient> clients)
    {
        AsyncFtpClient? cl;
        while (!clients.TryDequeue(out cl)) await Task.Yield();
        return cl;
    }

    protected static async Task<(AsyncFtpClient, AsyncFtpClient)> ExtractClientsAsync(ConcurrentQueue<AsyncFtpClient> clients1, ConcurrentQueue<AsyncFtpClient> clients2) =>
    (
        await ExtractClientAsync(clients1).ConfigureAwait(false),
        await ExtractClientAsync(clients2).ConfigureAwait(false)
    );

    protected static void CleanupClientsQueue(ConcurrentQueue<AsyncFtpClient> clients, AsyncFtpClient first)
    {
        while (clients.TryDequeue(out var dc))
            if (dc != first)
                dc.Dispose();
    }

    protected static void ValidateVshostingCert(object control, FtpSslValidationEventArgs e)
    {
        e.Accept = e.Certificate.Subject.Contains("vshosting.cz", StringComparison.OrdinalIgnoreCase);
    }
}

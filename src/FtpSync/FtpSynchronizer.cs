namespace FtpSync;

internal sealed class FtpSynchronizer : FtpBase
{
    private const byte _nStreams = 8;

    private AsyncFtpClient Client2 { get; set; }

    public FtpSynchronizer(string path, string baseFolder, string webName, string server1, string server2, string username, string password)
        : base(path, baseFolder, webName, server1, username, password)
    {
        Client2 = new AsyncFtpClient(server2, Client1.Credentials, config: Client1.Config);
    }

    public override void Dispose()
    {
        Client2.Dispose();
        base.Dispose();
    }

    public async Task SynchronizeAsync()
    {
        await ConnectClientAsync(Client1).ConfigureAwait(false);
        await ConnectClientAsync(Client2).ConfigureAwait(false);

        ImmutableList<FtpListItem>? files1 = null;
        ImmutableList<FtpListItem>? files2 = null;
        var tasks = new List<Func<Task>>
        {
            async () => files1 = await GetFileListingAsync(Client1, order: false).ConfigureAwait(false),
            async () => files2 = await GetFileListingAsync(Client2, order: true).ConfigureAwait(false),
        };
        try
        {
            await Task.WhenAll(tasks.AsParallel().Select(async task => await task().ConfigureAwait(false))).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
            {
                ColoredConsole.SetColor(ConsoleColor.Red)
                    .WriteLineError($"❌ Exception type {innerEx.GetType()} from {innerEx.Source}.")
                    .ResetColor();
            }
            return;
        }
        if (files1 is not null && files2 is not null)
        {
            await SynchronizeFilesAsync(files1, files2).ConfigureAwait(false);
        }
    }

    private async Task<ImmutableList<FtpListItem>> GetFileListingAsync(AsyncFtpClient client, bool order)
    {
        lock (_writeLock)
            ColoredConsole.SetColor(ConsoleColor.DarkYellow)
                .Write(client.Host)
                .ResetColor()
                .WriteLine($": Získávání informací o souborech {_path}...");

        var files = (await client.GetListing(_path, FtpListOption.Recursive | FtpListOption.Modify)
            .ConfigureAwait(false))
            .Where(f => f.Type == FtpObjectType.File && f.Size < 900_000_000);

        if (order)
            files = files.OrderBy(f => f.FullName, StringComparer.Ordinal);

        var filesList = files.ToImmutableList();

        lock (_writeLock)
            ColoredConsole.SetColor(ConsoleColor.Yellow)
                .Write(client.Host)
                .ResetColor()
                .WriteLine($": Nalezeno celkem {filesList.Count} souborů.");
        return filesList;
    }

    private async Task SynchronizeFilesAsync(ImmutableList<FtpListItem> files1, ImmutableList<FtpListItem> files2)
    {
        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️ Probíhá synchronizace souborů mezi {Client1.Host} a {Client2.Host}...")
            .ResetColor();

        // Inicializace více klientů pro paralelní streaming.
        var dcs = new ConcurrentQueue<AsyncFtpClient>();
        var ucs = new ConcurrentQueue<AsyncFtpClient>();
        dcs.Enqueue(Client1);
        ucs.Enqueue(Client2);
        for (byte i = 1; i < _nStreams; i++)
        {
            var dc = new AsyncFtpClient(Client1.Host, Client1.Credentials, config: Client1.Config);
            await dc.Connect().ConfigureAwait(false);
            dcs.Enqueue(dc);

            var uc = new AsyncFtpClient(Client2.Host, Client2.Credentials, config: Client2.Config);
            await uc.Connect().ConfigureAwait(false);
            ucs.Enqueue(uc);
        }

        // Spouštění synchronizačních tasků nad klienty ve frontě (je přidělen prvnímu volnému, jinak se čeká na navrácení do fronty).
        var tasks = new List<Task>();
        var comparer = new FtpFileComparer();
        foreach (var file1 in files1)
        {
            var i = files2.BinarySearch(file1, comparer);
            if (i < 0 || i > files2.Count || file1.Modified > files2[i].Modified)
            {
                AsyncFtpClient? cl1, cl2;
                while (!dcs.TryDequeue(out cl1)) await Task.Yield();
                while (!ucs.TryDequeue(out cl2)) await Task.Yield();

                tasks.Add(DownloadAndUploadAsync(cl1, cl2, file1.FullName, file1.FullName, dcs, ucs));
            }
        }
        if (tasks.Exists(t => !t.IsCompleted)) lock (_writeLock)
        {
            ColoredConsole.SetColor(ConsoleColor.White).WriteLine("🔄️ Čeká se na dokončení zbývajícíh operací...").WriteLine().ResetColor();
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
        ColoredConsole.SetColor(ConsoleColor.Green).WriteLine("✅ Synchronizace FTP serverů dokončena.").WriteLine().ResetColor();

        // Odpojení přidaných paralelních klientů po dokončení synchronizace.
        while (dcs.TryDequeue(out var dc))
            if (dc != Client1)
                dc.Dispose();

        while (ucs.TryDequeue(out var uc))
            if (uc != Client2)
                uc.Dispose();
    }

    private async Task DownloadAndUploadAsync(AsyncFtpClient sourceClient,
                                              AsyncFtpClient destinationClient,
                                              string sourcePath,
                                              string destinationPath,
                                              ConcurrentQueue<AsyncFtpClient> returnQueue1,
                                              ConcurrentQueue<AsyncFtpClient> returnQueue2,
                                              int retries = _defaultRetries)
    {
        var isPhp = sourcePath.EndsWith(".php", StringComparison.Ordinal);

        if (retries >= _defaultRetries)
            lock (_writeLock)
                ColoredConsole.Write("🔽 Probíhá download\t")
                    .SetColor(isPhp ? ConsoleColor.Cyan : ConsoleColor.DarkGray)
                    .WriteLine($"{sourcePath}{Symbols.PREVIOUS_COLOR}...");
        if (isPhp)
        {
            returnQueue2.Enqueue(destinationClient);

            await HandlePhpFileAsync(sourceClient, sourcePath).ConfigureAwait(false);
            lock (_writeLock)
                ColoredConsole.Write("🔽 Download dokončen\t")
                    .SetColor(ConsoleColor.DarkGreen)
                    .WriteLine($"{sourcePath}{Symbols.PREVIOUS_COLOR}...");

            returnQueue1.Enqueue(sourceClient);
            return;
        }
        try
        {
            using var stream = new MemoryStream();            
            if (await sourceClient.DownloadStream(stream, sourcePath).ConfigureAwait(false))
            {
                stream.Seek(0, SeekOrigin.Begin);
                lock (_writeLock)
                    ColoredConsole.WriteLine($"🔼 Probíhá upload\t{ConsoleColor.DarkGray}{destinationPath}{Symbols.PREVIOUS_COLOR}...");

                var status = await destinationClient.UploadStream(stream, destinationPath, createRemoteDir: true).ConfigureAwait(false);
                if (status.IsSuccess())
                {
                    returnQueue1.Enqueue(sourceClient);
                    returnQueue2.Enqueue(destinationClient);
                    return;
                }
            }
        }
        catch (FtpException ex)
        {
            if (ex.InnerException?.Message?.Contains("another read", StringComparison.Ordinal) is true)
                retries++;
            else if (retries == 1) lock (_writeLock)
                ColoredConsole.WriteLineError($"{ConsoleColor.Red}❌ {sourcePath}: {ex.Message}")
                    .WriteLineError($"   {ex.InnerException?.Message}").ResetColor();
        }
        if (--retries > 0)
        {
            await Task.Delay(retries * _random.Next(0, 100)).ConfigureAwait(false);
            await DownloadAndUploadAsync(sourceClient, destinationClient, sourcePath, destinationPath, returnQueue1, returnQueue2, retries)
                .ConfigureAwait(false);
            return;
        }
        returnQueue1.Enqueue(sourceClient);
        returnQueue2.Enqueue(destinationClient);
    }

    private async Task HandlePhpFileAsync(AsyncFtpClient sourceClient, string sourcePath)
    {
        //Na serveru je nový nebo upravený PHP soubor.
        //Cesta tohoto souboru jako lokální cesta na disku.
        var localPath = sourcePath.Replace($"/{_path}/", string.Empty, StringComparison.Ordinal);
        var localBase = Path.Join(_baseFolder, "weby", _webName);
        localPath = Path.Join(localBase, localPath);
        var localFileInfo = new FileInfo(localPath);

        if (!localFileInfo!.Directory!.Exists)
        {
            localFileInfo.Directory.Create();
        }
        //Stažení souboru
        var status = await sourceClient.DownloadFile(localFileInfo.FullName, sourcePath, FtpLocalExists.Overwrite).ConfigureAwait(false);
        if (status.IsSuccess())
        {
            //Pokud nenastala při stažení chyba, smazat zálohu souboru, pokud existuje
            //(byla stažena novější verze, také neupravená (odpovídá stavu na serveru mcrai1)).
            var backupFilePath = localPath.Replace(localBase, Path.Join(_baseFolder, "weby", "_backup", _webName), StringComparison.Ordinal);
            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);
        }
    }

    private sealed class FtpFileComparer : IComparer<FtpListItem>
    {
        public int Compare(FtpListItem? x, FtpListItem? y) => string.CompareOrdinal(x?.FullName, y?.FullName);
    }
}

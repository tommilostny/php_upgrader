namespace FtpSync;

internal sealed class FtpSynchronizer : FtpBase
{
    private AsyncFtpClient Client2 { get; set; }

    public FtpSynchronizer(string path, string baseFolder, string webName, string server1, string server2, string username, string password)
        : base(path, baseFolder, webName, server1, username, password)
    {
        Client2 = new AsyncFtpClient(server2, Client1.Credentials, port: 21, Client1.Config);
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
            await SynchronizeFilesAsync(files1, files2, Client1, Client2).ConfigureAwait(false);
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
            .Where(f => f.Type == FtpObjectType.File);
        
        if (order)
        {
            files = files.OrderBy(f => f.FullName, StringComparer.Ordinal);
        }
        var filesList = files.ToImmutableList();

        lock (_writeLock)
            ColoredConsole.SetColor(ConsoleColor.Yellow)
                .Write(client.Host)
                .ResetColor()
                .WriteLine($": Nalezeno celkem {filesList.Count} souborů.");
        return filesList;
    }

    private async Task SynchronizeFilesAsync(ImmutableList<FtpListItem> files1, ImmutableList<FtpListItem> files2, AsyncFtpClient client1, AsyncFtpClient client2)
    {
        using var dc2 = new AsyncFtpClient(client1.Host, client1.Credentials, config: client1.Config);
        using var dc3 = new AsyncFtpClient(client1.Host, client1.Credentials, config: client1.Config);
        using var dc4 = new AsyncFtpClient(client1.Host, client1.Credentials, config: client1.Config);
        await dc2.Connect().ConfigureAwait(false);
        await dc3.Connect().ConfigureAwait(false);
        await dc4.Connect().ConfigureAwait(false);
        using var uc2 = new AsyncFtpClient(client2.Host, client2.Credentials, config: client2.Config);
        using var uc3 = new AsyncFtpClient(client2.Host, client2.Credentials, config: client2.Config);
        using var uc4 = new AsyncFtpClient(client2.Host, client2.Credentials, config: client2.Config);
        await uc2.Connect().ConfigureAwait(false);
        await uc3.Connect().ConfigureAwait(false);
        await uc4.Connect().ConfigureAwait(false);

        var dcs = new ConcurrentQueue<AsyncFtpClient>(new[] { client1, dc2, dc3, dc4 });
        var ucs = new ConcurrentQueue<AsyncFtpClient>(new[] { client2, uc2, uc3, uc4 });

        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️ Probíhá synchronizace souborů mezi {client1.Host} a {client2.Host}...")
            .ResetColor();

        var tasks = new List<Task>();
        var comparer = new FtpFileComparer();
        foreach (var file1 in files1)
        {
            var j = files2.BinarySearch(file1, comparer);
            if (j < 0 || j > files2.Count || file1.Modified > files2[j].Modified)
            {
                AsyncFtpClient? cl1, cl2;
                while (!dcs.TryDequeue(out cl1)) await Task.Yield();
                while (!ucs.TryDequeue(out cl2)) await Task.Yield();

                tasks.Add(DownloadAndUploadAsync(cl1, cl2, file1.FullName, file1.FullName, dcs, ucs));
            }
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
        ColoredConsole.SetColor(ConsoleColor.Green).WriteLine("✅ Synchronizace FTP serverů dokončena.").WriteLine().ResetColor();
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

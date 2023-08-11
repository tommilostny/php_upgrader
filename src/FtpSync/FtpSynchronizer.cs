﻿namespace FtpSync;

internal sealed class FtpSynchronizer : FtpBase
{
    private readonly long _fileSizeLimit;

    private AsyncFtpClient Client2 { get; set; }

    public FtpSynchronizer(string path, string baseFolder, string webName, string server1, string server2, string username, string password, long maxFileSize)
        : base(path, baseFolder, webName, server1, username, password)
    {
        Client2 = new AsyncFtpClient(server2, Client1.Credentials, config: Client1.Config);
        _fileSizeLimit = maxFileSize;
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

        ImmutableArray<FtpListItem>? files1 = null;
        ImmutableDictionary<string, DateTime>? files2 = null;
        var tasks = new List<Func<Task>>
        {
            async () => files1 = await GetFileListing1Async().ConfigureAwait(false),
            async () => files2 = await GetFileListing2Async().ConfigureAwait(false),
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
            await SynchronizeFilesAsync(files1.Value, files2).ConfigureAwait(false);
        }
    }
    
    private async Task<IEnumerable<FtpListItem>> EnumerateServerFilesAsync(AsyncFtpClient client)
    {
        return (await client.GetListing(_path, FtpListOption.Recursive | FtpListOption.Modify).ConfigureAwait(false))
            .Where(f => f.Type == FtpObjectType.File);
    }

    private void PrintListingStartMessage(string hostname, bool checkLimit)
    {
        lock (_writeLock)
            ColoredConsole.SetColor(ConsoleColor.DarkYellow)
                .Write(hostname)
                .ResetColor()
                .Write($": Získávání informací o souborech {_path}")
                .WriteLine(checkLimit ? $" (limit velikosti souboru: {_fileSizeLimit.ToString(CultureInfo.InvariantCulture)} B)..." : "...");
    }

    private void PrintListingEndMessage(string hostname, int filesCount)
    {
        lock (_writeLock)
            ColoredConsole.SetColor(ConsoleColor.Yellow).Write(hostname)
                .ResetColor().WriteLine($": Nalezeno celkem {filesCount} souborů.");
    }

    private async Task<ImmutableDictionary<string, DateTime>> GetFileListing2Async()
    {
        PrintListingStartMessage(Client2.Host, checkLimit: false);

        var files = await EnumerateServerFilesAsync(Client2).ConfigureAwait(false);
        var dict = files.ToImmutableDictionary(f => f.FullName, f => f.Modified);

        PrintListingEndMessage(Client2.Host, dict.Count);
        return dict;
    }

    private async Task<ImmutableArray<FtpListItem>> GetFileListing1Async()
    {
        var checkLimit = _fileSizeLimit > 0;
        PrintListingStartMessage(Client1.Host, checkLimit);

        var ftpListItems = await EnumerateServerFilesAsync(Client1).ConfigureAwait(false);
        var files = ftpListItems
            .Where(f => !checkLimit || f.Size < _fileSizeLimit)
            .ToImmutableArray();

        PrintListingEndMessage(Client1.Host, files.Length);
        if (checkLimit)
        {
            var bigFiles = ftpListItems
                .Where(f => f.Type == FtpObjectType.File && f.Size >= _fileSizeLimit)
                .Select(f => (name: f.FullName, size: _ToGigaBytes(f.Size)))
                .ToArray();

            if (bigFiles.Length > 0) lock (_writeLock)
            {
                ColoredConsole.SetColor(ConsoleColor.Yellow).Write(Client1.Host)
                    .ResetColor().WriteLine($": Nalezeno celkem {bigFiles.Length} souborů větších než {_ToGigaBytes(_fileSizeLimit)} GB.");
                foreach (var (name, size) in bigFiles)
                {
                    ColoredConsole.SetColor(ConsoleColor.White)
                        .Write($"{size}\tGB\t")
                        .ResetColor().WriteLine(name);
                }
            }
        }    
        return files;

        static double _ToGigaBytes(long size) => size / 1024.0 / 1024.0 / 1024.0;
    }

    private async Task SynchronizeFilesAsync(ImmutableArray<FtpListItem> files1, ImmutableDictionary<string, DateTime> files2)
    {
        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️Probíhá synchronizace souborů mezi {Client1.Host} a {Client2.Host}...")
            .ResetColor();

        // Inicializace více klientů pro paralelní streaming.
        var downClients = await InitClientsQueueAsync(Client1).ConfigureAwait(false);
        var upClients = await InitClientsQueueAsync(Client2).ConfigureAwait(false);

        // Spouštění synchronizačních tasků nad klienty ve frontě (je přidělen prvnímu volnému, jinak se čeká na navrácení do fronty).
        var tasks = new List<Task>();
        foreach (var file1 in files1)
        {
            var exists = files2.TryGetValue(file1.FullName, out var modifiedOnServer2);
            if (!exists || file1.Modified > modifiedOnServer2)
            {
                var (cl1, cl2) = await ExtractClientsAsync(downClients, upClients).ConfigureAwait(false);
                tasks.Add(DownloadAndUploadAsync(cl1, cl2, file1.FullName, file1.FullName, downClients, upClients));
                continue;
            }
            if (file1.Name.EndsWith(".php", StringComparison.OrdinalIgnoreCase))
            {
                var cl = await ExtractClientAsync(downClients).ConfigureAwait(false);
                await HandlePhpFileAsync(cl, file1.FullName, PhpHandleMode.CheckLocal).ConfigureAwait(false);
                downClients.Enqueue(cl);
            }
        }
        await Task.Delay(1000).ConfigureAwait(false);
        if (tasks.Exists(t => !t.IsCompleted)) lock (_writeLock)
            {
                ColoredConsole.SetColor(ConsoleColor.White).WriteLine("🔄️Čeká se na dokončení posledních operací...").ResetColor();
            }
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Odpojení přidaných paralelních klientů po dokončení synchronizace.
        CleanupClientsQueue(downClients, Client1);
        CleanupClientsQueue(upClients, Client2);

        // Smazání souborů, které nejsou na serveru 1 (jsou na serveru 2 navíc).
        await DeleteRedundantFilesAsync(files1, files2).ConfigureAwait(false);

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

            await HandlePhpFileAsync(sourceClient, sourcePath, PhpHandleMode.Download).ConfigureAwait(false);
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

    private enum PhpHandleMode
    {
        Download,
        DeleteLocal,
        CheckLocal
    }

    private async Task HandlePhpFileAsync(AsyncFtpClient sourceClient, string sourcePath, PhpHandleMode handleMode)
    {
        //Na serveru je nový nebo upravený PHP soubor.
        //Cesta tohoto souboru jako lokální cesta na disku.
        var localPath = sourcePath.Replace($"/{_path}/", string.Empty, StringComparison.Ordinal);
        var localBase = Path.Join(_baseFolder, "weby", _webName);
        localPath = Path.Join(localBase, localPath);
        var localFileInfo = new FileInfo(localPath);

        //Stažení souboru nebo smazání dle zvoleného módu.
        var deleteBackupFile = false;
        switch (handleMode)
        {
            case PhpHandleMode.Download:
                if (!localFileInfo!.Directory!.Exists)
                    localFileInfo.Directory.Create();

                var fs = await sourceClient
                    .DownloadFile(localFileInfo.FullName, sourcePath, FtpLocalExists.Overwrite)
                    .ConfigureAwait(false);
                deleteBackupFile = fs.IsSuccess();
                break;

            case PhpHandleMode.DeleteLocal:
                if (localFileInfo.Exists)
                    localFileInfo.Delete();
                deleteBackupFile = true;
                break;

            case PhpHandleMode.CheckLocal:
                if (!localFileInfo.Exists)
                {
                    if (!localFileInfo.Directory!.Exists)
                        localFileInfo.Directory.Create();
                    try
                    {
                        await sourceClient.DownloadFile(localFileInfo.FullName, sourcePath).ConfigureAwait(false);
                        lock (_writeLock)
                            ColoredConsole.WriteLine($"🔽 Stažen chybějící soubor:\t{ConsoleColor.DarkGray}{sourcePath}{Symbols.PREVIOUS_COLOR}...");
                    }
                    catch (FtpException ex)
                    {
                        lock (_writeLock)
                            ColoredConsole.WriteLineError($"{ConsoleColor.Red}❌ {sourcePath}: {ex.Message}")
                                .WriteLineError($"   {ex.InnerException?.Message}").ResetColor();
                    }
                }
                break;
        }
        if (deleteBackupFile)
        {
            //Pokud nenastala při stažení chyba, smazat zálohu souboru, pokud existuje
            //(byla stažena novější verze, také neupravená (odpovídá stavu na serveru mcrai1)).
            var backupFilePath = localPath.Replace(localBase, Path.Join(_baseFolder, "weby", "_backup", _webName), StringComparison.Ordinal);
            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);
        }
    }

    private async Task DeleteRedundantFilesAsync(ImmutableArray<FtpListItem> files1, ImmutableDictionary<string, DateTime> files2)
    {
        ColoredConsole.SetColor(ConsoleColor.Cyan).WriteLine($"🔄️Probíhá kontrola přebytečných souborů (smazané na {Client1.Host})...").ResetColor();

        var sortedFiles1 = files1.Select(f => f.FullName).Order(StringComparer.Ordinal).ToImmutableList();
        foreach (var file2 in files2.Keys)
        {
            if (file2.EndsWith("ObjectBase.php", StringComparison.Ordinal)
                || file2.EndsWith("Exception.php", StringComparison.Ordinal))
                continue;

            var i = sortedFiles1.BinarySearch(file2, StringComparer.Ordinal);
            if (i < 0 || i >= files1.Length)
            {
                ColoredConsole.SetColor(ConsoleColor.Yellow)
                    .Write($"⚠️ {file2}")
                    .ResetColor()
                    .Write($" není na serveru {Client1.Host}.")
                    .SetColor(ConsoleColor.Red)
                    .WriteLine($" Bude z {Client2.Host} smazán.")
                    .ResetColor();
                await Client2.DeleteFile(file2).ConfigureAwait(false);

                if (file2.EndsWith(".php", StringComparison.Ordinal))
                    await HandlePhpFileAsync(Client2, file2, PhpHandleMode.DeleteLocal).ConfigureAwait(false);
            }
        }
    }
}

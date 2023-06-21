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

        FtpListItem[]? files1 = null;
        FtpListItem[]? files2 = null;
        var tasks = new List<Func<Task>>
        {
            async () => files1 = await GetFileListingAsync(Client1).ConfigureAwait(false),
            async () => files2 = await GetFileListingAsync(Client2).ConfigureAwait(false),
        };
        try
        {
            await Task.WhenAll(tasks.AsParallel().Select(async task => await task().ConfigureAwait(false))).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
            {
                ColoredConsole.WriteLineError($"{ConsoleColor.Red}❌ Exception type {innerEx.GetType()} from {innerEx.Source}.").ResetColor();
            }
            return;
        }
        if (files1 is not null && files2 is not null)
        {
            await SynchronizeFilesAsync(files1, files2, Client1, Client2).ConfigureAwait(false);
        }
    }

    private async Task SynchronizeFilesAsync(FtpListItem[] files1, FtpListItem[] files2, AsyncFtpClient client1, AsyncFtpClient client2)
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

        var clients1 = new AsyncFtpClient[] { client1, dc2, dc3, dc4 };
        var clients2 = new AsyncFtpClient[] { client2, uc2, uc3, uc4 };

        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️ Probíhá synchronizace souborů mezi {client1.Host} a {client2.Host}...")
            .ResetColor();

        var tasks = new List<Task>();
        for (int i = 0; i < files1.Length; i++)
        {
            var file1 = files1[i];
            var file2 = files2.FirstOrDefault(f => string.Equals(f.FullName, file1.FullName, StringComparison.Ordinal));
            if (file2 is null || file1.Modified > file2.Modified)
            {
                tasks.Add(DownloadAndUploadAsync(clients1[tasks.Count], clients2[tasks.Count], file1.FullName, file1.FullName));
            }
            if (tasks.Count == 4 || i == files1.Length - 1)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
                tasks.Clear();
            }
        }
        ColoredConsole.SetColor(ConsoleColor.Green).WriteLine("✅ Synchronizace FTP serverů dokončena.").WriteLine().ResetColor();
    }

    private async Task DownloadAndUploadAsync(AsyncFtpClient sourceClient,
                                              AsyncFtpClient destinationClient,
                                              string sourcePath,
                                              string destinationPath,
                                              int retries = _defaultRetries)
    {
        if (retries >= _defaultRetries) lock (_writeLock)
        {
            ColoredConsole.WriteLine($"🔽 Probíhá download\t{ConsoleColor.DarkGray}{sourcePath}{Symbols.PREVIOUS_COLOR}...");
        }
        if (sourcePath.EndsWith(".php", StringComparison.Ordinal))
        {
            await HandlePhpFileAsync(sourceClient, sourcePath).ConfigureAwait(false);
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
                    return;
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
            await DownloadAndUploadAsync(sourceClient, destinationClient, sourcePath, destinationPath, retries).ConfigureAwait(false);
        }
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
}

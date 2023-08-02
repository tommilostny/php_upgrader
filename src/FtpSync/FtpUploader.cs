namespace FtpSync;

internal sealed class FtpUploader : FtpBase
{
    public FtpUploader(string path, string baseFolder, string webName, string server, string username, string password)
        : base(path, baseFolder, webName, server, username, password)
    {
    }

    public async Task UploadPhpsAsync()
    {
        await ConnectClientAsync(Client1).ConfigureAwait(false);

        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️Probíhá nahrávání upravených PHP souborů na {Client1.Host}...")
            .ResetColor();

        var localFolder = Path.Join(_baseFolder, "weby", _webName);
        var clients = await InitClientsQueueAsync(Client1).ConfigureAwait(false);
        var tasks = new List<Task>();
        await UploadPhpsRecursivelyAsync(clients, localFolder, tasks).ConfigureAwait(false);
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
        CleanupClientsQueue(clients, Client1);

        ColoredConsole.SetColor(ConsoleColor.Green)
            .WriteLine($"✅ Nahrávání upravených PHP souborů na {Client1.Host} dokončeno.")
            .WriteLine()
            .ResetColor();
    }

    private async Task UploadPhpsRecursivelyAsync(ConcurrentQueue<AsyncFtpClient> clients, string localFolder, List<Task> tasks)
    {
        foreach (var dir in Directory.EnumerateDirectories(localFolder))
        {
            await UploadPhpsRecursivelyAsync(clients, dir, tasks).ConfigureAwait(false);
        }
        foreach (var file in Directory.EnumerateFiles(localFolder))
        {
            if (file.EndsWith(".php", StringComparison.Ordinal))
            {
                var client = await ExtractClientAsync(clients).ConfigureAwait(false);
                tasks.Add(_Upload(file, client));
            }
        }

        async Task _Upload(string localPath, AsyncFtpClient client)
        {
            var remotePath = $"{_path}{localPath[(_baseFolder.Length + 6 + _webName.Length)..].Replace('\\', '/')}";
            lock (_writeLock)
            {
                ColoredConsole.WriteLine($"🔼 Probíhá upload\t{ConsoleColor.DarkGray}{remotePath}{Symbols.PREVIOUS_COLOR}...");
            }
            await client.UploadFile(localPath, remotePath, createRemoteDir: true, existsMode: FtpRemoteExists.Overwrite).ConfigureAwait(false);
            clients.Enqueue(client);
        }
    }
}

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
            .WriteLine($"🔄️Probíhá kontrola existence adresářů na {Client1.Host}...")
            .ResetColor();

        var localFolder = Path.Join(_baseFolder, "weby", _webName);
        await CreateNonExistantDirectoriesAsync(localFolder, localFolder).ConfigureAwait(false);

        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️Probíhá nahrávání upravených PHP souborů na {Client1.Host}...")
            .ResetColor();

        await Client1.UploadDirectory(localFolder,
                                      remoteFolder: _path,
                                      existsMode: FtpRemoteExists.Overwrite,
                                      rules: PhpRules,
                                      progress: new FtpProgressReport(FO.Upload)).ConfigureAwait(false);

        ColoredConsole.SetColor(ConsoleColor.Green)
            .WriteLine($"✅ Nahrávání upravených PHP souborů na {Client1.Host} dokončeno.")
            .WriteLine()
            .ResetColor();
    }

    private async Task CreateNonExistantDirectoriesAsync(string localFolder, string baseToRemove)
    {
        foreach (var dir in Directory.EnumerateDirectories(localFolder))
        {
            var remoteDir = $"{_path}{dir[baseToRemove.Length..].Replace('\\', '/')}";
            if (!await Client1.DirectoryExists(remoteDir).ConfigureAwait(false))
            {
                Console.WriteLine($"📁 Vytvářím chybějící adresář {remoteDir}...");
                await Client1.CreateDirectory(remoteDir).ConfigureAwait(false);
            }
            await CreateNonExistantDirectoriesAsync(dir, baseToRemove).ConfigureAwait(false);
        }
    }
}

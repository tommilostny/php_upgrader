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
            .WriteLine($"🔄️ Probíhá nahrávání upravených PHP souborů na {Client1.Host}...")
            .ResetColor();

        await Client1.UploadDirectory(localFolder: Path.Join(_baseFolder, "weby", _webName),
                                      remoteFolder: _path,
                                      mode: FtpFolderSyncMode.Mirror,
                                      existsMode: FtpRemoteExists.Overwrite,
                                      rules: PhpRules,
                                      progress: new FtpProgressReport(FO.Upload)).ConfigureAwait(false);

        ColoredConsole.SetColor(ConsoleColor.Green)
            .WriteLine($"✅ Nahrávání upravených PHP souborů na {Client1.Host} dokončeno.")
            .WriteLine()
            .ResetColor();
    }
}

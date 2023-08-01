namespace FtpSync;

internal sealed class FtpDownloader : FtpBase
{
    public FtpDownloader(string path, string baseFolder, string webName, string server, string username, string password)
        : base(path, baseFolder, webName, server, username, password)
    {
    }

    public async Task DownloadPhpsAsync()
    {
        await ConnectClientAsync(Client1).ConfigureAwait(false);

        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️Probíhá stahování PHP souborů z {Client1.Host}...")
            .ResetColor();

        var temporaryPath = Path.Join(Path.GetTempPath(), _path);
        if (Directory.Exists(temporaryPath))
            Directory.Delete(temporaryPath, recursive: true);
        Directory.CreateDirectory(temporaryPath);

        await Client1.DownloadDirectory(localFolder: Path.GetTempPath(),
                                        remoteFolder: _path,
                                        existsMode: FtpLocalExists.Overwrite,
                                        rules: PhpRules,
                                        progress: new FtpProgressReport(FtpOp.Download)).ConfigureAwait(false);

        var realPath = Path.Join(_baseFolder, "weby", _webName);
        Directory.Move(temporaryPath, realPath);

        ColoredConsole.SetColor(ConsoleColor.Green)
            .WriteLine($"✅ Stahování PHP souborů z {Client1.Host} dokončeno.")
            .WriteLine()
            .ResetColor();
    }
}

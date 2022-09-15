namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// Stažení souborů ze serveru (FTP synchronizace do lokální složky).
/// </summary>
internal sealed class FtpDownloader : SynchronizableFtpOperation
{
    public FtpDownloader(string username, string password, string hostname) : base(username, password, hostname)
    {
    }

    protected override SynchronizationMode SynchronizationMode => SynchronizationMode.Local;

    public override void Run(string path, string baseFolder, string webName)
    {
        var startMessage = $"Probíhá synchronizace obsahu ze serveru pomocí FTP do lokální složky webu {webName}...";
        Synchronize(path, baseFolder, webName, startMessage);
    }

    public DirectoryInfo Run(ISet<RemoteFileInfo> filesToDownload, string baseFolder, string webName, string path)
    {
        TryOpenSession();
        var tempDirectory = Path.Join(baseFolder, $"_temp_{webName}");
        var tempDirectoryInfo = new DirectoryInfo(tempDirectory);

        Console.WriteLine($"Probíhá stahování nových souborů z {_sessionOptions.HostName} do dočasné složky {tempDirectoryInfo.FullName}...");
        foreach (var item in filesToDownload)
        {
            var localDirPath = Path.Join(tempDirectory, item.FullName.Replace($"/{path}/", string.Empty)
                                                                     .Replace(item.Name, string.Empty));
            Directory.CreateDirectory(localDirPath);
            try
            {
                _session.GetFileToDirectory(item.FullName, localDirPath);
            }
            catch (SessionRemoteException)
            {
                _session.Close();
                TryOpenSession(verbose: false);
                _session.GetFileToDirectory(item.FullName, localDirPath);
            }
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine($"✅ Stahování {filesToDownload.Count} souborů z {_sessionOptions.HostName} dokončeno.");
        Console.WriteLine();
        Console.ResetColor();
        _session.Close();
        return tempDirectoryInfo;
    }
}

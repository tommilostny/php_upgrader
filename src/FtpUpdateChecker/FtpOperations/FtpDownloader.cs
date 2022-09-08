namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// Stažení souborů ze serveru (FTP synchronizace do lokální složky).
/// </summary>
internal sealed class FtpDownloader : SynchronizableFtpOperation
{
    public FtpDownloader(string username, string password, string hostname) : base(username, password, hostname)
    {
    }

    public override void Run(string path, string baseFolder, string webName)
    {
        var startMessage = $"Probíhá synchronizace obsahu ze serveru pomocí FTP do lokální složky webu {webName}...";
        Synchronize(path, baseFolder, webName, SynchronizationMode.Local, startMessage);
    }
}

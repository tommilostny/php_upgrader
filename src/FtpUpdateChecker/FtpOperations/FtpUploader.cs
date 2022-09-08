namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// Nahrání souborů na server (FTP synchronizace do vzdálené složky).
/// </summary>
internal sealed class FtpUploader : SynchronizableFtpOperation
{
    public FtpUploader(string username, string password, string hostname) : base(username, password, hostname)
    {
    }

    public override void Run(string path, string baseFolder, string webName)
    {
        var startMessage = $"Probíhá synchronizace lokálně aktualizované složky webu {webName} na server pomocí FTP...";
        Synchronize(path, baseFolder, webName, SynchronizationMode.Remote, startMessage);
    }
}

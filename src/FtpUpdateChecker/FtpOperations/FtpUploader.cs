namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// Nahrání souborů na server (FTP synchronizace do vzdálené složky).
/// </summary>
internal sealed class FtpUploader : SynchronizableFtpOperation
{
    public FtpUploader(string username, string password, string hostname) : base(username, password, hostname)
    {
    }

    protected override SynchronizationMode SynchronizationMode => SynchronizationMode.Remote;

    public override void Run(string path, string baseFolder, string webName)
    {
        var startMessage = $"Probíhá synchronizace lokálně aktualizované složky webu {webName} na server {_sessionOptions.HostName}...";
        Synchronize(path, baseFolder, webName, startMessage);
    }

    public void Run(DirectoryInfo temporaryDirectory, string path)
    {
        TryOpenSession();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Probíhá nahrávání souborů z dočasné složky {temporaryDirectory.Name} na server {_sessionOptions.HostName}...");
        Console.ResetColor();
        try
        {
            var result = _session.PutFilesToDirectory(temporaryDirectory.FullName, path);
            result.Check();
            PrintResult(result);
        }
        catch (Exception ex)
        {
            Console.Write("\r❌ ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Při nahrávání souborů nastala chyba {ex.GetType().Name}:");
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
        }
        temporaryDirectory.Delete(recursive: true);
    }

    protected override void QueryReceived(object sender, QueryReceivedEventArgs e)
    {
        //TODO: (ukládat si chybové soubory, po dokončení přenosu ověřit, že na serveru existují? se správným datumem? zkusit znovu.)
        //Error transferring file 'C:\McRAI\_temp_olejemaziva\images_11-9-2022.backup\755132759.jpg'.
        //Copying files to remote side failed.
        //755132759.jpg: Append / Restart not permitted, try again
        base.QueryReceived(sender, e);
    }
}

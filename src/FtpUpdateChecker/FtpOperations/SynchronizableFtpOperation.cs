namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// FTP operace podporující WinSCP synchronizaci.
/// </summary>
internal abstract class SynchronizableFtpOperation : FtpOperation
{
    protected SynchronizableFtpOperation(string username, string password, string hostname) : base(username, password, hostname)
    {
        _session.FileTransferred += FileTransfered;
        _session.QueryReceived += QueryReceived;
    }

    protected void Synchronize(string path, string baseFolder, string webName, SynchronizationMode synchronizationMode, string startMessage)
    {
        TryOpenSession();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(startMessage);
        Console.ResetColor();
        try
        {
            var transferOptions = new TransferOptions
            {
                FileMask = "*.php"
            };
            var result = _session.SynchronizeDirectories(
                synchronizationMode,
                localPath: Path.Join(baseFolder, "weby", webName),
                remotePath: path,
                removeFiles: false,
                mirror: true,
                options: transferOptions
            );
            result.Check();
            PrintSyncResult(synchronizationMode, result);
        }
        catch (Exception ex)
        {
            Console.Write("\r❌ ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Při nahrávání souborů nastala chyba {ex.GetType().Name}:");
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }

    /// <summary> Událost, která nastane při přenosu souboru jako součást metod stahování a nahrávání. </summary>
    private void FileTransfered(object sender, TransferEventArgs e)
    {
        switch (e)
        {
            case { Error: not null }:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\r❌ ");
                Console.WriteLine(e.FileName);
                Console.ResetColor();
                break;
            default:
                Console.Write("\r✅ ");
                Console.WriteLine(e.FileName);
                break;
        }       
    }

    /// <summary> Událost, která nastane, když je potřeba rozhodnutí (tj. typicky u jakékoli nezávažné chyby). </summary>
    private void QueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (!e.Message.StartsWith("Lost connection."))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\r❌ ");
            Console.WriteLine(e.Message.Replace("\n", "\r\n   ").TrimEnd());
            Console.ResetColor();
        }
        e.Continue();
    }

    private static void PrintSyncResult(SynchronizationMode synchronizationMode, SynchronizationResult result)
    {
        Console.WriteLine("\nProces dokončen.");
        switch (synchronizationMode)
        {
            case SynchronizationMode.Local:
                Console.WriteLine($"Staženo {result.Downloads.Count} souborů.");
                break;
            case SynchronizationMode.Remote:
                Console.WriteLine($"Nahráno {result.Uploads.Count} souborů.");
                break;
        }
        Console.WriteLine();
    }
}

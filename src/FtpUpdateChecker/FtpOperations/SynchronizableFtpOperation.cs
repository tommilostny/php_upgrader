namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// FTP operace podporující WinSCP synchronizaci.
/// </summary>
internal abstract class SynchronizableFtpOperation : FtpOperation
{
    protected abstract SynchronizationMode SynchronizationMode { get; }

    protected SynchronizableFtpOperation(string username, string password, string hostname) : base(username, password, hostname)
    {
        _session.FileTransferProgress += FileTransferProgress;
        _session.FileTransferred += FileTransfered;
        _session.QueryReceived += QueryReceived;
    }

    protected void Synchronize(string remotePath, string localPath, string startMessage)
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
                SynchronizationMode,
                localPath,
                remotePath,
                removeFiles: false,
                mirror: true,
                options: transferOptions
            );
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
    }

    protected void Synchronize(string remotePath, string baseFolder, string webName, string startMessage)
    {
        Synchronize(remotePath, Path.Join(baseFolder, "weby", webName), startMessage);
    }

    private void FileTransferProgress(object sender, FileTransferProgressEventArgs e)
    {
        switch (SynchronizationMode)
        {
            case SynchronizationMode.Local:
                Console.Write("\r⏬ ");
                break;
            case SynchronizationMode.Remote:
                Console.Write("\r⏫ ");
                break;
        }
        Console.Write((int)(e.FileProgress * 100));
        Console.Write("%\t");
        Console.Write(e.FileName);
    }

    /// <summary> Událost, která nastane při přenosu souboru jako součást metod stahování a nahrávání. </summary>
    protected void FileTransfered(object sender, TransferEventArgs e)
    {
        switch (e)
        {
            case { Error: not null }:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\r❌\t");
                Console.WriteLine(e.FileName);
                Console.ResetColor();
                break;
            default:
                Console.Write("\r✅ 100%\t");
                Console.WriteLine(e.FileName);
                break;
        }
    }

    /// <summary> Událost, která nastane, když je potřeba rozhodnutí (tj. typicky u jakékoli nezávažné chyby). </summary>
    protected virtual void QueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (!e.Message.StartsWith("Lost connection.") && !e.Message.StartsWith("Connection failed."))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\r❌ ");
            Console.WriteLine(e.Message.Replace("\n", "\r\n   ").TrimEnd());
            Console.ResetColor();
        }
        e.Continue();
    }

    protected void PrintResult(OperationResultBase result)
    {
        Console.WriteLine("\nProces dokončen.");
        switch (result)
        {
            case SynchronizationResult syncResult:
                switch (SynchronizationMode)
                {
                    case SynchronizationMode.Local:
                        Console.WriteLine($"Staženo {syncResult.Downloads.Count} souborů.");
                        break;
                    case SynchronizationMode.Remote:
                        Console.WriteLine($"Nahráno {syncResult.Uploads.Count} souborů.");
                        break;
                }
                break;
            case TransferOperationResult transferResult:
                Console.WriteLine($"Přeneseno {transferResult.Transfers.Count} souborů.");
                break;
        }
        Console.WriteLine();
    }
}

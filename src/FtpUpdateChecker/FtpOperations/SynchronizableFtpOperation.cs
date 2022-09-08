﻿namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// FTP operace podporující WinSCP synchronizaci.
/// </summary>
internal abstract class SynchronizableFtpOperation : FtpOperation
{
    protected SynchronizableFtpOperation(string username, string password, string hostname) : base(username, password, hostname)
    {
        _session.FileTransferred += FileTransfered;
    }

    public void Synchronize(string path, string baseFolder, string webName, SynchronizationMode synchronizationMode, string startMessage)
    {
        if (!TryOpenSession())
        {
            return;
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(startMessage);
        Console.ResetColor();
        try
        {
            var result = _session.SynchronizeDirectories(
                synchronizationMode,
                localPath: Path.Join(baseFolder, "weby", webName),
                remotePath: path,
                removeFiles: false,
                mirror: true,
                options: new TransferOptions { FileMask = "*.php" }
            );
            result.Check();
            PrintSyncSuccess(synchronizationMode, result);
        }
        catch (Exception ex)
        {
            Console.Write("\r❌ ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Při nahrávání souborů nastala chyba:");
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }

    private void FileTransfered(object sender, TransferEventArgs e)
    {
        if (e.Error is null)
        {
            Console.Write("\r✅ ");
            Console.WriteLine(e.FileName);
            return;
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("\r❌ ");
        Console.WriteLine(e.FileName);
        Console.ResetColor();
    }

    private static void PrintSyncSuccess(SynchronizationMode synchronizationMode, SynchronizationResult result)
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

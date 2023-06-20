using System.Text.RegularExpressions;

namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// FTP operace podporující WinSCP synchronizaci.
/// </summary>
internal abstract partial class SynchronizableFtpOperation : FtpOperation
{
    [GeneratedRegex("^(Lost connection|Connection failed)\\.")]
    private static partial Regex ConnectionRegex();

    protected abstract SynchronizationMode SynchronizationMode { get; }

    protected SynchronizableFtpOperation(Output output, string username, string password, string hostname)
        : base(output, username, password, hostname)
    {
        //_session.FileTransferProgress += FileTransferProgress;
        _session.FileTransferred += FileTransfered;
        _session.QueryReceived += QueryReceived;
    }

    protected void Synchronize(string remotePath, string localPath, string startMessage)
    {
        var task = TryOpenSessionAsync();
        if (!task.IsCompleted)
        {
            task.RunSynchronously();
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(startMessage);
        Console.ResetColor();
        _output.Writer?.WriteLine(startMessage);
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
    private async void FileTransfered(object sender, TransferEventArgs e)
    {
        var messageBuilder = new StringBuilder();
        switch (e)
        {
            case { Error: not null }:
                await PrintErrorAsync(e.Error.Message);
                break;
            default:
                switch (SynchronizationMode)
                {
                    case SynchronizationMode.Local:
                        messageBuilder.Append("⏬ Staženo z ");
                        break;
                    case SynchronizationMode.Remote:
                        messageBuilder.Append("⏫ Nahráno na ");
                        break;
                }
                messageBuilder.Append(_sessionOptions.HostName).Append(": ").Append(e.FileName);
                await PrintMessageAsync(_output, messageBuilder.ToString());
                break;
        }
    }

    /// <summary> Událost, která nastane, když je potřeba rozhodnutí (tj. typicky u jakékoli nezávažné chyby). </summary>
    protected virtual async void QueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (!ConnectionRegex().IsMatch(e.Message))
        {
            await PrintMessageAsync(_output, string.Empty);
            await PrintErrorAsync(e.Message);
        }
        e.Continue();
    }

    private async Task PrintErrorAsync(string message)
    {
        const string errorSymbol = "❌ ";
        var formattedMessage = message.Replace("\n", "\r\n        ").TrimEnd();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(errorSymbol);
        Console.WriteLine(formattedMessage);
        Console.ResetColor();

        await _output.WriteToFileAsync(errorSymbol);
        await _output.WriteLineToFileAsync(formattedMessage);
    }

    protected void PrintResult(OperationResultBase result)
    {
        const string completedMessage = "\nProces dokončen.";
        Console.WriteLine(completedMessage);
        _output.Writer?.WriteLine(completedMessage);
        switch (result)
        {
            case SynchronizationResult syncResult:
                switch (SynchronizationMode)
                {
                    case SynchronizationMode.Local:
                        var downloaded = $"Staženo {syncResult.Downloads.Count} souborů.";
                        Console.WriteLine(downloaded);
                        _output.Writer?.WriteLine(downloaded);
                        break;
                    case SynchronizationMode.Remote:
                        var uploaded = $"Nahráno {syncResult.Uploads.Count} souborů.";
                        Console.WriteLine(uploaded);
                        _output.Writer?.WriteLine(uploaded);
                        break;
                }
                break;
            case TransferOperationResult transferResult:
                var transferred = $"Přeneseno {transferResult.Transfers.Count} souborů.";
                Console.WriteLine(transferred);
                _output.Writer?.WriteLine(transferred);
                break;
        }
        Console.WriteLine();
    }
}

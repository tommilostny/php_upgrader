using System.Text.RegularExpressions;

namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// FTP operace podporující WinSCP synchronizaci.
/// </summary>
internal abstract class SynchronizableFtpOperation : FtpOperation
{
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
        Thread.BeginCriticalRegion();
        await PrintNameAsync(_output);
        switch (e)
        {
            case { Error: not null }:
                await PrintErrorAsync(e.Error.Message);
                break;
            default:
                switch (SynchronizationMode)
                {
                    case SynchronizationMode.Local:
                        const string downloadedFrom = "⏬ Staženo z ";
                        Console.Write(downloadedFrom);
                        await _output.WriteToFileAsync(downloadedFrom);
                        break;
                    case SynchronizationMode.Remote:
                        const string uploadedTo = "⏫ Nahráno na ";
                        Console.Write(uploadedTo);
                        await _output.WriteToFileAsync(uploadedTo);
                        break;
                }
                Console.Write(_sessionOptions.HostName);
                Console.Write(": ");
                Console.WriteLine(e.FileName);
                await _output.WriteToFileAsync(_sessionOptions.HostName);
                await _output.WriteToFileAsync(": ");
                await _output.WriteLineToFileAsync(e.FileName);
                break;
        }
        Thread.EndCriticalRegion();
    }

    /// <summary> Událost, která nastane, když je potřeba rozhodnutí (tj. typicky u jakékoli nezávažné chyby). </summary>
    protected async void QueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (!Regex.IsMatch(e.Message, @"^(Lost connection|Connection failed)\."))
        {
            if (this is FtpUploader fu && e.Message.Contains("Permission denied"))
            {
                e.Abort();
                var remoteFileName = GetFileNameFromErrorMessage(e.Message);
                if (remoteFileName is not null)
                {
                    bool adding = false;
                    byte recheckNum = 0;
                    try
                    {
                        var existing = fu.RecheckRemotes.First(x => x.Path == remoteFileName);
                        adding = (recheckNum = ++existing.Retried) < 3;
                    }
                    catch
                    {
                        fu.RecheckRemotes.Add(new(remoteFileName));
                        adding = true;
                    }
                    if (adding)
                    {
                        Thread.BeginCriticalRegion();

                        var msg = $"{remoteFileName}, recheck #{recheckNum + 1}";
                        await fu.PrintNameAsync(_output);
                        Console.WriteLine(msg);
                        await _output.WriteLineToFileAsync(msg);

                        Thread.EndCriticalRegion();
                        return;
                    }
                }
            }
            Thread.BeginCriticalRegion();
            await PrintNameAsync(_output);
            await PrintErrorAsync(e.Message);
            Thread.EndCriticalRegion();
        }
        e.Continue();
    }

    private static string? GetFileNameFromErrorMessage(string message)
    {
        var match = Regex.Match(message, @"'(?<fn>.+?)'", RegexOptions.ExplicitCapture);
        var localPath = match.Groups["fn"].Value;
        var parts = localPath.Split(Path.DirectorySeparatorChar);
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].StartsWith("_temp_"))
            {
                return string.Join('/', parts[(i + 1)..]);
            }
        }
        return null;
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

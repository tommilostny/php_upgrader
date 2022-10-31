namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// Nahrání souborů na server (FTP synchronizace do vzdálené složky).
/// </summary>
internal sealed class FtpUploader : SynchronizableFtpOperation
{
    internal record Recheck(string Path) { public byte Retried = 0; }
    
    internal List<Recheck> RecheckRemotes { get; } = new();


    public FtpUploader(Output output, string username, string password, string hostname)
        : base(output, username, password, hostname)
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
        TryOpenSessionAsync().RunSynchronously();
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

    //protected override void QueryReceived(object sender, QueryReceivedEventArgs e)
    //{
    //    //TODO: (ukládat si chybové soubory, po dokončení přenosu ověřit, že na serveru existují? se správným datumem? zkusit znovu.)
    //    //Error transferring file 'C:\McRAI\_temp_olejemaziva\images_11-9-2022.backup\755132759.jpg'.
    //    //Copying files to remote side failed.
    //    //755132759.jpg: Append / Restart not permitted, try again
    //    base.QueryReceived(sender, e);
    //}

    /// <summary>
    /// Nahraje lokální soubory specifikované ve frontě <paramref name="q3"/>.
    /// </summary>
    /// <remarks> Task běží dokud první prvek fronty není null. </remarks>
    public async Task RunAsync(Queue<string?> q3, string baseFolder, string webName, string path)
    {
        await TryOpenSessionAsync();
        var tempDirectory = Path.Join(baseFolder, $"_temp_{webName}");

        Thread.BeginCriticalRegion();

        await PrintNameAsync(_output);
        var startMessage = $"Probíhá nahrávání nových souborů z dočasné složky {tempDirectory} na server {_sessionOptions.HostName}...";
        Console.WriteLine(startMessage);
        await _output.WriteLineToFileAsync(startMessage);

        Thread.BeginCriticalRegion();
        do
        {
            while (q3.Count == 0)
            {
                await Task.Delay(10);
            }
            var item = q3.Dequeue();
            if (item is null)
            {
                Thread.BeginCriticalRegion();

                await PrintNameAsync(_output);
                Console.ForegroundColor = ConsoleColor.Green;
                var endMessage = $"✅ Nahrávání souborů na {_sessionOptions.HostName} dokončeno.";
                Console.WriteLine(endMessage);
                Console.WriteLine();
                Console.ResetColor();
                _session.Close();
                await _output.WriteLineToFileAsync(endMessage);
                await _output.WriteLineToFileAsync(string.Empty);

                Thread.EndCriticalRegion();
                return;
            }
            var s = Path.DirectorySeparatorChar;
            var remoteDirPath = Path.Join(path, item.Replace($"{tempDirectory}{s}", string.Empty));
            if (s != '/')
            {
                remoteDirPath = remoteDirPath.Replace('\\', '/');
            }
            remoteDirPath = string.Join('/', remoteDirPath.Split('/')[..^1]);

            //Try to upload file
            await SafeSessionActionAsync(() =>
            {
                _session.PutFileToDirectory(item, remoteDirPath, remove: true);
            });
            //If any file needs rechecking, add them back to q3 if they don't actually exist.
            foreach (var recheck in RecheckRemotes.Where(x => x.Retried < 3))
            {
                await SafeSessionActionAsync(() =>
                {
                    if (!_session.FileExists(recheck.Path))
                    {
                        q3.Enqueue(recheck.Path);
                    }
                });
            }
        }
        while (true);
    }
}

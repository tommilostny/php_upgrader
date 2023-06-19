namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// Nahrání souborů na server (FTP synchronizace do vzdálené složky).
/// </summary>
internal sealed class FtpUploader : SynchronizableFtpOperation
{
    public FtpUploader(Output output, string username, string password, string hostname)
        : base(output, username, password, hostname)
    {
    }

    protected override SynchronizationMode SynchronizationMode => SynchronizationMode.Remote;

    protected override string Type => "FTP uploader";

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

    /// <summary>
    /// Nahraje lokální soubory specifikované ve frontě <paramref name="q3"/>.
    /// </summary>
    /// <remarks> Task běží dokud první prvek fronty není null. </remarks>
    public async Task RunAsync(ConcurrentQueue<string?> q3,
                               string baseFolder,
                               string webName,
                               string path)
    {
        await TryOpenSessionAsync();
        var tempDirectory = Path.Join(baseFolder, $"_temp_{webName}");
        await PrintMessageAsync(_output, $"Probíhá nahrávání nových souborů z dočasné složky {tempDirectory} na server {_sessionOptions.HostName}...");
        while (true)
        {
            string? item;
            while (!q3.TryDequeue(out item))
            {
                await Task.Yield();
            }
            if (item is null)
            {
                await PrintMessageAsync(_output, $"{ConsoleColor.Green}✅ Nahrávání souborů na {_sessionOptions.HostName} dokončeno.\n");
                _session.Close();
                return;
            }
            var s = Path.DirectorySeparatorChar;
            var remoteDirPath = Path.Join(path, item.Replace($"{tempDirectory}{s}", string.Empty));
            if (s != '/')
            {
                remoteDirPath = remoteDirPath.Replace('\\', '/');
            }
            remoteDirPath = string.Join('/', remoteDirPath.Split('/')[..^1]);
            await SafeSessionActionAsync(() =>
            {
                _session.PutFileToDirectory(item, remoteDirPath, remove: true);
                return Task.CompletedTask;
            });
        }
    }

    protected override async void QueryReceived(object sender, QueryReceivedEventArgs e)
    {
        base.QueryReceived(sender, e);
        await Task.Delay(23);
        if (e.Message.Contains("Permission denied"))
        {
            e.Abort();
        }
    }
}

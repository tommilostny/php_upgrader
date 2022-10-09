namespace FtpUpdateChecker.FtpOperations;

/// <summary>
/// Stažení souborů ze serveru (FTP synchronizace do lokální složky).
/// </summary>
internal sealed class FtpDownloader : SynchronizableFtpOperation
{
    public FtpDownloader(Output output, string username, string password, string hostname) : base(output, username, password, hostname)
    {
    }

    protected override SynchronizationMode SynchronizationMode => SynchronizationMode.Local;

    public override void Run(string path, string baseFolder, string webName)
    {
        var startMessage = $"Probíhá synchronizace obsahu ze serveru pomocí FTP do lokální složky webu {webName}...";
        Synchronize(path, baseFolder, webName, startMessage);
    }

    /// <summary>
    /// Do dočasné složky stáhne soubory specifikované frontou <paramref name="q2"/>
    /// a do fronty <paramref name="q3"/> umístí jejich lokální cesty.
    /// </summary>
    /// <remarks> Task běží dokud první prvek fronty není null. </remarks>
    public async Task<DirectoryInfo> RunAsync(Queue<RemoteFileInfo?> q2, Queue<string?> q3, string baseFolder, string webName, string path)
    {
        await TryOpenSessionAsync();
        var tempDirectory = Path.Join(baseFolder, $"_temp_{webName}");
        var tempDirectoryInfo = new DirectoryInfo(tempDirectory);

        await PrintNameAsync(_output);
        var startMessage = $"Probíhá stahování nových souborů z {_sessionOptions.HostName} do dočasné složky {tempDirectoryInfo.FullName}...";
        Console.WriteLine(startMessage);
        await _output.WriteLineToFileAsync(startMessage);
        do
        {
            while (q2.Count == 0)
            {
                await Task.Delay(10);
            }
            var item = q2.Dequeue();
            if (item is null)
            {
                q3.Enqueue(null);
                await PrintNameAsync(_output);
                var endMessage = $"✅ Stahování souborů z {_sessionOptions.HostName} dokončeno.";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(endMessage);
                Console.WriteLine();
                Console.ResetColor();
                _session.Close();
                await _output.WriteLineToFileAsync(endMessage);
                await _output.WriteLineToFileAsync(string.Empty);   
                return tempDirectoryInfo;
            }
            var localDirPath = Path.Join(tempDirectory, item.FullName.Replace($"/{path}/", string.Empty)
                                                                     .Replace(item.Name, string.Empty));
            Directory.CreateDirectory(localDirPath);
            await SafeSessionActionAsync(() =>
            {
                _session.GetFileToDirectory(item.FullName, localDirPath);
                q3.Enqueue(Path.Join(localDirPath, item.Name));
            });
        }
        while (true);
    }
}

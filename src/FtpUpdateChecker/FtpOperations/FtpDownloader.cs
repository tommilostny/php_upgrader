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

    protected override string Type => "FTP downloader";

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
    public async Task<DirectoryInfo> RunAsync(ConcurrentQueue<RemoteFileInfo?> q2,
                                              ConcurrentQueue<string?> q3,
                                              string baseFolder,
                                              string webName,
                                              string path)
    {
        await TryOpenSessionAsync();
        var tempDirectory = Path.Join(baseFolder, $"_temp_{webName}");
        var tempDirectoryInfo = new DirectoryInfo(tempDirectory);
        await PrintMessageAsync(_output, $"Probíhá stahování nových souborů z {_sessionOptions.HostName} do dočasné složky {tempDirectoryInfo.FullName}...");
        while (true)
        {
            RemoteFileInfo? item;
            while (!q2.TryDequeue(out item))
            {
                await Task.Yield();
            }
            if (item is null)
            {
                q3.Enqueue(null);
                await PrintMessageAsync(_output, $"{ConsoleColor.Green}✅ Stahování souborů z {_sessionOptions.HostName} dokončeno.\n");
                _session.Close();
                return tempDirectoryInfo;
            }
            var localDirPath = Path.Join(tempDirectory, item.FullName.Replace($"/{path}/", string.Empty)
                                                                     .Replace(item.Name, string.Empty));
            Directory.CreateDirectory(localDirPath);
            await SafeSessionActionAsync(() =>
            {
                _session.GetFileToDirectory(item.FullName, localDirPath);
                q3.Enqueue(Path.Join(localDirPath, item.Name));
                return Task.CompletedTask;
            });
        }
    }
}

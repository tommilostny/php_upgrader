using FluentFTP;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using InterpolatedColorConsole;
using System.Net;

namespace FtpSync;

public sealed class FtpSynchronizer
{
    private const int _defaultRetries = 3;
    private static readonly object _writeLock = new();
    private static readonly Random _random = new();
    private readonly string _path;
    private readonly string _baseFolder;
    private readonly string _webName;

    public FtpSynchronizer(string path, string baseFolder, string webName)
    {
        _path = path;
        _baseFolder = baseFolder;
        _webName = webName;
    }

    public async Task NewSync(string server1, string server2, string username, string password)
    {
        var creds = new NetworkCredential(username, password);
        var config = new FtpConfig()
        {
            EncryptionMode = FtpEncryptionMode.Explicit
        };
        using var dc1 = new AsyncFtpClient(server1, creds, port: 21, config);
        using var uc1 = new AsyncFtpClient(server2, creds, port: 21, config);
        ColoredConsole.WriteLine($"Připojování k {username}@{server1}...");
        await dc1.Connect();
        ColoredConsole.WriteLine($"Připojování k {username}@{server2}...");
        await uc1.Connect();

        FtpListItem[]? files1 = null;
        FtpListItem[]? files2 = null;
        var tasks = new List<Func<Task>>
        {
            async () => files1 = await GetFileListAsync(dc1),
            async () => files2 = await GetFileListAsync(uc1)
        };
        try
        {
            await Task.WhenAll(tasks.AsParallel().Select(async task => await task()));
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
            {
                ColoredConsole.WriteLineError($"{ConsoleColor.Red}❌ Exception type {innerEx.GetType()} from {innerEx.Source}.").ResetColor();
            }
            return;
        }
        if (files1 is not null && files2 is not null)
        {
            await SynchronizeFilesAsync(files1, files2, dc1, uc1);
        }
    }

    private async Task<FtpListItem[]> GetFileListAsync(AsyncFtpClient client)
    {
        lock (_writeLock)
            ColoredConsole.SetColor(ConsoleColor.DarkYellow)
                .Write(client.Host)
                .ResetColor()
                .WriteLine($": Získávání informací o souborech {_path}...");

        var fileList = await client.GetListing(_path, FtpListOption.Recursive | FtpListOption.Modify);
        var fileArray = fileList.Where(f => f.Type == FtpObjectType.File).ToArray();

        lock (_writeLock)
            ColoredConsole.SetColor(ConsoleColor.Yellow)
                .Write(client.Host)
                .ResetColor()
                .WriteLine($": Nalezeno celkem {fileArray.Length} souborů.");
        return fileArray;
    }

    private async Task SynchronizeFilesAsync(FtpListItem[] files1, FtpListItem[] files2, AsyncFtpClient client1, AsyncFtpClient client2)
    {
        using var dc2 = new AsyncFtpClient(client1.Host, client1.Credentials, config: client1.Config);
        using var dc3 = new AsyncFtpClient(client1.Host, client1.Credentials, config: client1.Config);
        using var dc4 = new AsyncFtpClient(client1.Host, client1.Credentials, config: client1.Config);
        await dc2.Connect();
        await dc3.Connect();
        await dc4.Connect();
        using var uc2 = new AsyncFtpClient(client2.Host, client2.Credentials, config: client2.Config);
        using var uc3 = new AsyncFtpClient(client2.Host, client2.Credentials, config: client2.Config);
        using var uc4 = new AsyncFtpClient(client2.Host, client2.Credentials, config: client2.Config);
        await uc2.Connect();
        await uc3.Connect();
        await uc4.Connect();

        var clients1 = new AsyncFtpClient[] { client1, dc2, dc3, dc4 };
        var clients2 = new AsyncFtpClient[] { client2, uc2, uc3, uc4 };

        ColoredConsole.SetColor(ConsoleColor.Cyan)
            .WriteLine($"🔄️ Probíhá synchronizace souborů mezi {client1.Host} a {client2.Host}...")
            .ResetColor();

        var tasks = new List<Task>();
        for (int i = 0; i < files1.Length; i++)
        {
            var file1 = files1[i];
            var file2 = files2.FirstOrDefault(f => f.FullName == file1.FullName);
            if (file2 is null || file1.Modified > file2.Modified)
            {
                tasks.Add(DownloadAndUploadAsync(clients1[tasks.Count], clients2[tasks.Count], file1.FullName, file1.FullName));
            }
            if (tasks.Count == 4 || i == files1.Length - 1)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }
        ColoredConsole.SetColor(ConsoleColor.Green).WriteLine("✅ Synchronizace FTP serverů dokončena.").WriteLine().ResetColor();
    }

    private async Task DownloadAndUploadAsync(AsyncFtpClient sourceClient,
                                              AsyncFtpClient destinationClient,
                                              string sourcePath,
                                              string destinationPath,
                                              int retries = _defaultRetries)
    {
        if (retries >= _defaultRetries) lock (_writeLock)
        {
            ColoredConsole.WriteLine($"🔽 Probíhá download\t{ConsoleColor.DarkGray}{sourcePath}{Symbols.PREVIOUS_COLOR}...");
        }
        if (sourcePath.EndsWith(".php"))
        {
            await HandlePhpFileAsync(sourceClient, sourcePath);
            return;
        }
        try
        {
            using var stream = new MemoryStream();            
            if (await sourceClient.DownloadStream(stream, sourcePath))
            {
                stream.Seek(0, SeekOrigin.Begin);
                lock (_writeLock)
                    ColoredConsole.WriteLine($"🔼 Probíhá upload\t{ConsoleColor.DarkGray}{destinationPath}{Symbols.PREVIOUS_COLOR}...");

                var status = await destinationClient.UploadStream(stream, destinationPath, createRemoteDir: true);        
                if (status.IsSuccess())
                    return;
            }
        }
        catch (FtpException ex)
        {
            if (ex.InnerException?.Message?.Contains("another read") is true)
                retries++;
            else if (retries == 1) lock (_writeLock)
                ColoredConsole.WriteLineError($"{ConsoleColor.Red}❌ {sourcePath}: {ex.Message}")
                    .WriteLineError($"   {ex.InnerException?.Message}").ResetColor();
        }
        if (--retries > 0)
        {
            await Task.Delay(retries * _random.Next(0, 100));
            await DownloadAndUploadAsync(sourceClient, destinationClient, sourcePath, destinationPath, retries);
        }
    }

    private async Task HandlePhpFileAsync(AsyncFtpClient sourceClient, string sourcePath)
    {
        //Na serveru je nový nebo upravený PHP soubor.
        //Cesta tohoto souboru jako lokální cesta na disku.
        var localPath = sourcePath.Replace($"/{_path}/", string.Empty);
        var localBase = Path.Join(_baseFolder, "weby", _webName);
        localPath = Path.Join(localBase, localPath);
        var localFileInfo = new FileInfo(localPath);

        if (!localFileInfo!.Directory!.Exists)
        {
            localFileInfo.Directory.Create();
        }
        //Stažení souboru
        var status = await sourceClient.DownloadFile(localFileInfo.FullName, sourcePath, FtpLocalExists.Overwrite);
        if (status.IsSuccess())
        {
            //Pokud nenastala při stažení chyba, smazat zálohu souboru, pokud existuje
            //(byla stažena novější verze, také neupravená (odpovídá stavu na serveru mcrai1)).
            var backupFilePath = localPath.Replace(localBase, Path.Join(_baseFolder, "weby", "_backup", _webName));
            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);
        }
    }
}

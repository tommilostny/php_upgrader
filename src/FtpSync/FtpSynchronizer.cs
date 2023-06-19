using FluentFTP;
using System.Net;

namespace FtpSync;

public class FtpSynchronizer
{
    static async Task Main(string[] args)
    {
        // FTP server details
        string server1 = "ftp://server1.com/";
        string server2 = "ftp://server2.com/";
        string username = "ftpuser";
        string password = "ftppassword";

        // Create FTP client objects
        using var client1 = new AsyncFtpClient(server1, new NetworkCredential(username, password));
        using var client2 = new AsyncFtpClient(server2, new NetworkCredential(username, password));
        await client1.Connect();
        await client2.Connect();
        
        FtpListItem[]? files1 = null;
        FtpListItem[]? files2 = null;
        var tasks = new List<Func<Task>>
        {
            async () => files1 = await GetFileListAsync(client1),
            async () => files2 = await GetFileListAsync(client2)
        };
        try
        {
            await Task.WhenAll(tasks.AsParallel().Select(async task => await task()));
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
                Console.WriteLine($"Exception type {innerEx.GetType()} from {innerEx.Source}.");
            return;
        }
        if (files1 is not null &&  files2 is not null)
        {
            await SynchronizeFilesAsync(files1, files2, client1, client2);
            Console.WriteLine("Synchronization complete.");
        }
    }

    static async Task<FtpListItem[]> GetFileListAsync(AsyncFtpClient client)
    {
        var fileList = await client.GetListing("httpdocs", FtpListOption.Recursive | FtpListOption.AllFiles | FtpListOption.Modify);
        return fileList.Where(f => f.Type == FtpObjectType.File).ToArray();
    }

    static async Task SynchronizeFilesAsync(FtpListItem[] files1, FtpListItem[] files2, AsyncFtpClient client1, AsyncFtpClient client2)
    {
        foreach (var file1 in files1)
        {
            var matchingFile = files2.FirstOrDefault(f => f.Name == file1.Name);
            if (matchingFile is null || file1.Modified > matchingFile.Modified)
            {
                Console.WriteLine("Synchronizing {0}...", file1.Name);
                await DownloadAndUploadAsync(client1, client2, file1.FullName, matchingFile?.FullName ?? file1.Name);
            }
        }
    }

    static async Task DownloadAndUploadAsync(AsyncFtpClient sourceClient, AsyncFtpClient destinationClient, string sourcePath, string destinationPath)
    {
        using var stream = new MemoryStream();
        if (await sourceClient.DownloadStream(stream, sourcePath))
        {
            stream.Seek(0, SeekOrigin.Begin);
            var status = await destinationClient.UploadStream(stream, destinationPath);
            return;
        }
        //File was not downloaded.
    }
}

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
        using var client1 = new FtpClient(server1, new NetworkCredential(username, password));
        using var client2 = new FtpClient(server2, new NetworkCredential(username, password));
        client1.Connect();
        client2.Connect();

        var files1 = await GetFileListAsync(client1);
        var files2 = await GetFileListAsync(client2);

        await SynchronizeFilesAsync(files1, files2, client1, client2);

        Console.WriteLine("Synchronization complete.");
    }

    static async Task<FtpListItem[]> GetFileListAsync(FtpClient client)
    {
        var fileList = client.GetListing();
        return fileList.Where(f => f.Type == FtpObjectType.File).ToArray();
    }

    static async Task SynchronizeFilesAsync(FtpListItem[] files1, FtpListItem[] files2, FtpClient client1, FtpClient client2)
    {
        foreach (var file1 in files1)
        {
            var matchingFile = files2.FirstOrDefault(f => f.Name == file1.Name);
            if (matchingFile == null)
            {
                Console.WriteLine("Synchronizing {0}...", file1.Name);
                await DownloadAndUploadAsync(client1, client2, file1.FullName, file1.Name);
                continue;
            }

            DateTime dt1 = await GetModifiedTimeAsync(client1, file1.FullName);
            DateTime dt2 = await GetModifiedTimeAsync(client2, matchingFile.FullName);

            if (dt1 > dt2)
            {
                Console.WriteLine("Synchronizing {0}...", file1.Name);
                await DownloadAndUploadAsync(client1, client2, file1.FullName, matchingFile.FullName);
            }
        }
    }

    static async Task DownloadAndUploadAsync(FtpClient sourceClient, FtpClient destinationClient, string sourcePath, string destinationPath)
    {
        using (var stream = new MemoryStream())
        {
            await sourceClient.DownloadAsync(stream, sourcePath);
            stream.Seek(0, SeekOrigin.Begin);
            await destinationClient.UploadAsync(stream, destinationPath);
        }
    }

    static async Task<DateTime> GetModifiedTimeAsync(FtpClient client, string path)
    {
        var modifiedTime = await client.GetModifiedTimeAsync(path);
        return modifiedTime.ToUniversalTime();
    }
}

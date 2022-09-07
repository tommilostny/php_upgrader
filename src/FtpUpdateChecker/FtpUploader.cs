namespace FtpUpdateChecker;

internal class FtpUploader : FtpOperation
{
    private readonly ISet<string> _localFiles;

    public FtpUploader(string username, string password, string hostname, ISet<string> localFiles)
        : base(username, password, hostname)
    {
        _localFiles = localFiles;
    }

    public override void Run(string path, string baseFolder, string webName)
    {
        if (!TryOpenSession())
        {
            return;
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Probíhá nahrávání aktualizovaných souborů webu ");
        Console.Write(webName);
        Console.WriteLine('.');
        Console.ResetColor();
        foreach (var item in _localFiles)
        {
            var (remoteDir, remoteFile) = GetRemoteDirectory(path, baseFolder, webName, item);
            Console.Write("🔼 ");
            Console.Write(remoteFile);
            Console.Write(" ...");
            try
            {
                _session.PutFileToDirectory(item, remoteDir);
                Console.Write("\r✅ ");
                Console.Write(remoteFile);
                Console.WriteLine("    ");
            }
            catch (Exception ex)
            {
                Console.Write("\r❌ ");
                Console.Write(remoteFile);
                Console.WriteLine("    ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write("Při nahrávání souboru '");
                Console.Error.Write(item.Split(Path.DirectorySeparatorChar).Last());
                Console.Error.Write("' do složky '");
                Console.Error.Write(remoteDir);
                Console.Error.WriteLine("' nastala chyba:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }

    private static (string remoteDir, string remoteFile) GetRemoteDirectory(string path, string baseFolder, string webName, string item)
    {
        var remoteFile = item.Replace(Path.Join(baseFolder, "weby", webName), path);
        var parts = remoteFile.Split(Path.DirectorySeparatorChar);

        var remoteDir = string.Join('/', parts[..^1]);
        remoteFile = $"{remoteDir}/{parts[^1]}";

        return (remoteDir, remoteFile);
    }
}

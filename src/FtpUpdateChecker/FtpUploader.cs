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
        foreach (var item in _localFiles)
        {
            Console.Write("Probíhá nahrávání souboru ");
            Console.Write(item.Replace(Path.Join(baseFolder, "weby", webName), string.Empty));
            Console.WriteLine(" ...");

            var remoteDir = GetRemoteDirectory(path, baseFolder, webName, item);
            try
            {
                _session.PutFileToDirectory(item, remoteDir);
            }
            catch (Exception ex)
            {
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

    private static string GetRemoteDirectory(string path, string baseFolder, string webName, string item)
    {
        var remoteFile = item.Replace(Path.Join(baseFolder, "weby", webName), path);
        var parts = remoteFile.Split(Path.DirectorySeparatorChar);

        return string.Join('/', parts[..^1]);
    }
}

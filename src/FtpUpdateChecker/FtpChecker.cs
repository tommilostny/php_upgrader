using EnumOpts = WinSCP.EnumerationOptions;

namespace FtpUpdateChecker;

/// <summary> Třída nad knihovnou WinSCP kontrolující soubory na FTP po určitém datu. </summary>
public class FtpChecker : IDisposable
{
    /// <summary> Datum, od kterého hlásit změnu. </summary>
    public DateTime FromDate { get; }

    /// <summary> Celkový počet souborů. </summary>
    public uint FileCount { get; private set; }

    /// <summary> Celkový počet složek. </summary>
    public uint FolderCount { get; private set; }

    /// <summary> Počet souborů přidaných po datu <see cref="FromDate"/>. </summary>
    public uint FoundCount { get; private set; }

    /// <summary> Počet PHP souborů přidaných po datu <see cref="FromDate"/>. </summary>
    public uint PhpFoundCount { get; private set; }

    internal ConsoleColor DefaultColor { get; } = Console.ForegroundColor;

    private SessionOptions SessionOptions { get; }

    private Session Session { get; } = new();

    /// <summary> Inicializace sezení spojení WinSCP, nastavení data. </summary>
    public FtpChecker(string username, string password, string hostname, DateTime fromDate)
    {
        SessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = hostname,
            UserName = username,
            Password = password,
            FtpSecure = FtpSecure.Explicit
        };
        FromDate = fromDate;
    }

    /// <summary> Spustit procházení všech souborů na FTP serveru v zadané cestě. </summary>
    public void Run(string path)
    {
        if (!Session.Opened)
        {
            Console.WriteLine($"Připojování k FTP {SessionOptions.UserName}@{SessionOptions.HostName} ...");
            try //Connect
            {
                Session.Open(SessionOptions);
                Console.WriteLine("Připojení proběhlo úspěšně!\n");
            }
            catch (SessionRemoteException)
            {
                ConsoleOutput.WriteError("Připojení k FTP serveru selhalo pro zadané uživatelské jméno a heslo.");
                return;
            }
        }
        else Console.WriteLine();

        Console.WriteLine($"Probíhá kontrola '{path}'...");
        var enumerationOptions = EnumOpts.EnumerateDirectories | EnumOpts.AllDirectories;
        var fileInfos = Session.EnumerateRemoteFiles(path, null, enumerationOptions);

        FileCount = FolderCount = PhpFoundCount = FoundCount = 0;
        int messageLength = this.WriteStatus();
        try //Enumerate files
        {
            foreach (var fileInfo in fileInfos)
            {
                Console.Write('\r');

                if (fileInfo.IsDirectory)
                {
                    FolderCount++;
                    messageLength = this.WriteStatus();
                    continue;
                }
                if (fileInfo.LastWriteTime >= FromDate)
                {
                    FoundCount++;
                    bool isPhp;
                    if (isPhp = fileInfo.FullName.EndsWith(".php"))
                    {
                        PhpFoundCount++;
                    }
                    this.WriteFoundFile(fileInfo, messageLength, isPhp);
                }
                FileCount++;
                messageLength = this.WriteStatus();
            }
        }
        catch (SessionRemoteException)
        {
            ConsoleOutput.WriteError($"Zadaná cesta '{path}' na serveru neexistuje.");
        }
        ConsoleOutput.WriteCompleted();
    }

    /// <summary> Uzavřít spojení k FTP serveru. </summary>
    public void Dispose()
    {
        if (Session.Opened)
        {
            Session.Close();
        }
        GC.SuppressFinalize(this);
    }
}

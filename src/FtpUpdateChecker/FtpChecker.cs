using EO = WinSCP.EnumerationOptions;

namespace FtpUpdateChecker;

/// <summary> Třída nad knihovnou WinSCP kontrolující soubory na FTP po určitém datu. </summary>
internal class FtpChecker : FtpOperation
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

    /// <summary> Inicializace sezení spojení WinSCP, nastavení data. </summary>
    public FtpChecker(string username, string password, string hostname, DateTime fromDate)
        : base(username, password, hostname)
    {
        FromDate = fromDate;
    }

    /// <summary> Spustit procházení všech souborů na FTP serveru v zadané cestě. </summary>
    public override void Run(string path, string baseFolder, string webName)
    {
        if (!TryOpenSession())
        {
            return;
        }
        var phpLogFilePath = $"{PhpLogsDir}/{_sessionOptions.UserName}-{path}.txt";
        if (File.Exists(phpLogFilePath))
        {
            File.Delete(phpLogFilePath);
        }
        else
        {
            Directory.CreateDirectory(PhpLogsDir);
        }
        Console.WriteLine($"Probíhá kontrola '{path}'...");
        var enumerationOptions = EO.EnumerateDirectories | EO.AllDirectories;
        var fileInfos = _session.EnumerateRemoteFiles(path, null, enumerationOptions);

        FileCount = FolderCount = PhpFoundCount = FoundCount = 0;
        var messageLength = Output.WriteStatus(this);
        try //Enumerate files
        {
            foreach (var fileInfo in fileInfos)
            {
                Console.Write('\r');

                if (fileInfo.IsDirectory)
                {
                    FolderCount++;
                    messageLength = Output.WriteStatus(this);
                    continue;
                }
                if (fileInfo.LastWriteTime >= FromDate)
                {
                    FoundCount++;
                    bool isPhp;
                    if (isPhp = fileInfo.FullName.EndsWith(".php"))
                    {
                        //Na serveru je nový nebo upravený PHP soubor.
                        PhpFoundCount++;
                        //Cesta tohoto souboru jako lokální cesta na disku.
                        var localPath = fileInfo.FullName.Replace($"/{path}/", string.Empty);
                        localPath = Path.Join(baseFolder, "weby", webName, localPath);
                        var localFileInfo = new FileInfo(localPath);

                        //Stažení souboru
                        var transferResult = _session.GetFileToDirectory(fileInfo.FullName, localFileInfo.Directory.FullName);
                        if (transferResult.Error is null)
                        {
                            //Pokud nenastala při stažení chyba, smazat zálohu souboru, pokud existuje
                            //(byla stažena novější verze, také neupravená (odpovídá stavu na serveru mcrai1)).
                            var backupFilePath = localPath.Replace(Path.Join(baseFolder, "weby", webName),
                                                                   Path.Join(baseFolder, "weby", "_backup", webName));
                            if (File.Exists(backupFilePath))
                                File.Delete(backupFilePath);
                        }
                    }
                    Output.WriteFoundFile(this, fileInfo, messageLength, isPhp, phpLogFilePath);
                }
                FileCount++;
                messageLength = Output.WriteStatus(this);
            }
        }
        catch (SessionRemoteException)
        {
            Output.WriteError($"Zadaná cesta '{path}' na serveru neexistuje.");
        }
        Output.WriteCompleted(phpLogFilePath, PhpFoundCount);
    }
}

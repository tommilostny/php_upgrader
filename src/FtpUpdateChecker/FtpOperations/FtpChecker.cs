﻿using EO = WinSCP.EnumerationOptions;

namespace FtpUpdateChecker.FtpOperations;

/// <summary> Třída nad knihovnou WinSCP kontrolující soubory na FTP po určitém datu. </summary>
internal sealed class FtpChecker : FtpOperation
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

    public ISet<RemoteFileInfo>? NonPhpFiles { get; private set; }

    public ISet<RemoteFileInfo> KnownNewNonPhpFiles { get; init; } = new HashSet<RemoteFileInfo>();

    /// <summary> Inicializace sezení spojení WinSCP, nastavení data. </summary>
    public FtpChecker(string username, string password, string hostname, string webName, string baseFolder, int day, int month, int year)
        : base(username, password, hostname)
    {
        FromDate = LoadFromDateTime(webName, baseFolder, day, month, year);
    }

    /// <summary> Spustit procházení všech souborů na FTP serveru v zadané cestě. </summary>
    /// <remarks> Výsledky prohledávání jsou dostupné ve veřejných atributech. </remarks>
    public override void Run(string path, string baseFolder, string webName)
    {
        TryOpenSession();

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
        
        NonPhpFiles = new HashSet<RemoteFileInfo>();
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
                var isPhp = fileInfo.FullName.EndsWith(".php");
                if (fileInfo.LastWriteTime >= FromDate)
                {
                    FoundCount++;
                    if (isPhp)
                    {
                        //Na serveru je nový nebo upravený PHP soubor.
                        PhpFoundCount++;
                        //Cesta tohoto souboru jako lokální cesta na disku.
                        var localPath = fileInfo.FullName.Replace($"/{path}/", string.Empty);
                        localPath = Path.Join(baseFolder, "weby", webName, localPath);
                        var localFileInfo = new FileInfo(localPath);

                        if (!localFileInfo.Directory.Exists)
                        {
                            localFileInfo.Directory.Create();
                        }
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
                    else
                    {
                        KnownNewNonPhpFiles.Add(fileInfo);
                    }
                    Output.WriteFoundFile(this, fileInfo, messageLength, isPhp, phpLogFilePath);
                }
                else if (!isPhp)
                {
                    NonPhpFiles.Add(fileInfo);
                }
                FileCount++;
                messageLength = Output.WriteStatus(this);
            }
        }
        catch (SessionRemoteException ex)
        {
            Console.WriteLine();
            Output.WriteError(ex.Message);
        }
        Output.WriteCompleted(_sessionOptions.HostName, phpLogFilePath, PhpFoundCount);
        _session.Close();
    }

    /// <summary> Kontrola daných souborů. </summary>
    /// <returns> Seznam souborů v <see cref="KnownNewNonPhpFiles"/>, které na serveru nejsou, nebo existuje pouze starší verze. </returns>
    public void Run(in ISet<RemoteFileInfo> remoteFilesToCheck, string hostname1 = McraiFtp.DefaultHostname1)
    {
        TryOpenSession();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"Probíhá kontrola '{_sessionOptions.HostName}' vůči změnám na '{hostname1}' (namapováno ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write(remoteFilesToCheck.Count);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" ne-PHP souborů)...");
        Console.ResetColor();

        var messageLength = Output.WriteStatusNoDate(this, remoteFilesToCheck.Count);
        foreach (var item in remoteFilesToCheck)
        {
            Console.Write('\r');
            _LoadAndCheckFileInfo(item);
            FileCount++;
            messageLength = Output.WriteStatusNoDate(this, remoteFilesToCheck.Count);
        }
        _session.Close();
        Output.WriteCompleted(_sessionOptions.HostName);

        void _LoadAndCheckFileInfo(RemoteFileInfo item)
        {
            try
            {
                var exists = _session.FileExists(item.FullName);
                var fileInfo = exists ? _session.GetFileInfo(item.FullName) : null;

                if (!exists || fileInfo.LastWriteTime < item.LastWriteTime)
                {
                    KnownNewNonPhpFiles.Add(item);
                    FoundCount++;
                    Output.WriteFilesDiff(this, hostname1, _sessionOptions.HostName, item, fileInfo, messageLength);
                }
            }
            catch //Chyba při komunikaci se serverem, znovu připojit a zkusit načíst informace o souboru.
            {
                _session.Close();
                TryOpenSession(verbose: false);
                _LoadAndCheckFileInfo(item);
            }
        }
    }

    /// <summary>
    /// Načte datum, po kterém hlásit soubory jako aktualizované.
    /// </summary>
    /// <remarks>
    /// Pokud se parametry <paramref name="day"/>, <paramref name="month"/> a <paramref name="year"/>
    /// neliší od výchozích hodnot (<see cref="McraiFtp.DefaultDay"/>, <see cref="McraiFtp.DefaultMonth"/> a <see cref="McraiFtp.DefaultYear"/>),
    /// pokusí se toto datum načíst ze souboru ".phplogs/date-<paramref name="webName"/>.txt",
    /// který existuje pokud již byly tyto kontroly dříve provedeny (soubor obsahuje datum, kdy se naposledy kontrolovala aktualita).
    /// Pokud tento soubor neexistuje, pokusí se datum načíst z <paramref name="webName"/> složky,
    /// (to odpovídá datu, kdy byly soubory webu poprvé staženy z FTP) jinak používá výchozí hodnoty.
    /// </remarks>
    private static DateTime LoadFromDateTime(string webName, string baseFolder, int day, int month, int year)
    {
        var modifiedDate = year != McraiFtp.DefaultYear || month != McraiFtp.DefaultMonth || day != McraiFtp.DefaultDay;
        var webPath = Path.Join(baseFolder, "weby", webName);

        var dateFile = Path.Join(PhpLogsDir, $"date-{webName}.txt");
        var date = File.Exists(dateFile)

            ? DateTime.Parse(File.ReadAllText(dateFile))

            : webName switch
            {
                not null and _ when !modifiedDate && Directory.Exists(webPath)
                    => Directory.GetCreationTime(webPath),
                _ => new(year, month, day)
            };

        File.WriteAllText(dateFile, DateTime.Now.ToString());
        return date;
    }
}

﻿using EO = WinSCP.EnumerationOptions;

namespace FtpUpdateChecker.FtpOperations;

/// <summary> Třída nad knihovnou WinSCP kontrolující soubory na FTP po určitém datu. </summary>
internal sealed class FtpChecker : FtpOperation
{
    private readonly string[]? _ignoreFolders;

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

    protected override string Type => "FTP checker";

    /// <summary> Inicializace sezení spojení WinSCP, nastavení data. </summary>
    public FtpChecker(Output output, string username, string password, string hostname, string webName, string baseFolder, int day, int month, int year, string[]? ignoreFolders = null)
        : base(output, username, password, hostname)
    {
        FromDate = LoadFromDateTime(webName, baseFolder, day, month, year);
        _ignoreFolders = ignoreFolders;
    }

    /// <summary> Spustit procházení všech souborů na FTP serveru v zadané cestě. </summary>
    /// <remarks> Výsledky prohledávání jsou dostupné ve veřejných atributech. </remarks>
    public override void Run(string path, string baseFolder, string webName)
    {

    }

    /// <summary> Spustit procházení všech souborů na FTP serveru v zadané cestě. </summary>
    /// <remarks>
    /// Výsledky prohledávání jsou dostupné ve frontě <paramref name="q1"/>.
    /// Do fronty <paramref name="q2"/> dá nové soubory po datu zadané <see cref="FromDate"/>.
    /// Na konec fronty vloží zarážku null (víme, kdy skončit v dalších krocích).
    /// </remarks>
    public async Task RunAsync(ConcurrentQueue<RemoteFileInfo?> q1,
                               ConcurrentQueue<RemoteFileInfo?> q2,
                               string path,
                               string baseFolder,
                               string webName)
    {
        await TryOpenSessionAsync();        
        await PrintMessageAsync(_output, $"Probíhá kontrola '{_sessionOptions}/{path}' na změny po {FromDate}...");

        var enumerationOptions = EO.EnumerateDirectories | EO.AllDirectories;
        var fileInfos = _session.EnumerateRemoteFiles(path, null, enumerationOptions);

        if (_ignoreFolders is not null && _ignoreFolders.Length > 0)
        {
            fileInfos = fileInfos.Where(fileInfo => _ignoreFolders.All(dir => !fileInfo.FullName.Contains($"/{dir}/")));
        }

        FileCount = FolderCount = PhpFoundCount = FoundCount = 0;
        try //Enumerace souborů.
        {
            await Parallel.ForEachAsync(fileInfos, async (fileInfo, ct) =>
            {
                if (fileInfo.IsDirectory)
                {
                    FolderCount++;
                    return;
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
                        q2.Enqueue(fileInfo);
                    }
                    await _output.WriteFoundFileAsync(this, fileInfo, isPhp);
                }
                else if (!isPhp)
                {
                    q1.Enqueue(fileInfo);
                }
                FileCount++;
            });
        }
        catch (SessionRemoteException ex)
        {
            Console.WriteLine();
            await _output.WriteErrorAsync(this, ex.Message);
        }
        finally
        {
            q1.Enqueue(null);
        }
        await _output.WriteCompletedAsync(this, _sessionOptions.HostName);
        _session.Close();
    }

    /// <summary>
    /// Kontrola daných souborů ve frontě <paramref name="q1"/>
    /// a vložení souborů, které je třeba stáhnout do fronty <paramref name="q2"/>.
    /// </summary>
    /// <remarks> Task běží dokud se z fronty nevyjme <em>null</em>. </remarks>
    public async Task RunAsync(ConcurrentQueue<RemoteFileInfo?> q1,
                               ConcurrentQueue<RemoteFileInfo?> q2,
                               string hostname1 = McraiFtp.DefaultHostname1)
    {
        await TryOpenSessionAsync();
        await PrintMessageAsync(_output, $"Probíhá kontrola '{_sessionOptions.HostName}' vůči změnám na '{hostname1}'...");
        while (true)
        {
            RemoteFileInfo? item;
            while (!q1.TryDequeue(out item))
            {
                await Task.Yield();
            }
            if (item is null)
            {
                _session.Close();
                await _output.WriteCompletedAsync(this, _sessionOptions.HostName);
                q2.Enqueue(null);
                return;
            }
            await SafeSessionActionAsync(async () =>
            {
                var exists = _session.FileExists(item.FullName);
                var fileInfo = exists ? _session.GetFileInfo(item.FullName) : null;

                if (!exists || fileInfo.LastWriteTime < item.LastWriteTime)
                {
                    q2.Enqueue(item);
                    FoundCount++;
                    await _output.WriteFilesDiffAsync(this, hostname1, _sessionOptions.HostName, item, fileInfo);
                }
            });
            FileCount++;
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

        var dateFile = Path.Join(McraiFtp.PhpLogsDir, $"date-{webName}.txt");
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

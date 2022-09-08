﻿using FtpUpdateChecker;

namespace PhpUpgrader;

class Program
{
    private static string _webName;
    private static string _baseFolder;

    private static readonly Lazy<McraiFtp> _ftp = new(() => new McraiFtp(_webName, _baseFolder));

    /// <summary>
    /// RS Mona a Rubicon PHP upgrader z verze 5 na verzi 7 (vytvořeno pro McRAI).
    /// Autor: Tomáš Milostný
    /// </summary>
    /// <param name="webName">Název webu ve složce 'weby' (nesmí chybět).</param>
    /// <param name="adminFolders">Složky obsahující administraci RS Mona (default prázdné: 1 složka admin)</param>
    /// <param name="rootFolders">Další "root" složky obsahující index.php, do kterého vložit mysqli_close na konec.</param>
    /// <param name="baseFolder">Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'.</param>
    /// <param name="db">Název nové databáze na mcrai2. (nechat prázdné pokud se používá stejná databáze, zkopíruje se ze souboru)</param>
    /// <param name="user">Uživatelské jméno k nové databázi na mcrai2.</param>
    /// <param name="password">Heslo k nové databázi na mcrai2.</param>
    /// <param name="host">URL databázového serveru.</param>
    /// <param name="beta">Přejmenovat proměnnou $beta tímto názvem (nezadáno => nepřejmenovávat).</param>
    /// <param name="connectionFile">Název souboru ve složce "/connect".</param>
    /// <param name="rubicon">Upgrade systému Rubicon (nezadáno => Mona).</param>
    /// <param name="ignoreConnect">Ignore DB connection arguments (--host, --db, --user, --password).</param>
    /// <param name="useBackup"> Neptat se a vždy načítat soubory ze zálohy. </param>
    /// <param name="ignoreBackup"> Neptat se a vždy ignorovat zálohu. </param>
    /// <param name="checkFtp"> Neptat se a vždy kontrolovat aktualitu se soubory na mcrai1. </param>
    /// <param name="ignoreFtp"> Neptat se a vždy ignorovat aktualitu souborů s FTP. </param>
    /// <param name="upload"> Neptat se a vždy po dokončení lokální aktualizace nahrát tyto soubory na FTP nového serveru. </param>
    /// <param name="dontUpload"> Neptat se a po aktualizaci soubory nenahrávat. </param>
    static void Main(string webName, string[]? adminFolders = null, string[]? rootFolders = null,
                     string baseFolder = "/McRAI", string? db = null, string? user = null, string? password = null,
                     string host = "localhost", string? beta = null, string connectionFile = "connection.php",
                     bool rubicon = false, bool ignoreConnect = false, bool useBackup = false, bool ignoreBackup = false,
                     bool checkFtp = false, bool ignoreFtp = false, bool upload = false, bool dontUpload = false)
    {
        _webName = webName;
        _baseFolder = baseFolder;

        //0. fáze: příprava PHP upgraderu (kontrola zadaných parametrů)
        // Může nastat případ, kdy složka webu neexistuje. Uživatel je tázán, zda se pokusit stáhnout z FTP mcrai1.
        var upgrader = LoadPhpUpgrader(rubicon, adminFolders, rootFolders, beta,
                                       connectionFile, ignoreConnect, db, user, password, host,
                                       ref ignoreFtp, out var workDir);
        if (upgrader is not null) //PHP upgrader se povedlo inicializovat.
        {
            //1. fáze: (pokud je vyžadováno)
            // Kontrola nově upravených souborů na původním serveru (mcrai1) a jejich případné stažení.
            CheckForUpdatesAndDownloadFromFtp(checkFtp, ignoreFtp);

            //2. fáze: Aktualizace celé složky webu
            // (případně i načtení souborů ze zálohy, pokud toto není spuštěno poprvé).
            RunUpgrade(upgrader, useBackup, ignoreBackup, workDir);
            PrintUpgradeResults(upgrader);

            //3. fáze: (pokud je vyžadováno)
            // Nahrání veškerých aktualizovaných souborů na nový server.
            UploadtToFtp(upgrader, upload, dontUpload);
        }
    }

    static PhpUpgraderBase? LoadPhpUpgrader(bool rubicon, string[] adminFolders, string[] rootFolders, string beta, string connectionFile, bool ignoreConnect, string db, string user, string password, string host, ref bool ignoreFtp, out string workDir)
    {
        workDir = Path.Join(_baseFolder, "weby", _webName);
        if (_webName == string.Empty)
        {
            Console.Error.WriteLine($"Složka {workDir} není validní, protože parametr '--web-name' není zadán.");
            return null;
        }
        if (!Directory.Exists(workDir))
        {
            Console.WriteLine($"Složka {workDir} neexistuje. Pokusit se načíst údaje z ftp_logins.txt a stáhnout z FTP {host}?");
            if (Console.Read() != 'y')
            {
                return null;
            }
            try
            {
                Directory.CreateDirectory(workDir);
                _ftp.Value.DownloadFromServer();
                ignoreFtp = true;
            }
            catch
            {
                return null;
            }
        }
        var upgrader = !rubicon ? new MonaUpgrader(_baseFolder, _webName)
        {
            AdminFolders = adminFolders,
            RenameBetaWith = beta,
            ConnectionFile = connectionFile,
        }
        : new RubiconUpgrader(_baseFolder, _webName);

        if (!ignoreConnect)
        {
            upgrader.Database = db;
            upgrader.Username = user;
            upgrader.Password = password;
            upgrader.Hostname = host;
        }
        upgrader.OtherRootFolders = rootFolders;
        return upgrader;
    }

    static void RunUpgrade(PhpUpgraderBase upgrader, bool useBackup, bool ignoreBackup, string workDir)
    {
        Console.Write($"Spuštěn PHP upgrade pro '{_webName}' použitím ");
        Console.Write(upgrader switch
        {
            RubiconUpgrader => "Rubicon",
            MonaUpgrader => "Mona",
            _ => throw new InvalidOperationException($"{nameof(upgrader)} je neznámého typu {upgrader.GetType().Name}.")
        });
        Console.WriteLine(" upgraderu...");

        Console.WriteLine("\nOznačení zpracovaných souborů:");
        Console.WriteLine($"Modifikován:   {FileWrapper.ModifiedSymbol}");
        Console.WriteLine($"Nemodifikován: {FileWrapper.UnmodifiedSymbol}");
        Console.WriteLine($"Varování:      {FileWrapper.WarningSymbol}");

        if (!ignoreBackup)
        {
            BackupManager.LoadBackupFiles(useBackup, _baseFolder, _webName);
        }
        Console.WriteLine("\nZpracované soubory:");
        upgrader.UpgradeAllFilesRecursively(workDir);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nAutomatický upgrade PHP webu {_webName} je dokončen!");
        Console.ResetColor();
    }

    static void PrintUpgradeResults(PhpUpgraderBase upgrader)
    {
        Console.WriteLine($"Celkem upravených souborů: {upgrader.ModifiedFiles.Count}/{upgrader.TotalFilesCount}");

        Console.WriteLine($"Soubory obsahující mysql_: {upgrader.FilesContainingMysql.Count}\n");
        foreach (var (fileName, matches) in upgrader.FilesContainingMysql)
        {
            Console.WriteLine(fileName);
            foreach (var (line, function) in matches)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{line}\t");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(function);
            }
            Console.ResetColor();
        }
    }

    static void CheckForUpdatesAndDownloadFromFtp(bool checkFtp, bool ignoreFtp)
    {
        if (ignoreFtp)
        {
            return;
        }
        if (!checkFtp)
        {
            Console.WriteLine($"Zkontrolovat a případně stáhnout aktuální verze souborů z FTP {McraiFtp.DefaultHostname1}? (y/n)");
            checkFtp = Console.Read() == 'y';
        }
        if (checkFtp)
        {
            _ftp.Value.GetUpdatesFromServer();
        }
    }

    static void UploadtToFtp(PhpUpgraderBase upgrader, bool upload, bool dontUpload)
    {
        if (dontUpload || upgrader.ModifiedFiles.Count == 0 || upgrader.FilesContainingMysql.Count > 0)
        {
            return;
        }
        if (!upload)
        {
            Console.WriteLine($"Nahrát modifikované soubory na FTP {McraiFtp.DefaultHostnameUpgrade}? (y/n)");
            upload = Console.Read() == 'y';
        }
        if (upload)
        {
            _ftp.Value.UploadToServer();
        }
    }
}

namespace PhpUpgrader;

public static class Program
{
    private static string _webName;
    private static string _baseFolder;
    private static Lazy<McraiFtp> _lazyFtp;
    private static Lazy<McraiFtp> _lazyRubiconFtp;

    /// <summary>
    /// RS Mona a Rubicon PHP upgrader z verze 5 na verzi 7 (vytvořeno pro McRAI).
    /// Autor: Tomáš Milostný
    /// </summary>
    /// <param name="webName"> Název webu ve složce 'weby' (nesmí chybět). </param>
    /// <param name="adminFolders"> Složky obsahující administraci RS Mona (default prázdné: 1 složka admin). </param>
    /// <param name="rootFolders"> Další "root" složky obsahující index.php, do kterého vložit mysqli_close na konec. </param>
    /// <param name="baseFolder"> Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'. </param>
    /// <param name="db"> Název nové databáze na mcrai2. (nechat prázdné pokud se používá stejná databáze, zkopíruje se ze souboru). </param>
    /// <param name="user"> Uživatelské jméno k nové databázi na mcrai2. </param>
    /// <param name="password"> Heslo k nové databázi na mcrai2. </param>
    /// <param name="host"> URL databázového serveru. </param>
    /// <param name="beta"> Přejmenovat proměnnou $beta tímto názvem (nezadáno => nepřejmenovávat). </param>
    /// <param name="connectionFile"> Název souboru ve složce "/connect". </param>
    /// <param name="rubicon"> Upgrade systému Rubicon (nezadáno => Mona). </param>
    /// <param name="ignoreConnect"> Ignore DB connection arguments (--host, --db, --user, --password). </param>
    /// <param name="useBackup"> Neptat se a vždy načítat soubory ze zálohy. </param>
    /// <param name="ignoreBackup"> Neptat se a vždy ignorovat zálohu. </param>
    /// <param name="checkFtp"> Neptat se a vždy kontrolovat aktualitu se soubory na mcrai1. </param>
    /// <param name="ignoreFtp"> Neptat se a vždy ignorovat aktualitu souborů s FTP. </param>
    /// <param name="upload"> Neptat se a vždy po dokončení lokální aktualizace nahrát tyto soubory na FTP nového serveru. </param>
    /// <param name="dontUpload"> Neptat se a po aktualizaci soubory nenahrávat. </param>
    /// <param name="dontUpgrade"> Nespouštět PHP upgrader, pouze ostatní nastavené procesy s FTP. </param>
    /// <param name="ftpMaxMb"> Limit velikosti souboru v MB při FTP synchronizaci (0 a menší => vypnuto). </param>
    /// <param name="devDb"> Databáze "rubicon_6_dev_...". </param>
    /// <param name="devUser"> Uživatelské jméno k dev databázi. </param>
    /// <param name="devPassword"> Heslo k dev databázi. </param>
    public static async Task Main(string webName, string[]? adminFolders = null, string[]? rootFolders = null,
                                  string baseFolder = "/McRAI", string? db = null, string? user = null, string? password = null,
                                  string host = "127.0.0.1", string? beta = null, string connectionFile = "connection.php",
                                  bool rubicon = false, bool ignoreConnect = false, bool useBackup = false, bool ignoreBackup = false,
                                  bool checkFtp = false, bool ignoreFtp = false, bool upload = false, bool dontUpload = false, bool dontUpgrade = false,
                                  double ftpMaxMb = 500, string? devDb = null, string? devUser = null, string? devPassword = null)
    {
        if (webName is null or { Length: 0 })
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Nebyl zadán žádný název webu argumentem '--web-name'.");
            Console.ResetColor();
            return;
        }
        var startTime = DateTime.Now;
        _baseFolder = baseFolder;
        _webName = webName;
        _lazyFtp = new(() => new McraiFtp(_webName, _baseFolder, Convert.ToInt64(ftpMaxMb * 1024 * 1024)));
        _lazyRubiconFtp = new(() => new McraiFtp($"{_webName}-rubicon", _baseFolder, -1));

        //0. fáze: příprava PHP upgraderu (kontrola zadaných argumentů)
        // Může nastat případ, kdy složka webu neexistuje. Uživatel je tázán, zda se pokusit stáhnout z FTP mcrai1.
        var uw = await LoadPhpUpgraderAsync(rubicon, adminFolders, rootFolders, beta,
                                            connectionFile, ignoreConnect, db, user, password, host, dontUpgrade,
                                            devDb, devUser, devPassword).ConfigureAwait(false);
        if (uw is not null and var (upgrader, workDir)) //PHP upgrader se povedlo inicializovat.
        {
            //1. fáze: (pokud je vyžadováno)
            // Kontrola nově upravených souborů na původním serveru (mcrai1) a jejich případné stažení.
            await CheckForUpdatesAndDownloadFromFtpAsync(checkFtp, ignoreFtp, upgrader).ConfigureAwait(false);
            if (!dontUpgrade)
            {
                //2. fáze: Aktualizace celé složky webu
                // (případně i načtení souborů ze zálohy, pokud toto není spuštěno poprvé).
                RunUpgrade(upgrader, useBackup, ignoreBackup, workDir);
                PrintUpgradeResults(upgrader);

                //3. fáze: (pokud je vyžadováno)
                // Nahrání veškerých aktualizovaných souborů na nový server.
                await UploadToFtpAsync(upgrader, upload, dontUpload).ConfigureAwait(false);
            }
        }
        Console.WriteLine($"Celkový čas: {DateTime.Now - startTime}");
    }

    static async Task<(PhpUpgraderBase, string workDir)?> LoadPhpUpgraderAsync(bool rubicon, string[] adminFolders, string[] rootFolders, string beta, string connectionFile, bool ignoreConnect, string db, string user, string password, string host, bool dontUpgrade, string? devDb, string? devUser, string? devPassword)
    {
        var workDir = Path.Join(_baseFolder, "weby", _webName);
        if (_webName == string.Empty)
        {
            Console.Error.WriteLine($"Složka {workDir} není validní, protože argument '--web-name' není zadán.");
            return null;
        }
        //Složka existuje, ale je prázdná, smazat.
        if (Directory.Exists(workDir) && !Directory.EnumerateFileSystemEntries(workDir).GetEnumerator().MoveNext())
        {
            Directory.Delete(workDir);
        }
        //Pokusit se stáhnout soubory, pokud složka neexistuje (aka děláme nový web poprvé).
        if (!dontUpgrade && !Directory.Exists(workDir))
        {
            Console.WriteLine($"Složka {workDir} neexistuje. Načítám údaje z ftp_logins.txt a stahuji z FTP {McraiFtp.DefaultHostname1}.");
            try
            {
                await _lazyFtp.Value.DownloadAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }
        if (dontUpgrade)
        {
            return (null, workDir);
        }
        //Složka webu k aktualizaci existuje, vytvořit PHP upgrader.
        var upgrader = !rubicon
            ? new MonaUpgrader(_baseFolder, _webName) { AdminFolders = adminFolders, RenameBetaWith = beta, ConnectionFile = connectionFile }
            : new RubiconUpgrader(_baseFolder, _webName);
        
        if (!ignoreConnect)
        {
            if (db is null || user is null || password is null)
            {
                var dbLogins = new DbLoginParser(_baseFolder, _webName);
                if (dbLogins.Success)
                {
                    db = dbLogins.Database;
                    user = dbLogins.UserName;
                    password = dbLogins.Password;
                    devDb ??= dbLogins.DevDatabase;
                    devUser ??= dbLogins.DevUsername;
                    devPassword ??= dbLogins.DevPassword;
                }
            }
            upgrader.Database = db; upgrader.Username = user;
            upgrader.Password = password; upgrader.Hostname = host;
            if (upgrader is RubiconUpgrader ru)
            {
                ru.DevDatabase = devDb;
                ru.DevUsername = devUser;
                ru.DevPassword = devPassword;
            }
        }
        upgrader.OtherRootFolders = rootFolders;
        return (upgrader, workDir);
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
            if (upgrader is RubiconUpgrader ru && ru.HasRubiconOutside)
            {
                BackupManager.LoadBackupFiles(useBackup, _baseFolder, $"{_webName}-rubicon");
            }
        }
        Console.WriteLine("\nZpracované soubory:");
        upgrader.RunUpgrade(workDir);

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

    static async Task CheckForUpdatesAndDownloadFromFtpAsync(bool checkFtp, bool ignoreFtp, PhpUpgraderBase upgrader)
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
            await _lazyFtp.Value.UpdateAsync().ConfigureAwait(false);
            if (upgrader is RubiconUpgrader ru && ru.HasRubiconOutside)
            {
                await _lazyRubiconFtp.Value.UpdateAsync().ConfigureAwait(false);
            }
        }
    }

    static async Task UploadToFtpAsync(PhpUpgraderBase upgrader, bool upload, bool dontUpload)
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
            await _lazyFtp.Value.UploadAsync().ConfigureAwait(false);
            if (upgrader is RubiconUpgrader ru && ru.HasRubiconOutside)
            {
                await _lazyRubiconFtp.Value.UploadAsync().ConfigureAwait(false);
            }
        }
    }
}

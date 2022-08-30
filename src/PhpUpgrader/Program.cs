namespace PhpUpgrader;

public static class Program
{
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
    public static void Main(string webName, string[]? adminFolders = null, string[]? rootFolders = null,
        string baseFolder = "/McRAI", string? db = null, string? user = null, string? password = null,
        string host = "localhost", string? beta = null, string connectionFile = "connection.php",
        bool rubicon = false, bool ignoreConnect = false, bool useBackup = false, bool ignoreBackup = false)
    {
        var upgrader = LoadPhpUpgrader(baseFolder, webName, rubicon, adminFolders, rootFolders, beta,
                                       connectionFile, ignoreConnect, db, user, password, host,
                                       out var workDir);
        if (upgrader is not null)
        {
            RunUpgrade(upgrader, webName, baseFolder, useBackup, ignoreBackup, workDir);
            PrintUpgradeResults(upgrader);
        }
    }

    private static PhpUpgraderBase? LoadPhpUpgrader(string baseFolder, string webName, bool rubicon, string[] adminFolders, string[] rootFolders, string beta, string connectionFile, bool ignoreConnect, string db, string user, string password, string host, out string workDir)
    {
        workDir = Path.Join(baseFolder, "weby", webName);

        if (webName == string.Empty)
        {
            Console.Error.WriteLine($"Složka {workDir} není validní, protože parametr '--web-name' není zadán.");
            return null;
        }
        if (!Directory.Exists(workDir))
        {
            Console.Error.WriteLine($"Složka {workDir} neexistuje.");
            return null;
        }

        var upgrader = !rubicon ? new MonaUpgrader(baseFolder, webName)
        {
            AdminFolders = adminFolders,
            RenameBetaWith = beta,
            ConnectionFile = connectionFile,
        }
        : new RubiconUpgrader(baseFolder, webName);

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

    private static void RunUpgrade(PhpUpgraderBase upgrader, string webName, string baseFolder, bool useBackup, bool ignoreBackup, string workDir)
    {
        Console.Write($"Spuštěn PHP upgrade pro '{webName}' použitím ");
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
            BackupManager.LoadBackupFiles(useBackup, baseFolder, webName);
        }
        Console.WriteLine("\nZpracované soubory:");
        upgrader.UpgradeAllFilesRecursively(workDir);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nAutomatický upgrade PHP webu {webName} je dokončen!");
        Console.ResetColor();
    }

    private static void PrintUpgradeResults(PhpUpgraderBase upgrader)
    {
        Console.WriteLine($"Celkem upravených souborů: {upgrader.ModifiedFilesCount}/{upgrader.TotalFilesCount}");

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
}

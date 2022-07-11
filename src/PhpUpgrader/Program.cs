namespace PhpUpgrader;

class Program
{
    /// <summary>
    /// RS Mona a Rubicon PHP upgrader z verze 5 na verzi 7 (vytvořeno pro McRAI).
    /// Autor: Tomáš Milostný
    /// </summary>
    /// <param name="webName">Název webu ve složce 'weby' (nesmí chybět).</param>
    /// <param name="adminFolders">Složky obsahující administraci RS Mona (default prázdné: 1 složka admin)</param>
    /// <param name="baseFolder">Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'.</param>
    /// <param name="db">Název nové databáze na mcrai2. (nechat prázdné pokud se používá stejná databáze, zkopíruje se ze souboru)</param>
    /// <param name="user">Uživatelské jméno k nové databázi na mcrai2.</param>
    /// <param name="password">Heslo k nové databázi na mcrai2.</param>
    /// <param name="host">URL databázového serveru.</param>
    /// <param name="beta">Přejmenovat proměnnou $beta tímto názvem (nezadáno => nepřejmenovávat).</param>
    /// <param name="connectionFile">Název souboru ve složce "/connect".</param>
    /// <param name="rubicon">Upgrade systému Rubicon (nezadáno => Mona).</param>
    /// <param name="ignoreConnect">Ignore DB connection arguments (--host, --db, --user, --password).</param>
    static void Main(string webName, string[]? adminFolders = null, string baseFolder = "/McRAI",
        string? db = null, string? user = null, string? password = null, string host = "localhost",
        string? beta = null, string connectionFile = "connection.php", bool rubicon = false,
        bool ignoreConnect = false)
    {
        var workDir = Path.Join(baseFolder, "weby", webName);

        if (webName == string.Empty)
        {
            Console.Error.WriteLine($"Složka {workDir} není validní, protože parametr '--web-name' není zadán.");
            return;
        }
        if (!Directory.Exists(workDir))
        {
            Console.Error.WriteLine($"Složka {workDir} neexistuje.");
            return;
        }

        Console.Write($"Spuštěn PHP upgrade pro '{webName}'");
        var upgrader = rubicon ? new RubiconUpgrader(baseFolder, webName)
        {
            Database = ignoreConnect ? null : db,
            Username = ignoreConnect ? null : user,
            Password = ignoreConnect ? null : password,
            Hostname = ignoreConnect ? null : host
        }
        : new MonaUpgrader(baseFolder, webName)
        {
            AdminFolders = adminFolders,
            Database = ignoreConnect ? null : db,
            Username = ignoreConnect ? null : user,
            Password = ignoreConnect ? null : password,
            Hostname = ignoreConnect ? null : host,
            RenameBetaWith = beta,
            ConnectionFile = connectionFile
        };
        Console.WriteLine($" použitím {(upgrader is RubiconUpgrader ? "Rubicon" : "Mona")} upgraderu...\n");

        Console.WriteLine($"Modifikován:   {FileWrapper.ModifiedSymbol}");
        Console.WriteLine($"Nemodifikován: {FileWrapper.UnmodifiedSymbol}");
        Console.WriteLine($"Varování:      {FileWrapper.WarningSymbol}");

        Console.WriteLine("\nZpracované soubory:");
        upgrader.UpgradeAllFilesRecursively(workDir);

        Console.WriteLine($"\nAutomatický upgrade PHP webu {webName} je dokončen!");
        Console.WriteLine($"Celkem upravených souborů: {upgrader.ModifiedFilesCount}/{upgrader.TotalFilesCount}");
        
        Console.WriteLine($"Soubory obsahující mysql_: {upgrader.FilesContainingMysql.Count}");
        foreach (var fileName in upgrader.FilesContainingMysql)
        {
            Console.WriteLine(fileName);
        }
    }
}

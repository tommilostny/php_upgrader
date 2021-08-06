using System;
using System.IO;

namespace PhpUpgrader
{
    class Program
    {
        /// <summary>RS Mona PHP upgrader z verze 5 na verzi 7</summary>
        /// <remarks>Created for McRAI by Tomáš Milostný</remarks>
        /// <param name="webName">Název webu ve složce 'weby' (nesmí chybět).</param>
        /// <param name="adminFolders">Složky obsahující administraci RS Mona (default prázdné: 1 složka admin)</param>
        /// <param name="baseFolder">Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'.</param>
        /// <param name="db">Název nové databáze na mcrai2. (nechat prázdné pokud se používá stejná databáze, zkopíruje se ze souboru)</param>
        /// <param name="user">Uživatelské jméno k nové databázi na mcrai2.</param>
        /// <param name="password">Heslo k nové databázi na mcrai2.</param>
        /// <param name="host">URL databázového serveru.</param>
        /// <param name="beta">Přejmenovat proměnnou $beta tímto názvem (nezadáno => nepřejmenovávat).</param>
        /// <param name="connectionFile">Název souboru ve složce "/connect".</param>
        static void Main(string webName, string[]? adminFolders = null, string baseFolder = @"C:\McRAI\",
            string? db = null, string? user = null, string? password = null, string host = "mcrai2.vshosting.cz",
            string? beta = null, string connectionFile = "connection.php")
        {
            var workDir = $@"{baseFolder}weby\{webName}";

            if (webName == string.Empty)
            {
                Console.Error.WriteLine($"Folder {workDir} is invalid because parameter '--web-name' is empty.");
                return;
            }
            if (!Directory.Exists(workDir))
            {
                Console.Error.WriteLine($"Folder {workDir} does not exist.");
                return;
            }

            Console.WriteLine($"Starting PHP upgrader for {webName}...\n");
            var upgrader = new MonaUpgrader
            {
                BaseFolder = baseFolder,
                WebName = webName,
                AdminFolders = adminFolders,
                Database = db,
                Username = user,
                Password = password,
                Hostname = host,
                RenameBetaWith = beta,
                ConnectionFile = connectionFile
            };

            Console.WriteLine($"Modified:   {FileWrapper.ModifiedSymbol}");
            Console.WriteLine($"Unmodified: {FileWrapper.UnmodifiedSymbol}");
            Console.WriteLine("\nProcessed files:");
            upgrader.UpgradeAllFilesRecursively(workDir);

            Console.WriteLine($"\nAutomatic PHP upgrade of {webName} is complete!");
            Console.WriteLine($"Files containing mysql_: {upgrader.FilesContainingMysql.Count}");

            foreach (var fileName in upgrader.FilesContainingMysql)
                Console.WriteLine(fileName);
        }
    }
}

using System;
using System.IO;

namespace PhpUpgrader
{
    class Program
    {
        /// <summary>
        /// RS Mona PHP upgrader z verze 5 na verzi 7
        /// Created for McRAI by Tomáš Milostný
        /// </summary>
        /// <param name="webName">Název webu ve složce 'weby' (nesmí chybět).</param>
        /// <param name="adminFolders">Složky obsahující administraci RS Mona (default prázdné: 1 složka admin)</param>
        /// <param name="baseFolder">Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'.</param>
        /// <param name="db">Název nové databáze na mcrai2. (nechat prázdné pokud se používá stejná databáze, zkopíruje se ze souboru)</param>
        /// <param name="user">Uživatelské jméno k nové databázi na mcrai2.</param>
        /// <param name="password">Heslo k nové databázi na mcrai2.</param>
        /// <param name="host">URL databázového serveru.</param>
        /// <param name="beta">Přejmenovat proměnnou $beta tímto názvem (null => nepřejmenovávat).</param>
        static void Main(string webName, string[]? adminFolders = null, string baseFolder = @"C:\McRAI\",
            string? db = null, string? user = null, string? password = null, string host = "mcrai2.vshosting.cz",
            string? beta = null)
        {
            var workDir = $@"{baseFolder}weby\{webName}";

            if (webName == string.Empty)
            {
                Console.Error.WriteLine($"Folder {workDir} is invalid because parameter '--web-name' is empty.");
                return;
            }
            else if (!Directory.Exists(workDir))
            {
                Console.Error.WriteLine($"Folder {workDir} does not exist.");
                return;
            }

            var upgrader = new PhpUpgrader(baseFolder, webName, adminFolders, db, user, password, host, beta);

            Console.WriteLine("\nProcessed files:\n");
            upgrader.UpgradeAllFilesRecursively(workDir);

            Console.WriteLine($"\nAutomatic PHP upgrade of {webName} is complete!");
            Console.WriteLine($"Files containing mysql_: {upgrader.FilesContainingMysql.Count}");

            foreach (var fileName in upgrader.FilesContainingMysql)
                Console.WriteLine(fileName);
        }
    }
}

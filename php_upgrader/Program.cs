using System;
using System.IO;

namespace php_upgrader
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
        static void Main(string webName, string[]? adminFolders = null, string baseFolder = @"C:\McRAI\",
            string? db = null, string? user = null, string? password = null, string host = "mcrai2.vshosting.cz")
        {
            var dir = $@"{baseFolder}weby\{webName}";

            if (webName == string.Empty)
            {
                Console.Error.WriteLine($"Folder {dir} is invalid because parameter '--web-name' is empty.");
            }
            else if (!Directory.Exists(dir))
            {
                Console.Error.WriteLine($"Folder {dir} does not exist.");
            }
            else
            {
                var findWhat = File.ReadAllLines($@"{baseFolder}important\find_what.txt");
                var replaceWith = File.ReadAllLines($@"{baseFolder}important\replace_with.txt");

                var upgrader = new PhpUpgrader(findWhat, replaceWith, baseFolder, webName, adminFolders, db, user, password, host);

                Console.WriteLine("\nProcessed files:\n");
                upgrader.UpgradeAllFilesRecursively(dir);

                Console.WriteLine($"\nAutomatic PHP upgrade of {webName} is complete!");
                Console.WriteLine($"Files containing mysql_: {upgrader.FilesContainingMysql.Count}");

                foreach (var fileName in upgrader.FilesContainingMysql)
                    Console.WriteLine(fileName);
            }
        }
    }
}

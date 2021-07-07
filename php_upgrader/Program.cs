using System;
using System.IO;

namespace php_upgrader
{
    class Program
    {
        /// <summary>
        /// McRAI RS Mona PHP upgrader z verze 5 na verzi 7
        /// autor: Tomáš Milostný
        /// </summary>
        /// <param name="webName">Název webu ve složce 'weby' (nesmí chybět).</param>
        /// <param name="adminFolders">Složky obsahující administraci RS Mona (default prázdné: 1 složka admin)</param>
        /// <param name="baseFolder">Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'.</param>
        static void Main(string webName, string[]? adminFolders = null, string baseFolder = @"C:\McRAI\")
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
                
                var upgrader = new PhpUpgrader(findWhat, replaceWith, baseFolder, webName, adminFolders);
                
                Console.WriteLine("\nProcessed files:\n");
                upgrader.UpgradeFilesInFolders(dir);
                upgrader.UpgradeFiles(dir);
                
                Console.WriteLine($"\nAutomatic PHP upgrade of {webName} is complete!");
                Console.WriteLine($"Files containing mysql_: {upgrader.FilesContainingMysql.Count}");
                
                foreach (var fileName in upgrader.FilesContainingMysql)
                    Console.WriteLine(fileName);
            }
        }
    }
}

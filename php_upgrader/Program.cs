using System;
using System.IO;

var baseFolder = @"C:\McRAI\";

if (args.Length > 0)
{
    var webName = args[0];
    var dir = baseFolder + "weby\\" + webName;
 
    if (Directory.Exists(dir))
    {
        var findWhat = File.ReadAllLines(baseFolder + @"\important\find_what.txt");
        var replaceWith = File.ReadAllLines(baseFolder + @"\important\replace_with.txt");

        var upgrader = new PhpUpgrader(findWhat, replaceWith, baseFolder, webName);
        
        Console.WriteLine("\nProcessed files:\n");
        upgrader.UpgradeFilesInFolders(dir);
        upgrader.UpgradeFiles(dir);

        Console.WriteLine($"\nAutomatic PHP upgrade of {webName} is complete!");
        Console.WriteLine($"Files containing mysql_: {upgrader.FilesContainingMysql.Count}");

        foreach (var fileName in upgrader.FilesContainingMysql)
            Console.WriteLine(fileName);
    }
    else
        Console.WriteLine($"Folder {dir} does not exist.");
}
else
    Console.WriteLine($"php_upgrader [WEB_FOLDER_NAME]\n\nweb folder name from {baseFolder}weby\\.");

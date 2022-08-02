namespace PhpUpgrader;

public static class BackupManager
{
    private const string _backupFolder = "_backup";

    /// <summary>
    /// Zkopíruje aktuální obsah souboru do zálohovací složky (<see cref="_backupFolder"/>), pokud už v ní neexistuje.
    /// </summary>
    /// <param name="filePath"> Cesta k souboru, který má být zálohován. </param>
    /// <param name="baseFolder"> Root složka. </param>
    /// <param name="webName"> Název webu. </param>
    public static void CreateBackupFile(string filePath, string baseFolder, string webName)
    {
        var s = Path.DirectorySeparatorChar;
        var backupFile = new FileInfo(filePath.Replace($"{baseFolder}{s}weby{s}{webName}{s}",
                                                       $"{baseFolder}{s}weby{s}{_backupFolder}{s}{webName}{s}"));
        backupFile.Directory.Create();
        if (!backupFile.Exists)
        {
            File.Copy(filePath, backupFile.FullName);
        }
    }

    /// <summary>
    /// Přepíše soubory jejich zálohou.
    /// </summary>
    /// <param name="useBackup"> False => uživatel bude tázán, zda chce obnovit zálohu, jinak obnovit bez ptaní. </param>
    /// <param name="baseFolder"> Root složka. </param>
    /// <param name="webName"> Název webu. </param>
    public static void LoadBackupFiles(bool useBackup, string baseFolder, string webName)
    {
        var backupDirPath = Path.Join("weby", _backupFolder, webName);
        var destDirPath = Path.Join("weby", webName);

        var dir = new DirectoryInfo(Path.Join(baseFolder, backupDirPath));
        if (dir.Exists && (useBackup || AskIfLoadBackup(dir.FullName)))
        {
            LoadBackupFiles(dir, backupDirPath, destDirPath);
            Console.WriteLine();
        }
    }

    private static bool AskIfLoadBackup(string fullBackupPath)
    {
        Console.Write("\nExistuje záloha ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(fullBackupPath);
        Console.ResetColor();
        Console.WriteLine(". Obnovit soubory ze zálohy? (y/n)");

        return Console.Read() == 'y';
    }

    private static void LoadBackupFiles(DirectoryInfo backupDir, string backupPathPart, string destinationPathPart)
    {
        foreach (var subDir in backupDir.GetDirectories())
        {
            LoadBackupFiles(subDir, backupPathPart, destinationPathPart);
        }
        foreach (var backupFile in backupDir.GetFiles())
        {
            var destinationFile = backupFile.FullName.Replace(backupPathPart, destinationPathPart);
            Console.Write("Kopíruji zálohu souboru ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(destinationFile);
            Console.ResetColor();
            Console.WriteLine(" ...");
            
            backupFile.CopyTo(destinationFile, overwrite: true);
        }
    }
}

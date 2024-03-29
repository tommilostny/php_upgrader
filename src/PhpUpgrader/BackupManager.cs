﻿namespace PhpUpgrader;

public static class BackupManager
{
    private const string _backupFolder = "_backup";

    /// <summary>
    /// Zkopíruje aktuální obsah souboru do zálohovací složky (<see cref="_backupFolder"/>), pokud už v ní neexistuje.
    /// </summary>
    /// <param name="filePath"> Cesta k souboru, který má být zálohován. </param>
    /// <param name="baseFolder"> Root složka. </param>
    /// <param name="webName"> Název webu. </param>
    /// <param name="modified">
    /// Příznak modifikace. Soubor se zálohuje pouze, pokud je modifikován.<br />
    /// Pokud soubor zálohy již existuje, ale soubor nebyl modifikován, zálohovaný soubor se může smazat
    /// (je totiž v původní nemodifikované podobě).
    /// </param>
    public static void CreateBackupFile(string filePath, string baseFolder, string webName, bool modified)
    {
        var s = Path.DirectorySeparatorChar;
        var backupFile = new FileInfo(filePath.Replace($"{baseFolder}{s}weby{s}{webName}{s}",
                                                       $"{baseFolder}{s}weby{s}{_backupFolder}{s}{webName}{s}",
                                                       StringComparison.Ordinal));
        if (!backupFile.Exists && modified)
        {
            backupFile.Directory.Create();
            File.Copy(filePath, backupFile.FullName);
            return;
        }
        if (backupFile.Exists && !modified)
        {
            backupFile.Delete();
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
            Console.Write("\nProbíhá kopírování souborů ze zálohy ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(dir.FullName);
            Console.ResetColor();
            Console.WriteLine("...");

            LoadBackupFiles(dir, backupDirPath, destDirPath);            
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
            var destinationFile = backupFile.FullName.Replace(backupPathPart,
                                                              destinationPathPart,
                                                              StringComparison.Ordinal);
            backupFile.CopyTo(destinationFile, overwrite: true);
        }
    }
}

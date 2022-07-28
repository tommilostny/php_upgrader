namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeTinyAjaxBehaviorRoutine
{
    /// <summary>
    /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
    /// </summary>
    public static bool UpgradeTinyAjaxBehavior(this MonaUpgrader upgrader, string filePath)
    {
        var file = new FileWrapper(filePath, string.Empty);

        if (upgrader.AdminFolders.Any(af => filePath.Contains(Path.Join(af, "include", "TinyAjaxBehavior.php"))))
        {
            var tabPath = Path.Join(upgrader.BaseFolder, "important", "TinyAjaxBehavior.txt");
            if (File.GetLastWriteTime(tabPath) == File.GetLastWriteTime(file.Path))
            {
                file.WriteStatus(modified: false);
            }
            else
            {
                File.Copy(tabPath, file.Path, overwrite: true);
                upgrader.ModifiedFilesCount++;
                file.WriteStatus(modified: true);
            }
            return true;
        }
        return false;
    }
}

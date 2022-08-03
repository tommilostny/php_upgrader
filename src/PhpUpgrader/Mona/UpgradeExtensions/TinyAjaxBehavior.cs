namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class TinyAjaxBehavior
{
    /// <summary>
    /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
    /// </summary>
    /// <returns> <b>True</b> <em>(není třeba aktualizovat)</em>, pokud se jednalo o TinyAjaxBehavior, jinak <b>False</b>. </returns>
    public static bool UpgradeTinyAjaxBehavior(this MonaUpgrader upgrader, string filePath)
    {
        var isTinyAjaxBehavior = upgrader.AdminFolders.Any(af => filePath.Contains(Path.Join(af, "include", "TinyAjaxBehavior.php"), StringComparison.Ordinal));
        if (isTinyAjaxBehavior)
        {
            var file = new FileWrapper(filePath, content: null);
            var tabPath = Path.Join(upgrader.BaseFolder, "important", "TinyAjaxBehavior.txt");

            if (File.GetLastWriteTime(tabPath) == File.GetLastWriteTime(file.Path))
            {
                file.WriteStatus(modified: false);
                return true;
            }
            File.Copy(tabPath, file.Path, overwrite: true);
            upgrader.ModifiedFilesCount++;
            file.WriteStatus(modified: true);
        }
        return isTinyAjaxBehavior;
    }
}

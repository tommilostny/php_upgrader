namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class TinyAjaxBehavior
{
    private static string[]? _afTinyAjaxBehaviorFiles = null;

    /// <summary>
    /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
    /// </summary>
    /// <returns> <b>True</b> <em>(není třeba aktualizovat)</em>, pokud se jednalo o TinyAjaxBehavior, jinak <b>False</b>. </returns>
    public static bool UpgradeTinyAjaxBehavior(this MonaUpgrader upgrader, string filePath)
    {
        _afTinyAjaxBehaviorFiles ??= upgrader.AdminFolders
            .Select(af => Path.Join(af, "include", "TinyAjaxBehavior.php"))
            .ToArray();

        var isTinyAjaxBehavior = _afTinyAjaxBehaviorFiles.Any(af => filePath.EndsWith(af, StringComparison.Ordinal));
        if (isTinyAjaxBehavior)
        {
            var file = new FileWrapper(filePath, content: null);
            var tabPath = Path.Join(upgrader.BaseFolder, "important", "TinyAjaxBehavior.txt");

            if (File.GetLastWriteTime(tabPath) == File.GetLastWriteTime(file.Path))
            {
                file.PrintStatus(modified: false);
                return true;
            }
            File.Copy(tabPath, file.Path, overwrite: true);
            upgrader.ModifiedFiles.Add(file.Path);
            file.PrintStatus(modified: true);
        }
        return isTinyAjaxBehavior;
    }
}

namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
    /// </summary>
    public bool UpgradeTinyAjaxBehavior(string filePath)
    {
        var file = new FileWrapper(filePath, string.Empty);

        if (AdminFolders.Any(af => filePath.Contains(Path.Join(af, "include", "TinyAjaxBehavior.php"))))
        {
            var tabPath = Path.Join(BaseFolder, "important", "TinyAjaxBehavior.txt");
            if (File.GetLastWriteTime(tabPath) == File.GetLastWriteTime(file.Path))
            {
                file.WriteStatus(false);
            }
            else
            {
                File.Copy(tabPath, file.Path, overwrite: true);
                ModifiedFilesCount++;
                file.WriteStatus(true);
            }
            return true;
        }
        return false;
    }
}

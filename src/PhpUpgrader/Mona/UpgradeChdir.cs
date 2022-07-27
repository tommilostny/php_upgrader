namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
    public void UpgradeChdir(FileWrapper file)
    {
        if (!AdminFolders.Any(af => file.Path.Contains(Path.Join(af, "funkce", "vytvoreni_adr.php"))))
        {
            return;
        }
        const string chdir = "chdir";
        const string commentedChdir = $"//{chdir}";
        if (!file.Content.Contains(commentedChdir))
        {
            file.Content.Replace(chdir, commentedChdir);
        }
    }
}

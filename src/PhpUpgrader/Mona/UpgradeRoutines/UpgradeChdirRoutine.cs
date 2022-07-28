namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class UpgradeChdirRoutine
{
    /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
    public static void UpgradeChdir(this FileWrapper file, IEnumerable<string> adminFolders)
    {
        if (!adminFolders.Any(af => file.Path.Contains(Path.Join(af, "funkce", "vytvoreni_adr.php"))))
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

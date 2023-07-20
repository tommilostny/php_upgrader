namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class Chdir
{
    private static string[]? _afVytvAdrFiles = null;

    /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
    public static FileWrapper UpgradeChdir(this FileWrapper file, string[] adminFolders)
    {
        _afVytvAdrFiles ??= adminFolders.Select(af => Path.Join(af, "funkce", "vytvoreni_adr.php")).ToArray();

        if (_afVytvAdrFiles.Any(af => file.Path.EndsWith(af, StringComparison.Ordinal)))
        {
            const string chdir = "chdir";
            const string commentedChdir = $"//{chdir}";
            if (!file.Content.Contains(commentedChdir))
            {
                file.Content.Replace(chdir, commentedChdir);
            }
        }
        return file;
    }
}

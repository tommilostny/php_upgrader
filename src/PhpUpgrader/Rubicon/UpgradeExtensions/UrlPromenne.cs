namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class UrlPromenne
{
    /// <summary> Opravuje chybně zapsanou proměnnou $modul v souboru funkce/url_promenne.php </summary>
    public static FileWrapper UpgradeUrlPromenne(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("funkce", "url_promene.php"), StringComparison.Ordinal))
        {
            file.Content.Replace("if($url != \"0\" AND modul != \"obsah\"):",
                                 "if($url != \"0\" AND $modul != \"obsah\"):");
        }
        return file;
    }
}

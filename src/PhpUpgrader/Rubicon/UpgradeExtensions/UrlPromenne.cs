namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class UrlPromenne
{
    private static readonly string[] _urlPromenneFiles = new[]
    {
        Path.Join("funkce", "url_promene.php"),
        Path.Join("funkce", "url_promenne.php"),
    };

    /// <summary> Opravuje chybně zapsanou proměnnou $modul v souboru funkce/url_promenne.php </summary>
    public static FileWrapper UpgradeUrlPromenne(this FileWrapper file)
    {
        if (_urlPromenneFiles.Any(f => file.Path.EndsWith(f, StringComparison.Ordinal)))
        {
            file.Content.Replace("if($url != \"0\" AND modul != \"obsah\"):",
                                 "if($url != \"0\" AND $modul != \"obsah\"):");
        }
        return file;
    }
}

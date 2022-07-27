namespace PhpUpgrader.Rubicon;

public partial class RubiconUpgrader
{
    /// <summary> Opravuje chybně zapsanou proměnnou $modul v souboru funkce/url_promenne.php </summary>
    public static void UpgradeUrlPromenne(FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("funkce", "url_promene.php")))
        {
            file.Content.Replace("if($url != \"0\" AND modul != \"obsah\"):",
                                 "if($url != \"0\" AND $modul != \"obsah\"):");
        }
    }
}

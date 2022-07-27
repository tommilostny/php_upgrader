namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// upravit soubor anketa/anketa.php - r.3 (odmazat ../)
    ///     - include_once "../setup.php"; na include_once "setup.php";
    /// </summary>
    public static void UpgradeAnketa(FileWrapper file)
    {
        if (file.Path.Contains(Path.Join("anketa", "anketa.php")))
        {
            file.Content.Replace(@"include_once(""../setup.php"")", @"include_once(""setup.php"")");
        }
    }
}

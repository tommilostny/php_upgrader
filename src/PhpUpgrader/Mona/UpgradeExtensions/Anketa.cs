namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class Anketa
{
    /// <summary>
    /// upravit soubor anketa/anketa.php - r.3 (odmazat ../)
    ///     - include_once "../setup.php"; na include_once "setup.php";
    /// </summary>
    public static FileWrapper UpgradeAnketa(this FileWrapper file)
    {
        if (file.Path.Contains(Path.Join("anketa", "anketa.php"), StringComparison.Ordinal))
        {
            file.Content.Replace(@"include_once(""../setup.php"")", @"include_once(""setup.php"")");
        }
        return file;
    }
}

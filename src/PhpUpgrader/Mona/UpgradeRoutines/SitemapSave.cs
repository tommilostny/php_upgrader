namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class SitemapSave
{
    /// <summary>
    /// upravit soubor admin/sitemap_save.php cca radek 84
    ///     - pridat podminku „if($query_text_all !== FALSE)“
    ///     a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
    /// </summary>
    public static FileWrapper UpgradeSitemapSave(this FileWrapper file, IEnumerable<string> adminFolders)
    {
        const string lookingFor = "while($data_stranky_text_all = mysqli_fetch_array($query_text_all))";
        const string adding = "if($query_text_all !== FALSE)";
        const string addingLine = $"          {adding}\n          {{\n";

        if (!adminFolders.Any(af => file.Path.EndsWith(Path.Join(af, "sitemap_save.php")))
            || !file.Content.Contains(lookingFor) || file.Content.Contains(adding))
        {
            return file;
        }
        var sfBracket = false;
        var lines = file.Content.Split();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains(lookingFor))
            {
                line.Insert(0, addingLine);
                sfBracket = true;
            }
            if (line.Contains('}') && sfBracket)
            {
                line.Append($"\n{line}");
                line.Insert(0, "    ");
                sfBracket = false;
            }
        }
        lines.JoinInto(file.Content);
        return file;
    }
}

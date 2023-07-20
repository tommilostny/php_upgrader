namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class SitemapSave
{
    private static string[]? _afSitemapFiles = null;

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

        _afSitemapFiles ??= adminFolders.Select(af => Path.Join(af, "sitemap_save.php")).ToArray();

        if (_afSitemapFiles.Any(af => file.Path.EndsWith(af, StringComparison.Ordinal))
            && file.Content.Contains(lookingFor) && !file.Content.Contains(adding))
        {
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
                    line.Append(new LineFormat(), $"\n{line}");
                    sfBracket = false;
                }
            }
            lines.JoinInto(file.Content);
        }
        return file;
    }

    private class LineFormat : IFormatProvider, ICustomFormatter
    {
        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => arg switch
        {
            not null and StringBuilder sb => sb.ToString()[4..],
            _ => null
        };

        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
    }
}

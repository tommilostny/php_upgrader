namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class FunkceHomeBasicFunkce_NajdiVDb
{
    public static FileWrapper UpgradeNajdiVDb(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("funkce", "home", "basic_funkce.php"), StringComparison.Ordinal))
        {
            var content = file.Content.ToString();
            var updated = VysledekRegex().Replace(content, _wrapInIfEval);
            file.Content.Replace(content, updated);
        }
        return file;
    }

    [GeneratedRegex(@"\$vysledek\s?=\s?(?<condVar>\$\w+?)\[(.|\n)*?return\s?\(?\$vysledek\)?;.*?\n", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex VysledekRegex();

    private static MatchEvaluator _wrapInIfEval = new
    (
        match => $@"if ({match.Groups["condVar"]} !== false) {{
        {match.Value.Replace("\treturn", "        return", StringComparison.Ordinal).Replace("//return", "    //return", StringComparison.Ordinal)}    }} else {{
        // Žádné výsledky nebyly nalezeny, můžete vrátit například null nebo jinou vhodnou hodnotu.
        return """";
    }}
");
}

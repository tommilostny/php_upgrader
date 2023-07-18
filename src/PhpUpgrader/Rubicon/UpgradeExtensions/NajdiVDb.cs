namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class NajdiVDb
{
    public static FileWrapper UpgradeNajdiVDb(this FileWrapper file)
    {
        if (file.Content.Contains("function najdi_v_db"))
        {
            file.Content.Replace(
                VysledekRegex().Replace(
                    file.Content.ToString(),
                    _wrapInIfEval
                )
            );
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

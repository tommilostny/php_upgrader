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

    [GeneratedRegex(@"(?<indent>[ \t]*)(?<line1>(?<var1>\$\w+?)\s?=\s?pg_query.*?;)[^}]*?(?<line2>(?<var2>\$\w+?)\s?=\s?pg_fetch_assoc.*?;)[^}]*?(?<result>\$vysledek\s?=\s?(\k<var2>)\[(.|\n)*?return\s?\(?\$vysledek\)?;.*?\n)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex VysledekRegex();

    private static MatchEvaluator _wrapInIfEval = new(match =>
    {
        var s = match.Groups["indent"].Value;
        var line1 = match.Groups["line1"].Value;
        var line2 = match.Groups["line2"].Value;
        var var1 = match.Groups["var1"].Value;
        var var2 = match.Groups["var2"].Value;
        var result = match.Groups["result"].Value;

        return $"{s}if (substr($query, -4) === \"= ''\") {{\n{s}\treturn '';\n{s}}}\n{s}{line1}\n{s}if ({var1} === false) {{\n{s}\treturn '';\n{s}}}\n{s}{line2}\n{s}if ({var2} === false) {{\n{s}\treturn '';\n{s}}}\n{s}{result}";
    });
}

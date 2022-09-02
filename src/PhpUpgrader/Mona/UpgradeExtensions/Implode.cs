namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class Implode
{
    /// <summary>
    /// PHPStorm:
    /// Funkce implode má upravené pořadí parametrů.
    /// Upravit <b>$pole, $string</b> >>> <b>$string, $pole</b>
    /// </summary>
    /// <remarks>
    /// Legacy signature (deprecated as of PHP 7.4.0, removed as of PHP 8.0.0):
    ///    implode(array $array, string $separator) : string
    /// </remarks>
    public static FileWrapper UpgradeImplode(this FileWrapper file)
    {
        if (file.Content.Contains("implode"))
        {
            var evaluator = new MatchEvaluator(ImplodeParamSwitch);
            var content = file.Content.ToString();
            var updated = Regex.Replace(content,
                                        @"implode\s?\(\s*?(?<array>\$\w+)\s*?,\s*?(?<sep>(?<quote>(""|')).*\k<quote>)\s*?\)",
                                        evaluator,
                                        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                                        TimeSpan.FromSeconds(4));
            file.Content.Replace(content, updated);
        }
        return file;
    }

    private static string ImplodeParamSwitch(Match match)
    {
        var array = match.Groups["array"];
        var separator = match.Groups["sep"];
        return $"implode({separator}, {array})";
    }
}

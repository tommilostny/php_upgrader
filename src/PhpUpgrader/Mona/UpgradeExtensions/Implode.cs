namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class Implode
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
            file.Content.Replace(
                OldImplodeRegex().Replace(
                    file.Content.ToString(),
                    _implodeParamSwitchEvaluator
                )
            );
        }
        return file;
    }

    private static readonly MatchEvaluator _implodeParamSwitchEvaluator = new(match =>
    {
        var array = match.Groups["array"];
        var separatorStr = match.Groups["sep"];
        return $"implode({separatorStr}, {array})";
    });

    [GeneratedRegex(@"implode\s?\(\s*?(?<array>\$\w+)\s*?,\s*?(?<sep>(?<quote>(""|')).*?(?<!\\)\k<quote>)\s*?\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex OldImplodeRegex();
}

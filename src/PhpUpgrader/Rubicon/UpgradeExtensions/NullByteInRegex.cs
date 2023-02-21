namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class NullByteInRegex
{
    /// <summary>
    /// PHPStan:
    /// ------ ---------------------------------------------------------------------------------
    ///  Line   /mnt/c/McRAI/weby/hokejova-vystroj/piwika/libs/upgradephp/upgrade.php
    /// ------ ---------------------------------------------------------------------------------
    ///  425    Regex pattern is invalid: Null byte in regex in pattern: /^[\-^_]+$/
    ///  440    Regex pattern is invalid: Null byte in regex in pattern: /^[^0-9A-Za-z\- ^?--]+$/
    /// </summary>
    public static FileWrapper UpgradeNullByteInRegex(this FileWrapper file)
    {
        if (file.Content.Contains("preg_"))
        {
            var evaluator = new MatchEvaluator(NullBytesInPatternEvaluator);
            var content = file.Content.ToString();
            var updated = NullByteInPatternRegex().Replace(content, evaluator);
            file.Content.Replace(content, updated);
        }
        return file;
    }

    private static string NullBytesInPatternEvaluator(Match match)
    {
        var pattern = match.Groups["pattern"];
        var updatedPattern = $"'{pattern.ValueSpan[1..^2]}'";
        return match.Value.Replace(pattern.Value, updatedPattern, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"preg_.*?(?<pattern>"".*\\((0(?![1-7]){1,3})|(\\x0(?![1-9a-fA-F]){1,2})).*"")\s?,", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 6666)]
    private static partial Regex NullByteInPatternRegex();
}

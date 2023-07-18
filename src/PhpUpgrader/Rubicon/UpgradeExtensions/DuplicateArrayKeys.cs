namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class DuplicateArrayKeys
{
    /// <summary> PHPStan: Array has 2 duplicate keys </summary>
    public static FileWrapper UpgradeDuplicateArrayKeys(this FileWrapper file)
    {
        file.Content.Replace(
            DupKeysArrayRegex().Replace(
                file.Content.ToString(),
                _arrayKeyValueEvaluator
            )
        );
        return file;
    }

    private static readonly MatchEvaluator _arrayKeyValueEvaluator = new(match =>
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var kvExpressions = new Stack<string>();
        var matches = ArrayKeyValueRegex().Matches(match.Value);

        foreach (var kv in matches.Reverse())
        {
            var keyEndIndex = kv.ValueSpan[1..].IndexOf('"') + 1;
            var key = kv.Value[1..keyEndIndex];

            if (keys.Contains(key))
            {
                continue;
            }
            keys.Add(key);
            kvExpressions.Push(kv.Value);
        }
        var bracketIndex = match.ValueSpan.IndexOf('(') + 1;
        return $"{match.ValueSpan[..bracketIndex]}{string.Join(", ", kvExpressions)})";
    });

    [GeneratedRegex(@"\$(cz_)?osetreni(_url)?\s?=\s?array\((""([^""]|\\""){0,9}""\s?=>\s?""([^""]|\\""){0,9}"",? ?)+\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex DupKeysArrayRegex();
    
    [GeneratedRegex(@"""([^""]|\\""){0,9}""\s?=>\s?""([^""]|\\""){0,9}""", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex ArrayKeyValueRegex();
}

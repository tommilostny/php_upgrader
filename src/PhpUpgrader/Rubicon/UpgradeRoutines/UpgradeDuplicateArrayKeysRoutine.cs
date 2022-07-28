namespace PhpUpgrader.Rubicon.UpgradeRoutines;

public static class UpgradeDuplicateArrayKeysRoutine
{
    /// <summary> PHPStan: Array has 2 duplicate keys </summary>
    public static void UpgradeDuplicateArrayKeys(this FileWrapper file)
    {
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(ArrayKeyValueEvaluator);
        var updated = Regex.Replace(content,
                                    @"\$(cz_)?osetreni(_url)?\s?=\s?array\((""([^""]|\\""){0,9}""\s?=>\s?""([^""]|\\""){0,9}"",? ?)+\)",
                                    evaluator,
                                    RegexOptions.Compiled);

        file.Content.Replace(content, updated);
    }

    private static string ArrayKeyValueEvaluator(Match match)
    {
        var keys = new HashSet<string>();
        var kvExpressions = new Stack<string>();
        var matches = Regex.Matches(match.Value,
                                    @"""([^""]|\\""){0,9}""\s?=>\s?""([^""]|\\""){0,9}""",
                                    RegexOptions.Compiled);

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
    }
}

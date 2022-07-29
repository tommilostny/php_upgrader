namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class UpgradeIfEmptyRoutine
{
    /// <summary> PHPStan: Right side of || is always false. </summary>
    /// <remarks> if ($id != "" || $id != null) </remarks>
    public static FileWrapper UpgradeIfEmpty(this FileWrapper file)
    {
        var evaluator = new MatchEvaluator(IfEmptyMatchEvaluator);
        var content = file.Content.ToString();
        var updated = Regex.Replace(content,
                                    @"if\s?\(\$\w+\s?!=\s?""""\s?\|\|\s?\$\w+\s?!=\s?null\)",
                                    evaluator,
                                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
        file.Content.Replace(content, updated);
        return file;
    }

    private static string IfEmptyMatchEvaluator(Match match)
    {
        var varStartIndex = match.ValueSpan.IndexOf('$');
        var varEndIndex = match.ValueSpan.IndexOf('!') - 1;
        var varValue1 = match.ValueSpan[varStartIndex..varEndIndex];

        varStartIndex = match.ValueSpan.LastIndexOf('|') + 2;
        varEndIndex = match.ValueSpan.LastIndexOf('!') - 1;
        var varValue2 = match.ValueSpan[varStartIndex..varEndIndex];

        return varValue1.SequenceEqual(varValue2) ? $"if (!empty({varValue1}))" : match.Value;
    }
}

namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class IfEmpty
{
    /// <summary> PHPStan: Right side of || is always false. </summary>
    /// <remarks> if ($id != "" || $id != null) </remarks>
    public static FileWrapper UpgradeIfEmpty(this FileWrapper file)
    {
        file.Content.Replace(
            AlwaysFalseIfRegex().Replace(file.Content.ToString(), _ifEmptyMatchEvaluator)
        );
        return file;
    }

    [GeneratedRegex(@"if\s?\(\$\w+\s?!=\s?""""\s?\|\|\s?\$\w+\s?!=\s?null\)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 66666)]
    private static partial Regex AlwaysFalseIfRegex();

    private static readonly MatchEvaluator _ifEmptyMatchEvaluator = new(match =>
    {
        var varStartIndex = match.ValueSpan.IndexOf('$');
        var varEndIndex = match.ValueSpan.IndexOf('!') - 1;
        var varValue1 = match.ValueSpan[varStartIndex..varEndIndex];

        varStartIndex = match.ValueSpan.LastIndexOf('|') + 2;
        varEndIndex = match.ValueSpan.LastIndexOf('!') - 1;
        var varValue2 = match.ValueSpan[varStartIndex..varEndIndex];

        return varValue1.SequenceEqual(varValue2) ? $"if (!empty({varValue1}))" : match.Value;
    });
}

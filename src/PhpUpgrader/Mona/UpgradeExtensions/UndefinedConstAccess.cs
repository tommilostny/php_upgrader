namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class UndefinedConstAccess
{
    private static MatchEvaluator? _evaluator;

    public static FileWrapper UpgradeUndefinedConstAccess(this FileWrapper file)
    {
        _evaluator ??= new MatchEvaluator(_UndefinedConstAccessEvaluator);
        file.Content.Replace(UndefinedConstAccessRegex().Replace(file.Content.ToString(), _evaluator));
        return file;

        string _UndefinedConstAccessEvaluator(Match match)
        {
            var notConst = match.Groups["notConst"].Value;
            if (file.Content.Contains($"define('{notConst}'", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }
            var var = match.Groups["var"].Value;
            return $"{var}['{notConst}']";
        }
    }

    [GeneratedRegex(@"(?<var>\$\w+?\S*?)\[(?<notConst>[a-z_]+?)\]", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex UndefinedConstAccessRegex();
}

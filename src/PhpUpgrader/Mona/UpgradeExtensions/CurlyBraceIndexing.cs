namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class CurlyBraceIndexing
{
    /// <summary>
    /// PhpStorm:
    /// Curly brace access syntax is deprecated since PHP 7.4
    /// </summary>
    public static FileWrapper UpgradeCurlyBraceIndexing(this FileWrapper file)
    {
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(m => CurlyToSquareBracketsIndex(m, content));

        var updated = CurlyBraceAccessRegex().Replace(content, evaluator);

        file.Content.Replace(content, updated);
        return file;
    }

    private static string CurlyToSquareBracketsIndex(Match match, string source)
    {
        var index = match.Groups["index"].Value;
        var array = match.Groups["array"].Value.TrimEnd();
        
        //Ošetření pro speciální případy, kdy nechceme upravovat.
        if (string.IsNullOrWhiteSpace(index)
            || array.EndsWith("->", StringComparison.Ordinal)
            || array.EndsWith("FROM", StringComparison.OrdinalIgnoreCase)
            || MatchInString(match, source))
        {
            return match.Value;
        }
        return $"{array}[{index}]";
    }

    private static bool MatchInString(Match match, string source)
    {
        bool startQuote = false, endQuote = false;
        for (var i = match.Index; i >= 0 && source[i] != '\n'; i--)
        {
            if (source[i] == '"')
            {
                startQuote = !startQuote;
            }
        }
        for (var i = match.Index + match.Length; i < source.Length && source[i] != '\n'; i++)
        {
            if (source[i] == '"')
            {
                endQuote = !endQuote;
            }
        }
        return startQuote && endQuote;
    }

    [GeneratedRegex(@"(?<array>\$[^;\n=.,(})/+*\s""'[]+?)\s?{(?<index>[^;\n]*?)}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 3456)]
    private static partial Regex CurlyBraceAccessRegex();
}

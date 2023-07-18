namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class CurlyBraceIndexing
{
    /// <summary>
    /// PhpStorm:
    /// Curly brace access syntax is deprecated since PHP 7.4
    /// </summary>
    public static FileWrapper UpgradeCurlyBraceIndexing(this FileWrapper file)
    {
        _content = file.Content.ToString();
        file.Content.Replace(
            CurlyBraceAccessRegex().Replace(_content, _curlyToSquareBracketEvaluator)
        );
        _content = null;
        return file;
    }

    private static string? _content = null;

    private static readonly MatchEvaluator _curlyToSquareBracketEvaluator = new(match =>
    {
        var index = match.Groups["index"].Value;
        var array = match.Groups["array"].Value.TrimEnd();
        
        //Ošetření pro speciální případy, kdy nechceme upravovat.
        if (string.IsNullOrWhiteSpace(index)
            || array.EndsWith("->", StringComparison.Ordinal)
            || array.EndsWith("FROM", StringComparison.OrdinalIgnoreCase)
            || MatchInString(match, _content))
        {
            return match.Value;
        }
        return $"{array}[{index}]";
    });

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

    [GeneratedRegex(@"(?<array>\$[^;\n=.,(})/+*\s""'[]+?)\s?{(?<index>[^;\n]*?)}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex CurlyBraceAccessRegex();
}

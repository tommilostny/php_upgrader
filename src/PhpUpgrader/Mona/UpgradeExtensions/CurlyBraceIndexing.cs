namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class CurlyBraceIndexing
{
    /// <summary>
    /// PhpStorm:
    /// Curly brace access syntax is deprecated since PHP 7.4
    /// </summary>
    public static FileWrapper UpgradeCurlyBraceIndexing(this FileWrapper file)
    {
        var evaluator = new MatchEvaluator(CurlyToSquareBracketsIndex);
        var content = file.Content.ToString();
        var updated = Regex.Replace(content,
                                    @"(?<array>\$[^;\n=.,(})]+?)\s?{(?<index>[^;\n]*?)}",
                                    evaluator,
                                    RegexOptions.ExplicitCapture | RegexOptions.Compiled,
                                    TimeSpan.FromSeconds(4));

        file.Content.Replace(content, updated);
        return file;
    }

    private static string CurlyToSquareBracketsIndex(Match match)
    {
        var index = match.Groups["index"].Value;
        var array = match.Groups["array"].Value.TrimEnd();
        
        //Ošetření pro speciální případy, kdy nechceme upravovat.
        if (string.IsNullOrWhiteSpace(index)
            || array.EndsWith("->", StringComparison.Ordinal)
            || array.EndsWith("FROM", StringComparison.OrdinalIgnoreCase))
        {
            return match.Value;
        }
        return $"{array}[{index}]";
    }
}

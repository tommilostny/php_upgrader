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
                                    @"(?<array>\$[^;\n=.,(})]+?)\s?{(?<index>.*?)(?<!.*;.*)}",
                                    evaluator,
                                    RegexOptions.ExplicitCapture | RegexOptions.Compiled,
                                    TimeSpan.FromSeconds(4));

        file.Content.Replace(content, updated);
        return file;
    }

    private static string CurlyToSquareBracketsIndex(Match match)
    {
        var index = match.Groups["index"];
        var array = match.Groups["array"];
        if (!index.Success || !array.Success
            || string.IsNullOrWhiteSpace(index.Value)
            || array.Value.TrimEnd().EndsWith("->", StringComparison.Ordinal)
            || array.Value.TrimEnd().EndsWith("FROM", StringComparison.OrdinalIgnoreCase))
        {
            return match.Value;
        }
        return $"{array}[{index}]";
    }
}

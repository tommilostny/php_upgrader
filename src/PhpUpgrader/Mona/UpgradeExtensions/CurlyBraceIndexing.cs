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
                                    @"(?<array>\$\w+?)\s?{(?<index>.*?)}",
                                    evaluator,
                                    RegexOptions.ExplicitCapture | RegexOptions.Compiled,
                                    TimeSpan.FromSeconds(4));

        file.Content.Replace(content, updated);
        return file;
    }

    private static string CurlyToSquareBracketsIndex(Match match)
    {
        var array = match.Groups["array"];
        var index = match.Groups["index"];
        return $"{array}[{index}]";
    }
}

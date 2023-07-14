namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class UnparenthesizedPlus
{
    /// <summary>
    /// Deprecated: The behavior of unparenthesized expressions containing both '.' and '+'/'-' will change in PHP 8:
    /// '+'/'-' will take a higher precedence in /var/www/vhosts/iviki.cz/rubicon/modules/card/create_order_data.php on line 102
    /// </summary>
    public static FileWrapper UpgradeUnparenthesizedPlus(this FileWrapper file)
    {
        var content = file.Content.ToString();
        var updated = PlusInStringConcatRegex().Replace(content, _addParenthesesEval);
        file.Content.Replace(content, updated);
        return file;
    }

    [GeneratedRegex(@"(?<p1>(?<='(?=.*?;).*?)""[^"".\r\n]*?\.)(?<inConcat>[^("".\r\n]*?\+[^)"".\r\n]*?)(?<p2>\.[^""\r\n]*?"")", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex PlusInStringConcatRegex();

    private static MatchEvaluator _addParenthesesEval = new
    (
        match => $"{match.Groups["p1"]}({match.Groups["inConcat"]}){match.Groups["p2"]}"
    );
}

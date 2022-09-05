namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class RegexFunctions
{
    private static readonly RegexOptions _options = RegexOptions.Compiled | RegexOptions.ExplicitCapture;
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(4);

    /// <summary>
    /// - funkci ereg nebo ereg_replace doplnit do prvního parametru delimetr na začátek a nakonec (if(ereg('.+@.+..+', $retezec))
    /// // puvodni, jiz nefunkcni >>> if(preg_match('#.+@.+..+#', $retezec)) // upravene - delimiter zvolen #)
    /// </summary>
    public static FileWrapper UpgradeRegexFunctions(this FileWrapper file)
    {
        var evaluator = new MatchEvaluator(PregMatchEvaluator);
        UpgradeEreg(file, evaluator);
        UpgradeSplit(file, evaluator);
        return file;
    }

    private static void UpgradeEreg(FileWrapper file, MatchEvaluator evaluator)
    {
        if (!file.Content.Contains("ereg"))
            return;

        var content = file.Content.ToString();

        var updated = Regex.Replace(content, @"ereg(_replace|i)?\s?\('(\\'|[^'])*'", evaluator, _options, _timeout);
        updated = Regex.Replace(updated, @"ereg(_replace|i)?\s?\(""(\\""|[^""])*""", evaluator, _options, _timeout);

        updated = Regex.Replace(updated, @"eregi?\s?\( ?\$", "preg_match($", _options, _timeout);
        updated = Regex.Replace(updated, @"ereg_replace\s?\(\s?\$", "preg_replace($", _options, _timeout);

        if (Regex.IsMatch(updated, @"\n[^//]{0,236}ereg[^(;"",]*\(", _options, _timeout))
        {
            file.Warnings.Add("Nemodifikovaná funkce ereg!");
        }
        file.Content.Replace(content, updated);
    }

    private static void UpgradeSplit(FileWrapper file, MatchEvaluator evaluator)
    {
        if (!file.Content.Contains("split") || file.Content.Contains("preg_split"))
            return;

        var javascript = false;
        var lines = file.Content.Split();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("<script")) javascript = true;
            if (line.Contains("</script")) javascript = false;

            if (!javascript && !line.Contains(".split") && line.Length > 7)
            {
                var lineStr = line.ToString();
                var updated = Regex.Replace(lineStr, @"\bsplit\s?\('(\\'|[^'])*'", evaluator, _options, _timeout);
                updated = Regex.Replace(updated, @"\bsplit\s?\(""(\\""|[^""])*""", evaluator, _options, _timeout);
                updated = Regex.Replace(updated,
                                        @"\bsplit\s?\(\s?(?<del>[^""'].*?),",
                                        new MatchEvaluator(SplitWithVarDelimiter),
                                        _options,
                                        _timeout);

                line.Replace(lineStr, updated);
            }
        }
        lines.JoinInto(file.Content);

        if (!file.Path.EndsWith(Path.Join("facebook", "src", "Facebook", "SignedRequest.php"), StringComparison.Ordinal)
            && !file.Path.EndsWith(Path.Join("funkce", "qrkod", "qrsplit.php"), StringComparison.Ordinal)
            && !file.Path.EndsWith(Path.Join("funkce", "qrkod", "phpqrcode.php"), StringComparison.Ordinal)
            && Regex.IsMatch(file.Content.ToString(), @"(?<!(_|\.))split\s?\(", _options, _timeout))
        {
            file.Warnings.Add("Nemodifikovaná funkce split!");
        }
    }

    private const char _delimiter = '~';

    private static string PregMatchEvaluator(Match match)
    {
        var bracketIndex = match.ValueSpan.IndexOf('(');
        var oldFunc = match.ValueSpan[..bracketIndex].TrimEnd();

        var pregFunction = oldFunc switch
        {
            var x when x.SequenceEqual("ereg_replace") => "preg_replace",
            var x when x.SequenceEqual("split") => "preg_split",
            _ => "preg_match"
        };
        char quote = match.ValueSpan[++bracketIndex];
        char? ignoreFlag = oldFunc.SequenceEqual("eregi") ? 'i' : null;

        var evaluator = new MatchEvaluator(PatternDelimiterEscapeEvaluator);
        var pattern = Regex.Replace(match.Value[++bracketIndex..^1],
                                    $@"(^{_delimiter})|([^\\]{_delimiter})",
                                    evaluator,
                                    RegexOptions.None,
                                    _timeout);

        return $"{pregFunction}({quote}{_delimiter}{pattern}{_delimiter}{ignoreFlag}{quote}";
    }

    private static string PatternDelimiterEscapeEvaluator(Match match)
    {
        var index = match.Value.StartsWith(_delimiter) ? 0 : 1;
        return match.Value.Insert(index, @"\");
    }

    private static string SplitWithVarDelimiter(Match match)
    {
        var delimiterVar = match.Groups["del"];
        return $"preg_split('{_delimiter}'.{delimiterVar}.'{_delimiter}',";
    }
}

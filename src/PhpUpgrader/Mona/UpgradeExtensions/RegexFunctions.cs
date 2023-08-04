namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class RegexFunctions
{
    private static readonly string _signedRequestPhp = Path.Join("facebook", "src", "Facebook", "SignedRequest.php");
    private static readonly string _qrSplitPhp = Path.Join("funkce", "qrkod", "qrsplit.php");
    private static readonly string _phpQrCodePhp = Path.Join("funkce", "qrkod", "phpqrcode.php");

    /// <summary>
    /// - funkci ereg nebo ereg_replace doplnit do prvního parametru delimetr na začátek a nakonec (if(ereg('.+@.+..+', $retezec))
    /// // puvodni, jiz nefunkcni >>> if(preg_match('#.+@.+..+#', $retezec)) // upravene - delimiter zvolen #)
    /// </summary>
    public static FileWrapper UpgradeRegexFunctions(this FileWrapper file)
    {
        UpgradeEreg(file);
        UpgradeSplit(file);
        return file;
    }

    private static void UpgradeEreg(FileWrapper file)
    {
        if (!file.Content.Contains("ereg"))
            return;

        var updated = EregSingleQuoteStrRegex().Replace(file.Content.ToString(), _pregMatchEvaluator);
        updated = EregDoubleQuoteStrRegex().Replace(updated, _pregMatchEvaluator);

        updated = EregiVarRegex().Replace(updated, "preg_match($");
        updated = EregReplaceVarRegex().Replace(updated, "preg_replace($");

        if (UncommentedEregRegex().IsMatch(updated))
        {
            file.Warnings.Add("Nemodifikovaná funkce ereg!");
        }
        file.Content.Replace(updated);
    }

    private static void UpgradeSplit(FileWrapper file)
    {
        if (!file.Content.Contains("split"))
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
                var updated = SplitSingleQuoteStrRegex().Replace(lineStr, _pregMatchEvaluator);
                updated = SplitDoubleQuoteStrRegex().Replace(updated, _pregMatchEvaluator);
                updated = SplitVarRegex().Replace(updated, _splitWithVarDelimiterEvaluator);

                line.Replace(updated);
            }
        }
        lines.JoinInto(file.Content);

        if (!file.Path.EndsWith(_signedRequestPhp, StringComparison.Ordinal)
            && !file.Path.EndsWith(_qrSplitPhp, StringComparison.Ordinal)
            && !file.Path.EndsWith(_phpQrCodePhp, StringComparison.Ordinal)
            && UnmodifiedSplitRegex().IsMatch(file.Content.ToString()))
        {
            file.Warnings.Add("Nemodifikovaná funkce split!");
        }
    }

    private const char _delimiter = '~';

    private static readonly MatchEvaluator _pregMatchEvaluator = new(match =>
    {
        var bracketIndex = match.ValueSpan.IndexOf('(');
        var oldFunc = match.ValueSpan[..bracketIndex].TrimEnd();

        var pregFunction = oldFunc switch
        {
            var x when x is "ereg_replace" => "preg_replace",
            var x when x is "split" => "preg_split",
            _ => "preg_match"
        };
        var quote = match.ValueSpan[++bracketIndex];
        char? ignoreFlag = oldFunc is "eregi" ? 'i' : null;
        var delimiter = match.Value.Contains("$atom", StringComparison.Ordinal) ? ';' : _delimiter;

        var pattern = Regex.Replace(match.Value[++bracketIndex..^1],
                                    $@"(^{delimiter})|([^\\]{delimiter})",
                                    _PatternDelimiterEscapeEvaluator,
                                    RegexOptions.ExplicitCapture,
                                    TimeSpan.FromSeconds(4));

        return $"{pregFunction}({quote}{delimiter}{pattern}{delimiter}{ignoreFlag}{quote}";

        string _PatternDelimiterEscapeEvaluator(Match match)
        {
            var index = match.Value.StartsWith(delimiter) ? 0 : 1;
            return match.Value.Insert(index, @"\");
        }
    });

    private static readonly MatchEvaluator _splitWithVarDelimiterEvaluator = new(match =>
    {
        var delimiterVar = match.Groups["del"];
        return $"preg_split('{_delimiter}'.{delimiterVar}.'{_delimiter}',";
    });

    [GeneratedRegex(@"ereg(_replace|i)?\s?\('(\\'|[^'])*'", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex EregSingleQuoteStrRegex();
    
    [GeneratedRegex(@"ereg(_replace|i)?\s?\(""(\\""|[^""])*""", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex EregDoubleQuoteStrRegex();
    
    [GeneratedRegex(@"eregi?\s?\( ?\$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex EregiVarRegex();
    
    [GeneratedRegex(@"ereg_replace\s?\(\s?\$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex EregReplaceVarRegex();
    
    [GeneratedRegex(@"\n[^//]{0,236}(?<!mb_)ereg[^(;"",]*\(", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex UncommentedEregRegex();
    
    [GeneratedRegex(@"\bsplit\s?\('(\\'|[^'])*'", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex SplitSingleQuoteStrRegex();
    
    [GeneratedRegex(@"\bsplit\s?\(""(\\""|[^""])*""", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex SplitDoubleQuoteStrRegex();
    
    [GeneratedRegex(@"\bsplit\s?\(\s?(?<del>[^""'].*?),", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex SplitVarRegex();
    
    [GeneratedRegex(@"(?<!(_|\.))split\s?\(", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex UnmodifiedSplitRegex();
}

namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class UpgradeRegexFunctionsRoutine
{
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

        var updated = Regex.Replace(content, @"ereg(_replace|i)? ?\('(\\'|[^'])*'", evaluator, RegexOptions.Compiled);
        updated = Regex.Replace(updated, @"ereg(_replace|i)? ?\(""(\\""|[^""])*""", evaluator, RegexOptions.Compiled);

        updated = Regex.Replace(updated, @"eregi? ?\( ?\$", "preg_match($", RegexOptions.Compiled);
        updated = Regex.Replace(updated, @"ereg_replace ?\( ?\$", "preg_replace($", RegexOptions.Compiled);

        if (updated.Contains("ereg"))
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
                var updated = Regex.Replace(lineStr, @"\bsplit ?\('(\\'|[^'])*'", evaluator, RegexOptions.Compiled);
                updated = Regex.Replace(updated, @"\bsplit ?\(""(\\""|[^""])*""", evaluator, RegexOptions.Compiled);

                line.Replace(lineStr, updated);
            }
        }
        lines.JoinInto(file.Content);

        if (!file.Path.EndsWith(Path.Join("facebook", "src", "Facebook", "SignedRequest.php"))
            && !file.Path.EndsWith(Path.Join("funkce", "qrkod", "qrsplit.php"))
            && !file.Path.EndsWith(Path.Join("funkce", "qrkod", "phpqrcode.php"))
            && Regex.IsMatch(file.Content.ToString(), @"[^_\.]split ?\(", RegexOptions.Compiled))
        {
            file.Warnings.Add("Nemodifikovaná funkce split!");
        }
    }

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
        var quote = match.ValueSpan[++bracketIndex];
        var insidePattern = match.ValueSpan[++bracketIndex..^1];
        char? ignoreMode = oldFunc.SequenceEqual("eregi") ? 'i' : null;

        return $"{pregFunction}({quote}~{insidePattern}~{ignoreMode}{quote}";
    }
}

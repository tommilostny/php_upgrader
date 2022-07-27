namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// - funkci ereg nebo ereg_replace doplnit do prvního parametru delimetr na začátek a nakonec (if(ereg('.+@.+..+', $retezec))
    /// // puvodni, jiz nefunkcni >>> if(preg_match('#.+@.+..+#', $retezec)) // upravene - delimiter zvolen #)
    /// </summary>
    public static void UpgradeRegexFunctions(FileWrapper file)
    {
        var evaluator = new MatchEvaluator(_PregMatchEvaluator);
        _UpgradeEreg();
        _UpgradeSplit();

        void _UpgradeEreg()
        {
            if (!file.Content.Contains("ereg"))
                return;

            var content = file.Content.ToString();

            var updated = Regex.Replace(content, @"ereg(_replace)? ?\('(\\'|[^'])*'", evaluator, RegexOptions.Compiled);
            updated = Regex.Replace(updated, @"ereg(_replace)? ?\(""(\\""|[^""])*""", evaluator, RegexOptions.Compiled);

            updated = Regex.Replace(updated, @"ereg ?\( ?\$", "preg_match($", RegexOptions.Compiled);
            updated = Regex.Replace(updated, @"ereg_replace ?\( ?\$", "preg_replace($", RegexOptions.Compiled);

            if (updated.Contains("ereg"))
            {
                file.Warnings.Add("Nemodifikovaná funkce ereg!");
            }
            file.Content.Replace(content, updated);
        }

        void _UpgradeSplit()
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

        static string _PregMatchEvaluator(Match match)
        {
            var bracketIndex = match.ValueSpan.IndexOf('(');

            var pregFunction = match.ValueSpan[..bracketIndex].TrimEnd() switch
            {
                var x when x.SequenceEqual("ereg_replace") => "preg_replace",
                var x when x.SequenceEqual("split") => "preg_split",
                _ => "preg_match"
            };
            var quote = match.ValueSpan[++bracketIndex];
            var insidePattern = match.ValueSpan[++bracketIndex..(match.ValueSpan.Length - 1)];

            return $"{pregFunction}({quote}~{insidePattern}~{quote}";
        }
    }
}

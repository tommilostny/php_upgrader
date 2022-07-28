namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeGetMagicQuotesGpcRoutine
{
    /// <summary>
    /// Function get_magic_quotes_gpc() is deprecated and won't be supported in future versions of PHP.
    /// </summary>
    public static void UpgradeGetMagicQuotesGpc(this FileWrapper file)
    {
        Lazy<string> contentStr = new(() => file.Content.ToString());
        if (!file.Content.Contains("get_magic_quotes_gpc()") || Is_GMQG_Commented(contentStr.Value))
        {
            return;
        }

        //Zpracování výrazu s ternárním operátorem.
        var evaluator = new MatchEvaluator(GetMagicQuotesGpcTernaryEvaluator);
        var updated = Regex.Replace(contentStr.Value,
                                    @"\(?!?get_magic_quotes_gpc\(\)\)?\s{0,5}\?\s{0,5}(/\*.*\*/)?\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))\s{0,5}:\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))",
                                    evaluator,
                                    RegexOptions.Compiled);
        //Pokud výraz s get_magic_quotes_gpc nebyl aktualizován, jedná se pravděpodobně o variantu s if else.
        if (!Is_GMQG_Commented(updated))
        {
            evaluator = new MatchEvaluator(GetMagicQuotesGpcIfElseEvaluator);
            updated = Regex.Replace(contentStr.Value,
                                    @"if\s?\(\s?get_magic_quotes_gpc\(\)\s?\)(\n|.){0,236}else(\n|.){0,236};",
                                    evaluator);

            if (!Is_GMQG_Commented(updated))
            {
                file.Warnings.Add("Nezakomentovaná funkce get_magic_quotes_gpc().");
                return;
            }
        }
        file.Content.Replace(contentStr.Value, updated);
    }

    private static bool Is_GMQG_Commented(string str)
    {
        return Regex.IsMatch(str, @"/\*.{0,6}get_magic_quotes_gpc\(\)(\n|.){0,236}\*/", RegexOptions.Compiled);
    }

    private static string GetMagicQuotesGpcTernaryEvaluator(Match match)
    {
        var colonIndex = match.ValueSpan.LastIndexOf(':') + 1;
        var afterColon = match.ValueSpan[colonIndex..];

        //negovaný výraz, vybrat true část mezi '?' a ':'.
        if (match.ValueSpan.Contains("!get_", StringComparison.Ordinal))
        {
            var qmarkIndex = match.ValueSpan.IndexOf('?') + 1;
            var beforeQMark = match.ValueSpan[..qmarkIndex];
            var afterQMark = match.ValueSpan[qmarkIndex..(colonIndex - 1)];
            return $"/*{beforeQMark}*/ {afterQMark} /*:{afterColon}*/";
        }

        //běžný podmíněný výraz, vybrat false část za ':'.
        var beforeColon = match.ValueSpan.Contains("*/", StringComparison.Ordinal)
            ? match.Value[..colonIndex].Replace("*/", "*//*") //volat string replace jen pokud je opravdu potřeba
            : match.ValueSpan[..colonIndex];

        return $"/*{beforeColon}*/{afterColon}";
    }

    private static string GetMagicQuotesGpcIfElseEvaluator(Match match)
    {
        //zakomentovat if else s get_magic_quotes_gpc a ponechat pouze else část.
        var elseIndex = match.ValueSpan.IndexOf("else") + 4;
        var beforeElse = match.ValueSpan[..elseIndex];
        var afterElse = match.ValueSpan[elseIndex..];

        return $"/*{beforeElse}*/{afterElse}";
    }
}

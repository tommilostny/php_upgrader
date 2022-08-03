﻿namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class GetMagicQuotesGpc
{
    /// <summary>
    /// Function get_magic_quotes_gpc() is deprecated and won't be supported in future versions of PHP.
    /// </summary>
    public static FileWrapper UpgradeGetMagicQuotesGpc(this FileWrapper file)
    {
        Lazy<string> contentStr = new(() => file.Content.ToString());
        switch (file.Path)
        {
            case var p when p.EndsWith(Path.Join("piwika", "libs", "HTML", "QuickForm2.php"), StringComparison.Ordinal):
                file.Content.Replace("$method, get_magic_quotes_gpc()", "$method /*, get_magic_quotes_gpc()*/");
                break;

            case var p when p.EndsWith(Path.Join("piwika", "core", "Common.php"), StringComparison.Ordinal):
                file.Content.Replace("&& get_magic_quotes_gpc()", "&& /*get_magic_quotes_gpc()*/ false");
                break;
        }
        if (!file.Content.Contains("get_magic_quotes_gpc()") || Is_GMQG_Commented(contentStr.Value))
        {
            return file;
        }
        //Zpracování výrazu s ternárním operátorem.
        var evaluator = new MatchEvaluator(GetMagicQuotesGpcTernaryEvaluator);
        var updated = Regex.Replace(contentStr.Value,
                                    @"\(?!?get_magic_quotes_gpc\(\)\)?\s{0,5}\?\s{0,5}(/\*.*\*/)?\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))\s{0,5}:\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))",
                                    evaluator,
                                    RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                                    TimeSpan.FromSeconds(5));
        //Pokud výraz s get_magic_quotes_gpc nebyl aktualizován, jedná se pravděpodobně o variantu s if else.
        if (!Is_GMQG_Commented(updated))
        {
            evaluator = new MatchEvaluator(GetMagicQuotesGpcIfElseEvaluator);
            updated = Regex.Replace(contentStr.Value,
                                    @"if\s?\(\s?get_magic_quotes_gpc\(\)\s?\)(\n|.){0,236}else(\n|.){0,236};",
                                    evaluator,
                                    RegexOptions.ExplicitCapture,
                                    TimeSpan.FromSeconds(5));

            if (!Is_GMQG_Commented(updated))
            {
                file.Warnings.Add("Nezakomentovaná funkce get_magic_quotes_gpc().");
                return file;
            }
        }
        file.Content.Replace(contentStr.Value, updated);
        return file;
    }

    private static bool Is_GMQG_Commented(string str)
    {
        return Regex.IsMatch(str,
                             @"/\*.{0,6}get_magic_quotes_gpc\(\)(\n|.){0,236}\*/",
                             RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                             TimeSpan.FromSeconds(5));
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
            ? match.Value[..colonIndex].Replace("*/", "*//*", StringComparison.Ordinal) //volat string replace jen pokud je opravdu potřeba
            : match.ValueSpan[..colonIndex];

        return $"/*{beforeColon}*/{afterColon}";
    }

    private static string GetMagicQuotesGpcIfElseEvaluator(Match match)
    {
        //zakomentovat if else s get_magic_quotes_gpc a ponechat pouze else část.
        var elseIndex = match.ValueSpan.IndexOf("else", StringComparison.Ordinal) + 4;
        var beforeElse = match.ValueSpan[..elseIndex];
        var afterElse = match.ValueSpan[elseIndex..];

        return $"/*{beforeElse}*/{afterElse}";
    }
}

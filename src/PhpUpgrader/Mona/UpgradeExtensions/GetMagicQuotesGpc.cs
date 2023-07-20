using System.Globalization;

namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class GetMagicQuotesGpc
{
    private static readonly string _quickform2Php = Path.Join("piwika", "libs", "HTML", "QuickForm2.php");
    private static readonly string _coreCommonPhp = Path.Join("piwika", "core", "Common.php");

    /// <summary>
    /// Function get_magic_quotes_gpc() is deprecated and won't be supported in future versions of PHP.
    /// </summary>
    public static FileWrapper UpgradeGetMagicQuotesGpc(this FileWrapper file)
    {
        Lazy<string> contentStr = new(file.Content.ToString);
        switch (file.Path)
        {
            case var p when p.EndsWith(_quickform2Php, StringComparison.Ordinal):
                file.Content.Replace("$method, get_magic_quotes_gpc()", "$method /*, get_magic_quotes_gpc()*/");
                break;

            case var p when p.EndsWith(_coreCommonPhp, StringComparison.Ordinal):
                file.Content.Replace("&& get_magic_quotes_gpc()", "&& /*get_magic_quotes_gpc()*/ false");
                break;
        }
        if (!file.Content.Contains("get_magic_quotes_gpc()") && !file.Content.Contains("get_magic_quotes_runtime")
            || Is_GMQG_Commented(contentStr.Value))
        {
            return file;
        }
        //Zpracování výrazu s ternárním operátorem.
        var updated = GetMagicQuotesTernaryRegex().Replace(contentStr.Value, _getMagicQuotesGpcTernaryEvaluator);
        //Pokud výraz s get_magic_quotes_gpc nebyl aktualizován, jedná se pravděpodobně o variantu v if.
        if (!Is_GMQG_Commented(updated))
        {
            //nahradit podmínku v if za if (false) nebo if (true), podle pravdivostní hodnoty volání get_magic_quotes_gpc.
            updated = GetMagicQuotesIfRegex().Replace(updated, _getMagicQuotesGpcIfElseEvaluator);
            if (!Is_GMQG_Commented(updated))
            {
                file.Warnings.Add("Nezakomentovaná funkce get_magic_quotes_...");
                return file;
            }
        }
        file.Content.Replace(updated);
        return file;
    }

    private static bool Is_GMQG_Commented(string str)
    {
        return GetMagicQuotesCommentedRegex().IsMatch(str);
    }

    private static readonly MatchEvaluator _getMagicQuotesGpcTernaryEvaluator = new(match =>
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
    });

    private static readonly MatchEvaluator _getMagicQuotesGpcIfElseEvaluator = new(match =>
    {
        var negation = match.Groups["neg"];
        var booleanValue = negation.Success.ToString().ToLower(CultureInfo.InvariantCulture);

        return $"if ({booleanValue} /*{negation}{match.Groups["fn"]}()*/)";
    });

    [GeneratedRegex(@"\(?!?get_magic_quotes_(gpc|runtime)\(\)\)?\s{0,5}\?\s{0,5}(/\*.*\*/)?\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))\s{0,5}:\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex GetMagicQuotesTernaryRegex();

    [GeneratedRegex(@"if\s?\(\s?(?<neg>!)?\s?(?<fn>get_magic_quotes_(gpc|runtime))\(\)\s?\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex GetMagicQuotesIfRegex();

    [GeneratedRegex(@"/\*.{0,6}get_magic_quotes_(gpc|runtime)\(\)(\n|.){0,236}\*/", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex GetMagicQuotesCommentedRegex();
}

namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class RequiredParameterFollowsOptional
{
    /// <summary>
    /// PHPStan: Deprecated in PHP 8.0: Required parameter ${...} follows optional parameter $domain.
    /// </summary>
    public static FileWrapper UpgradeRequiredParameterFollowsOptional(this FileWrapper file)
    {
        if (file.Content.Contains("function"))
        {
            var evaluator = new MatchEvaluator(StartOptionalParamsToRequired);
            var content = file.Content.ToString();
            var updated = FunctionWithParametersRegex().Replace(content, evaluator);
            file.Content.Replace(content, updated);
        }
        return file;
    }

    private static string StartOptionalParamsToRequired(Match match)
    {
        IEnumerable<Match> parameters = OptionalParametersRegex().Matches(match.Value);
        var updatedParameters = new Stack<string>();
        byte state = 0;
        //Procházíme parametry v opačném pořadí (volitelné parametry z konce přeskočit, jelikož je to povolené chování),
        //dokud nenarazíme na první povinný parametr (nemá výchozí hodnotu) => přepnutí stavu z false na true.
        //Poté zakomentovat výchozí hodnotu všech "nepovinných" parametrů (nelze je nezadat, takže jsou de facto povinné).
        //Aktualizované parametry ukládáme do zásobníku (LIFO), aby byly zapsány ve správném pořadí.
        foreach (var paramMatch in parameters.Reverse())
        {
            var defaultValue = paramMatch.Groups["defval"];
            var paramValue = paramMatch.Value;
            //Stav 0: Hledá se první povinný parametr, nyní ponechat volitelné výchozí hodnoty.
            if (state == 0 && !defaultValue.Success)
            {
                state = 1;
            }
            //Stav 1,2: Nalezen povinný parametr, následující volitelné budou komentované.
            else if (state is 1 or 2 && defaultValue.Success)
            {
                var paramName = paramValue.AsSpan(0, defaultValue.Index - paramMatch.Index).TrimEnd();
                var separator = paramValue.Last(); //,)

                paramValue = $"{paramName}/*{defaultValue}*/{separator}";
                //Stav 2: Nalezen aspoň jeden "volitelný" parametr k zakomentování.
                state = 2;
            }
            //Odstranění zbytečných mezer (mohou se objevit, pokud byly parametry na více řádcích).
            paramValue = UnnecessarySpacesRegex().Replace(paramValue, " ");
            updatedParameters.Push(paramValue);
        }
        //Vrátit původní hodnotu, pokud se nedošlo do stavu 2 (nebyl vlastně nalezen žádný "volitelný" parametr).
        if (state != 2 || updatedParameters.Count == 0)
        {
            return match.Value;
        }
        //Sestavení výsledné hlavičky funkce.
        //Začátek až do první závorky + aktualizované parametry uložené v zásobníku.
        return BuildFunctionHeader(match, updatedParameters);
    }

    private static string BuildFunctionHeader(Match match, Stack<string> parameters)
    {
        var result = new StringBuilder().Append(match.ValueSpan[..(1 + match.Value.IndexOf('(', StringComparison.Ordinal))]);
        AppendParameters();
        return result.ToString();

        void AppendParameters()
        {
            var param = parameters.Pop();
            if (param.EndsWith(')') && char.IsWhiteSpace(param[^2]))
            {
                param = $"{param.AsSpan(0, param.Length - 2).TrimEnd()})";
            }
            result.Append(param);
            //Vrátit se a přidat další parametr, pokud ještě nějaké zbývají.
            if (parameters.Count > 0)
            {
                result.Append(' ');
                AppendParameters();
            }
        }
    }

    [GeneratedRegex(@"function(?!_)\s+?\w+\s*?\((?!\))((.|\n)(?!{|;))*?\$\w+\s?=\s?(.|\n)*?\)(?!,)(?!.*?\))", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 6666)]
    private static partial Regex FunctionWithParametersRegex();
    
    [GeneratedRegex(@"\s{2,}", RegexOptions.None, matchTimeoutMilliseconds: 6666)]
    private static partial Regex UnnecessarySpacesRegex();
    
    [GeneratedRegex(@"(\w+?\s+?)?&?\$\w+\s*?(?<defval>=\s*?(((?<strq>""|').*?\k<strq>)|(array\s?\(.*?\))|([^,'""(]*?)))?\s*?(,|\))", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 6666)]
    private static partial Regex OptionalParametersRegex();
}

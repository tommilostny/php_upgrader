namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class RequiredParameterFollowsOptional
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
            var updated = Regex.Replace(content,
                                        @"function(?!_)\s+?\w+\s*?\((?!\))((.|\n)(?!{|;))*?\$\w+\s?=\s?(.|\n)*?\)(?!,)(?!.*?\))",
                                        evaluator,
                                        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                                        TimeSpan.FromSeconds(4));
            file.Content.Replace(content, updated);
        }
        return file;
    }

    private static string StartOptionalParamsToRequired(Match match)
    {
        IEnumerable<Match> parameters = Regex.Matches(match.Value,
                                                      @"(\w+?\s+?)?&?\$\w+\s*?(?<defval>=\s*?(((?<strq>""|').*?\k<strq>)|(array\s?\(.*?\))|([^,'""(]*?)))?\s*?(,|\))",
                                                      RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase,
                                                      TimeSpan.FromSeconds(2));
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
            paramValue = Regex.Replace(paramValue, @"\s{2,}", " ", RegexOptions.None, TimeSpan.FromSeconds(2));
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
}

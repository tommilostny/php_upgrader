namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class CreateFunction
{
    public static FileWrapper UpgradeCreateFunction(this FileWrapper file)
    {
        if (file.Content.Contains("create_function"))
        {
            var content = file.Content.ToString();
            (var updated, _) = UpgradeCreateFunction(content);

            file.Content.Replace(content, updated);
            file.Warnings.Add("create_function!!!");
        }
        return file;
    }

    private static (string updated, string[] args) UpgradeCreateFunction(string content)
    {
        var args = Array.Empty<string>(); //parametry výsledné anonymní funkce.

        var evaluator = new MatchEvaluator(m => CreateFunctionToAnonymousFunction(m, content, out args));
        var updated = Regex.Replace(content,
                                    @"@?create_function\s?\(\s*'(?<args>.*)'\s?,\s*(?<quote>'|"")(?<code>(.|\n)*?(;|\}|\s))\k<quote>\s*\)",
                                    evaluator,
                                    RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                                    TimeSpan.FromSeconds(5));

        return (updated, args);
    }

    private static string CreateFunctionToAnonymousFunction(Match match, string content, out string[] args)
    {
        //bílé znaky od začátku aktuálního řádku, kde se volá create_function.
        var whitespace = LoadLineStartWhiteSpace(match, content);

        //načíst kód anonymní funkce.
        var code = match.Groups["code"].Value.Replace("\n", $"\n{whitespace}", StringComparison.Ordinal)
                                             .Replace($"\n{whitespace}{whitespace}", $"\n{whitespace}    ", StringComparison.Ordinal);

        //načíst argumenty a proměnné z rodičovského scope.
        LoadArgsAndParentVariables(match, code, out args, out var parentVars);

        //úprava escapovaných znaků v " stringu.
        code = UpgradeEscapedChars(match, code);

        //úprava ' string konkatenací.
        code = UpgradeConcats(code);

        //doplnění use (...), pokud jsou nějaké proměnné z rodičovského scope.
        var useStatement = CreateUseStatement(ref code, parentVars);

        return $"function ({string.Join(", ", args)}){useStatement} {{\n{whitespace}    {code}\n{whitespace}}}";
    }

    private static string CreateUseStatement(ref string code, HashSet<string> parentVars)
    {
        //zkontrolovat, jestli kód anonymní funkce také neobsahuje create_function (+ smazat z use její argumenty).
        if (code.Contains("create_function", StringComparison.Ordinal))
        {
            (code, var childArgs) = UpgradeCreateFunction(code.Replace("\\'", "'", StringComparison.Ordinal));
            foreach (var arg in childArgs)
            {
                parentVars.Remove(arg);
            }
        }
        //zkontrolovat, jestli kód obsahuje přiřazení do lokální proměnné (smazat z use, není rodičovská).
        foreach (var variable in parentVars)
        {
            if (code.Contains($"{variable} =", StringComparison.Ordinal) || code.Contains($"{variable}=", StringComparison.Ordinal))
            {
                parentVars.Remove(variable);
            }
        }
        var useStatement = parentVars.Count > 0 ? $" use ({string.Join(", ", parentVars)})" : null;
        return useStatement;
    }

    private static string LoadLineStartWhiteSpace(Match match, string content)
    {
        var whitespace = new StringBuilder();
        byte state = 0;
        for (var i = match.Index; state <= 1; i += state == 1 ? 1 : -1)
        {
            switch (state)
            {
                case 0 when content[i] == '\n' || i == 0:
                    state = 1;
                    break;
                case 1:
                    if (!char.IsWhiteSpace(content[i]))
                    {
                        state = 2; //konec
                        break;
                    }
                    whitespace.Append(content[i]);
                    break;
            }
        }
        return whitespace.ToString();
    }

    private static void LoadArgsAndParentVariables(Match match, string code, out string[] args, out HashSet<string> parentVars)
    {
        //načíst včechny argumenty
        args = match.Groups["args"].Value.Split(',');
        for (int i = 0; i < args.Length; i++)
        {
            args[i] = args[i].Trim();
        }
        //proměnné z rodičovského scope (které je potřeba uvést ve výrazu "use"),
        //nejsou to argumenty, ale jsou použity v těle anonymní funkce.
        parentVars = new(StringComparer.Ordinal);
        //projít všedchny proměnné v těle funkce a přidat je, pokud se nejedná o argument.
        IEnumerable<Match> allVars = Regex.Matches(code,
                                                   @"\$\w+",
                                                   RegexOptions.ExplicitCapture,
                                                   TimeSpan.FromSeconds(3));
        foreach (var variable in allVars)
        {
            if (args.All(arg => !arg.EndsWith(variable.Value, StringComparison.Ordinal)))
            {
                parentVars.Add(variable.Value);
            }
        }
        //obsahuje anonymní funkce klíčové slovo "global"?
        //globální proměnné nejsou v "use" výrazu uvedeny, smazat je z rodičovského scope.
        var globalsMatch = Regex.Match(code,
                                       @"global(?<vars>(\s?\$\w+,?)+);",
                                       RegexOptions.ExplicitCapture,
                                       TimeSpan.FromSeconds(2));
        if (globalsMatch.Success)
        {
            var globals = globalsMatch.Groups["vars"].Value.Split(',');
            foreach (var variable in globals)
            {
                parentVars.Remove(variable.Trim());
            }
        }
        //proměnné ve statických třídách také smazat z rodičovského scope,
        //jsou dostupné globáně class::$var.
        IEnumerable<Match> staticProperties = Regex.Matches(code,
                                                            @"\w+::\$\w+",
                                                            RegexOptions.ExplicitCapture,
                                                            TimeSpan.FromSeconds(2));
        foreach (var property in staticProperties)
        {
            var stored = parentVars.FirstOrDefault(pVar => property.Value.EndsWith(pVar, StringComparison.Ordinal));
            if (!string.IsNullOrEmpty(stored))
            {
                parentVars.Remove(stored);
            }
        }
    }

    private static string UpgradeEscapedChars(Match match, string code)
    {
        if (string.Equals(match.Groups["quote"].Value, "\"", StringComparison.Ordinal))
        {
            var evaluator = new MatchEvaluator(DoubleQuoteStringCodeEvaluator);
            code = Regex.Replace(code,
                                 @"(\\(\$|\\|""))|('\$.*?')",
                                 evaluator,
                                 RegexOptions.ExplicitCapture,
                                 TimeSpan.FromSeconds(3));
        }
        return code;
    }

    private static string UpgradeConcats(string code)
    {
        var inside = (Match m) => m.Groups["inside"].Value.Trim();

        var evaluator = new MatchEvaluator(m => _ConcatStringVariant1(m, code));
        code = Regex.Replace(code,
                             @"('\s?\.)(?<inside>.*?)(\.\s?')",
                             evaluator,
                             RegexOptions.ExplicitCapture,
                             TimeSpan.FromSeconds(3));

        evaluator = new MatchEvaluator(_ConcatStringVariant2);
        code = Regex.Replace(code,
                             @"(\.\s?')(?<inside>.*?)('\s?\.)",
                             evaluator,
                             RegexOptions.ExplicitCapture,
                             TimeSpan.FromSeconds(3));
        return code;

        // ' . . '
        string _ConcatStringVariant1(Match match, string content)
        {
            return content[match.Index - 1] == '"' ? $"\".{inside(match)}.\"" : $". {inside(match)} .";
        }
        // . ' ' .
        string _ConcatStringVariant2(Match match)
        {
            return $". \"{inside(match)}\" .";
        }
    }

    private static string DoubleQuoteStringCodeEvaluator(Match match)
    {
        if (match.Value.Length == 2) //escapovaný znak
        {
            return match.Value[1].ToString();
        }
        //proměnná, jejíž string reprezentace měla být v jednoduchých uvozovkách
        //'$var' >> "$var" zajistí, že její hodnota bude v tomto stringu.
        return match.Value.Replace('\'', '"');
    }
}

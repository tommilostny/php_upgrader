namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class WhileListEach
{
    /// <summary>
    /// Funkce <b>each</b> je zastaralá (v PHP 8 navíc odstraněna).
    /// <code>reset($polozky); while(list($key, $val) = each($polozky))</code> je nevalidní kód.
    /// </summary>
    public static FileWrapper UpgradeWhileListEach(this FileWrapper file, string? workingDirectory)
    {
        Match m;
        var content = file.Content.ToString();
        var initialContent = content;

        while ((m = ResetWhileListEachRegex().Match(content)).Success)
        {
            //nahradit while(list(...)=each(...)) >> foreach(...)
            var updated = WhileListEachToForeach(m, out var lookForEndWhile, out var arrayKeyvalAsIndexReplace, content, workingDirectory);
            var updatedSB = new StringBuilder(content).Replace(m.Value, updated, m.Index, m.Value.Length + 1);

            //byl while ve formátu "while(...):"? hledat příslušný endwhile; a nahradit endforeach;.
            EndWhileToEndForeach(updatedSB, lookForEndWhile, m.Index);

            //pokud není null (jednalo se o variantu foreach($array as $keyval),
            //nahradit přístup k poli přes $keyval jako index za $keyval.
            arrayKeyvalAsIndexReplace?.Upgrade(updatedSB);
            
            //před další iterací uložit aktuálně upravený obsah.
            content = updatedSB.ToString();
        }
        if (!string.Equals(content, initialContent, StringComparison.Ordinal))
        {
            file.Content.Replace(initialContent, content);
            file.Warnings.Add("Nahrazeno while(list(...)=each(...)) => foreach(...)");
        }
        return file;
    }

    private static string WhileListEachToForeach(Match match, out bool lookForEndWhile, out ArrayKeyValAsIndexReplace? arrayKeyVal, string content, string? baseDir)
    {
        var array = match.Groups["array2"].Value;
        var keyVal = match.Groups["keyval"];
        
        //match končí dvojtečkou => hledat endwhile a nahradit jej za endforeach.
        //jinak jsou použity složené závorky, které není třeba upravovat.
        char? colon = (lookForEndWhile = match.Value.EndsWith(':')) ? ':' : null;
        string? inBetween = null;

        //match obsahuje část s funkcí reset, načíst obsah mezi tím a cyklem while.
        if (match.Groups["reset"].Success)
        {
            inBetween = match.Groups["in_between"].Value;
            var arrayInReset = match.Groups["array1"].Value;

            inBetween = string.Equals(arrayInReset, array, StringComparison.Ordinal)
                ? inBetween.TrimStart()
                : $"reset({arrayInReset});{inBetween}";
        }

        //index "keyVal" je použit i na jiné proměnné než "array" (např. $SETY["$ids"] a $DATA["$ids"]).
        var allArrayAccesses = ArrayAccessRegex().Matches(content);
        var isUsedByOtherVarAsIndex = allArrayAccesses
            .Where(x => string.Equals(x.Groups["keyval"].Value, keyVal.Value, StringComparison.Ordinal))
            .All(x =>
            {
                var otherArray = x.Groups["array"].Value;

                return string.Equals(otherArray, array, StringComparison.Ordinal)
                    || Regex.IsMatch(content, @$"each\s?\(\s?\{otherArray}(\)|\sas)",
                                     RegexOptions.ExplicitCapture, matchTimeout: TimeSpan.FromSeconds(66));
            });

        //proměnná "keyVal" se používá samostatně (i někde jinde např. v SQL dotazu, nejen jako index do pole).
        //indikuje jestli se má nahradit $ARRAY["$keyval"] => $keyval (automaticky ne, pokud se používá jako index u jiné proměnné).
        var isUsed = (allArrayAccesses.Count > 0 && !isUsedByOtherVarAsIndex)
            || NotIndexVarRegex().Matches(content).Any(x => string.Equals(x.Value, keyVal.Value, StringComparison.Ordinal));

        switch ((keyVal.Success, isUsed))
        {
            //volání funkce list má jeden parametr, převést pouze na "as $keyvalue".
            case (true, false):
                arrayKeyVal = new ArrayKeyValAsIndexReplace(array, keyVal.Value);
                ReplaceKeyValInIncludedFiles(arrayKeyVal, content, baseDir);
                return $"{inBetween}foreach ({array} as {keyVal}){colon}";

            //jestli se proměnná používá i jinak než index, nahradit pouze s array_keys (pak jsou to indexy do pole).
            case (true, true):
                arrayKeyVal = null;
                return $"{inBetween}foreach (array_keys({array}) as {keyVal}){colon}";

            //volání funkce list má dva parametry, převést na "as $key => $value".
            default:
                arrayKeyVal = null;
                var key = match.Groups["key"];
                var value = match.Groups["val"];
                return $"{inBetween}foreach ({array} as {key} => {value}){colon}";
        }
    }

    private static void ReplaceKeyValInIncludedFiles(ArrayKeyValAsIndexReplace arrayKeyVal, string content, string? baseDir)
    {
        var templatesPath = Path.Join(baseDir, "templates");
        if (baseDir is null || !Directory.Exists(templatesPath))
            return;
        //najít všechny includes
        var includes = IncludeRegex().Matches(content).Select(x => x.Groups["file"].Value).ToArray();
        //TML_URL: složka "templates/{něco}" + soubor "/product/product_prehled_buy.php"
        foreach (var templateDir in Directory.EnumerateDirectories(templatesPath))
        {
            foreach (var includeFile in includes)
            {
                var path = Path.Join(templateDir, includeFile);
                if (File.Exists(path))
                {
                    var includeFileContent = new StringBuilder(File.ReadAllText(path));
                    arrayKeyVal.Upgrade(includeFileContent);
                    do try
                    {
                        File.WriteAllText(path, includeFileContent.ToString());
                        break;
                    }
                    catch (IOException)
                    {
                        Task.Delay(100).GetAwaiter().GetResult();
                    }
                    while (true);
                }
            }
        }
    }

    private static void EndWhileToEndForeach(StringBuilder builder, bool lookForEndWhile, int matchIndex)
    {
        if (!lookForEndWhile)
            return;

        const string endWhile = "endwhile;";
        const string endForeach = "endforeach;";

        var content = builder.ToString()[matchIndex..];

        var whileMatch = WhileRegex().Match(content);
        var nextWhileIndex = whileMatch.Index;
        var endWhileIndex = content.IndexOf(endWhile, StringComparison.Ordinal);

        if (!whileMatch.Success || endWhileIndex < nextWhileIndex)
        {
            builder.Replace(endWhile, endForeach, matchIndex + endWhileIndex, endWhile.Length);
            return;
        }
        EndWhileToEndForeach(builder, lookForEndWhile: true, matchIndex + endWhileIndex + endWhile.Length);
    }

    /// <summary>
    /// Nalezeno v /templates/.../product/top9.php:<br />
    /// Přístup k poli ve foreach jako $PRODUCT_TOP9["$idp"] nahradit za $idp.
    /// </summary>
    /// <remarks>
    /// Předchozí verze s while(list...each) měla $idp jako index do pole, nyní je to samotný záznam.
    /// </remarks>
    private record ArrayKeyValAsIndexReplace(string Array, string KeyVal)
    {
        public void Upgrade(StringBuilder builder)
        {
            builder.Replace($"{Array}[\"{KeyVal}\"]", KeyVal)
                .Replace($"{Array}[{KeyVal}]", KeyVal)
                .Replace($"reset({KeyVal});", string.Empty);
        }
    }

    [GeneratedRegex(@"(?<reset>reset\s?\((?<array1>\$[^)]+)\);(?<in_between>((.|\n)(?!reset\s?\())*?))?while\s?\(\s?list\s?\((((?<key>\$[^),]+)\s?,\s?(?<val>\$[^),]+))|(\s*?,\s*?)*?(?<keyval>\$[^)]+))\)\s?=\s?each\s?\((?<array2>\$[^)]+)\){2}(\s?:)?", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex ResetWhileListEachRegex();
    
    [GeneratedRegex(@"while\s?\(.+\)\s*:", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex WhileRegex();

    [GeneratedRegex(@"(?<!list\s?\(\s?,\s?|as\s|[""[]|(list|reset)\s?\(\s?)\$\w+(?![[""'\]\w])", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex NotIndexVarRegex();

    [GeneratedRegex(@"include TML_URL\s?\.\s?(""|')(?<file>.+?)(""|')", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex IncludeRegex();

    [GeneratedRegex(@"(?<array>\$\w+?)\[""(?<keyval>\$\w+?)""\]", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 666666)]
    private static partial Regex ArrayAccessRegex();
}

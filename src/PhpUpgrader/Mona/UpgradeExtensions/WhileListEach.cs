namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class WhileListEach
{
    /// <summary>
    /// Funkce <b>each</b> je zastaralá (v PHP 8 navíc odstraněna).
    /// <code>reset($polozky); while(list($key, $val) = each($polozky))</code> je nevalidní kód.
    /// </summary>
    public static FileWrapper UpgradeWhileListEach(this FileWrapper file)
    {
        Match m;
        var content = file.Content.ToString();
        var initialContent = content;

        while ((m = ResetWhileListEachRegex().Match(content)).Success)
        {
            //nahradit while(list(...)=each(...)) >> foreach(...)
            var updatedLine = WhileListEachToForeach(m, out var lookForEndWhile, out var arrayKeyvalAsIndexReplace, content);
            var updatedSB = new StringBuilder(content).Replace(m.Value, updatedLine, m.Index, m.Value.Length);

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

    private static string WhileListEachToForeach(Match match, out bool lookForEndWhile, out ArrayKeyValAsIndexReplace? arrayKeyVal, in string content)
    {
        //match končí dvojtečkou => hledat endwhile a nahradit jej za endforeach.
        //jinak jsou použity složené závorky, které není třeba upravovat.
        char? colon = (lookForEndWhile = match.Value.EndsWith(':')) ? ':' : null;

        var array = match.Groups["array2"].Value;
        string? inBetween = null;

        if (match.Groups["reset"].Success) //match obsahuje část s funkcí reset, načíst obsah mezi tím a cyklem while.
        {
            inBetween = match.Groups["in_between"].Value;
            var arrayInReset = match.Groups["array1"].Value;

            inBetween = string.Equals(arrayInReset, array, StringComparison.Ordinal)
                ? inBetween.TrimStart()
                : $"reset({arrayInReset});{inBetween}";
        }
        var keyVal = match.Groups["keyval"];
        var isUsed = NotIndexVarRegex().Matches(content).Any(x => string.Equals(x.Value.Trim(), keyVal.Value.Trim(), StringComparison.Ordinal));

        switch ((keyVal.Success, isUsed))
        {
            //volání funkce list má jeden parametr, převést pouze na "as $keyvalue".
            case (true, false):
                arrayKeyVal = new ArrayKeyValAsIndexReplace(array, keyVal.Value);
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

    [GeneratedRegex(@"(?<reset>reset\s?\((?<array1>\$[^)]+)\);(?<in_between>((.|\n)(?!reset\s?\())*?))?while\s?\(\s?list\s?\((((?<key>\$[^),]+)\s?,\s?(?<val>\$[^),]+))|(\s*?,\s*?)*?(?<keyval>\$[^)]+))\)\s?=\s?each\s?\((?<array2>\$[^)]+)\){2}(\s?:)?", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 6666)]
    private static partial Regex ResetWhileListEachRegex();
    
    [GeneratedRegex(@"while\s?\(.+\)\s*:", RegexOptions.None, matchTimeoutMilliseconds: 6666)]
    private static partial Regex WhileRegex();

    [GeneratedRegex(@"(?<!as\s|[""[]|(list|reset)\s?\(\s?)\$\w+(?![[""'\]\w])", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 6666)]
    private static partial Regex NotIndexVarRegex();
}

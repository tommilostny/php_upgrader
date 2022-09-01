namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class WhileListEach
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

        while ((m = NextMatch(content)).Success)
        {
            //nahradit while(list(...)=each(...)) >> foreach(...)
            var updatedLine = WhileListEachToForeach(m, out var lookForEndWhile);
            var updatedSB = new StringBuilder(content).Replace(m.Value, updatedLine, m.Index, m.Value.Length);

            //byl while ve formátu "while(...):"? hledat příslušný endwhile; a nahradit endforeach;.
            EndWhileToEndForeach(updatedSB, lookForEndWhile, m.Index);
            content = updatedSB.ToString();
        }
        file.Content.Replace(initialContent, content);
        return file;
    }

    private static Match NextMatch(string content)
    {
        return Regex.Match(content,
            @"(?<reset>reset\s?\((?<array1>\$[^)]+)\);(?<in_between>((.|\n)(?!reset\s?\())*?))?while\s?\(list\((((?<key>\$[^),]+)\s?,\s?(?<val>\$[^),]+))|(?<keyval>\$[^)]+))\)\s?=\s?each\s?\((?<array2>\$[^)]+)\){2}:?",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            TimeSpan.FromSeconds(5));
    }

    private static string WhileListEachToForeach(Match match, out bool lookForEndWhile)
    {
        //match končí dvojtečkou => hledat endwhile a nahradit jej za endforeach.
        //jinak jsou použity složené závorky, které není třeba upravovat.
        char? colon = (lookForEndWhile = match.Value.EndsWith(':')) ? ':' : null;

        var array = match.Groups["array2"].Value;
        string? inBetween = null;

        if (match.Groups["reset"].Success) //match obsahuje část s funckí reset, načíst obsah mezi tím a cyklem while.
        {
            inBetween = match.Groups["in_between"].Value;
            var arrayInReset = match.Groups["array1"].Value;

            inBetween = string.Equals(arrayInReset, array, StringComparison.Ordinal)
                ? inBetween.TrimStart()
                : $"reset({arrayInReset});{inBetween}";
        }
        var keyval = match.Groups["keyval"];
        if (keyval.Success)
        {
            //volání funkce list má jeden parametr, převést pouze na "as $keyvalue".
            return $"{inBetween}foreach ({array} as {keyval}){colon}";
        }
        //volání funkce list má dva parametry, převést na "as $key => $value".
        var key = match.Groups["key"];
        var value = match.Groups["val"];
        return $"{inBetween}foreach ({array} as {key} => {value}){colon}";
    }

    private static void EndWhileToEndForeach(StringBuilder builder, bool lookForEndWhile, int matchIndex)
    {
        if (lookForEndWhile)
        {
            const string endWhile = "endwhile;";
            const string endForeach = "endforeach;";

            var content = builder.ToString()[matchIndex..];

            var whileMatch = Regex.Match(content, @"while\s?\(.+\)\s*:", RegexOptions.None, TimeSpan.FromSeconds(5));
            var nextWhileIndex = whileMatch.Index;
            var endWhileIndex = content.IndexOf(endWhile, StringComparison.Ordinal);

            if (!whileMatch.Success || endWhileIndex < nextWhileIndex)
            {
                builder.Replace(endWhile, endForeach, matchIndex + endWhileIndex, endWhile.Length);
                return;
            }
            EndWhileToEndForeach(builder, true, matchIndex + endWhileIndex + endWhile.Length);
        }
    }
}

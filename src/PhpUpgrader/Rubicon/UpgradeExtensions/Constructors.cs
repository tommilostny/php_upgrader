namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class Constructors
{
    /// <summary> Old style constructor function ClassName() => function __construct() </summary>
    /// <remarks> Deprecated: Methods with the same name as their class will not be constructors in a future version of PHP; </remarks>
    public static FileWrapper UpgradeConstructors(this FileWrapper file)
    {
        if (!file.Content.Contains("class"))
        {
            return file;
        }
        var contentStr = file.Content.ToString();
        var contentAhead = contentStr;
        var initialContent = contentStr;
        //procházíme soubor po znacích.
        for (var i = 0; i < file.Content.Length; i++)
        {
            //nalézt další třídu v souboru a přesunout se na její index.
            var classMatch = Regex.Match(contentAhead, @"class\s.+\s*\{", RegexOptions.Multiline | RegexOptions.Compiled);
            if (!classMatch.Success) //skončit, pokud kód neobsahuje další třídu.
            {
                break;
            }
            i += classMatch.Index + classMatch.Value.Length;
            contentAhead = contentStr[i..];

            //jméno třídy, jejíž konstruktory hledáme a upravujeme.
            var className = LoadClassName(classMatch.ValueSpan).ToString();
            //parametry všech konstruktorů (jak nového __construct, který se neupravuje, tak {className})
            //a uloží jejich příznak upravenosti (funkce je __construct).
            var constructors = LoadContructorsParameters(contentAhead, className);
            //není nutné procházet třídu znovu, pokud jsou všechny její konstruktory již aktualizovány.
            if (constructors.All(x => x.Value))
            {
                continue;
            }
            //prochází třídu, dokud nenarazí na funkci, zde se zavolá následující akce,
            //která zkontroluje, zda se jedná o konstruktor a případně jej aktualizuje.
            GoThroughClass(contentStr, i, onFunctionFindAction: (j) =>
            {
                var lowerHalf = contentStr.AsSpan(0, j + 2);
                var higherHalf = contentStr[(j + 2)..];
                //jedná se o funkci {className}, aka starý konstruktor?
                var match = Regex.Match(higherHalf, $@"^{className}\s?\(.*\)\s");
                if (match.Success)
                {
                    //ano, jedná se o starý konstruktor. Pokud neexistuje jeho aktualizovaná varianta __construct, doplň.
                    var @params = LoadParameters(match.Value);
                    if (!constructors[ParametersKey(@params)])
                    {
                        var oldConstructor = $"{className}({@params})";
                        higherHalf = $"__construct{higherHalf.AsSpan(className.Length)}";

                        var compatibilityConstructorBuilder = new StringBuilder(oldConstructor)
                            .AppendLine()
                            .AppendLine("    {")
                            .AppendLine($"        self::__construct({ParametersWithoutDefaultValues(@params)});")
                            .AppendLine("    }")
                            .AppendLine();

                        contentStr = $"{lowerHalf}{compatibilityConstructorBuilder}    public function {higherHalf}";
                    }
                }
            });
            //uložit aktualizovaný kód třídy do "souboru" před přesunem na další.
            file.Content.Replace(initialContent, contentStr);
            initialContent = contentStr;
        }
        return file;
    }

    private static ReadOnlySpan<char> LoadClassName(ReadOnlySpan<char> classMatchVal)
    {
        var afterClassKW = classMatchVal[6..];
        for (var i = 0; i < afterClassKW.Length; i++)
        {
            var currentChar = afterClassKW[i];
            if (char.IsWhiteSpace(currentChar) || currentChar == '{')
            {
                return afterClassKW[..i];
            }
        }
        return afterClassKW;
    }

    private static IReadOnlyDictionary<string, bool> LoadContructorsParameters(string content, string className)
    {
        var result = new Dictionary<string, bool>();

        GoThroughClass(content, 0, onFunctionFindAction: (i) =>
        {
            var match = Regex.Match(content[(i + 2)..], $@"^(__construct|{className})\s?\(.*\)\s");
            if (match.Success)
            {
                var @params = LoadParameters(match.Value);
                var key = ParametersKey(@params);

                if (match.Value.StartsWith("__construct"))
                {
                    result[key] = true;
                    return;
                }
                result[key] = result.ContainsKey(key);
            }
        });
        return result;
    }

    private static string LoadParameters(string functionMatch)
    {
        var paramsStartIndex = functionMatch.IndexOf('(') + 1;
        var paramsEndIndex = functionMatch.LastIndexOf(')');
        return functionMatch[paramsStartIndex..paramsEndIndex];
    }

    private static string ParametersKey(string parameters)
    {
        return parameters.Replace(" ", string.Empty);
    }

    private static void GoThroughClass(string content, int startIndex, Action<int> onFunctionFindAction)
    {
        short scope = 1;
        byte functionCursor = 0;
        bool functionFlag = false, inBlockComment = false, inLineComment = false;

        for (var i = startIndex; i < content.Length; i++)
        {
            var currentChar = content[i];
            //přeskočit komentáře.
            if (inLineComment && currentChar == '\n')
            {
                inLineComment = false;
                continue;
            }
            if (i < content.Length - 2)
            {
                var twoCharSlice = $"{currentChar}{content[i + 1]}";
                if (CommentCheck(twoCharSlice, ref inBlockComment, ref i, "/*")
                    || CommentCheck(twoCharSlice, ref inBlockComment, ref i, "*/", matchValue: false)
                    || inBlockComment
                    || CommentCheck(twoCharSlice, ref inLineComment, ref i, "//", increment: false))
                {
                    continue;
                }
            }
            //hlídání zda jsme uvnitř třídy na scope minimálně 1.
            switch (currentChar)
            {
                case '{': scope++; break;
                case '}':
                    if (--scope == 0)
                        return;
                    break;
            }
            //jsme ve třídě mimo funkci, hledání řetězce "function".
            if (scope == 1 && !functionFlag)
            {
                functionFlag = InFunction(currentChar, ref functionCursor);
            }
            //nalezena funkce, načteme její jméno a zkontrolujeme, jestli se nejedná {className}.
            if (functionFlag)
            {
                onFunctionFindAction(i);
                functionFlag = false;
            }
        }
    }

    private static bool InFunction(char current, ref byte functionCursor)
    {
        //kurzor se posouvá po řetězci "function" dle aktuálního vstupního znaku.
        if (functionCursor < 8)
        {
            if (current != "function"[functionCursor++])
            {
                functionCursor = 0;
            }
            if (functionCursor != 8)
            {
                return false;
            }
        }
        functionCursor = 0;
        return true;
    }

    private static bool CommentCheck(ReadOnlySpan<char> twoCharSlice, ref bool inComment, ref int i, ReadOnlySpan<char> commentStartSequence, bool matchValue = true, bool increment = true)
    {
        if (twoCharSlice.SequenceEqual(commentStartSequence))
        {
            inComment = matchValue;
            if (increment)
            {
                i++;
            }
            return true;
        }
        return false;
    }

    private static string ParametersWithoutDefaultValues(ReadOnlySpan<char> parameters)
    {
        var sb = new StringBuilder().Append(parameters);
        var @params = sb.Split(',');
        for (var i = 0; i < @params.Count; i++)
        {
            var param = @params[i];
            var name = param.Split('=')[0].Replace(" ", string.Empty).Replace("&", string.Empty);
            param.Clear();
            param.Append(name);
        }
        @params.JoinInto(sb, ", ");
        return sb.ToString();
    }
}

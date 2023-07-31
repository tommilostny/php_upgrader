namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class ClassConstructors
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
            var classMatch = ClassRegex().Match(contentAhead);
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
                AddCompatibilityConstructor(ref contentStr, className, j, constructors);
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
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);

        GoThroughClass(content, 0, onFunctionFindAction: (i) =>
        {
            var match = Regex.Match(content[(i + 2)..],
                                    $@"^(__construct|{className})\s?\(.*\)\s",
                                    RegexOptions.None,
                                    TimeSpan.FromSeconds(4));
            if (match.Success)
            {
                var @params = LoadParameters(match.ValueSpan);
                Span<char> keySpan = stackalloc char[@params.Length];
                ParametersKey(@params, ref keySpan);

                var key = keySpan.ToString();

                if (match.Value.StartsWith("__construct", StringComparison.Ordinal))
                {
                    result[key] = true;
                    return;
                }
                result[key] = result.ContainsKey(key);
            }
        });
        return result;
    }

    private static ReadOnlySpan<char> LoadParameters(in ReadOnlySpan<char> functionMatch)
    {
        var paramsStartIndex = functionMatch.IndexOf('(') + 1;
        var paramsEndIndex = functionMatch.LastIndexOf(')');
        return functionMatch[paramsStartIndex..paramsEndIndex];
    }

    private static void ParametersKey(in ReadOnlySpan<char> parameters, ref Span<char> resultKeys)
    {
        //return parameters.Replace(" ", string.Empty, StringComparison.Ordinal);
        for (var (i, j) = (0, 0); i < parameters.Length; i++)
        {
            if (!char.IsWhiteSpace(parameters[i]))
            {
                resultKeys[j++] = parameters[i];
            }
        }
        var nullIndex = resultKeys.IndexOf('\0');
        if (nullIndex != -1)
        {
            resultKeys = resultKeys[..nullIndex];
        }
    }

    internal static void GoThroughClass(string content, int startIndex, Action<int> onFunctionFindAction)
    {
        ushort scope = 1;
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
                var twoCharSlice = content.AsSpan().Slice(i, 2);
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

    private static void AddCompatibilityConstructor(ref string contentStr, string className, int index, IReadOnlyDictionary<string, bool> constructors)
    {
        var lowerHalf = contentStr[..(index + 2)];
        var higherHalf = contentStr[(index + 2)..];
        //jedná se o funkci {className}, aka starý konstruktor?
        var match = Regex.Match(higherHalf, $@"^{className}\s?\(.*\)\s", RegexOptions.None, TimeSpan.FromSeconds(4));
        if (match.Success)
        {
            //ano, jedná se o starý konstruktor. Pokud neexistuje jeho aktualizovaná varianta __construct, doplň.
            var @params = LoadParameters(match.ValueSpan);
            Span<char> keySpan = stackalloc char[@params.Length];
            ParametersKey(@params, ref keySpan);

            string? key = null;
            foreach (var param in constructors.Keys)
            {
                if (keySpan.SequenceEqual(param.AsSpan()))
                {
                    key = param;
                    break;
                }
            }
            if (key is not null && !constructors[key])
            {
                var whitespace = PublicFunctionSpacesRegex().Match(lowerHalf).Groups["spaces"].ValueSpan;
                var higherHalfSpan = higherHalf.AsSpan(className.Length);

                contentStr = AddCompatibilityConstructor(lowerHalf, higherHalfSpan, className, @params, whitespace);
            }
        }
    }

    private static string AddCompatibilityConstructor(in ReadOnlySpan<char> lowerHalf, in ReadOnlySpan<char> higherHalf, in ReadOnlySpan<char> className, in ReadOnlySpan<char> @params, in ReadOnlySpan<char> whitespace)
    {
        using var ccb = ZString.CreateStringBuilder();
        ccb.Append(lowerHalf);
        ccb.Append(className);
        ccb.Append('(');
        ccb.Append(@params);
        ccb.Append(')');
        ccb.AppendLine();
        ccb.Append(whitespace);
        ccb.Append('{');
        ccb.AppendLine();
        ccb.Append(whitespace);
        ccb.Append(whitespace);

        ccb.Append("self::__construct(");
        var loadingVar = true;
        var lastCommaIndex = @params.LastIndexOf(',');
        for (var i = 0; i < @params.Length; i++)
        {
            var currentChar = @params[i];
            if (loadingVar && (!char.IsWhiteSpace(currentChar) || @params[i - 1] == ','))
            {
                if (currentChar == '=')
                {
                    if (lastCommaIndex != -1 && i <= lastCommaIndex)
                    {
                        ccb.Append(',');
                        ccb.Append(' ');
                    }
                    loadingVar = false;
                    continue;
                }
                if (currentChar != '&')
                {
                    ccb.Append(currentChar);
                }
            }
            if (!loadingVar && currentChar == '$')
            {
                loadingVar = true;
                i--;
            }
        }
        ccb.Append(");");
        ccb.AppendLine();
        ccb.Append(whitespace);
        ccb.Append('}');
        ccb.AppendLine();
        ccb.AppendLine();
        ccb.Append("    public function __construct");
        ccb.Append(higherHalf);
        return ccb.ToString();
    }

    [GeneratedRegex(@"class\s.+\s*\{", RegexOptions.Multiline, matchTimeoutMilliseconds: 66666)]
    private static partial Regex ClassRegex();
    
    [GeneratedRegex(@"(?<spaces>[ \t]*)(public\s?)?function\s$", RegexOptions.Multiline | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex PublicFunctionSpacesRegex();
}

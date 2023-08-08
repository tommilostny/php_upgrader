namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class UndefinedConstAccess
{
    public static FileWrapper UpgradeUndefinedConstAccess(this FileWrapper file)
    {
        var contentStr = file.Content.ToString();
        if (UndefinedConstAccessRegex().IsMatch(contentStr))
        {
            var updated = UndefinedConstAccessRegex().Replace(contentStr, _UndefinedConstAccessEvaluator);
            file.Content.Replace(
                InvalidUseInStringRegex().Replace(updated, _FixInvalidConstAccessInString)
            );
        }
        return file;

        string _UndefinedConstAccessEvaluator(Match match)
        {
            var notConst = match.Groups["notConst"].Value;
            if (contentStr.Contains($"define('{notConst}'", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }
            var var = match.Groups["var"].Value;
            return $"{var}['{notConst}']";
        }

        static string _FixInvalidConstAccessInString(Match stringMatch)
        {
            using var sb = ZString.CreateStringBuilder();
            var i = 0;
            foreach (var varMatch in UndefinedConstAccessRegex().Matches(stringMatch.Value).Cast<Match>())
            {
                sb.Append(stringMatch.ValueSpan[i..varMatch.Index]);
                sb.Append('"');
                sb.Append('.');
                sb.Append(varMatch.ValueSpan);
                sb.Append('.');
                sb.Append('"');
                i = varMatch.Index + varMatch.Length;
            }
            sb.Append(stringMatch.ValueSpan[i..]);
            return sb.ToString();
        }
    }

    [GeneratedRegex(@"(?<var>\$\w+?\S*?)\[(?<notConst>[a-z_]+?)\]", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex UndefinedConstAccessRegex();

    [GeneratedRegex(@"(?<=<\?(([pP][hH][pP])|=)((.|\n)(?!(\?>|\/(\/|\*(?!.*?\*\/)))))*?)""([^""]|(?<=(?<!\\)\\)"")*?(?<!{)\$\w+?\S*?\['[a-z_]+?'\](?!}).*?""", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex InvalidUseInStringRegex();

    ///// <summary>
    ///// $mail['telo'] = "Děkujeme za Vaši poptávku, <br /><br />formulář obsahuje tyto položky: <br />".$mail['vybava']."\n\n<br /> e-mail: ".$mail['email']."\n\n";
    ///// $mail['wadresa'] = "formular.set@vestavne-spotrebice.cz, spotrebice@mcrai.eu, $mail['email']";
    ///// $mail['predmet'] = "poptavka na set - vestavne-spotrebice.cz";
    ///// 
    ///// $mail['odesilatel']  = "formular.set@vestavne-spotrebice.cz";
    ///// 
    ///// $hlavicka = "From: $mail['odesilatel']\r\n";
    ///// $hlavicka.= "Reply-To: $mail['odesilatel']\r\n";
    ///// $hlavicka.="Content-Type: text/html; charset=utf-8\n";	
    ///// $hlavicka.= "X-Mailer: PHP";
    ///// 
    ///// mail("$mail['wadresa']", "$mail['predmet']", "$mail['telo']", "$hlavicka")
    ///// or die("error");
    ///// </summary>
    //private static void FixInvalidUseInString(FileWrapper file)
    //{
    //    var stringStartIndex = -1;  // "string"
    //    var inPhpBlock = false;     // <?php code ?>
    //    var inLineComment = false;  // /* comment */
    //    var inBlockComment = false; // // comment
    //    var inSimpleString = false; // 'string'
    //
    //    for (var i = 0; i < file.Content.Length; i++)
    //    {
    //        if (IsInPhp(file.Content, ref i, ref inPhpBlock) == ReturnState.Continue
    //            || stringStartIndex == -1 && IsInComment(file.Content, ref i, ref inLineComment, ref inBlockComment) == ReturnState.Continue
    //            || IsInSimpleString(file.Content, i, ref inSimpleString, ref stringStartIndex) == ReturnState.Continue)
    //        {
    //            continue;
    //        }
    //        var isQuote = file.Content[i] == '"';
    //        if (!isQuote || (isQuote && i > 0 && file.Content[i - 1] == '\\'))
    //        {
    //            continue;
    //        }
    //        if (isQuote)
    //        {
    //            if (stringStartIndex == -1 && i < file.Content.Length - 2 && file.Content[i + 1] != '.')
    //            {
    //                stringStartIndex = i + 1;
    //                continue;
    //            }
    //            i = _ReplaceInvalidUseInString(i);
    //            stringStartIndex = -1;
    //        }
    //    }
    //    int _ReplaceInvalidUseInString(in int stringEndIndex)
    //    {
    //        var count = stringEndIndex - stringStartIndex;
    //        Span<char> stringCharsSpan = stackalloc char[count];
    //        file.Content.CopyTo(stringStartIndex, stringCharsSpan, count);
    //        /*
    //        if (file.Path.EndsWith("step_03_old.php", StringComparison.Ordinal))
    //        {
    //            Console.WriteLine("===============================================");
    //            Console.WriteLine(stringCharsSpan.ToString());
    //            //Console.WriteLine(stringChars);
    //            //Console.WriteLine("-----------------------------------------------");
    //            //Console.WriteLine(updated);
    //            Console.WriteLine("===============================================");
    //            Console.ReadKey();
    //        }
    //        */
    //        if (stringCharsSpan.Contains('['))
    //        {
    //            var stringChars = stringCharsSpan.ToString();
    //            var updated = InvalidUseInStringRegex().Replace(stringChars, m => $"\".{m.Value}.\"");
    //
    //
    //            file.Content.Replace(stringChars, updated, stringStartIndex, updated.Length);
    //            return stringStartIndex + updated.Length;
    //        }
    //        return stringEndIndex;
    //    }
    //}
    //
    //private static ReturnState IsInPhp(StringBuilder content, ref int i, ref bool isPhp)
    //{
    //    if (!isPhp && i < content.Length - 5)
    //    {
    //        isPhp = content[i] == '<' && content[i + 1] == '?'
    //            && ((content[i + 2] == 'p' && content[i + 3] == 'h' && content[i + 4] == 'p')
    //                || (content[i + 2] == 'P' && content[i + 3] == 'H' && content[i + 4] == 'P'));
    //        if (isPhp)
    //        {
    //            i += 4;
    //            return ReturnState.Continue;
    //        }
    //    }
    //    if (isPhp && i < content.Length - 2)
    //    {
    //        isPhp = !(content[i] == '?' && content[i + 1] == '>');
    //        if (!isPhp)
    //        {
    //            i += 1;
    //            return ReturnState.Continue;
    //        }
    //    }
    //    return ReturnState.Ok;
    //}
    //
    //private static ReturnState IsInComment(StringBuilder content, ref int i, ref bool inLineComment, ref bool inBlockComment)
    //{
    //    if (!inLineComment && !inBlockComment && content[i] == '/' && i + 1 < content.Length)
    //    {
    //        if (content[i + 1] == '/')
    //        {
    //            inLineComment = true;
    //            return ReturnState.Continue;
    //        }
    //        if (content[i + 1] == '*')
    //        {
    //            inBlockComment = true;
    //            return ReturnState.Continue;
    //        }
    //    }
    //    if (inLineComment && content[i] == '\n')
    //    {
    //        inLineComment = false;
    //        return ReturnState.Continue;
    //    }
    //    if (inBlockComment && content[i] == '*' && i + 1 < content.Length && content[i + 1] == '/')
    //    {
    //        inBlockComment = false;
    //        i++;
    //        return ReturnState.Continue;
    //    }
    //    return inLineComment || inBlockComment ? ReturnState.Continue : ReturnState.Ok;
    //}
    //
    //private static ReturnState IsInSimpleString(StringBuilder content, int i, ref bool inSimpleString, ref int stringStartIndex)
    //{
    //    if (stringStartIndex != -1 && inSimpleString)
    //    {
    //        inSimpleString = false;
    //        return ReturnState.Ok;
    //    }
    //    if (content[i] == '\'')
    //    {
    //        if (inSimpleString && i > 0 && content[i - 1] == '\\')
    //        {
    //            return ReturnState.Continue;
    //        }
    //        inSimpleString = !inSimpleString;
    //        return ReturnState.Continue;
    //    }
    //    return inSimpleString ? ReturnState.Continue : ReturnState.Ok;
    //}
    //
    //[GeneratedRegex(@"(?<!{|\s?\.\s?|(<\?[pP][hH][pP](.(?!\?>))*?))\$\w+?\['.+?'\](?!}|\s?\.\s?)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    //private static partial Regex InvalidUseInStringRegex();
    //
    //private enum ReturnState
    //{
    //    Ok, Continue,
    //}
}

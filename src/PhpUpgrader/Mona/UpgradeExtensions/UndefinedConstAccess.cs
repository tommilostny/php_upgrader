namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class UndefinedConstAccess
{
    public static FileWrapper UpgradeUndefinedConstAccess(this FileWrapper file)
    {
        if (UndefinedConstAccessRegex().IsMatch(file.Content.ToString()))
        {
            file.Content.Replace(
                UndefinedConstAccessRegex()
                    .Replace(file.Content.ToString(), _UndefinedConstAccessEvaluator)
            );
            FixInvalidUseInString(file);
        }
        return file;

        string _UndefinedConstAccessEvaluator(Match match)
        {
            var notConst = match.Groups["notConst"].Value;
            if (file.Content.Contains($"define('{notConst}'", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }
            var var = match.Groups["var"].Value;
            return $"{var}['{notConst}']";
        }
    }

    [GeneratedRegex(@"(?<var>\$\w+?\S*?)\[(?<notConst>[a-z_]+?)\]", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex UndefinedConstAccessRegex();

    /// <summary>
    /// $mail['telo'] = "Děkujeme za Vaši poptávku, <br /><br />formulář obsahuje tyto položky: <br />".$mail['vybava']."\n\n<br /> e-mail: ".$mail['email']."\n\n";
    /// $mail['wadresa'] = "formular.set@vestavne-spotrebice.cz, spotrebice@mcrai.eu, $mail['email']";
    /// $mail['predmet'] = "poptavka na set - vestavne-spotrebice.cz";
    /// 
    /// $mail['odesilatel']  = "formular.set@vestavne-spotrebice.cz";
    /// 
    /// $hlavicka = "From: $mail['odesilatel']\r\n";
    /// $hlavicka.= "Reply-To: $mail['odesilatel']\r\n";
	/// $hlavicka.="Content-Type: text/html; charset=utf-8\n";	
    /// $hlavicka.= "X-Mailer: PHP";
    /// 
    /// mail("$mail['wadresa']", "$mail['predmet']", "$mail['telo']", "$hlavicka")
    /// or die("error");
    /// </summary>
    private static void FixInvalidUseInString(FileWrapper file)
    {
        var stringStartIndex = -1;
        for (var i = 0; i < file.Content.Length; i++)
        {
            if (file.Content[i] != '"')
            {
                continue;
            }
            if (stringStartIndex == -1)
            {
                stringStartIndex = i + 1;
                continue;
            }
            i = _ReplaceInvalidUseInString(i);
            stringStartIndex = -1;
        }
        int _ReplaceInvalidUseInString(in int stringEndIndex)
        {
            var count = stringEndIndex - stringStartIndex;
            Span<char> stringCharsSpan = stackalloc char[count];
            file.Content.CopyTo(stringStartIndex, stringCharsSpan, count);

            if (stringCharsSpan.Contains('['))
            {
                var stringChars = stringCharsSpan.ToString();
                var updated = InvalidUseInStringRegex().Replace(stringChars, m => $"\".{m.Value}.\"");
                file.Content.Replace(stringChars, updated, stringStartIndex, updated.Length);
                return stringStartIndex + updated.Length;
            }
            return stringEndIndex;
        }
    }

    [GeneratedRegex(@"(?<!{|\s?\.\s?|(<\?php(.(?!\?>))*?))\$\w+?\['.+?'\](?!}|\s?\.\s?)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex InvalidUseInStringRegex();
}

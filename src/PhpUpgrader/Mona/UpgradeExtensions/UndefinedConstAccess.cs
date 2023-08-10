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
                StringWithInvalidArrayAccessRegex().Replace(updated, _FixInvalidConstAccessInString)
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
            if (!stringMatch.Groups["str"].Success || PhpOpeningTagRegex().IsMatch(stringMatch.Value))
            {
                return stringMatch.Value;
            }
            using var sb = ZString.CreateStringBuilder();
            var i = 0;
            var invalidVarAccesses = InvalidArrayAccessInStringRegex()
                .Matches(stringMatch.Value)
                .Where(m => m.Groups["inv"].Success);

            foreach (var varMatch in invalidVarAccesses)
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

    [GeneratedRegex(@"(?<var>(?<!\\|\/\/.*?)\$\w+?[^\s.&@\]""']*?)\[(?<notConst>[a-z_][a-z_0-9]*?)\]", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex UndefinedConstAccessRegex();

    [GeneratedRegex(@"""\.\$\w+?[^\s.&@\]""']*?\['[a-z_][a-z_0-9]*?'\]\.""|(?<inv>\$\w+?[^\s.&@\]""']*?\['[a-z_][a-z_0-9]*?'\])", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex InvalidArrayAccessInStringRegex();

    [GeneratedRegex(@"\/(\/.*?\n|\*(.|\n)*?(\*\/|$))|(?<q>['""](?!<\?(([pP][hH][pP])|=)))(((?!\k<q>).|(?<=(?<!\\)\\)\k<q>)(?!\$?\w+?[^\s.&@\]""']*?\['[a-z_][a-z_0-9]*?'\]))*(?<!\\)\k<q>|(?<str>""(?!<\?(([pP][hH][pP])|=))([^""\n]|(?<=(?<!\\)\\)"")*(?<!{)\$\w+?[^\s.&@\]""']*?\['[a-z_][a-z_0-9]*?'\](?!})([^""\n]|(?<=(?<!\\)\\)"")*"")", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex StringWithInvalidArrayAccessRegex();

    [GeneratedRegex(@"<\?(([pP][hH][pP])|=)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 666666)]
    private static partial Regex PhpOpeningTagRegex();
}

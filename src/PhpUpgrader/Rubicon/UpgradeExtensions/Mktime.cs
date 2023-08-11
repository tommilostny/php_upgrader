namespace PhpUpgrader.Rubicon.UpgradeExtensions;

/// <summary>
/// Warning: mktime() expects parameter 5 to be int, string given in /var/www/vhosts/vestavne-spotrebice.cz/rubicon/modules/sety/sety_darky_zdarma.php on line 156
/// </summary>
public static partial class Mktime
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0011:IFormatProvider is missing", Justification = "<Pending>")]
    public static FileWrapper UpgradeMktime(this FileWrapper file)
    {
        if (file.Content.Contains("mktime"))
        {
            file.Content.Replace(MktimeCallRegex().Replace(file.Content.ToString(), _MktimeCallEvaluator));
        }
        return file;

        static string _MktimeCallEvaluator(Match match)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append("mktime(");
            for (byte i = 1; i < 7; i++)
            {
                var param = match.Groups[i].Value;
                var isInt = int.TryParse(param, out _);
                sb.Append(isInt ? param : $"intval({param})");
                if (i < 6)
                    sb.Append(", ");
            }
            sb.Append(");");
            return sb.ToString();
        }
    }

    [GeneratedRegex(@"mktime\s?\(\s?(.+?)\s?,\s?(.+?)\s?,\s?(.+?)\s?,\s?(.+?)\s?,\s?(.+?)\s?,\s?(.+?)\s?\);", RegexOptions.None, matchTimeoutMilliseconds: 666666)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "MA0023:Add RegexOptions.ExplicitCapture", Justification = "<Pending>")]
    private static partial Regex MktimeCallRegex();
}

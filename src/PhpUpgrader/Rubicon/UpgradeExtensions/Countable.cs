namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class Countable
{
    /// <summary>
    /// Warning: count(): Parameter must be an array or an object that implements Countable in /var/www/vhosts/nicom.cz/httpdocs/rubicon/modules/product/main.php on line 386
    /// </summary>
    public static FileWrapper UpgradeCountableWarning(this FileWrapper file)
    {
        file.Content.Replace(
            CountableIfRegex().Replace(file.Content.ToString(), _AddIfEmptyEvaluator)
        );
        return file;

        static string _AddIfEmptyEvaluator(Match match)
        {
            char? bang = match.Groups["op"].ValueSpan[0] switch
            {
                '>' => '!',
                _ => null
            };
            return $"if ({bang}empty({match.Groups["var"].ValueSpan})";
        }
    }

    [GeneratedRegex(@"\bif\s?\(\s?count\s?\(\s?(?<var>\$\w+?)\s?\)\s?(?<op>>|===?)\s?0", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 666666)]
    private static partial Regex CountableIfRegex();

}

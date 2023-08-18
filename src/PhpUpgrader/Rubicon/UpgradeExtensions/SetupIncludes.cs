namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class SetupIncludes
{
    /// <summary>
    /// ("include (\"setup.php\");\ninclude_once (\"../setup.php\");\ninclude_once (\"../../setup.php\");\ninclude_once (\"../../../setup.php\");",
    ///  "if (!(include \"setup.php\")):\n\tif (!(include \"../setup.php\")):\n\t\tif (!(include \"../../setup.php\")):\n\t\t\tif (!(include \"../../../setup.php\")):\n\t\t\t\techo \"CHYBA - Nenalezen soubor setup.php!\";//Error\n\t\t\t\texit;\n\t\t\tendif;\n\t\tendif;\n\tendif;\nendif;"
    /// ),
    /// </summary>
    public static FileWrapper UpgradeSetupIncludes(this FileWrapper file)
    {
        if (file.Content.Contains("include (\"setup.php\");"))
        {
            file.Content.Replace(IncludesRegex().Replace(file.Content.ToString(), _CascadeIfIncludesEvaluator));
        }
        return file;

        static string _CascadeIfIncludesEvaluator(Match match)
        {
            var count = 1;
            using var sb = ZString.CreateStringBuilder();
            do
            {
                sb.Append("../");
                if (!match.ValueSpan.Contains(sb.AsSpan(), StringComparison.Ordinal))
                {
                    break;
                }
                count++;
            }
            while (true);
            return CreateIncludesCascade(match.Groups["file"].ValueSpan, count);
        }
    }

    public static string CreateIncludesCascade(ReadOnlySpan<char> file, int levels)
    {
        using var sb = ZString.CreateStringBuilder();
        for (var i = 0; i < levels; i++)
        {
            for (var j = 0; j < i; j++)
            {
                sb.Append("\t");
            }
            sb.Append("if (!(include_once \"");
            for (var j = 0; j < i; j++)
            {
                sb.Append("../");
            }
            sb.Append(file);
            sb.AppendLine("\")):");
        }
        for (var j = 0; j < levels; j++)
        {
            sb.Append("\t");
        }
        sb.Append("echo \"CHYBA - Nenalezen soubor ");
        sb.Append(file);
        sb.AppendLine("!\";");
        for (var j = 0; j < levels; j++)
        {
            sb.Append("\t");
        }
        sb.Append("exit();");
        for (var i = 0; i < levels; i++)
        {
            sb.AppendLine();
            for (var j = 0; j < levels - i - 1; j++)
            {
                sb.Append("\t");
            }
            sb.Append("endif;");
        }
        return sb.ToString();
    }

    [GeneratedRegex(@"include(_once)?\s?\([""'](?<file>.+?)[""']\);(\s*?include(_once)?\s?\([""'](\.\.\/)+?\k<file>[""']\);)+", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex IncludesRegex();
}

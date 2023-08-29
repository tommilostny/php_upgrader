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

    public static string CreateIncludesCascade(in ReadOnlySpan<char> file, in int levels)
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append("// Include ");
        sb.Append(file);
        sb.Append(" recursively from up to ");
        sb.Append(levels - 1);
        sb.AppendLine(" levels up.");
        sb.Append("$_inc_file = '");
        sb.Append(file);
        sb.AppendLine("';");
        sb.Append("for ($i = 0; $i < ");
        sb.Append(levels);
        sb.AppendLine("; $i++) {");
        sb.AppendLine("\tif (file_exists($_inc_file)) {");
        sb.AppendLine("\t\tinclude_once $_inc_file;");
        sb.AppendLine("\t\tbreak;");
        sb.AppendLine("\t}");
        sb.AppendLine("\t$_inc_file = \"../$_inc_file\";");
        sb.AppendLine('}');
        sb.AppendLine("if (!file_exists($_inc_file)) {");
        sb.Append("\techo \"CHYBA - Nenalezen soubor ");
        sb.Append(file);
        sb.AppendLine("!\";");
        sb.AppendLine("\texit;");
        sb.Append('}');
        return sb.ToString();
    }

    [GeneratedRegex(@"include(_once)?\s?\([""'](?<file>.+?)[""']\);(\s*?include(_once)?\s?\([""'](\.\.\/)+?\k<file>[""']\);)+", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 55555)]
    private static partial Regex IncludesRegex();
}

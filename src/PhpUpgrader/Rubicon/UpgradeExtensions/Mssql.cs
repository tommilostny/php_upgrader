namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class Mssql
{
    private static string? _mssqlOverwritePhp = null;

    /// <summary>
    /// PhpStorm:
    /// 'mssql_query' was removed in 7.0 PHP version.<br />
    /// (Zatím neřešit, nepoužívá se (pouze nějaký ruční import)).
    /// </summary>
    /// <remarks>
    /// 1. Obsahuje mssql funkce?<br />
    /// 2. Nahoru doplnit require_once("mssql_overwrite.php"); se správnou cestou k souboru,
    ///    který bych dal do root složky (aka doplnit příslušný počet "../").<br />
    /// 3. Zkopírovat soubor mssql_overwrite.php ze složky important.<br />
    /// 4. Profit.
    /// </remarks>
    public static FileWrapper UpgradeMssql(this FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (file.Content.Contains("mssql_"))
        {
            AddMssqlOverwriteRequire(file, upgrader);
            EnsureMssqlOverwriteFileExists(upgrader);
            UpgradeMssqlPConnect(file);
            file.Content.Replace("//echo \"CHYBA - MSSQL nepripojeno!\";//Error", "echo \"CHYBA - MSSQL nepripojeno!\";//Error\n\tdie(print_r(sqlsrv_errors(), true));");
        }
        return file;
    }

    private static void AddMssqlOverwriteRequire(FileWrapper file, PhpUpgraderBase upgrader)
    {
        var folderScopeLevel = file.Path.Split(Path.DirectorySeparatorChar).Length;
        folderScopeLevel -= upgrader.WebFolder.Split(Path.DirectorySeparatorChar).Length;

        file.Content.Insert(file.Content.IndexOf("<?php") + 6,
            SetupIncludes.CreateIncludesCascade("mssql_overwrite.php", folderScopeLevel)
        );
    }

    private static void EnsureMssqlOverwriteFileExists(PhpUpgraderBase upgrader)
    {
        if (_mssqlOverwritePhp is null)
        {
            _mssqlOverwritePhp = Path.Join(upgrader.WebFolder, "mssql_overwrite.php");
            var sourceFilePath = Path.Join(upgrader.BaseFolder, "important", "mssql_overwrite.php");
            File.Copy(sourceFilePath, _mssqlOverwritePhp, overwrite: true);
        }
    }

    private static void UpgradeMssqlPConnect(FileWrapper file)
    {
        if (!file.Content.Contains("mssql_pconnect"))
        {
            return;
        }
        string? dblinkProp = null;
        file.Content.Replace(MssqlPConnectRegex().Replace(file.Content.ToString(), _MssqlPconnectEvaluator));
        if (dblinkProp is not null)
        {
            file.Content.Insert(file.Content.IndexOf(dblinkProp), "//");
        }

        string _MssqlPconnectEvaluator(Match match)
        {
            var dblinkMatch = match.Groups["dblink"];
            if (dblinkMatch.Value.StartsWith("$this->", StringComparison.Ordinal))
            {
                dblinkProp = $"var ${dblinkMatch.ValueSpan[7..]}";
            }
            using var sb = ZString.CreateStringBuilder();

            sb.Append(match.Groups["conn"].ValueSpan);
            sb.Append(" = mssql_connect(");
            sb.Append(match.Groups["host"].ValueSpan);
            sb.Append(", ");
            sb.Append(match.Groups["user"].ValueSpan);
            sb.Append(", ");
            sb.Append(match.Groups["pass"].ValueSpan);
            sb.Append(", ");
            sb.Append(match.Groups["db"].ValueSpan);
            sb.Append(')');
            sb.Append(match.Groups["in_between"].ValueSpan);
            sb.Append("//");
            var spaces = match.Groups["spaces"].Value;
            sb.Append(match.Groups["to_comment"].Value.Replace(spaces, $"{spaces}//", StringComparison.Ordinal));
            
            return sb.ToString();
        }
    }

    [GeneratedRegex(@"(?<conn>\$\S+?)\s?=\s?mssql_pconnect\s?\((?<host>\$\S+?),(?<user>\$\S+?),(?<pass>\$\S+?)\)(?<in_between>(.|\n)*?)(?<to_comment>((?<dblink>\$\S+?)\s?=\s?)?mssql_select_db\s?\((?<db>\$\S+?)\)\s?;\n(?<spaces>\s*?)if\s?\(.*?\k<dblink>(.|\n)*?(}|endif;))", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex MssqlPConnectRegex();
}

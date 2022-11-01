namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class HodnoceniConnect
{
    /// <summary>
    /// Soubory pdf/p_listina.php, pdf/p_listina.php a rss/hodnoceni.php
    /// obsahují stejný kód využívající mysqli s proměnnou $beta_hod nebo $hodnoceni_conn.
    /// </summary>
    public static FileWrapper UpgradeHodnoceniDBCalls(this FileWrapper file)
    {
        if (AllUpgradableFiles().Any(f => file.Path.EndsWith(f, StringComparison.Ordinal)))
        {
            var (connVar, dbVar) = HodnoceniConnFiles().Any(f => file.Path.EndsWith(f, StringComparison.Ordinal))
                                 ? ("hodnoceni_conn", "database_hodnoceni_conn") 
                                 : ("beta_hod", "dtb_hod");

            file.Content.Replace($"mysql_select_db(${dbVar}, ${connVar})", $"mysqli_select_db(${connVar}, ${dbVar})")
                        .Replace($"mysql_errno(${connVar})", $"mysqli_errno(${connVar})")
                        .Replace($"mysql_error(${connVar})", $"mysqli_error(${connVar})")
                        .Replace("mysqli_error($beta)", $"mysqli_error(${connVar})");

            var evaluator = new MatchEvaluator(_MysqliQueryEvaluator);
            var content = file.Content.ToString();
            var updated = MysqliQueryBetaRegex().Replace(content, evaluator);
            file.Content.Replace(content, updated);

            string _MysqliQueryEvaluator(Match match)
            {
                return new StringBuilder(match.Value)
                    .Replace("mysqli_query($beta,", $"mysqli_query(${connVar},")
                    .Replace($", ${connVar})", ")")
                    .Replace($",${connVar})", ")")
                    .ToString();
            }
        }
        return file;
    }

    private static IEnumerable<string> AllUpgradableFiles()
    {
        foreach (var file in HodnoceniConnFiles())
        {
            yield return file;
        }
        foreach (var file in BetaHodFiles())
        {
            yield return file;
        }
    }

    private static IEnumerable<string> HodnoceniConnFiles()
    {
        yield return Path.Join("rss", "update_to_mysql.php");
        yield return Path.Join("rubicon", "modules", "category", "menu.php");
    }

    private static IEnumerable<string> BetaHodFiles()
    {
        yield return Path.Join("pdf", "p_listina.php");
        yield return Path.Join("pdf", "p_listina_u.php");
        yield return Path.Join("rss", "hodnoceni.php");
    }

    [GeneratedRegex(@"mysqli_query\(\$beta,.+;", RegexOptions.None, matchTimeoutMilliseconds: 1234)]
    private static partial Regex MysqliQueryBetaRegex();
}

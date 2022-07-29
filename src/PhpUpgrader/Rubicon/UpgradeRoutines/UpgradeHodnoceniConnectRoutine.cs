namespace PhpUpgrader.Rubicon.UpgradeRoutines;

public static class UpgradeHodnoceniConnectRoutine
{
    /// <summary>
    /// Soubory pdf/p_listina.php, pdf/p_listina.php a rss/hodnoceni.php
    /// obsahují stejný kód využívající mysqli s proměnnou $beta_hod nebo $hodnoceni_conn.
    /// </summary>
    public static FileWrapper UpgradeHodnoceniDBCalls(this FileWrapper file)
    {
        if (Regex.IsMatch(file.Path, @"(pdf|rss)(\\|/)(p_listina(_u)?|hodnoceni|update_to_mysql)\.php$", RegexOptions.Compiled))
        {
            var (connVar, dbVar) = file.Path.EndsWith("update_to_mysql.php")
                                 ? ("hodnoceni_conn", "database_hodnoceni_conn") 
                                 : ("beta_hod", "dtb_hod");

            file.Content.Replace($"mysql_select_db(${dbVar}, ${connVar})", $"mysqli_select_db(${connVar}, ${dbVar})")
                        .Replace($"mysql_errno(${connVar})", $"mysqli_errno(${connVar})")
                        .Replace($"mysql_error(${connVar})", $"mysqli_error(${connVar})")
                        .Replace("mysqli_error($beta)", $"mysqli_error(${connVar})");

            var evaluator = new MatchEvaluator(MysqliQueryEvaluator);
            var content = file.Content.ToString();
            var updated = Regex.Replace(content, @"mysqli_query\(\$beta,.+;", evaluator);
            file.Content.Replace(content, updated);

            string MysqliQueryEvaluator(Match match)
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
}

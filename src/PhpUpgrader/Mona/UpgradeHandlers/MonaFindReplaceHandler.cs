namespace PhpUpgrader.Mona.UpgradeHandlers;

public class MonaFindReplaceHandler : FindReplaceHandler
{
    /// <summary>
    /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
    /// </summary>
    public override void UpgradeFindReplace(FileWrapper file)
    {
        foreach (var (find, replace) in Replacements)
        {
            file.Content.Replace(find, replace);
        }
    }

    /// <summary> Co a čím to nahradit. </summary>
    public override HashSet<(string find, string replace)> Replacements { get; } = new()
    {
        ( "=& new", "= new" ),
        ( "mysql_num_rows", "mysqli_num_rows" ),
        ( "MySQL_num_rows", "mysqli_num_rows" ),
        ( "mysql_error()", "mysqli_error($beta)" ),
        ( "mysql_connect", "mysqli_connect" ),
        ( "mysql_close", "mysqli_close" ),
        ( "MySQL_Close", "mysqli_close" ),
        ( "MySQL_close", "mysqli_close" ),
        ( "mysqli_close()", "mysqli_close($beta)" ),
        ( "mysql_fetch_row", "mysqli_fetch_row" ),
        ( "mysql_Fetch_Row", "mysqli_fetch_row" ),
        ( "mysql_fetch_array", "mysqli_fetch_array" ),
        ( "mysql_fetch_assoc", "mysqli_fetch_assoc" ),
        ( "mysql_fetch_object", "mysqli_fetch_object" ),
        ( "MySQL_fetch_object", "mysqli_fetch_object" ),
        ( "MYSQL_ASSOC", "MYSQLI_ASSOC" ),
        ( "mysql_select_db(DB_DATABASE, $this->db)", "mysqli_select_db($this->db, DB_DATABASE)" ),
        ( "mysql_select_db($database_beta, $beta)", "mysqli_select_db($beta, $database_beta)" ),
        ( "mysql_query(", "mysqli_query($beta, " ),
        ( "mysql_query (", "mysqli_query($beta, " ),
        ( "MySQL_Query(", "mysqli_query($beta, " ),
        ( "MySQL_Query (", "mysqli_query($beta, " ),
        ( ", $beta)", ")" ),
        ( ",$beta)", ")" ),
        ( "eregi(", "preg_match(" ),
        ( "eregi (", "preg_match(" ),
        ( "preg_match('^<tr(.*){0,}</tr>$'", "preg_match('/^<tr(.*){0,}< \\/tr>$/'" ),
        ( "mysql_data_seek", "mysqli_data_seek" ),
        ( "mysql_real_escape_string", "mysqli_real_escape_string" ),
        ( "mysql_free_result", "mysqli_free_result" ),
        ( "mysql_list_tables($database_beta);", "mysqli_query($beta, \"SHOW TABLES FROM `$database_beta`\");" ),
        ( "$table_all .= \"`\".mysql_tablename($result, $i).\"`\";", "$table_all .= \"`\".mysqli_fetch_row($result)[0].\"`\";" ),
        ( "<?php/", "<?php /" ),
        ( "<?PHP/", "<?PHP /" ),
    };
}

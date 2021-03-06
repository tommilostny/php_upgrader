using PhpUpgrader.Rubicon.UpgradeRoutines;

namespace PhpUpgrader.Rubicon;

/// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
public class RubiconUpgrader : MonaUpgrader
{
    /// <summary> Konstruktor Rubicon > Mona upgraderu. </summary>
    /// <remarks> Do <see cref="MonaUpgrader.FindReplace"/> přidá případy specifické pro Rubicon. </remarks>
    public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
    {
        foreach (var (find, replace) in AdditionalFindReplace())
        {
            FindReplace.Add(find, replace);
        }
    }

    /// <summary> Procedura aktualizace Rubicon souborů. </summary>
    /// <remarks> Použita ve volání metody <see cref="MonaUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
    /// <returns> Upravený soubor. </returns>
    protected override FileWrapper? UpgradeProcedure(string filePath)
    {
        return base.UpgradeProcedure(filePath) switch
        {
            //MonaUpgrader končí s null, také hned skončit.
            null => null,
            //jinak máme soubor k aktualizaci dalšími metodami specifickými pro Rubicon.
            var file => file.UpgradeObjectClass(this)
                            .UpgradeConstructors()
                            .UpgradeScriptLanguagePhp()
                            .UpgradeIncludesInHtmlComments()
                            .UpgradeAegisxDetail()
                            .UpgradeLoadData()
                            .UpgradeHomeTopProducts()
                            .UpgradeUrlPromenne()
                            .UpgradeDuplicateArrayKeys()
                            .UpgradeOldUnparsableAlmostEmpty()
                            .UpgradeHodnoceniDBCalls()
        };
    }

    private static IEnumerable<(string find, string replace)> AdditionalFindReplace()
    {
        yield return ("mysql_select_db($database_beta);",
                      "//mysql_select_db($database_beta);"
                     );
        yield return ("////mysql_select_db($database_beta);",
                      "//mysql_select_db($database_beta);"
                     );
        yield return ("function_exists(\"mysqli_real_escape_string\") ? mysqli_real_escape_string($theValue) : mysql_escape_string($theValue)",
                      "mysqli_real_escape_string($beta, $theValue)"
                     );
        yield return ("mysql_select_db($database_sportmall_import, $sportmall_import);",
                      "mysqli_select_db($sportmall_import, $database_sportmall_import);"
                     );
        yield return ("mysql_select_db($database_iviki_mysql, $iviki_mysql);",
                      "mysqli_select_db($iviki_mysql, $database_iviki_mysql);"
                     );
        yield return ("mysqli_query($beta, $query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
                      "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
                     );
        yield return ("mysqli_query($beta, $query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
                      "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
                     );
        yield return ("mysqli_query($beta, $query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
                      "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
                     );
        yield return ("mysqli_query($beta,$query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
                      "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
                     );
        yield return ("mysqli_query($beta,$query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
                      "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
                     );
        yield return ("mysqli_query($beta,$query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
                      "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
                     );
        yield return ("emptiable(strip_tags($obj->category_name.', '.$obj->style_name)), $title)",
                      "emptiable(strip_tags($obj->category_name.', '.$obj->style_name), $title))"
                     );
        yield return (@"preg_match(""^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$"", $email)",
                      @"preg_match("";^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$;"", $email)"
                     );
        yield return ("preg_match(\"ID\", $nazev)",
                      "preg_match('~ID~', $nazev)"
                     );
        yield return ("MySQL_query($query, $DBLink)",
                      "mysqli_query($DBLink, $query)"
                     );
        yield return ("MySQL_errno()",
                      "mysqli_errno($DBLink)"
                     );
        yield return ("MySQL_errno($DBLink)",
                      "mysqli_errno($DBLink)"
                     );
        yield return ("MySQL_error()",
                      "mysqli_error($DBLink)"
                     );
        //Použití <? ... ?> způsobuje, že kód neprojde PHP parserem, který vyhodí chybu.
        yield return ("<? ", "<?php ");
        yield return ("<?\n", "<?php\n");
        yield return ("<?\r", "<?php\r");
        yield return ("<?\t", "<?php\t");
        //PHPStan: Undefined variable: $PHP_SELF
        yield return ("<?php $PHP_SELF.\"#",
                      "<?= $_SERVER['PHP_SELF'].\"#"
                     );
        yield return ("<?= $PHP_SELF.\"#",
                      "<?= $_SERVER['PHP_SELF'].\"#"
                     );
        yield return ("$PHP_SELF",
                      "$_SERVER['PHP_SELF']"
                     );
        yield return ("\"<a href='$_SERVER['PHP_SELF']",
                      "\"<a href='\".$_SERVER['PHP_SELF'].\""
                     );
        //PHPStan: Function pg_select_db not found
        yield return ("pg_connect(DB_HOST, DB_USER, DB_PASSWORD)",
                      "pg_connect(\"host=\".DB_HOST.\" dbname=\".DB_DATABASE.\" user=\".DB_USER.\" password=\".DB_PASSWORD)"
                     );
        yield return ("pg_select_db(DB_DATABASE, $this->db)",
                      "//pg_select_db(DB_DATABASE, $this->db)"
                     );
        yield return ("pg_query(\"SET CHARACTER SET utf8\", $this->db)",
                      "pg_query($this->db, \"SET CHARACTER SET utf8\")"
                     );
    }
}

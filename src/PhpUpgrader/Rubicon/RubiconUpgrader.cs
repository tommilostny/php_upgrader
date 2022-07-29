using PhpUpgrader.Rubicon.UpgradeRoutines;

namespace PhpUpgrader.Rubicon;

/// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
public class RubiconUpgrader : MonaUpgrader
{
    /// <summary> Konstruktor Rubicon > Mona upgraderu. </summary>
    /// <remarks> Přidá specifické případy pro Rubicon do <see cref="MonaUpgrader.FindReplace"/>. </remarks>
    public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
    {
        //Přidat do FindReplace další dvojice pro nahrazení specifické pro Rubicon.
        var afr = new (string find, string replace)[]
        {
            ("mysql_select_db($database_beta);",
             "//mysql_select_db($database_beta);"
            ),
            ("////mysql_select_db($database_beta);",
             "//mysql_select_db($database_beta);"
            ),
            ("function_exists(\"mysqli_real_escape_string\") ? mysqli_real_escape_string($theValue) : mysql_escape_string($theValue)",
             "mysqli_real_escape_string($beta, $theValue)"
            ),
            ("mysql_select_db($database_sportmall_import, $sportmall_import);",
             "mysqli_select_db($sportmall_import, $database_sportmall_import);"
            ),
            ("mysql_select_db($database_iviki_mysql, $iviki_mysql);",
             "mysqli_select_db($iviki_mysql, $database_iviki_mysql);"
            ),
            ("mysqli_query($beta, $query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
             "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
            ),
            ("mysqli_query($beta, $query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
             "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
            ),
            ("mysqli_query($beta, $query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
             "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
            ),
            ("mysqli_query($beta,$query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
             "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
            ),
            ("mysqli_query($beta,$query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
             "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
            ),
            ("mysqli_query($beta,$query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
             "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
            ),
            ("emptiable(strip_tags($obj->category_name.', '.$obj->style_name)), $title)",
             "emptiable(strip_tags($obj->category_name.', '.$obj->style_name), $title))"
            ),
            (@"preg_match(""^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$"", $email)",
             @"preg_match("";^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$;"", $email)"
            ),
            ("preg_match(\"ID\", $nazev)",
             "preg_match('~ID~', $nazev)"
            ),
            ("MySQL_query($query, $DBLink)",
             "mysqli_query($DBLink, $query)"
            ),
            ("MySQL_errno()",
             "mysqli_errno($DBLink)"
            ),
            ("MySQL_errno($DBLink)",
             "mysqli_errno($DBLink)"
            ),
            ("MySQL_error()",
             "mysqli_error($DBLink)"
            ),
            //Použití <? ... ?> způsobuje, že kód neprojde PHP parserem, který vyhodí chybu.
            ("<? ", "<?php "),
            ("<?\n", "<?php\n"),
            ("<?\r", "<?php\r"),
            ("<?\t", "<?php\t"),
            //PHPStan: Undefined variable: $PHP_SELF
            ("<?php $PHP_SELF.\"#",
             "<?= $_SERVER['PHP_SELF'].\"#"
            ),
            ("<?= $PHP_SELF.\"#",
             "<?= $_SERVER['PHP_SELF'].\"#"
            ),
            ("$PHP_SELF",
             "$_SERVER['PHP_SELF']"
            ),
            ("\"<a href='$_SERVER['PHP_SELF']",
             "\"<a href='\".$_SERVER['PHP_SELF'].\""
            ),
            //PHPStan: Function pg_select_db not found
            ("pg_connect(DB_HOST, DB_USER, DB_PASSWORD)",
             "pg_connect(\"host=\".DB_HOST.\" dbname=\".DB_DATABASE.\" user=\".DB_USER.\" password=\".DB_PASSWORD)"
            ),
            ("pg_select_db(DB_DATABASE, $this->db)",
             "//pg_select_db(DB_DATABASE, $this->db)"
            ),
            ("pg_query(\"SET CHARACTER SET utf8\", $this->db)",
             "pg_query($this->db, \"SET CHARACTER SET utf8\")"
            ),
        };
        Array.ForEach(afr, additional => FindReplace.Add(additional.find, additional.replace));
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
                            .UpgradePListina()
        };
    }
}

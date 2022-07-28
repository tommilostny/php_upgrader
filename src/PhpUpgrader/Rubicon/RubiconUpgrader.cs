using PhpUpgrader.Rubicon.UpgradeRoutines;

namespace PhpUpgrader.Rubicon;

/// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
public class RubiconUpgrader : MonaUpgrader
{
    /// <summary> Konstruktor Rubicon > Mona upgraderu. </summary>
    /// <remarks> Přidá specifické případy pro Rubicon do <see cref="MonaUpgrader.FindReplace"/>. </remarks>
    public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
    {
        //Přidat do FindReplace dvojice pro nahrazení specifické pro Rubicon.
        new List<KeyValuePair<string, string>>
        {
            new("mysql_select_db($database_beta);",
                "//mysql_select_db($database_beta);"
            ),
            new("////mysql_select_db($database_beta);",
                "//mysql_select_db($database_beta);"
            ),
            new("function_exists(\"mysqli_real_escape_string\") ? mysqli_real_escape_string($theValue) : mysql_escape_string($theValue)",
                "mysqli_real_escape_string($beta, $theValue)"
            ),
            new("mysql_select_db($database_sportmall_import, $sportmall_import);",
                "mysqli_select_db($sportmall_import, $database_sportmall_import);"
            ),
            new("mysql_select_db($database_iviki_mysql, $iviki_mysql);",
                "mysqli_select_db($iviki_mysql, $database_iviki_mysql);"
            ),
            new("mysqli_query($beta, $query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
                "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
            ),
            new("mysqli_query($beta, $query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
            ),
            new("mysqli_query($beta, $query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
            ),
            new("mysqli_query($beta,$query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
                "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
            ),
            new("mysqli_query($beta,$query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
            ),
            new("mysqli_query($beta,$query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
            ),
            new("emptiable(strip_tags($obj->category_name.', '.$obj->style_name)), $title)",
                "emptiable(strip_tags($obj->category_name.', '.$obj->style_name), $title))"
            ),
            new(@"preg_match(""^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$"", $email)",
                @"preg_match("";^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$;"", $email)"
            ),
            new("preg_match(\"ID\", $nazev)",
                "preg_match('~ID~', $nazev)"
            ),
            new("MySQL_query($query, $DBLink)",
                "mysqli_query($DBLink, $query)"
            ),
            new("MySQL_errno()",
                "mysqli_errno($DBLink)"
            ),
            new("MySQL_errno($DBLink)",
                "mysqli_errno($DBLink)"
            ),
            new("MySQL_error()",
                "mysqli_error($DBLink)"
            ),
            //Použití <? ... ?> způsobuje, že kód neprojde PHP parserem, který vyhodí chybu.
            new("<? ", "<?php "),
            new("<?\n", "<?php\n"),
            new("<?\r", "<?php\r"),
            new("<?\t", "<?php\t"),
            //PHPStan: Undefined variable: $PHP_SELF
            new("<?php $PHP_SELF.\"#", "<?= $_SERVER['PHP_SELF'].\"#"),
            new("<?= $PHP_SELF.\"#", "<?= $_SERVER['PHP_SELF'].\"#"),
            new("$PHP_SELF", "$_SERVER['PHP_SELF']"),
            //PHPStan: Function pg_select_db not found
            new("pg_connect(DB_HOST, DB_USER, DB_PASSWORD)",
                "pg_connect(\"host=\".DB_HOST.\" dbname=\".DB_DATABASE.\" user=\".DB_USER.\" password=\".DB_PASSWORD)"
            ),
            new("pg_select_db(DB_DATABASE, $this->db)",
                "//pg_select_db(DB_DATABASE, $this->db)"
            ),
            new("pg_query(\"SET CHARACTER SET utf8\", $this->db)",
                "pg_query($this->db, \"SET CHARACTER SET utf8\")"
            ),
        }
        .ForEach(afr => FindReplace[afr.Key] = afr.Value);
    }

    /// <summary> Procedura aktualizace Rubicon souborů. </summary>
    /// <remarks> Použita ve volání metody <see cref="MonaUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
    /// <returns> Upravený soubor. </returns>
    protected override FileWrapper? UpgradeProcedure(string filePath)
    {
        var file = base.UpgradeProcedure(filePath);
        if (file is not null)
        {
            file.UpgradeObjectClass(this);
            file.UpgradeConstructors();
            file.UpgradeScriptLanguagePhp();
            file.UpgradeIncludesInHtmlComments();
            file.UpgradeAegisxDetail();
            file.UpgradeLoadData();
            file.UpgradeHomeTopProducts();
            file.UpgradeUrlPromenne();
            file.UpgradeDuplicateArrayKeys();
        }
        return file;
    }
}

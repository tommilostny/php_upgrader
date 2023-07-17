using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Rubicon.UpgradeHandlers;

public sealed class RubiconFindReplaceHandler : MonaFindReplaceHandler, IFindReplaceHandler
{
    /// <summary> <see cref="IFindReplaceHandler.Replacements"/> se specifickými případy pro Rubicon. </summary>
    /// <remarks>
    /// Před prvním zavoláním je stejné jako pro <seealso cref="MonaFindReplaceHandler"/>.
    /// Do této kolekce jsou přidány předpřipravené případy z <see cref="_additionalReplacements"/>, které je poté smazáno a nastaveno na null.
    /// </remarks>
    public override ISet<(string find, string replace)> Replacements
    {
        get
        {
            if (_additionalReplacements is not null)
            {
                _additionalReplacements.ForEach(ar => base.Replacements.Add(ar));
                _additionalReplacements.Clear();
                _additionalReplacements = null;
            }
            return base.Replacements;
        }
    }

    private List<(string find, string replace)>? _additionalReplacements = new()
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
        ("mysql_stat($link)",
         "mysqli_stat($link)"
        ),
        ("= &func_get_args();",
         "= func_get_args();"
        ),
        ("@get_magic_quotes_runtime();",
         "false /*@get_magic_quotes_runtime()*/;"
        ),
        ("get_magic_quotes_runtime();",
         "false /*get_magic_quotes_runtime()*/;"
        ),
        ("$shop_id = pg_fetch_row(pg_query(\"SELECT shop_id FROM shop WHERE domain = '{$_SERVER['HTTP_HOST']}' \"))\n",
         "$shop_id = pg_fetch_row(pg_query(\"SELECT shop_id FROM shop WHERE domain = '{$_SERVER['HTTP_HOST']}' \"));\n"),
        ("$shop_id = pg_fetch_row(pg_query(\"SELECT shop_id FROM shop WHERE domain = '{$_SERVER['HTTP_HOST']}' \"))\r\n",
         "$shop_id = pg_fetch_row(pg_query(\"SELECT shop_id FROM shop WHERE domain = '{$_SERVER['HTTP_HOST']}' \"));\r\n"),
        ("mysql_select_db($nazev_databaze,$db)",
         "mysqli_select_db($db, $nazev_databaze)"),
        ("mysql_select_db('pazitka', $link)",
         "mysqli_select_db($link, 'pazitka')"),
        ("$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\r\nheader('Location: ' . $actual_lin\r\n?>",
         "$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\r\nheader('Location: ' . $actual_link);\r\n?>"),
        ("$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\nheader('Location: ' . $actual_lin\n?>",
         "$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\nheader('Location: ' . $actual_link);\n?>"),
        ("/*\r\nif ($kontrola<>\"\") {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\r\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\r\n\t$message = \"Generovani probehlo s chybami.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$message .= $kontrola;\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n\t//echo $message;\r\n}\t*/",
         "/*\r\nif ($kontrola<>\"\") {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\r\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\r\n\t$message = \"Generovani probehlo s chybami.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$message .= $kontrola;\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n\t//echo $message;\r\n/*}\t*/"),
        ("/*\nif ($kontrola<>\"\") {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\n\t$message = \"Generovani probehlo s chybami.\";\n\t$message .= \"<br /><br />\";\n\t$message .= $kontrola;\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n\t//echo $message;\n}\t*/",
         "/*\nif ($kontrola<>\"\") {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\n\t$message = \"Generovani probehlo s chybami.\";\n\t$message .= \"<br /><br />\";\n\t$message .= $kontrola;\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n\t//echo $message;\n/*}\t*/"),
        ("//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\r\n/*\r\nif ($include_importy<>true) {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"mb@mcrai.eu\";\r\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\r\n\t$message = \"Generovani probehlo.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n}*/",
         "//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\r\n/*\r\nif ($include_importy<>true) {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"mb@mcrai.eu\";\r\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\r\n\t$message = \"Generovani probehlo.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n/*}*/"),
        ("//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\n/*\nif ($include_importy<>true) {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"mb@mcrai.eu\";\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\n\t$message = \"Generovani probehlo.\";\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n}*/",
         "//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\n/*\nif ($include_importy<>true) {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"mb@mcrai.eu\";\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\n\t$message = \"Generovani probehlo.\";\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n/*}*/"),
        ("//NASTAVENI PRI VYPNUTEM SLIM\r\n\t\tif (!$ms_beta) {\r\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\r\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\r\n\t\t\t$mnozstvi_central_value = $row_result['units'];\r\n\t\t",
         "//NASTAVENI PRI VYPNUTEM SLIM\r\n\t\tif (!$ms_beta) {\r\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\r\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\r\n\t\t\t$mnozstvi_central_value = $row_result['units'];\r\n\t\t}"),
        ("//NASTAVENI PRI VYPNUTEM SLIM\n\t\tif (!$ms_beta) {\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\n\t\t\t$mnozstvi_central_value = $row_result['units'];\n\t\t",
         "//NASTAVENI PRI VYPNUTEM SLIM\n\t\tif (!$ms_beta) {\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\n\t\t\t$mnozstvi_central_value = $row_result['units'];\n\t\t}"),
        ("setcookie($UserCookie, \"\", time()-3600);",
         "setcookie(isset($UserCookie) ? $UserCookie : $_SESSION[\"user\"], \"\", time()-3600);"),
        ("if ($url != \"0\" AND modul != \"obsah\") {",
         "if ($url != \"0\" AND $modul != \"obsah\") {"),
        ("<?php $type = pg_fetch_row(pg_query(\"SELECT style_name FROM product JOIN style_info USING(style_id) WHERE product_id = '\".$_GET['idp'].\"' \"))[0] ?>",
         "<?php if(isset($_GET['idp'])) $type = pg_fetch_row(pg_query(\"SELECT style_name FROM product JOIN style_info USING(style_id) WHERE product_id = '\".$_GET['idp'].\"' \"))[0]; ?>"),
        ("return $idx1 == $idx2 ? 0 : $idx1 < $idx2 ? -1 : 1;",
         "return $idx1 == $idx2 ? 0 : ($idx1 < $idx2 ? -1 : 1);"),
        ("function getmediaurl ($mediaid,$mediastyle,$category)",
         "function getmediaurl($mediaid, $mediastyle, $category = false)"),
    };
}

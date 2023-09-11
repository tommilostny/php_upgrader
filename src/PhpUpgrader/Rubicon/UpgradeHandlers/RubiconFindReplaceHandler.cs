using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Rubicon.UpgradeHandlers;

public sealed class RubiconFindReplaceHandler : MonaFindReplaceHandler, IFindReplaceHandler
{
    /// <summary> <see cref="IFindReplaceHandler.Replacements"/> se specifickými případy pro Rubicon. </summary>
    /// <remarks>
    /// Před prvním zavoláním je stejné jako pro <seealso cref="MonaFindReplaceHandler"/>.
    /// Do této kolekce jsou přidány předpřipravené případy z <see cref="_additionalReplacements"/>, které je poté smazáno a nastaveno na null.
    /// </remarks>
    public override IList<(string find, string replace)> Replacements
    {
        get
        {
            if (_additionalReplacements is not null)
            {
                _additionalReplacements.ForEach(_replacements.Add);
                _additionalReplacements.Clear();
                _additionalReplacements = null;
            }
            return _replacements;
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
         "$shop_id = pg_fetch_row(pg_query(\"SELECT shop_id FROM shop WHERE domain = '{$_SERVER['HTTP_HOST']}' \"));\n"
        ),
        ("$shop_id = pg_fetch_row(pg_query(\"SELECT shop_id FROM shop WHERE domain = '{$_SERVER['HTTP_HOST']}' \"))\r\n",
         "$shop_id = pg_fetch_row(pg_query(\"SELECT shop_id FROM shop WHERE domain = '{$_SERVER['HTTP_HOST']}' \"));\r\n"
        ),
        ("mysql_select_db($nazev_databaze,$db)",
         "mysqli_select_db($db, $nazev_databaze)"
        ),
        ("mysql_select_db('pazitka', $link)",
         "mysqli_select_db($link, 'pazitka')"
        ),
        ("$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\r\nheader('Location: ' . $actual_lin\r\n?>",
         "$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\r\nheader('Location: ' . $actual_link);\r\n?>"
        ),
        ("$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\nheader('Location: ' . $actual_lin\n?>",
         "$actual_link = \"http://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]\";\nheader('Location: ' . $actual_link);\n?>"
        ),
        ("/*\r\nif ($kontrola<>\"\") {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\r\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\r\n\t$message = \"Generovani probehlo s chybami.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$message .= $kontrola;\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n\t//echo $message;\r\n}\t*/",
         "/*\r\nif ($kontrola<>\"\") {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\r\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\r\n\t$message = \"Generovani probehlo s chybami.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$message .= $kontrola;\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n\t//echo $message;\r\n/*}\t*/"
        ),
        ("/*\nif ($kontrola<>\"\") {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\n\t$message = \"Generovani probehlo s chybami.\";\n\t$message .= \"<br /><br />\";\n\t$message .= $kontrola;\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n\t//echo $message;\n}\t*/",
         "/*\nif ($kontrola<>\"\") {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"jz@mcrai.eu, jo@mcrai.eu, podpora@mcrai.eu\";//\n\t$subject = \"prehledy \".$setup_domena.\" CHYBA\";\n\t$message = \"Generovani probehlo s chybami.\";\n\t$message .= \"<br /><br />\";\n\t$message .= $kontrola;\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n\t//echo $message;\n/*}\t*/"
        ),
        ("//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\r\n/*\r\nif ($include_importy<>true) {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"mb@mcrai.eu\";\r\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\r\n\t$message = \"Generovani probehlo.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n}*/",
         "//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\r\n/*\r\nif ($include_importy<>true) {\r\n\t$from = \"prehledy@mcrai.eu\";\r\n\t$to = \"mb@mcrai.eu\";\r\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\r\n\t$message = \"Generovani probehlo.\";\r\n\t$message .= \"<br /><br />\";\r\n\t$headers = \"FROM: \".$from.\"\\n\";\r\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\r\n\t/*if (@mail($to, $subject, $message, $headers)):\r\n\t\techo \" \";\r\n\telse:\r\n\t\techo \" \";\r\n\tendif;*/\r\n/*}*/"
        ),
        ("//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\n/*\nif ($include_importy<>true) {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"mb@mcrai.eu\";\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\n\t$message = \"Generovani probehlo.\";\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n}*/",
         "//pg_query(\"COMMIT\")or die(\"Transaction commit ERROR\");\n/*\nif ($include_importy<>true) {\n\t$from = \"prehledy@mcrai.eu\";\n\t$to = \"mb@mcrai.eu\";\n\t$subject = \"prehledy \".$setup_domena.\" OK\";\n\t$message = \"Generovani probehlo.\";\n\t$message .= \"<br /><br />\";\n\t$headers = \"FROM: \".$from.\"\\n\";\n\t$headers .= \"Content-Type: text/html; charset = utf-8\\n\";\n\t/*if (@mail($to, $subject, $message, $headers)):\n\t\techo \" \";\n\telse:\n\t\techo \" \";\n\tendif;*/\n/*}*/"
        ),
        ("//NASTAVENI PRI VYPNUTEM SLIM\r\n\t\tif (!$ms_beta) {\r\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\r\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\r\n\t\t\t$mnozstvi_central_value = $row_result['units'];\r\n\t\t",
         "//NASTAVENI PRI VYPNUTEM SLIM\r\n\t\tif (!$ms_beta) {\r\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\r\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\r\n\t\t\t$mnozstvi_central_value = $row_result['units'];\r\n\t\t}"
        ),
        ("//NASTAVENI PRI VYPNUTEM SLIM\n\t\tif (!$ms_beta) {\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\n\t\t\t$mnozstvi_central_value = $row_result['units'];\n\t\t",
         "//NASTAVENI PRI VYPNUTEM SLIM\n\t\tif (!$ms_beta) {\n\t\t\t$sql_dotaz = \"SELECT units FROM store_central WHERE product_id = '\".$row_slim_sklad['product_id'].\"' AND product_spec_id = '\".$row_slim_sklad['product_spec_id'].\"' AND dodavatel_id = '213954795'\";\n\t\t\t$result = pg_query($sql_dotaz);$row_result = pg_fetch_assoc($result);\n\t\t\t$mnozstvi_central_value = $row_result['units'];\n\t\t}"
        ),
        ("setcookie($UserCookie, \"\", time()-3600);",
         "setcookie(isset($UserCookie) ? $UserCookie : $_SESSION[\"user\"], \"\", time()-3600);"
        ),
        ("if ($url != \"0\" AND modul != \"obsah\") {",
         "if ($url != \"0\" AND $modul != \"obsah\") {"
        ),
        ("<?php $type = pg_fetch_row(pg_query(\"SELECT style_name FROM product JOIN style_info USING(style_id) WHERE product_id = '\".$_GET['idp'].\"' \"))[0] ?>",
         "<?php if(isset($_GET['idp'])) $type = pg_fetch_row(pg_query(\"SELECT style_name FROM product JOIN style_info USING(style_id) WHERE product_id = '\".$_GET['idp'].\"' \"))[0]; ?>"
        ),
        ("return $idx1 == $idx2 ? 0 : $idx1 < $idx2 ? -1 : 1;",
         "return $idx1 == $idx2 ? 0 : ($idx1 < $idx2 ? -1 : 1);"
        ),
        ("function getmediaurl ($mediaid,$mediastyle,$category)",
         "function getmediaurl($mediaid, $mediastyle, $category = false)"
        ),
        ("function getmediaurl_3d ($mediaid,$mediastyle,$category)",
         "function getmediaurl_3d($mediaid, $mediastyle, $category = false)"
        ),
        ("function getGlassDesc($art_no, $language, $table_name = NULL)\r\n{\r\n    if ($table_name == NULL){\r\n        break;",
         "function getGlassDesc($art_no, $language, $table_name = NULL)\r\n{\r\n    if ($table_name == NULL){\r\n        return;"
        ),
        ("function getGlassDesc($art_no, $language, $table_name = NULL)\n{\n    if ($table_name == NULL){\n        break;",
         "function getGlassDesc($art_no, $language, $table_name = NULL)\n{\n    if ($table_name == NULL){\n        return;"
        ),
        ("if (isset($_GET['search_value']))  $search_value = $_GET['search_value'];\r\nelse $search_value = '';\r\nif (count($search_value) > 1) $search_value2['value'] = $search_value['value'];\r\nelse $search_value2 = $search_value;",
         "if (isset($_GET['search_value'])) {\r\n    $search_value = $_GET['search_value'];\r\n    if (count($search_value) > 1) {\r\n        $search_value2['value'] = $search_value['value'];\r\n    } else {\r\n        $search_value2 = $search_value;\r\n    }\r\n} else {\r\n    $search_value2 = $search_value = '';\r\n}"
        ),
        ("if (isset($_GET['search_value']))  $search_value = $_GET['search_value'];\nelse $search_value = '';\nif (count($search_value) > 1) $search_value2['value'] = $search_value['value'];\nelse $search_value2 = $search_value;",
         "if (isset($_GET['search_value'])) {\n    $search_value = $_GET['search_value'];\n    if (count($search_value) > 1) {\n        $search_value2['value'] = $search_value['value'];\n    } else {\n        $search_value2 = $search_value;\n    }\n} else {\n    $search_value2 = $search_value = '';\n}"
        ),
        ("echo $query_data = \"\tSELECT * from aegisx_produkty_all WHERE language_id = 1",
         "$query_data = \"\tSELECT * from aegisx_produkty_all WHERE language_id = 1"
        ),
        ("foreach($VARIANTY[\"$idp\"] as $varianta) {",
         "if (isset($VARIANTY[\"$idp\"])) foreach($VARIANTY[\"$idp\"] as $varianta) {"
        ),
        ("count($VARIANTY[\"$idp\"]) >",
         "isset($VARIANTY[\"$idp\"]) && count($VARIANTY[\"$idp\"]) >"
        ),
        ("count($VARIANTY[\"$idp\"]) <",
         "isset($VARIANTY[\"$idp\"]) && count($VARIANTY[\"$idp\"]) <"
        ),
        ("count($VARIANTY[\"$idp\"]) ==",
         "isset($VARIANTY[\"$idp\"]) && count($VARIANTY[\"$idp\"]) =="
        ),
        ("count($VARIANTY[\"$idp\"]) !",
         "isset($VARIANTY[\"$idp\"]) && count($VARIANTY[\"$idp\"]) !"
        ),
        ("$category_id = najdi_v_db(\"shop_category\",\"seo_url\",$page_url,\"category_id\");//prevod name na id\r\n\t$menu_q = \"SELECT * FROM shop_category WHERE shop_id_up = '\".$category_id.\"'\";\r\n\t$menu_d = pg_query($menu_q);\r\n\t$pocet_menu = pg_num_rows($menu_d);\r\n\t$MENU_CTG = array();\r\n\twhile($menu_r = pg_fetch_assoc($menu_d)) {\r\n\t\t$MENU_CTG[] = $menu_r['category_id'];\t\t\r\n\t}",
         "$category_id = najdi_v_db(\"shop_category\",\"seo_url\",$page_url,\"category_id\");//prevod name na id\r\n\tif (!empty($category_id)) {\r\n\t\t$menu_q = \"SELECT * FROM shop_category WHERE shop_id_up = '\".$category_id.\"'\";\r\n\t\t$menu_d = pg_query($menu_q);\r\n\t\t$pocet_menu = pg_num_rows($menu_d);\r\n\t\t$MENU_CTG = array();\r\n\t\twhile($menu_r = pg_fetch_assoc($menu_d)) {\r\n\t\t\t$MENU_CTG[] = $menu_r['category_id'];\t\t\r\n\t\t}\r\n\t}"
        ),
        ("$category_id = najdi_v_db(\"shop_category\",\"seo_url\",$page_url,\"category_id\");//prevod name na id\n\t$menu_q = \"SELECT * FROM shop_category WHERE shop_id_up = '\".$category_id.\"'\";\n\t$menu_d = pg_query($menu_q);\n\t$pocet_menu = pg_num_rows($menu_d);\n\t$MENU_CTG = array();\n\twhile($menu_r = pg_fetch_assoc($menu_d)) {\n\t\t$MENU_CTG[] = $menu_r['category_id'];\t\t\n\t}",
         "$category_id = najdi_v_db(\"shop_category\",\"seo_url\",$page_url,\"category_id\");//prevod name na id\n\tif (!empty($category_id)) {\n\t\t$menu_q = \"SELECT * FROM shop_category WHERE shop_id_up = '\".$category_id.\"'\";\n\t\t$menu_d = pg_query($menu_q);\n\t\t$pocet_menu = pg_num_rows($menu_d);\n\t\t$MENU_CTG = array();\n\t\twhile($menu_r = pg_fetch_assoc($menu_d)) {\n\t\t\t$MENU_CTG[] = $menu_r['category_id'];\t\t\n\t\t}\n\t}"
        ),
        ("class McBalikovna\r\n{\r\n\r\n    private $source_xml = 'http://napostu.ceskaposta.cz/vystupy/balikovny.xml';\r\n    private $domain = 1;\r\n    private $hostname = null;\r\n    private $username = null;\r\n    private $password = null;\r\n    private $database = null;\r\n    private $connport = null;\r\n\r\n    public function __construct($domain = 1, $hostname, $username, $password, $database, $connport)",
         "class McBalikovna\r\n{\r\n\r\n    private $source_xml = 'http://napostu.ceskaposta.cz/vystupy/balikovny.xml';\r\n    private $domain = 1;\r\n    private $hostname = null;\r\n    private $username = null;\r\n    private $password = null;\r\n    private $database = null;\r\n    private $connport = null;\r\n\r\n    public function __construct($domain = 1, $hostname = null, $username = null, $password = null, $database = null, $connport = null)"
        ),
        ("class McBalikovna\n{\n\n    private $source_xml = 'http://napostu.ceskaposta.cz/vystupy/balikovny.xml';\n    private $domain = 1;\n    private $hostname = null;\n    private $username = null;\n    private $password = null;\n    private $database = null;\n    private $connport = null;\n\n    public function __construct($domain = 1, $hostname, $username, $password, $database, $connport)",
         "class McBalikovna\n{\n\n    private $source_xml = 'http://napostu.ceskaposta.cz/vystupy/balikovny.xml';\n    private $domain = 1;\n    private $hostname = null;\n    private $username = null;\n    private $password = null;\n    private $database = null;\n    private $connport = null;\n\n    public function __construct($domain = 1, $hostname = null, $username = null, $password = null, $database = null, $connport = null)"
        ),
        ("public function GetData($SearchString, $id) {\r\n        $data = [];\r\n\r\n        if (!empty($SearchString)) {\r\n            $search = ' WHERE '.$SearchString;\r\n        }\r\n\r\n        if (!empty($id) && is_numeric($id)) {\r\n            $search = ' WHERE id = ?';\r\n            $data = [$id];\r\n        }",
         "public function GetData($SearchString = null, $id = null) {\r\n        $data = [];\r\n\r\n        if (!empty($SearchString)) {\r\n            $search = ' WHERE '.$SearchString;\r\n        }\r\n\r\n        if (!empty($id) && is_numeric($id)) {\r\n            $search = ' WHERE id = ?';\r\n            $data = [$id];\r\n        }"
        ),
        ("public function GetData($SearchString, $id) {\n        $data = [];\n\n        if (!empty($SearchString)) {\n            $search = ' WHERE '.$SearchString;\n        }\n\n        if (!empty($id) && is_numeric($id)) {\n            $search = ' WHERE id = ?';\n            $data = [$id];\n        }",
         "public function GetData($SearchString = null, $id = null) {\n        $data = [];\n\n        if (!empty($SearchString)) {\n            $search = ' WHERE '.$SearchString;\n        }\n\n        if (!empty($id) && is_numeric($id)) {\n            $search = ' WHERE id = ?';\n            $data = [$id];\n        }"
        ),
        ("//}\r\n\t\t\t\t}\r\n\t\t\t}\r\n\t\t}\r\n\t}\r\n} else {\r\n  fputs($fp,\"XML ma nulovou velikost\\n\");\r\n}\r\nfclose($fp);",
         "//}\r\n\t\t\t\t}\r\n\t\t\t}\r\n\t\t}\r\n\t}\r\nelse {\r\n  fputs($fp,\"XML ma nulovou velikost\\n\");\r\n}\r\nfclose($fp);"
        ),
        ("//}\n\t\t\t\t}\n\t\t\t}\n\t\t}\n\t}\n} else {\n  fputs($fp,\"XML ma nulovou velikost\\n\");\n}\nfclose($fp);",
         "//}\n\t\t\t\t}\n\t\t\t}\n\t\t}\n\t}\nelse {\n  fputs($fp,\"XML ma nulovou velikost\\n\");\n}\nfclose($fp);"
        ),
        ("function add_to_card($ADD_TO_CARD,$PRODUCT,$POCET_ROLL_SET,$pole_gift){",
         "function add_to_card($ADD_TO_CARD, $PRODUCT, $POCET_ROLL_SET, $pole_gift = null){"
        ),
        ("$setup_T_EMAIL = 'pro.jirka@seznam.cz';",
         "$setup_T_EMAIL = 'info@botaska.cz'; //'pro.jirka@seznam.cz';"
        ),
        ("$query_products = \"SELECT \r\n\t\t\t\tproduct.product_id,\r\n\t\t\t\tstyle_info.style_name,\r\n\t\t\t\tstyle_info.style_recykl,\r\n\t\t\t\tproducer_info.producer_name,\r\n\t\t\t\tproduct_info.product_name,\r\n\t\t\t\tproduct_info.short_info,\r\n\t\t\t\tproduct_info.long_info,\r\n\t\t\t\tproduct_info.seo_title,\r\n\t\t\t\tproduct_info.seo_keywords,\r\n\t\t\t\tproduct_info.seo_description,\r\n\t\t\t\tproduct_info.akce,\r\n\t\t\t\tproduct_info.hodnota_akce,\r\n\t\t\t\tproduct_info.zacatek_akce,\r\n\t\t\t\tproduct_info.hodnota_akce,\r\n\t\t\t\tproduct_info.text_akce",
         "$query_products = \"SELECT \r\n\t\t\t\tproduct.product_id,\r\n\t\t\t\tstyle_info.style_id,\r\n\t\t\t\tstyle_info.style_name,\r\n\t\t\t\tstyle_info.style_recykl,\r\n\t\t\t\tproducer_info.producer_name,\r\n\t\t\t\tproduct_info.product_name,\r\n\t\t\t\tproduct_info.short_info,\r\n\t\t\t\tproduct_info.long_info,\r\n\t\t\t\tproduct_info.seo_title,\r\n\t\t\t\tproduct_info.seo_keywords,\r\n\t\t\t\tproduct_info.seo_description,\r\n\t\t\t\tproduct_info.akce,\r\n\t\t\t\tproduct_info.hodnota_akce,\r\n\t\t\t\tproduct_info.zacatek_akce,\r\n\t\t\t\tproduct_info.hodnota_akce,\r\n\t\t\t\tproduct_info.text_akce"
        ),
        ("$query_products = \"SELECT \n\t\t\t\tproduct.product_id,\n\t\t\t\tstyle_info.style_name,\n\t\t\t\tstyle_info.style_recykl,\n\t\t\t\tproducer_info.producer_name,\n\t\t\t\tproduct_info.product_name,\n\t\t\t\tproduct_info.short_info,\n\t\t\t\tproduct_info.long_info,\n\t\t\t\tproduct_info.seo_title,\n\t\t\t\tproduct_info.seo_keywords,\n\t\t\t\tproduct_info.seo_description,\n\t\t\t\tproduct_info.akce,\n\t\t\t\tproduct_info.hodnota_akce,\n\t\t\t\tproduct_info.zacatek_akce,\n\t\t\t\tproduct_info.hodnota_akce,\n\t\t\t\tproduct_info.text_akce",
         "$query_products = \"SELECT \n\t\t\t\tproduct.product_id,\n\t\t\t\tstyle_info.style_id,\n\t\t\t\tstyle_info.style_name,\n\t\t\t\tstyle_info.style_recykl,\n\t\t\t\tproducer_info.producer_name,\n\t\t\t\tproduct_info.product_name,\n\t\t\t\tproduct_info.short_info,\n\t\t\t\tproduct_info.long_info,\n\t\t\t\tproduct_info.seo_title,\n\t\t\t\tproduct_info.seo_keywords,\n\t\t\t\tproduct_info.seo_description,\n\t\t\t\tproduct_info.akce,\n\t\t\t\tproduct_info.hodnota_akce,\n\t\t\t\tproduct_info.zacatek_akce,\n\t\t\t\tproduct_info.hodnota_akce,\n\t\t\t\tproduct_info.text_akce"
        ),
        ("$query_s_obr = \"SELECT product_id FROM product_category WHERE category_id = '\".$product_obr.\"'\r\n\t\t\t\t\t\t\t\t\t\t\tand product_id != 199669181\r\n\t\t\t\t\t\t\t\t\t\t\tORDER BY product_id ASC\";\t\t\t\r\n\t\t\t\t\t\t\t$category_s_obr = pg_query($query_s_obr);\r\n\t\t\t\t\t\t\t$row_s_obr = pg_fetch_assoc($category_s_obr);\r\n\t\t\t\t\t\t\t$product_obr = $row_s_obr['product_id'];\r\n\t\t\t\t\t\t\t//$product_obr = najdi_v_db(\"product_media\",\"product_id\",$product_obr,\"media_id\");\r\n\t\t\t\t\t\t\t$query_product_obr = \"SELECT media_id FROM product_media WHERE product_id ='\".$product_obr.\"'\";\r\n\t\t\t\t\t\t\t$data_product_obr = pg_query($query_product_obr);",
         "if (!empty($product_obr)) {\r\n\t\t\t\t\t\t\t\t$query_s_obr = \"SELECT product_id FROM product_category WHERE category_id = '\".$product_obr.\"'\r\n\t\t\t\t\t\t\t\t\t\t\t\tand product_id != 199669181\r\n\t\t\t\t\t\t\t\t\t\t\t\tORDER BY product_id ASC\";\t\t\t\r\n\t\t\t\t\t\t\t\t$category_s_obr = pg_query($query_s_obr);\r\n\t\t\t\t\t\t\t\t$row_s_obr = pg_fetch_assoc($category_s_obr);\r\n\t\t\t\t\t\t\t\t$product_obr = $row_s_obr['product_id'];\r\n\t\t\t\t\t\t\t\t//$product_obr = najdi_v_db(\"product_media\",\"product_id\",$product_obr,\"media_id\");\r\n\t\t\t\t\t\t\t\t$query_product_obr = \"SELECT media_id FROM product_media WHERE product_id ='\".$product_obr.\"'\";\r\n\t\t\t\t\t\t\t\t$data_product_obr = pg_query($query_product_obr);\r\n\t\t\t\t\t\t\t}"
        ),
        ("$query_s_obr = \"SELECT product_id FROM product_category WHERE category_id = '\".$product_obr.\"'\n\t\t\t\t\t\t\t\t\t\t\tand product_id != 199669181\n\t\t\t\t\t\t\t\t\t\t\tORDER BY product_id ASC\";\t\t\t\n\t\t\t\t\t\t\t$category_s_obr = pg_query($query_s_obr);\n\t\t\t\t\t\t\t$row_s_obr = pg_fetch_assoc($category_s_obr);\n\t\t\t\t\t\t\t$product_obr = $row_s_obr['product_id'];\n\t\t\t\t\t\t\t//$product_obr = najdi_v_db(\"product_media\",\"product_id\",$product_obr,\"media_id\");\n\t\t\t\t\t\t\t$query_product_obr = \"SELECT media_id FROM product_media WHERE product_id ='\".$product_obr.\"'\";\n\t\t\t\t\t\t\t$data_product_obr = pg_query($query_product_obr);",
         "if (!empty($product_obr)) {\n\t\t\t\t\t\t\t\t$query_s_obr = \"SELECT product_id FROM product_category WHERE category_id = '\".$product_obr.\"'\n\t\t\t\t\t\t\t\t\t\t\t\tand product_id != 199669181\n\t\t\t\t\t\t\t\t\t\t\t\tORDER BY product_id ASC\";\t\t\t\n\t\t\t\t\t\t\t\t$category_s_obr = pg_query($query_s_obr);\n\t\t\t\t\t\t\t\t$row_s_obr = pg_fetch_assoc($category_s_obr);\n\t\t\t\t\t\t\t\t$product_obr = $row_s_obr['product_id'];\n\t\t\t\t\t\t\t\t//$product_obr = najdi_v_db(\"product_media\",\"product_id\",$product_obr,\"media_id\");\n\t\t\t\t\t\t\t\t$query_product_obr = \"SELECT media_id FROM product_media WHERE product_id ='\".$product_obr.\"'\";\n\t\t\t\t\t\t\t\t$data_product_obr = pg_query($query_product_obr);\n\t\t\t\t\t\t\t}"
        ),
        ("$query_products = \"SELECT * FROM product,product_info WHERE product.product_id = \".$_GET['idp'].\" AND product.product_id = product_info.product_id AND product_info.product_name != ''::text AND product_info.language_id = 1 $SQL_sklad ORDER BY product_info.product_name ASC\";\r\n  \t$products = pg_query($query_products);\r\n  \t$row_products = pg_fetch_assoc($products);\r\n  \t$media_id = najdi_v_db(\"product_media\",\"product_id\",$row_products['product_id'],\"media_id\");",
         "$media_id = NULL;\r\n    if (isset($_GET['idp'])) {\r\n      $query_products = \"SELECT * FROM product,product_info WHERE product.product_id = \".$_GET['idp'].\" AND product.product_id = product_info.product_id AND product_info.product_name != ''::text AND product_info.language_id = 1 $SQL_sklad ORDER BY product_info.product_name ASC\";\r\n      $products = pg_query($query_products);\r\n      $row_products = pg_fetch_assoc($products);\r\n      $media_id = najdi_v_db(\"product_media\",\"product_id\",$row_products['product_id'],\"media_id\");\r\n    }"
        ),
        ("$query_products = \"SELECT * FROM product,product_info WHERE product.product_id = \".$_GET['idp'].\" AND product.product_id = product_info.product_id AND product_info.product_name != ''::text AND product_info.language_id = 1 $SQL_sklad ORDER BY product_info.product_name ASC\";\n  \t$products = pg_query($query_products);\n  \t$row_products = pg_fetch_assoc($products);\n  \t$media_id = najdi_v_db(\"product_media\",\"product_id\",$row_products['product_id'],\"media_id\");",
         "$media_id = NULL;\n    if (isset($_GET['idp'])) {\n      $query_products = \"SELECT * FROM product,product_info WHERE product.product_id = \".$_GET['idp'].\" AND product.product_id = product_info.product_id AND product_info.product_name != ''::text AND product_info.language_id = 1 $SQL_sklad ORDER BY product_info.product_name ASC\";\n      $products = pg_query($query_products);\n      $row_products = pg_fetch_assoc($products);\n      $media_id = najdi_v_db(\"product_media\",\"product_id\",$row_products['product_id'],\"media_id\");\n    }"
        ),
        ("// testování zda je uživatel již přihlášen\r\n\tfunction logged() {\r\n\t\t$query=\"SELECT user_name, user_surname FROM $this->table WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"' AND ip='\".$this->ip.\"'  AND lasttime>=DATE_SUB(now(),INTERVAL \".$this->checktimelimit.\" SECOND)\";\r\n\t\t$result = @pg_query($query);\r\n\t\t$data = @pg_fetch_assoc($result);\r\n\r\n\t\tif (pg_num_rows($result)==1){\r\n\r\n\t\t\t$query=\"UPDATE $this->table SET lasttime=now() WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"'\";\r\n\t\t\t$result = @pg_query($query);\r\n\r\n\t\t\t$this->load();\r\n\t\t\treturn (1);\r\n\t\t} else {\r\n\t\t\treturn (0);\r\n\t\t}\r\n\t}",
         "// testování zda je uživatel již přihlášen\r\n\tfunction logged() {\r\n\t\t$query=\"SELECT user_name, user_surname FROM $this->table WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"' AND ip='\".$this->ip.\"'  AND lasttime>=DATE_SUB(now(),INTERVAL \".$this->checktimelimit.\" SECOND)\";\r\n\t\t$result = @pg_query($query);\r\n\t\t$data = @pg_fetch_assoc($result);\r\n\t\tif ($result !== false && pg_num_rows($result) == 1) {\r\n\t\t\t$query=\"UPDATE $this->table SET lasttime=now() WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"'\";\r\n\t\t\t$result = @pg_query($query);\r\n\t\t\t$this->load();\r\n\t\t\treturn (1);\r\n\t\t} else {\r\n\t\t\treturn (0);\r\n\t\t}\r\n\t}"
        ),
        ("// testování zda je uživatel již přihlášen\n\tfunction logged() {\n\t\t$query=\"SELECT user_name, user_surname FROM $this->table WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"' AND ip='\".$this->ip.\"'  AND lasttime>=DATE_SUB(now(),INTERVAL \".$this->checktimelimit.\" SECOND)\";\n\t\t$result = @pg_query($query);\n\t\t$data = @pg_fetch_assoc($result);\n\n\t\tif (pg_num_rows($result)==1){\n\n\t\t\t$query=\"UPDATE $this->table SET lasttime=now() WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"'\";\n\t\t\t$result = @pg_query($query);\n\n\t\t\t$this->load();\n\t\t\treturn (1);\n\t\t} else {\n\t\t\treturn (0);\n\t\t}\n\t}",
         "// testování zda je uživatel již přihlášen\n\tfunction logged() {\n\t\t$query=\"SELECT user_name, user_surname FROM $this->table WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"' AND ip='\".$this->ip.\"'  AND lasttime>=DATE_SUB(now(),INTERVAL \".$this->checktimelimit.\" SECOND)\";\n\t\t$result = @pg_query($query);\n\t\t$data = @pg_fetch_assoc($result);\n\t\tif ($result !== false && pg_num_rows($result) == 1) {\n\t\t\t$query=\"UPDATE $this->table SET lasttime=now() WHERE session='\".$this->session_login_string.\"' AND login='\".$this->login_name.\"'\";\n\t\t\t$result = @pg_query($query);\n\t\t\t$this->load();\n\t\t\treturn (1);\n\t\t} else {\n\t\t\treturn (0);\n\t\t}\n\t}"
        ),
        ("$product_review_query = \"SELECT * FROM review_product WHERE url='\".$PRODUCT['url'].\"' OR ean ='\".$PRODUCT['ean'].\"' WHERE rating >=4.0 AND unix_timestamp > \".$rew_from.\" ORDER BY random() LIMIT 6\";",
         "$product_review_query = \"SELECT * FROM review_product WHERE url='\".$PRODUCT['url'].\"' OR ean ='\".$PRODUCT['ean'].\"' AND rating >= 4.0 AND unix_timestamp > \".$rew_from.\" ORDER BY random() LIMIT 6\";"
        ),
        ("else://pokud se jedna o shop (musime nacist i podkategory)\r\n\t\t$SQL_NEWS_SPECIAL = \"WHERE category_id = \".$NAVIGACE[\"1\"][\"id\"].\" \";\r\n\t\t//load db\r\n\t\t$query_nk = \"SELECT category_id,shop_id_up FROM shop_category WHERE shop_id_up = \".$NAVIGACE[\"1\"][\"id\"].\" AND category_id IN (SELECT category_id FROM news) ORDER BY category_id ASC\";\r\n\t\t$nk = pg_query($query_nk);\r\n\t\t$row_nk = pg_fetch_assoc($nk);\r\n\t\t$totalRows_nk = pg_num_rows($nk);\r\n\t\tif($totalRows_nk > 0):\r\n\t\t\tdo {\r\n\t\t\t\t$SQL_NEWS_SPECIAL .= \" OR category_id = \".$row_nk['category_id'].\" \";\r\n\t\t\t} while ($row_nk = pg_fetch_assoc($nk));\r\n\t\tendif;\r\n\t\t//\t\r\n\t\t\r\n\tendif;",
         "else: //pokud se jedna o shop (musime nacist i podkategory)\r\n\t\tif (isset($NAVIGACE[\"1\"])):\r\n\t\t\t$SQL_NEWS_SPECIAL = \"WHERE category_id = \".$NAVIGACE[\"1\"][\"id\"].\" \";\r\n\t\t\t//load db\r\n\t\t\t$query_nk = \"SELECT category_id,shop_id_up FROM shop_category WHERE shop_id_up = \".$NAVIGACE[\"1\"][\"id\"].\" AND category_id IN (SELECT category_id FROM news) ORDER BY category_id ASC\";\r\n\t\t\t$nk = pg_query($query_nk);\r\n\t\t\t$row_nk = pg_fetch_assoc($nk);\r\n\t\t\t$totalRows_nk = pg_num_rows($nk);\r\n\t\t\tif($totalRows_nk > 0):\r\n\t\t\t\tdo {\r\n\t\t\t\t\t$SQL_NEWS_SPECIAL .= \" OR category_id = \".$row_nk['category_id'].\" \";\r\n\t\t\t\t} while ($row_nk = pg_fetch_assoc($nk));\r\n\t\t\tendif;\r\n\t\tendif;\r\n\tendif;"
        ),
        ("else://pokud se jedna o shop (musime nacist i podkategory)\n\t\t$SQL_NEWS_SPECIAL = \"WHERE category_id = \".$NAVIGACE[\"1\"][\"id\"].\" \";\n\t\t//load db\n\t\t$query_nk = \"SELECT category_id,shop_id_up FROM shop_category WHERE shop_id_up = \".$NAVIGACE[\"1\"][\"id\"].\" AND category_id IN (SELECT category_id FROM news) ORDER BY category_id ASC\";\n\t\t$nk = pg_query($query_nk);\n\t\t$row_nk = pg_fetch_assoc($nk);\n\t\t$totalRows_nk = pg_num_rows($nk);\n\t\tif($totalRows_nk > 0):\n\t\t\tdo {\n\t\t\t\t$SQL_NEWS_SPECIAL .= \" OR category_id = \".$row_nk['category_id'].\" \";\n\t\t\t} while ($row_nk = pg_fetch_assoc($nk));\n\t\tendif;\n\t\t//\t\n\t\t\n\tendif;",
         "else: //pokud se jedna o shop (musime nacist i podkategory)\n\t\tif (isset($NAVIGACE[\"1\"])):\n\t\t\t$SQL_NEWS_SPECIAL = \"WHERE category_id = \".$NAVIGACE[\"1\"][\"id\"].\" \";\n\t\t\t//load db\n\t\t\t$query_nk = \"SELECT category_id,shop_id_up FROM shop_category WHERE shop_id_up = \".$NAVIGACE[\"1\"][\"id\"].\" AND category_id IN (SELECT category_id FROM news) ORDER BY category_id ASC\";\n\t\t\t$nk = pg_query($query_nk);\n\t\t\t$row_nk = pg_fetch_assoc($nk);\n\t\t\t$totalRows_nk = pg_num_rows($nk);\n\t\t\tif($totalRows_nk > 0):\n\t\t\t\tdo {\n\t\t\t\t\t$SQL_NEWS_SPECIAL .= \" OR category_id = \".$row_nk['category_id'].\" \";\n\t\t\t\t} while ($row_nk = pg_fetch_assoc($nk));\n\t\t\tendif;\n\t\tendif;\n\tendif;"
        ),
        ("<?PHP } elseif ($auth->isLoggedIn()) { ?>",
         "<?php } elseif (isset($auth) && $auth->isLoggedIn()) { ?>"
        ),
        ("$search = new Search($searchValue, $language_id, $domainid);",
         "$search = new Search($searchValue, $language_id, $domainid, $_SERVER['SERVER_NAME']);"
        ),
        ("$search = new Search($searchValue, $DOMAIN_ID);",
         "$search = new Search($searchValue, $DOMAIN_ID, $_SERVER['SERVER_NAME']);"
        ),
        ("$search = new Search($searchValue, $domainId);",
         "$search = new Search($searchValue, $domainId, $_SERVER['SERVER_NAME']);"
        ),
        (@"public static function insert($query, $parameters = [])
	{
		try
		{
			$tmp = self::$conn->prepare($query);
			$tmp->execute($parameters);

            return self::$conn->lastInsertId();
		}
		catch (PDOException $e)
		{
			echo $e->getMessage();
			Debugger::handleError($e);
		}
	}", @"public static function insert($query, $parameters = [])
	{
		try
		{
			$tmp = self::$conn->prepare($query);
			$tmp->execute($parameters);

            return self::$conn->lastInsertId();
		}
		catch (PDOException $e)
		{
			//echo $e->getMessage();
			Debugger::handleError($e);
		}
	}"),
      ("round((real)$", "round((float)$"),
      ("echo found;", "echo 'found';"),
    };
}

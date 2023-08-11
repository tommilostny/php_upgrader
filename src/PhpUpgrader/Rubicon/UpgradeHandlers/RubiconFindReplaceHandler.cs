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
        ("if (count($MENU_CTG) > 0",
         "if ($MENU_CTG !== null && count($MENU_CTG) > 0"
        ),
        ("if (count($MENU_CTG) == 0",
         "if ($MENU_CTG === null || count($MENU_CTG) == 0"
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
    };
}

using PhpUpgrader.Mona.UpgradeExtensions;

namespace PhpUpgrader.Tests;

public class WhileListEachTests : UnitTestWithOutputBase
{
    public WhileListEachTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("$pocet_prvku = count($NAVIGACE);\r\n\t\t$repeat = 0;\r\n\t\treset($NAVIGACE);\r\n\t\tksort($NAVIGACE);\r\n\t\twhile(list($idn)=each($NAVIGACE)):\r\n\t\t//prevod na male pismena")]
    [InlineData("//------------------------\r\n\t\t//producer_index\r\n\t\t$producer_index = \"0\";//reset\r\n\t\treset($PRODUCER); while(list($idp)=each($PRODUCER)):\r\n\t\t\tif ($PRODUCER[\"$idp\"][\"id\"] == $row_basic['producer_id']):\r\n\t\t\t\t$producer_index = $PRODUCER[\"$idp\"][\"index\"];\r\n\t\t\t\t$producer_id = $PRODUCER[\"$idp\"][\"id\"];\r\n\t\t\t\t$producer_select = $PRODUCER[\"$idp\"][\"name\"];\t\t\t\t\t\t\t\r\n\t\t\tendif;\t\r\n\t\tendwhile;\r\n\t\t//style_index\r\n\t\t$style_index = \"0\";//reset\r\n\t\treset($STYLE); while(list($idp)=each($STYLE)):\r\n\t\t\tif ($STYLE[\"$idp\"][\"id\"] == $row_basic['style_id']):\t\t\t\t\t\t\r\n\t\t\t\t$style_index = $STYLE[\"$idp\"][\"index\"];\r\n\t\t\t\t$style_id = $STYLE[\"$idp\"][\"id\"];\t\t\t\t\t\t\t\r\n\t\t\t\t$style_select = $STYLE[\"$idp\"][\"name\"];\t\t\t\t\t\t\t\r\n\t\t\tendif;\t\r\n\t\tendwhile;\r\n\t\t// STAVY SKLADU\r\n\t\t$query_stav_skladu = \"SELECT * FROM stavy_skladu WHERE language_id = \".USER_LANG.\" ORDER BY stav_id ASC \";\r\n\t\t$data_stav_skladu = pg_query($query_stav_skladu);\r\n\t\t$row_stav_skladu = pg_fetch_assoc($data_stav_skladu);\r\n\t\t$index = 1;")]
    [InlineData("<?php\r\nfunction vypsani_obsahu_adresare($id)\r\n  {\r\n    if($handle=opendir(\"../data_\".$id.\"/\"))\r\n      {\r\n        while($file = readdir($handle))\r\n          {\r\n            $polozky[count($polozky)] = $file;\r\n          }\r\n\r\n        closedir($handle);\r\n        sort($polozky);\r\n      }\r\n\r\n    reset($polozky);\r\n\r\n\r\n\r\n    while(list($key, $val) = each($polozky))\r\n      {\r\n        $val_explode = explode(\".\", $val);\r\n\r\n        if(($val != \".\") && ($val != \"..\")&& (!isset($val_explode[1])))\r\n          {\r\n            echo \"<br />(DIR) \";\r\n            echo \" \".$val.\"<br />\\n\";\r\n          }\r\n        elseif(($val != \".\") && ($val != \"..\"))\r\n          {\r\n            echo \"<br />\".$val.\" neni adresar<br />\";\r\n          }\r\n      }\r\n  }\r\n?>")]
    [InlineData("//var_dump($SHOP_MENU);\r\nif($SHOP_MENU != NULL):\r\nreset($SHOP_MENU);\r\n$zobrazovat_menu_ano = 0;\r\nwhile(list($category_id)=each($SHOP_MENU)):\r\n reset($SHOP_MENU[\"$category_id\"]);\t\r\n\r\n$category_id_nad_1 = najdi_v_db(\"shop_category\",\"category_id\",$category_id,\"shop_id_up\");\r\n//$category_id_nad_2 = najdi_v_db(\"shop_category\",\"category_id\",$category_id_nad_1,\"shop_id_up\");\r\n//$category_id_nad_3 = najdi_v_db(\"shop_category\",\"category_id\",$category_id_nad_2,\"shop_id_up\");")]
    [InlineData("function sestav_darky_roll($SETY,$DATA,$PRODUCT){\r\n\t//kategorie_setu\r\n\tif($SETY != NULL):\r\n\t\treset($SETY);\r\n\t\twhile(list($ids)=each($SETY)):\r\n\t\t\treset($SETY[\"$ids\"]);\r\n\t\t\tif($DATA[\"$ids\"] != NULL):\r\n\t\t\t\t//SHOW SKUPINA\r\n\t\t\t\techo '<br />'.LNG_DETAIL_SKUPINA_DAREK.': '.$SETY[\"$ids\"][\"name\"].'<br />';\r\n\t\t\t\techo '<select name=\"darek_roll_'.$SETY[\"$ids\"][\"id\"].'\" id=\"darek_roll_'.$SETY[\"$ids\"][\"id\"].'\" onchange=\"add_darek(\\'darek_roll_'.$SETY[\"$ids\"][\"id\"].'\\',\\'darek_pole\\');\">';\r\n\t\t\t\techo '<option value=\"---------\">---------</option>';//value = 9x - kvuli js kontrole\r\n\t\t\t\twhile(list($idp)=each($DATA[\"$ids\"])):\r\n\t\t\t\t\treset($DATA[\"$ids\"][\"$idp\"]);\r\n\t\t\t\t\t//SHOW DATA DO ROLL MENU\r\n\t\t\t\t\t//echo '<option value=\"'.$DATA[\"$ids\"][\"$idp\"][\"varianta_id\"].'\">'.$DATA[\"$ids\"][\"$idp\"][\"znacka\"].' - '.$DATA[\"$ids\"][\"$idp\"][\"name\"] .' - '. $DATA[\"$ids\"][\"$idp\"][\"varianta_name\"].'</option>';\r\n\t\t\t\t\techo '<option value=\"'.$DATA[\"$ids\"][\"$idp\"][\"varianta_id\"].'\">'.$DATA[\"$ids\"][\"$idp\"][\"name\"].'</option>';\t\t\t\t\t\r\n\t\t\t\t\t//echo '- name: '.$DATA[\"$ids\"][\"$idp\"][\"name\"].' - '.$DATA[\"$ids\"][\"$idp\"][\"varianta_name\"].'<br>';\t\t\t\t\t\r\n\t\t\t\tendwhile;\r\n\t\t\t\techo '</select>';\t\t\t\r\n\t\t\tendif;//pokud skupina neni null\r\n\t\tendwhile;\t\t\t\r\n\tendif;\t\r\n}")]
    [InlineData("/**\r\n     * Constructor\r\n     *\r\n     * @param  array $options Associative array of options\r\n     * @throws Zend_Cache_Exception\r\n     * @return void\r\n     */\r\n    public function __construct(array $options = array())\r\n    {\r\n        while (list($name, $value) = each($options)) {\r\n            $this->setOption($name, $value);\r\n        }\r\n    }\r\n\r\n    /**\r\n     * Set the frontend directives\r\n     *\r\n     * @param  array $directives Assoc of directives\r\n     * @throws Zend_Cache_Exception\r\n     * @return void\r\n     */\r\n    public function setDirectives($directives)\r\n    {\r\n        if (!is_array($directives)) Zend_Cache::throwException('Directives parameter must be an array');\r\n        while (list($name, $value) = each($directives)) {\r\n            if (!is_string($name)) {\r\n                Zend_Cache::throwException(\"Incorrect option name : $name\");\r\n            }\r\n            $name = strtolower($name);\r\n            if (array_key_exists($name, $this->_directives)) {\r\n                $this->_directives[$name] = $value;\r\n            }\r\n\r\n        }\r\n\r\n        $this->_loggerSanity();\r\n    }")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeWhileListEach();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
    }
}

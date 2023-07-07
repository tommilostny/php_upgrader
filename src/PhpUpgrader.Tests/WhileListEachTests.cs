﻿namespace PhpUpgrader.Tests;

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
    [InlineData("  function SetColorDefaults() {\r\n    $this->Debug(\"Sparkline :: SetColorDefaults()\", DEBUG_SET);\r\n    $colorDefaults = array(array('aqua',   '#00FFFF'),\r\n                           array('black',  '#010101'), // TODO failure if 000000?\r\n                           array('blue',   '#0000FF'),\r\n                           array('fuscia', '#FF00FF'),\r\n                           array('gray',   '#808080'),\r\n                           array('grey',   '#808080'),\r\n                           array('green',  '#008000'),\r\n                           array('lime',   '#00FF00'),\r\n                           array('maroon', '#800000'),\r\n                           array('navy',   '#000080'),\r\n                           array('olive',  '#808000'),\r\n                           array('purple', '#800080'),\r\n                           array('red',    '#FF0000'),\r\n                           array('silver', '#C0C0C0'),\r\n                           array('teal',   '#008080'),\r\n                           array('white',  '#FFFFFF'),\r\n                           array('yellow', '#FFFF00'));\r\n    while (list(, $v) = each($colorDefaults)) {\r\n      if (!array_key_exists($v[0], $this->colorList)) {\r\n        $this->SetColorHtml($v[0], $v[1]);\r\n      }\r\n    }\r\n  }")]
    [InlineData("    public static function getAllCapabilities(TeraWurfl $wurflObj)\r\n    {\r\n\r\n        foreach ($wurflObj->capabilities as $group) {\r\n            if (!is_array($group)) {\r\n                continue;\r\n            }\r\n            while (list ($key, $value) = each($group)) {\r\n                if (is_bool($value)) {\r\n                    // to have the same type than the official WURFL API\r\n                    $features[$key] = ($value ? 'true' : 'false');\r\n                } else {\r\n                    $features[$key] = $value;\r\n                }\r\n            }\r\n        }\r\n        return $features;\r\n    }")]
    [InlineData("\t<?php reset($PRODUCT_TOP9);while(list($idp)=each($PRODUCT_TOP9)):reset($PRODUCT_TOP9[\"$idp\"]);//START PRODUCT opakovaní !!! NEMENIT !!!?>  \r\n    \t<a href=\"<?php echo $PRODUCT_TOP9[\"$idp\"][\"url\"];?>\">\r\n            <div class=\"blok_produkt\">\r\n                <div class=\"blok_produkt_in\">\r\n                    <div class=\"blok_produkt_pozadi\">\r\n                        <div class=\"nej_obal_foto_vypis\">\r\n                            <?php\r\n                              $odsazeni = 0;\r\n                              if ($PRODUCT_TOP9[\"$idp\"][\"ks_skladem\"] > 0) :\r\n                            ?>\r\n                                <div class=\"akce bg_zelena <?php echo \"t\".$odsazeni; ?>\">\r\n                                  <span>skladem</span>\r\n                                </div>\r\n                            <?php\r\n                                $odsazeni += 28;\r\n                              endif\r\n                            ?>")]
    [InlineData("$products = array();\r\n\t\t\t\t  if($USER_KOSIK_POLOZKY != NULL):\r\n\t\t\t\t  \treset($USER_KOSIK_POLOZKY);\r\n\t\t\t\t\twhile(list($data_id)=each($USER_KOSIK_POLOZKY)):\r\n\t\t\t\t\t\treset($USER_KOSIK_POLOZKY[\"$data_id\"]);\r\n\t\t\t\t\t\t$products[] =$USER_KOSIK_POLOZKY[\"$data_id\"][\"product_id\"];\r\n\t\t\t\t\tendwhile;\r\n\t\t\t\t  endif;\r\n\t\t\t\t  $doprava_zdarma = \"0\";\r\n\t\t\t\t  $language_id = najdi_v_db(\"shop\",\"shop_id\",$DOMAIN_ID,\"default_language_id\");\r\n\t\t\t\t  $dop_zdarma_q = \"SELECT doprava_zdarma FROM product_info WHERE product_id IN (\".implode(\",\",$products).\") AND language_id = \".$language_id.\"\";\r\n\t\t\t\t  $dop_zdarma_d = pg_query($dop_zdarma_q);\r\n\t\t\t\t  while($dop_zdarma_row = pg_fetch_assoc($dop_zdarma_d)) {\r\n\t\t\t\t  \tif ($dop_zdarma_row['doprava_zdarma'] == 1) {\r\n\t\t\t\t\t\t$doprava_zdarma = \"1\";\r\n\t\t\t\t\t}\r\n\t\t\t\t  }\r\n\t\t\t\t  $doprava_zdarma = ( round($USER_KOSIK['db_max_cena_s_dph']) >= ($promena_postovne_zdarma_od/$kurz)) ? 1 : 0;\r\n\t\t\t\t  $doprava_cena_q = \"SELECT * FROM doprava WHERE hmotnost_min <= \".$USER_KOSIK['hmotnost'].\" AND hmotnost_max > \".$USER_KOSIK['hmotnost'].\"\";\r\n\t\t\t\t  $doprava_cena_d = pg_query($doprava_cena_q);\r\n\t\t\t\t  $DOPRAVA_HM = array();\r\n\t\t\t\t  while($doprava_cena_r = pg_fetch_assoc($doprava_cena_d)) {\r\n\t\t\t\t\t  if ($doprava_zdarma == 1 && $doprava_cena_r['doprava_id'] != 2) {\r\n\t\t\t\t\t\t  $DOPRAVA_HM[$doprava_cena_r['doprava_id']] = array(\r\n\t\t\t\t\t\t\t'poradi' => $DOPRAVA[$doprava_cena_r['doprava_id']]['poradi'],\r\n\t\t\t\t\t\t\t'id' => $doprava_cena_r['doprava_id'],\r\n\t\t\t\t\t\t\t'name' => $doprava_cena_r['dopravce_nazev'],\r\n\t\t\t\t\t\t\t'cena' => str_replace(\",\",\".\",0),\r\n\t\t\t\t\t\t\t'platba' => $DOPRAVA[$doprava_cena_r['doprava_id']]['platba']\r\n\t\t\t\t\t\t  );\r\n\t\t\t\t\t  } else {\r\n\t\t\t\t\t\t  $DOPRAVA_HM[$doprava_cena_r['doprava_id']] = array(\r\n\t\t\t\t\t\t\t'poradi' => $DOPRAVA[$doprava_cena_r['doprava_id']]['poradi'],\r\n\t\t\t\t\t\t\t'id' => $doprava_cena_r['doprava_id'],\r\n\t\t\t\t\t\t\t'name' => $doprava_cena_r['dopravce_nazev'],\r\n\t\t\t\t\t\t\t'cena' => str_replace(\",\",\".\",$doprava_cena_r['cena']),\r\n\t\t\t\t\t\t\t'platba' => $DOPRAVA[$doprava_cena_r['doprava_id']]['platba']\r\n\t\t\t\t\t\t  );\r\n\t\t\t\t\t  }\r\n\t\t\t\t  }")]
    [InlineData("<?php\r\n$d_kosik_produkty = array();\r\nif ($USER_KOSIK_POLOZKY != NULL) :\r\n    reset($USER_KOSIK_POLOZKY);\r\n    while (list($data_id) = each($USER_KOSIK_POLOZKY)) :\r\n        reset($USER_KOSIK_POLOZKY[\"$data_id\"]);\r\n        array_push($d_kosik_produkty, array(\r\n            \"code\" => $USER_KOSIK_POLOZKY[\"$data_id\"][\"ean\"],\r\n            \"name\" => $USER_KOSIK_POLOZKY[\"$data_id\"][\"product_name\"],\r\n            \"totalPrice\" => array(\r\n                \"amount\" => str_replace('.','',$USER_KOSIK_POLOZKY[\"$data_id\"][\"cena_s_dph_all\"]) * 100,\r\n                \"currency\" => \"CZK\"\r\n            ),\r\n            \"totalVat\" => array(\r\n                \"amount\" => ($USER_KOSIK_POLOZKY[\"$data_id\"][\"db_cena_s_dph_all\"] - $USER_KOSIK_POLOZKY[\"$data_id\"][\"db_cena_bez_dph\"]) * 100,\r\n                \"currency\" => \"CZK\",\r\n                \"vatRate\" =>  $USER_KOSIK_POLOZKY[\"$data_id\"][\"cena_dph\"]\r\n            ),\r\n        ));\r\n    endwhile;\r\nendif;\r\n\r\n?>")]
    [InlineData("if ($data != 'none') {\r\n\t\t\tif ($data) {\r\n\t\t\t\treset($data);\r\n\t\t\t\twhile (list($key)=each($data)) {\r\n\t\t\t\t\tif ($data[$key]->data) {\r\n\t\t\t\t\t\t//echo  $data[$key]->v_name.'<br>';\r\n\t\t\t\t\t\treset($data[$key]->data);\r\n\t\t\t\t\t\twhile (list($key_data)=each($data[$key]->data)) {\r\n\t\t\t\t\t\t\t//echo \"t:\".$data[$key]->data[$key_data]->term_name.'<br>';\r\n\t\t\t\t\t\t\t$string .= $data[$key]->data[$key_data]->term_name.',';\r\n\t\t\t\t\t\t}\r\n\t\t\t\t\t}\r\n\t\t\t\t}\r\n\t\t\t}\r\n\t\t}")]
    [InlineData("if($zdroj == \"shop\") {\r\n\tif($SHOP_MENU != NULL):\r\n\treset($SHOP_MENU);\r\n\t$SHOP_SUB_MENU = $SHOP_MENU;\r\n\t$sl=0;\r\n\t$rad=0;\r\n\twhile(list($category_id)=each($SHOP_MENU)):\t\t\t\t\r\n\t\treset($SHOP_MENU[\"$category_id\"]);\r\n\t\tif($SHOP_MENU[\"$category_id\"][\"home_aktiv\"] == \"yes\")://menu s odkazem or $SHOP_MENU[\"$category_id\"][\"home_click_category\"] == \"yes\"\r\n\t\t\t if (preg_match(\"~vánoční~\",mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\")) || preg_match(\"~akční~\",mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\")) || preg_match(\"~výprodej~\",mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\"))) {\t\t\t \r\n\t\t\t\t++$sl;\r\n\t\t\t\techo'<div class=\"sloupec_vypis_kat\">';\r\n\t\t\t\t\techo'<div class=\"polozka_vypis_kat hlavni_kategorie_vypis_kat\" id=\"polozka_vypis_kat_'.$rad.'_'.$sl.'\">';\r\n\t\t\t\t\t\tif($left_menu_is == \"producer\") {\r\n\t\t\t\t\t\t\techo'<h2 class=\"h2_vypis_kategorie\">'.ucfirst(mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\")).' '.$vypis_table[\"nazev1\"].'</h2>';\r\n\t\t\t\t\t\t} else {\t\t\t\t\t\t\t\t\r\n\t\t\t\t\t\t\techo'<h2 class=\"h2_vypis_kategorie\"><a href=\"'.$SHOP_MENU[\"$category_id\"][\"url\"].'\">'.ucfirst(mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\")).'</a></h2>';\r\n\t\t\t\t\t\t}\r\n\t\t\t\t\t\techo'<div class=\"obal_img_vypis_kategorie\">';\r\n\t\t\t\t\t\t\t//$product_obr = najdi_v_db(\"shop_category\",\"shop_id_up\",$category_id,\"category_id\");\r\n\t\t\t\t\t\t\t$category_query = \"SELECT * FROM shop_category WHERE category_primary != 1 AND home = 1 AND shop_id = \".$DOMAIN_ID.\" AND shop_id_up = \".$category_id.\" AND category_id IN (SELECT category_id FROM shop_category WHERE category_id IN (SELECT category_id FROM product_category) ) ORDER BY shop_pozice ASC\";\r\n\t\t\t\t\t\t\t$category_data = pg_query($category_query);\r\n\t\t\t\t\t\t\t$row_category = pg_fetch_assoc($category_data);\r\n\t\t\t\t\t\t\t$product_obr = $row_category['category_id'];\r\n\t\t\t\t\t\t\t$query_s_obr = \"SELECT product_id FROM product_category WHERE category_id = '\".$product_obr.\"'\r\n\t\t\t\t\t\t\t\t\t\t\tand product_id != 199669181\r\n\t\t\t\t\t\t\t\t\t\t\tORDER BY product_id ASC\";\t\t\t\r\n\t\t\t\t\t\t\t$category_s_obr = pg_query($query_s_obr);\r\n\t\t\t\t\t\t\t$row_s_obr = pg_fetch_assoc($category_s_obr);\r\n\t\t\t\t\t\t\t$product_obr = $row_s_obr['product_id'];\r\n\t\t\t\t\t\t\t//$product_obr = najdi_v_db(\"product_media\",\"product_id\",$product_obr,\"media_id\");\r\n\t\t\t\t\t\t\t$query_product_obr = \"SELECT media_id FROM product_media WHERE product_id ='\".$product_obr.\"'\";\r\n\t\t\t\t\t\t\t$data_product_obr = pg_query($query_product_obr);\r\n\t\t\t\t\t\t\t $img_url = najdi_v_db(\"shop_category\", \"category_id\" , $category_id, \"img_url\");\r\n\r\n\t\t\t\t ?>\r\n\t\t\t\t <input type=\"hidden\" name=\"dddaa\" value=\"<?php echo $category_id;?>\">\r\n\t\t\t\t <?php\r\n\t\t\t\t\t\t\t if(trim($img_url) != '') {\r\n\t\t\t\t\t\t\t\t echo '<a href=\"'.$SHOP_MENU[\"$category_id\"][\"url\"].'\"><img src=\"/'.$img_url.'\" alt=\"'.mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\").'\" title=\"\"/></a>';\r\n\r\n\t\t\t\t\t\t\t }\r\n\t\t\t\t\t\t\t else {\r\n\t\t\t\t\t\t\twhile($row_product_obr = pg_fetch_assoc($data_product_obr)) {\r\n\t\t\t\t\t\t\t\t$product_obr = $row_product_obr['media_id'];\r\n\t\t\t\t\t\t\t\t if ($product_obr <> \"\") {\r\n\t\t\t\t\t\t\t\t\techo'<a href=\"'.$SHOP_MENU[\"$category_id\"][\"url\"].'\"><img src=\"'.getmediaurl($product_obr,'vestavnespotrebicedarek').'\" alt=\"'.mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\").'\" title=\"\"/></a>';\r\n\t\t\t\t\t\t\t\t\tbreak;\r\n\t\t\t\t\t\t\t\t}\r\n\t\t\t\t\t\t\t}\r\n\t\t\t\t\t\t\tif ($product_obr == \"\") {\r\n\t\t\t\t\t\t\t\techo'<img src=\"/images/web/no.gif\" alt=\"'.mb_strtolower($SHOP_MENU[\"$category_id\"][\"name\"],\"utf-8\").'\" title=\"\"/>';\r\n\t\t\t\t\t\t\t}\r\n\t\t\t\t\t\t\t }\r\n\t\t\t\t\t\techo'</div>';\t\t\t\t\t\t\r\n\t\t\t\t\t\t\t\r\n\t\t\t\t\t\techo' <div class=\"obal_txt_vypis_kategorie\">';\r\n\t\t\t\t\t\t\treset($SHOP_SUB_MENU);\r\n\t\t\t\t\t\t\t$pocet_podmenu = 0;\r\n\t\t\t\t\t\t\twhile(list($sub_category_id)=each($SHOP_SUB_MENU)):\r\n\t\t\t\t\t\t\t\treset($SHOP_SUB_MENU[\"$sub_category_id\"]);\r\n\t\t\t\t\t\t\t\tif (!($SHOP_SUB_MENU[\"$sub_category_id\"][\"shop_id_up\"])) {\r\n\t\t\t\t\t\t\t\t\t$SHOP_SUB_MENU[\"$sub_category_id\"][\"shop_id_up\"] = najdi_v_db(\"shop_category\",\"category_id\",$sub_category_id,\"shop_id_up\");\r\n\t\t\t\t\t\t\t\t}\r\n\t\t\t\t\t\t\t\tif($SHOP_SUB_MENU[\"$sub_category_id\"][\"shop_id_up\"] == $category_id) {\r\n\t\t\t\t\t\t\t\t\t++$pocet_podmenu;\r\n\t\t\t\t\t\t\t\t\t//if ($left_menu_is != \"producer\") {\r\n\t\t\t\t\t\t\t\t\t\tif ($pocet_podmenu < 5) {\r\n\t\t\t\t\t\t\t\t\t\t\techo'<a href=\"'.$SHOP_SUB_MENU[\"$sub_category_id\"][\"url\"].'\"><span class=\"odkaz_vypis_kategorie\">'.$SHOP_SUB_MENU[\"$sub_category_id\"][\"name\"].'</span></a>';\r\n\t\t\t\t\t\t\t\t\t\t} else {\r\n\t\t\t\t\t\t\t\t\t\t\techo '<div class=\"obal_zobrazit_vypis\">';\r\n\t\t\t\t\t\t\t\t\t\t\techo'<a href=\"'.$SHOP_MENU[\"$category_id\"][\"url\"].'\"><span class=\"odkaz_vypis_kategorie\">ZOBRAZIT VŠECHNY</span></a>';\r\n\t\t\t\t\t\t\t\t\t\t\techo '</div>';\r\n\t\t\t\t\t\t\t\t\t\t}\r\n\t\t\t\t\t\t\t\t\t/*} else {\r\n\t\t\t\t\t\t\t\t\t\techo'<a href=\"'.$SHOP_SUB_MENU[\"$sub_category_id\"][\"url\"].'\"><span class=\"odkaz_vypis_kategorie\">'.$SHOP_SUB_MENU[\"$sub_category_id\"][\"name\"].'</span></a>';\r\n\t\t\t\t\t\t\t\t\t}*/\r\n\t\t\t\t\t\t\t\t}\r\n\t\t\t\t\t\t\tendwhile;\r\n\t\t\t\t\t\techo' </div>';\r\n\t\t\t\t\t echo'</div>';\r\n\t\t\t\t echo'</div>';\r\n\t\t\t\t if ($sl % 4 == 0) {\t\t\t\t\t \r\n\t\t\t\t\t ?>\r\n\t\t\t\t\t <script language=\"javascript\">\r\n\t\t\t\t\t\tvyska_menu(<?PHP echo $rad; ?>);\r\n\t\t\t\t\t </script>\r\n                     <?PHP\r\n\t\t\t\t\t ++$rad;\r\n\t\t\t\t\t $sl = 0;\r\n\t\t\t\t }\r\n\t\t\t } \r\n\t\tendif;\r\n\tendwhile;")]
    [InlineData("      <div class=\"card_1_price\"><?php echo $CARD_TEXT['4'];?></div>\r\n      <div class=\"card_1_quantity\"><?php echo $CARD_TEXT['5'];?></div>\r\n      <div class=\"card_1_price_total\"><?php echo $CARD_TEXT['6'];?></div>\r\n      <div class=\"card_1_btn_recount\"></div>\r\n      <div class=\"card_1_btn_remove\"></div>\r\n    </div>\r\n    <div class=\"card_1_line\"></div>\r\n      <?php if($USER_KOSIK_POLOZKY != NULL):reset($USER_KOSIK_POLOZKY);while(list($data_id)=each($USER_KOSIK_POLOZKY)):reset($USER_KOSIK_POLOZKY[\"$data_id\"]);//START SHOP_TOP_MENU opakovaní !!! NEMENIT !!!?>\r\n\r\n        <div class=\"card_1_in_product\">\r\n          <?php\r\n          //dodatecne data\r\n\r\n          $nazev_zbozi = $USER_KOSIK_POLOZKY[\"$data_id\"][\"druh_zbozi\"].\" \".$USER_KOSIK_POLOZKY[\"$data_id\"][\"product_name\"].\" - vel.: \".$USER_KOSIK_POLOZKY[\"$data_id\"][\"varianta\"];\r\n          $query_s_name2 = \"SELECT * FROM product_info WHERE product_id = '\".$USER_KOSIK_POLOZKY[\"$data_id\"][\"product_id\"].\"' AND language_id = '\".LANG_ID.\"' \";\r\n          $s_name2 = pg_query($query_s_name2);\r\n          $row_s_name2 = pg_fetch_assoc($s_name2);\r\n          $totalRows_s_name2 = pg_num_rows($s_name2);\r\n\r\n          $max_query = \"SELECT units FROM store_central WHERE product_id \t = '\".$USER_KOSIK_POLOZKY[\"$data_id\"][\"product_id\"].\"' AND product_spec_id = '\".$USER_KOSIK_POLOZKY[\"$data_id\"][\"product_spec_id\"].\"'\";\r\n\r\n          $max_data = pg_query($max_query);\r\n          $max_row = pg_fetch_assoc($max_data);\r\n          $stav_skladu_js = $max_row['units'];\r\n\r\n          //\r\n          unset($NAZEV_PRODUKUTU);\r\n          $NAZEV_PRODUKUTU = $USER_KOSIK_POLOZKY[\"$data_id\"][\"product_name\"];\r\n          /*\r\n          if ($row_s_name2['nazev2']<>\"\") {\r\n            $NAZEV_PRODUKUTU = $NAZEV_PRODUKUTU.\" \".$row_s_name2['nazev2'];\r\n          }\r\n          if (($row_s_name2['zaruka']<>\"\")and($row_s_name2['zaruka']>\"2\")) {\r\n             if(($row_s_name2['zaruka'] == 2) or ($row_s_name2['zaruka'] == 3) or ($row_s_name2['zaruka'] == 4)){\r\n                     $zaruka_let = 'roky';\r\n             }elseif ($row_s_name2['zaruka'] == 50){\r\n                     $zaruka_let = 'měsíců';\r\n             }else{\r\n                     $zaruka_let = 'let';\r\n             }\r\n             $NAZEV_PRODUKUTU = $NAZEV_PRODUKUTU.\" + Záruka \".$row_s_name2['zaruka'].\" \".$zaruka_let;\r\n          }\r\n          if ($row_s_name2['doprava']==0) {\r\n            $NAZEV_PRODUKUTU = $NAZEV_PRODUKUTU.\" + Doprava zdarma!\";\r\n          }\r\n          */\r\n          ?>\r\n          <?php\r\n\r\n          $file_exists = explode(\"/\",getmediaurl($USER_KOSIK_POLOZKY[\"$data_id\"][\"media_id\"],'konf_mini'));\r\n          $file_exists = './media/images/'.substr($file_exists[4],4,9).'.jpg';\r\n          if (file_exists($file_exists)) { ?>\r\n            <div class=\"card_1_img\">\r\n              <a href=\"<?php echo $USER_KOSIK_POLOZKY[\"$data_id\"][\"url\"];?>\" target=\"_blank\"><img src=\"<?php echo getmediaurl($USER_KOSIK_POLOZKY[\"$data_id\"][\"media_id\"],\"konf_mini\");?>\" alt=\"<?php echo $USER_KOSIK_POLOZKY[\"$data_id\"][\"product_name\"];?>\" /></a>\r\n            </div>\r\n          <?php } else { ?>\r\n            <div class=\"card_1_img\">\r\n              <img src=\"/images/web/no.gif\" alt=\"<?php echo $USER_KOSIK_POLOZKY[\"$data_id\"][\"product_name\"];?>\" />\r\n            </div>;\r\n            <?php\r\n          }\r\n          ?>\r\n\r\n          <!-- product text -->\r\n          <div class=\"card_1_name\">\r\n            <?php echo $USER_KOSIK_POLOZKY[\"$data_id\"][\"druh_zbozi\"];?> <?php echo $NAZEV_PRODUKUTU;\r\n            if ($USER_KOSIK_POLOZKY[\"$data_id\"][\"varianta\"]!=\"\") {\r\n              echo \" - vel.: \".$USER_KOSIK_POLOZKY[\"$data_id\"][\"varianta\"];\r\n            }\r\n\r\n            ?>\r\n          </div>\r\n            <form id=\"card_form_update\" name=\"card_form_update\" method=\"post\" action=\"\" onsubmit=\"add_to_cart('<?= $USER_KOSIK_POLOZKY[\"$data_id\"][\"id\"];?>', '<?= $USER_KOSIK_POLOZKY[\"$data_id\"][\"product_name\"];?>', '<?= $USER_KOSIK_POLOZKY[\"$data_id\"][\"znacka\"];?>', '<?= round($USER_KOSIK_POLOZKY[\"$data_id\"][\"db_cena_bez_dph\"], 2);?>', '<?= $USER_KOSIK_POLOZKY[\"$data_id\"][\"ks\"];?>', $('#card_form_update_ks').val());\">\r\n              <input type=\"hidden\" name=\"card_form_update_id\" id=\"card_form_update_id\" value=\"<?php echo $USER_KOSIK_POLOZKY[\"$data_id\"][\"id\"];?>\"/>\r\n              <input type=\"hidden\" name=\"card_funkce\" id=\"card_funkce\" value=\"update\" />\r\n              <input type=\"hidden\" name=\"js_history\" id=\"js_history\" value=\"<?php echo $js_history;?>\" />\r\n\r\n              <!-- cena kus s DPH-->\r\n              <div class=\"card_1_price\">\r\n                <?php echo strtr($USER_KOSIK_POLOZKY[\"$data_id\"][\"cena_s_dph\"],'.',' ');?> <?php echo LNG_GLOBAL_02_MENA ?>\r\n              </div>\r\n\r\n              <!-- množství -->\r\n              <div class=\"card_1_quantity\">\r\n                <input type=\"text\" name=\"card_form_update_ks\" id=\"card_form_update_ks\" size=\"3\" value=\"<?php echo $USER_KOSIK_POLOZKY[\"$data_id\"][\"ks\"];?>\" onchange=\"buy_onchange(this,'<?php echo $stav_skladu_js;?>','<?php echo $nazev_zbozi;?>')\" onload=\"buy_onchange(this,'<?php echo $stav_skladu_js;?>','<?php echo $nazev_zbozi;?>')\" <?php echo $INPUT_ONLY_NUMBER;?>/>\r\n              </div>\r\n\r\n              <!-- cena celkem s DPH-->\r\n              <div class=\"card_1_price_total\">\r\n                <?php echo strtr($USER_KOSIK_POLOZKY[\"$data_id\"][\"cena_s_dph_all\"],'.',' ');?> <?php echo LNG_GLOBAL_02_MENA ?>\r\n              </div>\r\n\r\n              <!-- přepočítat -->\r\n              <input type=\"submit\" name=\"card_form_update_submit\" id=\"card_form_update_submit\" onmouseover=\"btn_change_refresh_over(this);\" onmouseout=\"btn_change_refresh_out(this);\" value=\"<?php echo $CARD_TEXT['10'];?>\" class=\"card_form_update_submit card_1_btn_recount\"/>\r\n              <input type=\"hidden\" name=\"stav_skladu_js\" id=\"stav_skladu_js\" value=\"<?php echo $stav_skladu_js;?>\" />\r\n            </form>\r\n\r\n            <form class=\"card_1_btn_remove\" id=\"card_form_delete\" name=\"card_form_delete\" method=\"post\" action=\"\">\r\n              <input type=\"hidden\" name=\"card_form_delete_id\" id=\"card_form_delete_id\" value=\"<?php echo $USER_KOSIK_POLOZKY[\"$data_id\"][\"id\"];?>\" />\r\n              <input type=\"hidden\" name=\"card_funkce\" id=\"card_funkce\" value=\"delete\" />\r\n              <input type=\"hidden\" name=\"js_history\" id=\"js_history\" value=\"<?php echo $js_history;?>\" />\r\n              <input type=\"submit\" name=\"card_form_delete_submit\" id=\"card_form_delete_submit\" onmouseover=\"btn_change_delete_over(this);\" onmouseout=\"btn_change_delete_out(this);\" value=\"<?php echo $CARD_TEXT['11'];?>\" class=\"card_form_delete_submit\" />\r\n            </form>\r\n\r\n\r\n        </div>\r\n        <div class=\"card_1_line\"></div>\r\n\r\n\r\n      <?php endwhile;endif;//END SHOP_TOP_MENU opakovaní !!! NEMENIT !!!?>\r\n    <div class=\"card_1_in_result\">\r\n      <!-- součty -->\r\n      <div class=\"card_1_name\">\r\n        <?php echo $CARD_TEXT['7'];?>:&nbsp;\r\n      </div>\r\n\r\n      <!-- množství -->\r\n      <?php /*\r\n      <div>\r\n         //echo $USER_KOSIK['max_ks'];?> <?php //echo LNG_GLOBAL_01_KS ?>&nbsp;\r\n      </div> */?>\r\n\r\n      <!-- peníze -->")]
    [InlineData("function cmp($a, $b) {\r\n \treturn substr($a[\"hodnota\"],3) - substr($b[\"hodnota\"],3);\r\n\t//return $a[\"hodnota\"] - $b[\"hodnota\"];\r\n}\r\nreset($FILTRACE_GLOBAL);\r\n$k_count = 1;\r\nwhile(list($kategorie)=each($FILTRACE_GLOBAL)){\r\n\t//-----------\r\n\t//HEAD\r\n\techo '<span id=\"zalozka_'.$k_count.'\" class=\"zCSS zalozka'.$zac.' '.$zflc.'\" '.$zspecStyle.' data-id=\"'.$k_count.'\" data-fid=\"\">'.$kategorie.'</span>';\r\n\t\r\n\t//-----------\r\n\t//Sort znacky\r\n\tusort($FILTRACE_GLOBAL[$kategorie], \"cmp\");\r\n\t//special CSS\r\n\tunset($ozhc);\r\n\tif($k_count != 1)$ozhc = 'hide';//active (show/hide)\r\n//\t$ozhc = 'hide';//hide all\r\n\t$ozhc = '';//hide all\r\n\tif($k_count == 1)$ozhc = '';//1 polozka vyditelna\r\n\t//\r\n\techo '<div id=\"obsah_zalozka_'.$k_count.'\" class=\"obsah_zalozky '.$ozhc.'\">';\r\n\t//------------------\r\n\t//content\r\n\tunset($FD);\r\n\t$FD = $FILTRACE_GLOBAL[$kategorie];//predani dat\r\n\t//PRUCHOD pro CSS\r\n\tunset($special_css_span);\r\n\t$hodnota_sirka = 0;\r\n\twhile(list($id_h)=each($FD)){\r\n\t\tif(strlen($FD[$id_h]['hodnota']) > $hodnota_sirka)\r\n\t\t\t$hodnota_sirka = strlen($FD[$id_h]['hodnota']);\r\n\t}\t\r\n\treset($FD);\r\n\t//\r\n\t//-------------------------\r\n\twhile(list($id_h)=each($FD)){\r\n\t\t//-------------------\r\n\t\t//DATA element\r\n\t\tunset($data_element);\r\n\t\twhile(list($id_p)=each($FD[$id_h]['products'])){\r\n\t\t\t$data_element .= $id_p.'#';\r\n\t\t}\r\n\t\t//-------------------\r\n\t\t//SPECIAL CSS DIV HODNOTY\r\n\t\t$special_css_span = 'style=\"width:'.($hodnota_sirka*8).'px;\"';\t\r\n\t\t$special_css_span = '';\t\r\n\t\t//\r\n\t\techo '<div id=\"filtr_'.$FD[$id_h]['hodnota_id'].'\" class=\"fl data-zalozka-'.$k_count.'\" data-zalozka=\"'.$k_count.'\" data-from=\"'.$FD[$id_h]['from'].'\" data-products=\"'.$data_element.'\">';\r\n\t\techo '<input type=\"checkbox\" name=\"f_checkbox\" value=\"'.$FD[$id_h]['hodnota'].'\" id=\"check_'.$FD[$id_h]['hodnota_id'].'\" data-id=\"'.$FD[$id_h]['hodnota_id'].'\" data-from=\"'.$FD[$id_h]['from'].'\" data-nid=\"'.$FD[$id_h]['value_id'].'\">';\r\n\t\techo '<span id=\"span_'.$FD[$id_h]['hodnota_id'].'\" data-id=\"'.$FD[$id_h]['hodnota_id'].'\" data-from=\"'.$FD[$id_h]['from'].'\" data-nid=\"'.$FD[$id_h]['value_id'].'\" '.$special_css_span.'>'.substr($FD[$id_h]['hodnota'],3).'</span>';\r\n\t\t//echo '<span id=\"span_'.$FD[$id_h]['hodnota_id'].'\" data-id=\"'.$FD[$id_h]['hodnota_id'].'\" data-from=\"'.$FD[$id_h]['from'].'\" data-nid=\"'.$FD[$id_h]['value_id'].'\" '.$special_css_span.'>'.$FD[$id_h]['hodnota'].'</span>';\r\n\t\techo '</div>';\r\n\t}\r\n    //------------------\r\n\techo '</div>';\r\n\t$k_count++;")]
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

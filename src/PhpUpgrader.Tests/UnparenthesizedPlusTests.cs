namespace PhpUpgrader.Tests;

public class UnparenthesizedPlusTests : UnitTestWithOutputBase
{
    public UnparenthesizedPlusTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("if ($ord_10 == \"Okamžitá sleva\") {\r\n\t$ord_data = pg_query(\"SELECT * FROM orders_data_online WHERE order_id = '\".$ord_1.\"' AND product_id = '\".$USER_KOSIK_POLOZKY[\"$data_id\"][\"darek_k\"].\"'\");\r\n\t$ord_d = pg_fetch_assoc($ord_data);\r\n\t$sql_dotaz = \"UPDATE orders_data_online SET price_bez_dph = '\".$ord_d['price_bez_dph']+$ord_7.\"', price_s_dph = '\".$ord_d['price_s_dph']+$ord_8.\"', price_bez_dph_all = '\".$ord_d['price_bez_dph_all']+$ord_12.\"', price_s_dph_all = '\".$ord_d['price_s_dph_all']+$ord_13.\"' WHERE order_id = '\".$ord_1.\"' AND product_id = '\".$USER_KOSIK_POLOZKY[\"$data_id\"][\"darek_k\"].\"'\";\r\n} else {\r\n\t$sql_dotaz = \"INSERT INTO orders_data_online VALUES ('$ord_1','$ord_2','$ord_3','$ord_4','$ord_5','$ord_6','$ord_7','$ord_8','$ord_9','$ord_10','$ord_11','$ord_12','$ord_13','$ord_14','$ord_15','$ord_16','$ord_17','$ord_18')\";\r\n}")]
    public void UpdatesStringConcatWithPlus_AddsParentheses(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeUnparenthesizedPlus();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
    }

    [Theory]
    [InlineData("for ($pocet_a = 1; $pocet_a <= 191; ++$poceta) {\r\n                $polozka = \"polozka\".$pocet_a;\r\n                $$polozka = $data[$pocet_a+17];\r\n        }\r\n        fputs($fp, $product_id.\"\\n\");")]
    [InlineData("if ($b->selectCommandPrint()) {\r\n                        echo'<fieldset',($_GET[\"modify\"]?'':' class=\"jsonly\"'),'><legend>Změnit</legend><div>\r\n<input type=\"submit\" value=\"Uložit\"',($_GET[\"modify\"]?'':' title=\"'.'Ctrl+klikněte na políčko, které chcete změnit.'.'\"'),'>\r\n</div></fieldset>\r\n<fieldset><legend>Označené <span id=\"selected\"></span></legend><div>\r\n<input type=\"submit\" name=\"edit\" value=\"Upravit\">\r\n<input type=\"submit\" name=\"clone\" value=\"Klonovat\">\r\n<input type=\"submit\" name=\"delete\" value=\"Smazat\">',confirm(),'</div></fieldset>\r\n';\r\n                    }$hd=$b->dumpFormat();\r\n                    foreach ((array)$_GET[\"columns\"]as$e) {\r\n                        if ($e[\"fun\"]) {\r\n                            unset($hd['sql']);\r\n                            break;\r\n                        }")]
    public void IgnoresNotValid(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeUnparenthesizedPlus();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.False(file.IsModified);
    }
}

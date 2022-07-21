namespace PhpUpgrader.Tests;

public class UpgradeMysqlResultTests : UnitTestWithOutputBase
{
    public UpgradeMysqlResultTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("if ($loginFoundUser) {\r\n    \r\n    $loginStrGroup  = mysql_result($LoginRS,0,'valid');\r\n\t$loginUserid  = mysql_result($LoginRS,0,'user_id');\r\n    \r\n    //declare 4 session variables and assign them\r\n    $_SESSION['MM_Username'] = $loginUsername;\r\n\t$_SESSION['MM_Userpass'] = $password;\r\n\t$_SESSION['MM_Userid'] = $loginUserid;\r\n    $_SESSION['MM_UserGroup'] = $loginStrGroup;\t")]
    [InlineData("// nacteni existencni promenne textu k hlavni odkazu jestli existuje a ma se zobrazovat\r\n      $pocet_text_all = mysql_result(mysql_query(\"SELECT COUNT(*) FROM \".$_SESSION['db_nazev_table'].\"_text_all where jazyk = \".$_SESSION['session_jazykova_mutace'].\" and zobrazit = '1'\"), 0);\r\n      if($pocet_text_all > 0) { $text_all_exist = 1; }\r\n      else{ $text_all_exist = 0; }")]
    [InlineData("$pocet_text_all = mysql_result(mysql_query(\"SELECT COUNT(*)")]
    [InlineData("$pocet_text_all = mysql_result(mysql_query(\"SELECT COUNT(*)blaCOUNT(*)\nCOUNT(*)\nmysql_result(mysql_query(\"SELECT COUNT(*)blaCOUNT(*)blaCOUNT(*)")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper(Path.Join("test-site", "funkce", "secure", "login.php"), content);

        //Act
        MonaUpgrader.UpgradeMysqlResult(file);

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine(content);
        _output.WriteLine("================================================");
        _output.WriteLine(updatedContent);
        Assert.True(file.IsModified);
        Assert.NotEqual(content, updatedContent);
        Assert.DoesNotContain("mysql_result", updatedContent);
        Assert.Empty(file.Warnings);
    }
}

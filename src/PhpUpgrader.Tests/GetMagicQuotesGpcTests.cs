namespace PhpUpgrader.Tests;

public class GetMagicQuotesGpcTests : UnitTestWithOutputBase
{
    public GetMagicQuotesGpcTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("$LoginRS__query=sprintf(\"SELECT login, pass, aktiv, login_id FROM dodavatel_login WHERE login='%s' AND pass='%s'\",\r\n  get_magic_quotes_gpc() ? $loginUsername : addslashes($loginUsername), get_magic_quotes_gpc() ? $password : addslashes($password)); \r\n   \r\n  $LoginRS = pg_query($LoginRS__query);\r\n  $loginFoundUser = pg_num_rows($LoginRS);")]
    [InlineData("global $sportmall_import;\r\n\r\n  $theValue = get_magic_quotes_gpc() ? stripslashes($theValue) : $theValue;\r\n\r\n  $theValue = mysqli_real_escape_string($sportmall_import, $theValue);\r\n\r\n  switch ($theType) {")]
    [InlineData("{\r\n  $theValue = (!get_magic_quotes_gpc()) ? addslashes($theValue) : $theValue;\r\n\r\n  switch ($theType) {")]
    [InlineData("if ( ( !is_string($value) && !is_numeric($value) ) || !is_string($key) )\r\n\t\t\tcontinue;\r\n\r\n\t\tif ( get_magic_quotes_gpc() )\r\n\t\t\t$value = htmlspecialchars( stripslashes((string)$value) );\r\n\t\telse\r\n\t\t\t$value = htmlspecialchars( (string)$value );\r\n?>")]
    [InlineData("<?php\r\n      if((isset($_REQUEST['search'])) && ($_REQUEST['search'] != \"\"))\r\n        {\r\n          $hledany_retezec = (get_magic_quotes_gpc()) ? $_POST['search'] : addslashes($_POST['search']);\r\n          $zaznam_exist = 0;\r\n     ?>   \r\n          <h1><?php echo $hledat_vysledek_vyrazu; ?> \"<?php echo $hledany_retezec; ?>\"</h1>\r\n              <img src=\"<?php echo $cesta_vystup; ?>img/line.gif\" alt=\"line\" class=\"line\" />\r\n          <p>&nbsp;</p>\r\n          <div class=\"spacer\"></div><br />")]
    [InlineData("if ( ( !is_string($value) && !is_numeric($value) ) || !is_string($key) )\r\n\t\t\tcontinue;\r\n\r\n\t\tif ( get_magic_quotes_gpc() )\r\n\t\t{\r\n\t\t\t$value = htmlspecialchars( stripslashes((string)$value) );\r\n\t\t}\r\n\t\telse\r\n\t\t{\r\n\t\t\t$value = htmlspecialchars( (string)$value );\r\n\t\t}\r\n?>")]
    [InlineData(", get_magic_quotes_gpc() ? /*$password : addslashes($password));*/ $hashed_pwd : addslashes($hashed_pwd)); ")]
    [InlineData("function remove_slashes($ng, $Tc=false)\r\n{\r\n    if (get_magic_quotes_gpc()) {\r\n        while (list($z, $X)=each($ng)) {\r\n            foreach ($X\r\n            as$be=>$W) {\r\n                unset($ng[$z][$be]);\r\n                if (is_array($W)) {\r\n                    $ng[$z][stripslashes($be)]=$W;\r\n                    $ng[]=&$ng[$z][stripslashes($be)];\r\n                } else {\r\n                    $ng[$z][stripslashes($be)]=($Tc?$W:stripslashes($W));\r\n                }\r\n            }\r\n        }\r\n    }\r\n}")]
    [InlineData("function remove_slashes($ng, $Tc=false)\r\n{\r\n    if (!get_magic_quotes_gpc()) {\r\n        while (list($z, $X)=each($ng)) {\r\n            foreach ($X\r\n            as$be=>$W) {\r\n                unset($ng[$z][$be]);\r\n                if (is_array($W)) {\r\n                    $ng[$z][stripslashes($be)]=$W;\r\n                    $ng[]=&$ng[$z][stripslashes($be)];\r\n                } else {\r\n                    $ng[$z][stripslashes($be)]=($Tc?$W:stripslashes($W));\r\n                }\r\n            }\r\n        }\r\n    }\r\n}")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("somefile.php", content);

        //Act
        file.UpgradeGetMagicQuotesGpc();

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine(content);
        _output.WriteLine("================================================");
        _output.WriteLine(updatedContent);
        Assert.Empty(file.Warnings);
        Assert.True(file.IsModified);
        Assert.NotEqual(content, updatedContent);
        Assert.Matches(@"/\*.{0,6}get_magic_quotes_gpc\(\)", updatedContent);
    }

    [Fact]
    public void ReportsUnknownVariant()
    {
        //Arrange
        var content = "<?php /* some random php code with */ get_magic_quotes_gpc(); ?>";
        var file = new FileWrapper("somefile.php", content);

        //Act
        file.UpgradeGetMagicQuotesGpc();

        //Assert
        _output.WriteLine(file.Warnings.FirstOrDefault());
        Assert.NotEmpty(file.Warnings);
        Assert.False(file.IsModified);
        Assert.Equal(content, file.Content.ToString());
    }

    [Theory]
    [InlineData("$LoginRS__query=sprintf(\"SELECT login, pass, aktiv, login_id FROM dodavatel_login WHERE login='%s' AND pass='%s'\",\r\n  /*get_magic_quotes_gpc() ? $loginUsername :*/ addslashes($loginUsername), /*get_magic_quotes_gpc() ? $password :*/ addslashes($password)); \r\n   \r\n  $LoginRS = pg_query($LoginRS__query);\r\n  $loginFoundUser = pg_num_rows($LoginRS);")]
    [InlineData("{\r\n  $theValue = /*(!get_magic_quotes_gpc()) ?*/  addslashes($theValue)  /*: $theValue*/;\r\n\r\n  switch ($theType) {")]
    [InlineData("<?php\r\n      if((isset($_REQUEST['search'])) && ($_REQUEST['search'] != \"\"))\r\n        {\r\n          $hledany_retezec = /*(get_magic_quotes_gpc()) ? $_POST['search'] :*/ addslashes($_POST['search']);\r\n          $zaznam_exist = 0;\r\n     ?>   \r\n          <h1><?php echo $hledat_vysledek_vyrazu; ?> \"<?php echo $hledany_retezec; ?>\"</h1>\r\n              <img src=\"<?php echo $cesta_vystup; ?>img/line.gif\" alt=\"line\" class=\"line\" />\r\n          <p>&nbsp;</p>\r\n          <div class=\"spacer\"></div><br />")]
    [InlineData("global $sportmall_import;\r\n\r\n  $theValue = /*get_magic_quotes_gpc() ? stripslashes($theValue) :*/ $theValue;\r\n\r\n  $theValue = mysqli_real_escape_string($sportmall_import, $theValue);\r\n\r\n  switch ($theType) {")]
    [InlineData("if ( ( !is_string($value) && !is_numeric($value) ) || !is_string($key) )\r\n\t\t\tcontinue;\r\n\r\n\t\t/*if ( get_magic_quotes_gpc() )\r\n\t\t\t$value = htmlspecialchars( stripslashes((string)$value) );\r\n\t\telse*/\r\n\t\t\t$value = htmlspecialchars( (string)$value );\r\n?>")]
    [InlineData("if ( ( !is_string($value) && !is_numeric($value) ) || !is_string($key) )\r\n\t\t\tcontinue;\r\n\r\n\t\t/*if ( get_magic_quotes_gpc() )\r\n\t\t{\r\n\t\t\t$value = htmlspecialchars( stripslashes((string)$value) );\r\n\t\t}\r\n\t\telse*/\r\n\t\t{\r\n\t\t\t$value = htmlspecialchars( (string)$value );\r\n\t\t}\r\n?>")]
    [InlineData("<table border=\"1\" cellspacing=\"0\" id=\"outputSample\">\r\n\t\t<colgroup><col width=\"120\"></colgroup>\r\n\t\t<thead>\r\n\t\t\t<tr>\r\n\t\t\t\t<th>Field&nbsp;Name</th>\r\n\t\t\t\t<th>Value</th>\r\n\t\t\t</tr>\r\n\t\t</thead>\r\n<?php\r\n\r\nif (!empty($_POST))\r\n{\r\n\tforeach ( $_POST as $key => $value )\r\n\t{\r\n\t\tif ( ( !is_string($value) && !is_numeric($value) ) || !is_string($key) )\r\n\t\t\tcontinue;")]
    [InlineData("/* get_magic_quotes_gpc() */")]
    public void DoesNotUpgradeUpdatedOrNotContainingFile(string content)
    {
        //Arrange
        var file = new FileWrapper("somefile.php", content);

        //Act
        file.UpgradeGetMagicQuotesGpc();

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine(content);
        _output.WriteLine("================================================");
        _output.WriteLine(updatedContent);
        Assert.Empty(file.Warnings);
        Assert.False(file.IsModified);
        Assert.Equal(content, updatedContent);
    }
}

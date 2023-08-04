namespace PhpUpgrader.Tests;

public sealed class UndefinedConstTests : UnitTestWithOutputBase
{
    public UndefinedConstTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("<?php if ($send == \"yes\") { \r\nfunction check_email($email) {\r\n$atom = '[-a-z0-9!#$%&\\'*+/=?^_`{|}~]'; // znaky tvořící uživatelské jméno\r\n$domain = '[a-z0-9]([-a-z0-9]{0,61}[a-z0-9])'; // jedna komponenta domény\r\nreturn eregi(\"^$atom+(\\\\.$atom+)*@($domain?\\\\.)+$domain\\$\", $email);\r\n}\r\nif (check_email($mail[email]) == 1) {\r\n?>\r\n<?php \r\n$mail[vybava] = $trouba.\", \".$mikrovlnka.\", \".$kavovar.\", \".$pracka.\",<br />\".$deska_plynova.\", \".$deska_sklokeramicka.\", \".$deska_indukcni.\",<br />\".$odsavac_klasicky.\", \".$odsavac_kominovy.\", \".$odsavac_ostruvkovy.\",<br />\".$mycka_60.\", \".$mycka_45.\",<br />\".$chlazeni_lednice.\", \".$chlazeni_mrazak.\", \".$chlazeni_kombinace.\",<br />\".$cena_do20.\", \".$cena_do30.\", \".$cena_do50.\", \".$cena_nad50;\r\n?>\t\r\n\t\r\n\t\r\n\t        <?php\r\n\r\n$mail[telo] = \"Děkujeme za Vaši poptávku, <br /><br />formulář obsahuje tyto položky: <br />\".$mail[vybava].\"\\n\\n<br /> e-mail: \".$mail[email].\"\\n\\n\";\r\n$mail[wadresa] = \"formular.set@vestavne-spotrebice.cz, spotrebice@mcrai.eu, $mail[email]\";\r\n$mail[predmet] = \"poptavka na set - vestavne-spotrebice.cz\";\r\n\r\n$mail[odesilatel]  = \"formular.set@vestavne-spotrebice.cz\";\r\n\r\n    $hlavicka = \"From: $mail[odesilatel]\\r\\n\";\r\n    $hlavicka.= \"Reply-To: $mail[odesilatel]\\r\\n\";\r\n\t$hlavicka.=\"Content-Type: text/html; charset=utf-8\\n\";\t\r\n    $hlavicka.= \"X-Mailer: PHP\";\r\n\r\n    mail (\"$mail[wadresa]\", \"$mail[predmet]\", \"$mail[telo]\", \"$hlavicka\") \r\n    or die (\"error\"); \r\n\t\t?>\r\n<div align=\"center\">\r\n        <?php\r\n    echo \"<br /><br /><br /> <h1>Vaše poptávka na set byla odeslána.</h1> <br /><br /><br /><br />\";\r\n\t?>\r\n</div>\r\n<?php } else { echo \"<div align=\\\"center\\\"><br /><br /><br /> <h1>E-mail je nutné korektně vyplnit!</h1> <br /><a href=\\\"https://$domena\\\">opravit</a><br /><br /><br /></div>\"; } ?>\r\n<?php } ?>")]
    public void UpdatesStringConcatWithPlus_AddsParentheses(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeUndefinedConstAccess();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
    }

    [Theory]
    [InlineData("<?php include('a_safe.php'); ?>\r\n<?php\r\n\r\n$pole = $_REQUEST;//poslane xml data\r\n//print_r ($pole);\r\n//echo \"<br />_<br /><br />\";\r\n//echo $_FILES[\"jmeno_souboru_h_foto\"][\"tmp_name\"].\"<br /><br />\";\r\n\r\n// odstraneni pripadnych apostrofu\r\n$pole['seo_title'] = str_replace(\"'\", \"\", $pole['seo_title']);\r\n$pole['seo_description'] = str_replace(\"'\", \"\", $pole['seo_description']);\r\n$pole['seo_keywords'] = str_replace(\"'\", \"\", $pole['seo_keywords']);\r\n$pole['zkraceny_vypis'] = str_replace(\"'\", \"\", $pole['zkraceny_vypis']);\r\n$pole['text'] = str_replace(\"'\", \"\", $pole['text']);\r\n$pole['product_name'] = str_replace(\"'\", \"\", $pole['product_name']);\r\n  \r\n$cz_osetreni = array(\"(\" => \"\", \")\" => \"\", \"-\" => \"\", \"ě\" => \"e\", \"š\" => \"s\", \"č\" => \"c\", \"ř\"=>\"r\",\"ž\"=>\"z\",\"ý\"=>\"y\",\"á\"=>\"a\",\"í\"=>\"i\",\"é\"=>\"e\",\"ú\"=>\"u\",\"ů\"=>\"u\",\"Ě\" => \"e\", \"Š\" => \"s\", \"Č\" => \"c\", \"Ř\"=>\"r\",\"Ž\"=>\"z\",\"Ý\"=>\"y\",\"Á\"=>\"a\",\"Í\"=>\"i\",\"É\"=>\"e\",\"Ú\"=>\"u\",\"Ů\"=>\"u\",\"ą\" => \"a\",\"ć\" => \"c\",\"ę\" => \"e\",\"ł\" => \"l\",\"ń\" => \"n\",\"ó\" => \"o\",\"ś\" => \"s\",\"ź\" => \"z\",\"ż\" => \"z\",\"Ą\" => \"a\",\"Ć\" => \"c\",\"Ę\" => \"e\",\"Ł\" => \"l\",\"Ń\" => \"n\",\"Ó\" => \"o\",\"Ś\" => \"s\",\"Ź\" => \"z\",\"Ż\" => \"z\",\"á\" => \"a\",\"ä\" => \"a\",\"č\" => \"c\",\"ď\" => \"d\",\"dž\" => \"dz\",\"é\" => \"e\",\"í\" => \"i\",\"ľ\" => \"l\",\" ĺ \" => \"l\",\"ň\" => \"n\",\"ó\" => \"o\",\"ô\" => \"o\",\"ŕ\" => \"r\",\"š\" => \"s\",\"ť\" => \"t\",\"ú\" => \"u\",\"ý\" => \"y\",\"ž \" => \"z\",\"Á\" => \"a\",\"Ä\" => \"a\",\"Č\" => \"c\",\"Ď\" => \"d\",\"DŽ\" => \"dz\",\"É\" => \"e\",\"Í\" => \"i\",\"Ľ\" => \"l\",\" Ĺ \" => \"l\",\"Ň\" => \"n\",\"Ó\" => \"o\",\"Ô\" => \"o\",\"Ŕ\" => \"r\",\"Š\" => \"s\",\"Ť\" => \"t\",\"Ú\" => \"u\",\"Ý\" => \"y\",\"Ž \" => \"z\",\"*\" => \"\",\"!\" => \"\",\"\\\"\" => \"\",\" \" => \"_\",\"/\" => \"\");\r\n\r\n\t//media\r\n\tif ($_FILES[\"jmeno_souboru_h_foto\"][\"tmp_name\"]<>\"\") {\r\n\t\t$s_nazev \t\t= $pole['product_name'];\r\n\t\t$s_p_nazev \t\t= $pole['product_name'];\r\n\t\t\r\n\t\t$maxDimW = 1000;\r\n\t\t$maxDimH = 1000;\r\n\t\tlist($width, $height, $type, $attr) = getimagesize( $_FILES[\"jmeno_souboru_h_foto\"][\"tmp_name\"] );\r\n\t\tif ( $width > $maxDimW || $height > $maxDimH ) {")]
    public void DoesNotUpdateStringConcatWithPlus_WhenAlreadyParentheses(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeUndefinedConstAccess();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.False(file.IsModified);
    }
}

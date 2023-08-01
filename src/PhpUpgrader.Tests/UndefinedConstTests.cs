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
}

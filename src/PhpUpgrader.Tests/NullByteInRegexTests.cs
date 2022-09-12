namespace PhpUpgrader.Tests;

public class NullByteInRegexTests : UnitTestWithOutputBase
{
    public NullByteInRegexTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void DoesNotUpgradeInvalidFile()
    {
        //Arrange
        var content = "if (!function_exists(\"ctype_alnum\")) {\r\n   function ctype_alnum($text) {\r\n      return preg_match(\"/^[A-Za-z\\d\\300-\\377]+$/\", $text);\r\n   }\r\n   function ctype_alpha($text) {\r\n      return preg_match(\"/^[a-zA-Z\\300-\\377]+$/\", $text);\r\n   }\r\n   function ctype_digit($text) {\r\n      return preg_match(\"/^\\d+$/\", $text);\r\n   }\r\n   function ctype_xdigit($text) {\r\n      return preg_match(\"/^[a-fA-F0-9]+$/\", $text);\r\n   }\r\n   function ctype_cntrl($text) {\r\n      return preg_match(\"/^[\\000-\\037]+$/\", $text);\r\n   }\r\n   function ctype_space($text) {\r\n      return preg_match(\"/^\\s+$/\", $text);\r\n   }\r\n   function ctype_upper($text) {\r\n      return preg_match(\"/^[A-Z\\300-\\337]+$/\", $text);\r\n   }\r\n   function ctype_lower($text) {\r\n      return preg_match(\"/^[a-z\\340-\\377]+$/\", $text);\r\n   }\r\n   function ctype_graph($text) {\r\n      return preg_match(\"/^[\\041-\\176\\241-\\377]+$/\", $text);\r\n   }\r\n   function ctype_punct($text) {\r\n      return preg_match(\"/^[^0-9A-Za-z\\000-\\040\\177-\\240\\300-\\377]+$/\", $text);\r\n   }\r\n   function ctype_print($text) {\r\n      return ctype_punct($text) && ctype_graph($text);\r\n   }\r\n}";
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeNullByteInRegex();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
        Assert.NotEqual(content, updated);
    }
}

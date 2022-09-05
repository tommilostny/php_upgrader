using PhpUpgrader.Mona.UpgradeExtensions;

namespace PhpUpgrader.Tests;

public class CurlyBraceIndexingTests : UnitTestWithOutputBase
{
    public CurlyBraceIndexingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("\t\t\t\t\tif ($sublen == 6) {\r\n\t\t\t\t\t\t$t = bcmul(''.ord($code{0}), '1099511627776');\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{1}), '4294967296'));\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{2}), '16777216'));\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{3}), '65536'));\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{4}), '256'));\r\n\t\t\t\t\t\t$t = bcadd($t, ''.ord($code{5}));\r\n\t\t\t\t\t\tdo {\r\n\t\t\t\t\t\t\t$d = bcmod($t, '900');\r\n\t\t\t\t\t\t\t$t = bcdiv($t, '900');\r\n\t\t\t\t\t\t\tarray_unshift($cw, $d);\r\n\t\t\t\t\t\t} while ($t != '0');\r\n\t\t\t\t\t} else {\r\n\t\t\t\t\t\tfor ($i = 0; $i < $sublen; ++$i) {\r\n\t\t\t\t\t\t\t$cw[] = ord($code{$i});\r\n\t\t\t\t\t\t}\r\n\t\t\t\t\t}\r\n\t\t\t\t\t$code = $rest;")]
    [InlineData("for ($s = 0; $s < $chrlen; $s++){\r\n\t\t\t\t$seq .= $chr[$char_bar]{$s} . $chr[$char_space]{$s};\r\n\t\t\t}\r\n\t\t\t$seqlen = strlen($seq);")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeCurlyBraceIndexing();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
    }
}

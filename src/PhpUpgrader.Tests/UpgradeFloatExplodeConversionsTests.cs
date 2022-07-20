namespace PhpUpgrader.Tests;

public class UpgradeFloatExplodeConversionsTests : UnitTestWithOutputBase
{
    public UpgradeFloatExplodeConversionsTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void UpgradesValidFile()
    {
        //Arrange
        const string content = "}\r\n\t$az = $od + $vypsano - 1;\r\n\t$stranka_end = $stranka_pocet / 10;\r\n\t$stranka_end = explode(\".\", $stranka_end);\r\n\t$stranka_end = $stranka_end[0];\r\n\t\t$stranka_end = $stranka_end * 10 + 10;\r\n\r\n\tif($stranka_pocet <> 0)";
        var file = new FileWrapper("some-file.php", content);
        _output.WriteLine(file.Content.ToString());

        //Act
        MonaUpgrader.UpgradeFloatExplodeConversions(file);

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine("==================================================");
        _output.WriteLine(updatedContent);
        Assert.True(file.IsModified);
        Assert.NotEqual(content, updatedContent);
        Assert.Contains("(int)($stranka_pocet / 10);", updatedContent);
    }
}

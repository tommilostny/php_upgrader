namespace PhpUpgrader.Tests;

public class UpgradeClanekVypisTests : UnitTestWithOutputBase
{
    public UpgradeClanekVypisTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void UpgradesValidFile()
    {
        //Arrange
        var file = new FileWrapper("system\\clanek.php", "Some other stuff\n\n\t$vypis_table_clanek[\"sdileni_fotogalerii\"]\nHello");

        //Act
        MonaUpgrader.UpgradeClanekVypis(file);

        //Assert
        _output.WriteLine(file.Content.ToString());
        Assert.True(file.IsModified);
    }

    [Theory]
    [InlineData("//Some other stuff:\\n\\n\\t$vypalerii\\\"]\\nHello\"")]
    [InlineData("//Contains adding:\n$p_sf = array();\n\t$vypis_table_clanek[\"sdileni_fotogalerii\"]\nHello")]
    public void DoesNotUpgradeInvalidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("system\\clanek.php", content);

        //Act
        MonaUpgrader.UpgradeClanekVypis(file);

        //Assert
        _output.WriteLine(file.Content.ToString());
        Assert.False(file.IsModified);
    }
}

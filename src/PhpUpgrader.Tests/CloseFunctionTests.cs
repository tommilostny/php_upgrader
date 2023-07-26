namespace PhpUpgrader.Tests;

public class CloseFunctionTests : UnitTestWithOutputBase
{
    public CloseFunctionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void AddsMysqliCloseToMonaIndexFile()
    {
        //Arrange
        var file = new FileWrapper("index.php", "<?php\n\n/* Some PHP code */\n\n?>");

        //Act
        file.UpgradeIndex(new MonaUpgraderFixture());

        //Assert
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
        Assert.EndsWith("<?php mysqli_close($beta); ?>", updated);
    }

    [Fact]
    public void AddsPgCloseToMonaIndexFile()
    {
        //Arrange
        var file = new FileWrapper("index.php", "<?php\n\n/* Some PHP code */\n\n?>");

        //Act
        file.UpgradeIndex(new RubiconUpgrader(null, null));

        //Assert
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
        Assert.EndsWith("<?php pg_close($beta); ?>", updated);
    }
}

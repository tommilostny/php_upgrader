using PhpUpgrader.Mona.UpgradeExtensions;

namespace PhpUpgrader.Tests;

public class RenameBetaUnitTest : UnitTestWithOutputBase
{
    public RenameBetaUnitTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void NotPresetShouldRenameBetaVariable()
    {
        //Arrange
        var content = "mysqli_query($beta, $query);";
        var upgrader = new MonaUpgraderFixture();

        //Act
        content = upgrader.RenameVar(content, "gama");

        //Assert
        var expected = "mysqli_query($gama, $query);";
        _output.WriteLine(expected);
        _output.WriteLine(content);
        Assert.Equal(expected, content);
    }

    [Fact]
    public void PresetShouldRenameBetaVariable()
    {
        //Arrange
        var content = "mysqli_query($beta, $query);";
        var upgrader = new MonaUpgraderFixture { RenameBetaWith = "gama" };

        //Act
        content = upgrader.RenameVar(content);

        //Assert
        var expected = "mysqli_query($gama, $query);";
        _output.WriteLine(expected);
        _output.WriteLine(content);
        Assert.Equal(expected, content);
    }

    [Fact]
    public void UnsetShouldNotRename()
    {
        //Arrange
        var content = "mysqli_query($beta, $query);";
        var upgrader = new MonaUpgraderFixture();

        //Act
        content = upgrader.RenameVar(content);

        //Assert
        Assert.Equal("mysqli_query($beta, $query);", content);
    }

    [Fact]
    public void PresetShouldBeOverridenByParameter()
    {
        //Arrange
        var content = "mysqli_query($beta, $query);";
        var upgrader = new MonaUpgraderFixture { RenameBetaWith = "gama" };

        //Act
        content = upgrader.RenameVar(content, "alfa");

        //Assert
        var expected = "mysqli_query($alfa, $query);";
        _output.WriteLine(expected);
        _output.WriteLine(content);
        Assert.Equal(expected, content);
    }

    [Fact]
    public void SettingShouldRenameBetaInFindReplaceDictionary()
    {
        //Arrange
        var upgrader = new MonaUpgraderFixture();

        //Act, Assert
        var originalCount = upgrader.FindReplaceHandler.Replacements.Count;

        Assert.Contains(upgrader.FindReplaceHandler.Replacements, fr => fr.Key.Contains("beta") || fr.Value.Contains("beta"));
        upgrader.RenameBetaWith = "gama";
        Assert.DoesNotContain(upgrader.FindReplaceHandler.Replacements, fr => fr.Key.Contains("beta") || fr.Value.Contains("beta"));
        Assert.Contains(upgrader.FindReplaceHandler.Replacements, fr => fr.Key.Contains("gama") || fr.Value.Contains("gama"));

        Assert.Equal(originalCount, upgrader.FindReplaceHandler.Replacements.Count);
    }
}

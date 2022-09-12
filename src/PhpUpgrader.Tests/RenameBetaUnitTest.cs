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
    public void Mona_RenameBetaInFindReplace()
    {
        TestRenameInFindReplaceFor(new MonaUpgraderFixture());
    }

    [Fact]
    public void Rubicon_RenameBetaInFindReplace()
    {
        TestRenameInFindReplaceFor(new RubiconUpgrader(null, null));
    }

    private static void TestRenameInFindReplaceFor(MonaUpgrader upgrader)
    {
        //Arrange
        var originalCount = upgrader.FindReplaceHandler.Replacements.Count;
        var unchangedItemsWithIndexes = new Dictionary<int, (string find, string replace)>();
        for (int i = 0; i < upgrader.FindReplaceHandler.Replacements.Count; i++)
        {
            var item = upgrader.FindReplaceHandler.Replacements.ElementAt(i);
            if (!item.find.Contains("beta") && !item.replace.Contains("beta"))
            {
                unchangedItemsWithIndexes[i] = item;
            }
        }

        //Act, Assert
        Assert.Contains(upgrader.FindReplaceHandler.Replacements, fr => fr.find.Contains("beta") || fr.replace.Contains("beta"));
        upgrader.RenameBetaWith = "gama";
        Assert.DoesNotContain(upgrader.FindReplaceHandler.Replacements, fr => fr.find.Contains("beta") || fr.replace.Contains("beta"));
        Assert.Contains(upgrader.FindReplaceHandler.Replacements, fr => fr.find.Contains("gama") || fr.replace.Contains("gama"));

        Assert.Equal(originalCount, upgrader.FindReplaceHandler.Replacements.Count);

        foreach (var item in unchangedItemsWithIndexes) //nezměněné pořadí (nové položky nebyly přidány na konec).
        {
            var updated = upgrader.FindReplaceHandler.Replacements.ElementAt(item.Key);
            Assert.Equal(item.Value, updated);
        }
    }
}

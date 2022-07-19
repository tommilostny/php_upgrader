namespace PhpUpgrader.Tests;

public class StrankovaniUnitTest : UnitTestWithOutputBase
{
    public StrankovaniUnitTest(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)")]
    [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)")]
    [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)")]
    public void ModifiesKnownVariants(string input)
    {
        var file = new FileWrapper(@"test-site\funkce\strankovani.php", input);

        MonaUpgrader.UpgradeStrankovani(file);

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.NotEqual(input, contentStr);
    }

    [Fact]
    public void DoesNotModifyUnknownVariant()
    {
        string input = "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $vz_vypis)";
        var file = new FileWrapper(@"test-site\funkce\strankovani.php", input);

        MonaUpgrader.UpgradeStrankovani(file);

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.False(file.IsModified);
        Assert.Equal(input, contentStr);
    }

    [Fact]
    public void DoesNotModifyNotContainingPredchoziDalsi()
    {
        string input = "mysqli_query($beta, $query);";
        var file = new FileWrapper(@"test-site\funkce\strankovani.php", input);

        MonaUpgrader.UpgradeStrankovani(file);

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.False(file.IsModified);
        Assert.Equal(input, contentStr);
    }

    [Fact]
    public void DoesNotModifyOtherFiles()
    {
        string input = "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)";
        var file = new FileWrapper(@"path-to\admin\table_x_edit.php", input);

        MonaUpgrader.UpgradeStrankovani(file);

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.False(file.IsModified);
        Assert.Equal(input, contentStr);
    }
}

using PhpUpgrader.Mona.UpgradeRoutines;

namespace PhpUpgrader.Tests;

public class EregUnitTest : UnitTestWithOutputBase
{
    public EregUnitTest(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(@"if(ereg('.+@.+\..+', $mail))")]
    [InlineData("ereg($this->_fnmatch2regexp(strtolower($pattern)), strtolower($file));")]
    [InlineData("eregi($this->_fnmatch2regexp(strtolower($pattern)), strtolower($file));")]
    [InlineData(@"ereg_replace(""[\r|\n]\'\""+"",""<br />"",$vypis_gm['text'])")]
    [InlineData(@"ereg_replace ($variable, ""this string"");")]
    [InlineData(@"ereg_replace(""[\r |\n] + "","" < br /> "",$vypis_gm['text']); if(ereg ('.+@.+\..+', 'this string'))")]
    [InlineData(@"ereg (""p\""att'ern"", ""target""); ereg_replace('patt""e\'rn', 'target');")]
    public void PregShouldReplaceEreg(string content)
    {
        var file = new FileWrapper(string.Empty, content);
        _output.WriteLine(content);
        _output.WriteLine(string.Empty);

        file.UpgradeRegexFunctions();

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.DoesNotContain("ereg", contentStr);
    }

    [Fact]
    public void NotContainingEregShoulRemainTheSame()
    {
        var file = new FileWrapper(string.Empty, "mysqli_query($beta, $query);");

        file.UpgradeRegexFunctions();

        Assert.False(file.IsModified);
        Assert.Equal("mysqli_query($beta, $query);", file.Content.ToString());
    }

    [Fact]
    public void ReplacesSplitFunction()
    {
        var file = new FileWrapper(string.Empty, "$var = split('pattern', $query);");

        file.UpgradeRegexFunctions();

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.Equal("$var = preg_split('~pattern~', $query);", contentStr);
    }

    [Fact]
    public void PregSplitShouldRemainTheSame()
    {
        var file = new FileWrapper(string.Empty, "preg_split('~pattern~', $query);");

        file.UpgradeRegexFunctions();

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.False(file.IsModified);
        Assert.Equal("preg_split('~pattern~', $query);", contentStr);
    }

    [Fact]
    public void PregSplitShouldWorkAroundJavascript()
    {
        var file = new FileWrapper(string.Empty,
            "<script language=\"javascript\" type=\"text / javascript\">var split_pomlcky = hodnota_polozky.split(\" - \");\n" +
            "</script> <?php if (ereg('pattern', $blabla))\n" +
            "\t$kill = split('that', $man);");

        file.UpgradeRegexFunctions();

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.Equal("<script language=\"javascript\" type=\"text / javascript\">var split_pomlcky = hodnota_polozky.split(\" - \");\n" +
            "</script> <?php if (preg_match('~pattern~', $blabla))\n" +
            "\t$kill = preg_split('~that~', $man);", contentStr);
    }

    [Fact]
    public void Should_AddIgnoreCase_ForEregi()
    {
        var file = new FileWrapper(string.Empty, @"if(eregi('.+@.+\..+', $mail))");

        file.UpgradeRegexFunctions();

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.Equal(@"if(preg_match('~.+@.+\..+~i', $mail))", contentStr);
    }

    [Theory]
    [InlineData(@"if(ereg('.+~@~.+\..+', $mail))", @"if(preg_match('~.+\~@\~.+\..+~', $mail))")]
    [InlineData(@"if(ereg('~.+~@~.+\..+', $mail))", @"if(preg_match('~\~.+\~@\~.+\..+~', $mail))")]
    public void Should_EscapeDelimiter(string input, string expectedOutput)
    {
        var file = new FileWrapper(string.Empty, input);

        file.UpgradeRegexFunctions();

        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.Equal(expectedOutput, contentStr);
    }
}

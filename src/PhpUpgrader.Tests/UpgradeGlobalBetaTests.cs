namespace PhpUpgrader.Tests;

public class UpgradeGlobalBetaTests : UnitTestWithOutputBase
{
    public UpgradeGlobalBetaTests(ITestOutputHelper output) : base(output)
    {
    }

    private const string _content = "<?php\n\nfunction test()\n{\n\t$data = mysqli_query($beta, ...);\n}\n?>";

    [Fact]
    public void UpgradesValidFile()
    {
        //Arrange
        var file = new FileWrapper("somefile.php", _content);

        //Act
        MonaUpgrader.UpgradeGlobalBeta(file);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.True(file.IsModified);
        Assert.NotEqual(_content, file.Content.ToString());
    }

    [Theory]
    [InlineData("<?php\nif($query_text_all !== FALSE) {\n\twhile($data_stranky_text_all = mysqli_fetch_array($query_text_all))\n}\n?>")]
    [InlineData("<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData("<?php\n\nfunction test() {\n\tglobal $beta;\n\t$data = mysqli_query($beta, ...);\n}\n?>")]
    [InlineData("<?php\n\nfunction test() {\n\t$data = mysqli_query($this->link, ...);\n}\n?>")]
    [InlineData("<?php\n\nfunction test()\n{\n\t$data = other_function($varecka, ...);\n}\n?>")]
    public void DoesNotUpgradeInvalidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("somefile.php", content);

        //Act
        MonaUpgrader.UpgradeGlobalBeta(file);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.False(file.IsModified);
        Assert.Equal(content, file.Content.ToString());
    }
}

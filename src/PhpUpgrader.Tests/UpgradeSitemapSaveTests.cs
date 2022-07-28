using PhpUpgrader.Mona.UpgradeRoutines;

namespace PhpUpgrader.Tests;

public class UpgradeSitemapSaveTests : UnitTestWithOutputBase
{
    public UpgradeSitemapSaveTests(ITestOutputHelper output) : base(output)
    {
    }

    private const string _content = "<?php\n\nwhile($data_stranky_text_all = mysqli_fetch_array($query_text_all))\n\n?>";
    private const string _fileOfInterest = "admin\\sitemap_save.php";

    [Fact]
    public void UpgradesValidFile()
    {
        //Arrange
        var file = new FileWrapper(_fileOfInterest, _content);
        var upgrader = new MonaUpgraderFixture();

        //Act
        file.UpgradeSitemapSave(upgrader.AdminFolders);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.True(file.IsModified);
        Assert.NotEqual(_content, file.Content.ToString());
    }

    [Theory]
    [InlineData(_fileOfInterest, "<?php\nif($query_text_all !== FALSE) {\n\twhile($data_stranky_text_all = mysqli_fetch_array($query_text_all))\n}\n?>")]
    [InlineData("aeiouy\\randomfile.php", _content)]
    [InlineData("aeiouy\\randomfile.php", "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData(_fileOfInterest, "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    public void DoesNotUpgradeInvalidFile(string filePath, string content)
    {
        //Arrange
        var file = new FileWrapper(filePath, content);
        var upgrader = new MonaUpgraderFixture();

        //Act
        file.UpgradeSitemapSave(upgrader.AdminFolders);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.False(file.IsModified);
        Assert.Equal(content, file.Content.ToString());
    }
}

using PhpUpgrader.Mona.UpgradeExtensions;

namespace PhpUpgrader.Tests;

public class UpgradeSitemapSaveTests : UnitTestWithOutputBase
{
    public UpgradeSitemapSaveTests(ITestOutputHelper output) : base(output)
    {
    }

    private const string _upgradableContent = "<?php\n\nwhile($data_stranky_text_all = mysqli_fetch_array($query_text_all))\n\n?>";

    [Fact]
    public void UpgradesValidFile()
    {
        //Arrange
        var file = new FileWrapper(Path.Join("admin", "sitemap_save.php"), _upgradableContent);
        var upgrader = new MonaUpgraderFixture();

        //Act
        file.UpgradeSitemapSave(upgrader.AdminFolders);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.True(file.IsModified);
        Assert.NotEqual(_upgradableContent, file.Content.ToString());
    }

    [Theory]
    [InlineData("admin", "sitemap_save.php", "<?php\nif($query_text_all !== FALSE) {\n\twhile($data_stranky_text_all = mysqli_fetch_array($query_text_all))\n}\n?>")]
    [InlineData("aeiouy", "randomfile.php", _upgradableContent)]
    [InlineData("aeiouy", "randomfile.php", "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData("admin", "sitemap_save.php", "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    public void DoesNotUpgradeInvalidFile(string folder, string fileName, string content)
    {
        //Arrange
        var file = new FileWrapper(Path.Join(folder, fileName), content);
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

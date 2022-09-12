namespace PhpUpgrader.Tests;

public class UpgradeSitemapSaveTests : UnitTestWithOutputBase
{
    public UpgradeSitemapSaveTests(ITestOutputHelper output) : base(output)
    {
    }

    private const string _upgradableContent = "<?php\n\n// TODO vypsani stranek k modulum pokud existuji - start\r\n            $query_text_all = mysqli_query($beta, \"SELECT * FROM table_\".$data_podmenu_all[\"id\"].\"_text_all where url != '' order by id asc \");\r\n\r\n            while($data_stranky_text_all = mysqli_fetch_array($query_text_all))\r\n              {\r\n                $data_stranky_out.=\"<url>\r\n                            <loc>\r\n                              \".$home.\"\".$data_stranky_text_all[\"url\"].\"\r\n                            </loc>\r\n                            <lastmod>\r\n                              \".date(\"Y-m-d\", time()).\"\r\n                            </lastmod>\r\n                            <changefreq>\r\n                              \".$_REQUEST[\"frekvence_podmenu\"].\"\r\n                            </changefreq>\r\n                            <priority>\r\n                              \".$_REQUEST[\"priorita_podmenu\"].\"\r\n                            </priority>\r\n                          </url>\";\r\n                $pocet_polozek_xml++;\r\n              }\r\n\r\n            // TODO vypsani stranek k modulum pokud existuji - end\r\n\r\n            $query_podmenu = mysqli_query($beta, \"SELECT * FROM table_\".$data_podmenu_all[\"id\"].\" order by poradi asc \");\n\n?>";

    [Fact]
    public void UpgradesValidFile()
    {
        //Arrange
        var file = new FileWrapper(Path.Join("admin", "sitemap_save.php"), _upgradableContent);
        var upgrader = new MonaUpgraderFixture();

        //Act
        file.UpgradeSitemapSave(upgrader.AdminFolders);

        //Assert
        _output.WriteLine(_upgradableContent);
        _output.WriteLine("=========================================================");
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

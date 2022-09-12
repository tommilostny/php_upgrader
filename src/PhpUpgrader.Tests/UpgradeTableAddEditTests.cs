namespace PhpUpgrader.Tests;

public class UpgradeTableAddEditTests : UnitTestWithOutputBase
{
    public UpgradeTableAddEditTests(ITestOutputHelper output) : base(output)
    {
    }

    private const string _upgradableContent = "<?php\n\n$pocet_text_all = mysqli_num_rows(...)\n\n?>";

    [Theory]
    [InlineData("admin", "table_x_add.php")]
    [InlineData("admin", "table_x_edit.php")]
    public void UpgradesValidFile(string folder, string fileName)
    {
        //Arrange
        var file = new FileWrapper(Path.Join(folder, fileName), _upgradableContent);
        var upgrader = new MonaUpgraderFixture();

        //Act
        file.UpgradeTableXAddEdit(upgrader.AdminFolders);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.True(file.IsModified);
        Assert.NotEqual(_upgradableContent, file.Content.ToString());
    }

    [Theory]
    [InlineData("admin", "table_x_add.php",  "<?php\n\n@$pocet_text_all = mysqli_num_rows(...)\n\n?>")]
    [InlineData("admin", "table_x_edit.php", "<?php\n\n@$pocet_text_all = mysqli_num_rows(...)\n\n?>")]
    [InlineData("aeiouy", "randomfile.php",  _upgradableContent)]
    [InlineData("aeiouy", "randomfile.php",  "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData("admin", "table_x_add.php", "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData("admin", "table_x_edit.php", "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    public void DoesNotUpgradeInvalidFile(string folder, string fileName, string content)
    {
        //Arrange
        var file = new FileWrapper(Path.Join(folder, fileName), content);
        var upgrader = new MonaUpgraderFixture();

        //Act
        file.UpgradeTableXAddEdit(upgrader.AdminFolders);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.False(file.IsModified);
        Assert.Equal(content, file.Content.ToString());
    }
}

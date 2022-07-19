namespace PhpUpgrader.Tests;

public class UpgradeTableAddEditTests : UnitTestWithOutputBase
{
    public UpgradeTableAddEditTests(ITestOutputHelper output) : base(output)
    {
    }

    private const string _content = "<?php\n\n$pocet_text_all = mysqli_num_rows(...)\n\n?>";
    private const string _fileOfInterest1 = "admin\\table_x_add.php";
    private const string _fileOfInterest2 = "admin\\table_x_edit.php";

    [Theory]
    [InlineData(_fileOfInterest1)]
    [InlineData(_fileOfInterest2)]
    public void UpgradesValidFile(string filePath)
    {
        //Arrange
        var file = new FileWrapper(filePath, _content);
        var upgrader = new MonaUpgraderFixture();

        //Act
        upgrader.UpgradeTableAddEdit(file);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.True(file.IsModified);
        Assert.NotEqual(_content, file.Content.ToString());
    }

    [Theory]
    [InlineData(_fileOfInterest1,  "<?php\n\n@$pocet_text_all = mysqli_num_rows(...)\n\n?>")]
    [InlineData(_fileOfInterest2, "<?php\n\n@$pocet_text_all = mysqli_num_rows(...)\n\n?>")]
    [InlineData("aeiouy\\randomfile.php",  _content)]
    [InlineData("aeiouy\\randomfile.php",  "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData(_fileOfInterest1,  "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData(_fileOfInterest2, "<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    public void DoesNotUpgradeInvalidFile(string filePath, string content)
    {
        //Arrange
        var file = new FileWrapper(filePath, content);
        var upgrader = new MonaUpgraderFixture();

        //Act
        upgrader.UpgradeTableAddEdit(file);

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.False(file.IsModified);
        Assert.Equal(content, file.Content.ToString());
    }
}

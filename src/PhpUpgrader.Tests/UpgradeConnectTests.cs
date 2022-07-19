﻿namespace PhpUpgrader.Tests;

public class UpgradeConnectTests
{
    [Theory]
    [InlineData("connect\\connection.php")]
    [InlineData("system\\connection.php")]
    [InlineData("Connections\\connection.php")]
    [InlineData("abcd\\connect\\connection.php")]
    [InlineData("test-site\\system\\connection.php")]
    [InlineData("C:\\McRAI\\weby\\test-site\\Connections\\connection.php")]
    public void UpgradeConnect_ValidFileTest(string path)
    {
        //Arrange
        var file1 = new FileWrapper(path, string.Empty);
        var file2 = new FileWrapper(path, "<?php echo(\"some random PHP code\"); ?>");
        var upgrader1 = new MonaUpgraderFixture();
        var upgrader2 = new MonaUpgraderFixture(baseFolder: "/McRAI", "test-site");
        upgrader1.ConnectionFile = upgrader2.ConnectionFile = "connection.php";

        //Act & Assert
        /* Gets through the file path check and encounters an exception
         * because there is no ".../important/connection.txt" file or content is empty.
         */
        var exception = Record.Exception(() => upgrader1.UpgradeConnect(file1));
        Assert.NotNull(exception);
        Assert.True(exception is IndexOutOfRangeException);
        Assert.False(file1.IsModified);
        Assert.Equal(string.Empty, file1.Content.ToString());

        file1.Content.Append("<?php echo(\"some random PHP code\"); ?>");
        exception = Record.Exception(() => upgrader1.UpgradeConnect(file1));
        Assert.NotNull(exception);
        Assert.True(exception is DirectoryNotFoundException);

        //upgrader2 has a valid BaseFolder and proceeds to update the file correctly.
        exception = Record.Exception(() => upgrader2.UpgradeConnect(file2));
        Assert.Null(exception);
        Assert.True(file2.IsModified);
        Assert.Contains("$beta = mysqli_connect", file2.Content.ToString());
    }

    [Fact]
    public void UpgradeConnect_InvalidFileTest()
    {
        //Arrange
        var file = new FileWrapper("admin\\index.php", "");
        var upgrader = new MonaUpgraderFixture()
        {
            ConnectionFile = "connection.php"
        };
        //Act & Assert
        var exception = Record.Exception(() => upgrader.UpgradeConnect(file));
        Assert.Null(exception);
    }
}
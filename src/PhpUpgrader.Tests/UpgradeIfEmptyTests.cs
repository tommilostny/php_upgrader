namespace PhpUpgrader.Tests;

public class UpgradeIfEmptyTests : UnitTestWithOutputBase
{
    public UpgradeIfEmptyTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("foreach ($ids as $id) {\r\n\tif ($id != \"\" || $id != null) {\r\n\t\t$query_filter .= $id . \",\";\r\n\t}\r\n}")]
    [InlineData("}\r\n\tif ($query_filter != \"\" || $query_filter != null) {\r\n\t\t$query_filter = substr($query_filter,0,-1);")]
    [InlineData("foreach ($ids as $id) {\r\n\tif ($id != \"\" || $id != NULL) {\r\n\t\t$query_filter .= $id . \",\";\r\n\t}\r\n}")]
    [InlineData("}\r\n\tif ($query_filter != \"\" || $query_filter != NULL) {\r\n\t\t$query_filter = substr($query_filter,0,-1);")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("some-file.php", content);
        _output.WriteLine(file.Content.ToString());

        //Act
        RubiconUpgrader.UpgradeIfEmpty(file);

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine("==================================================");
        _output.WriteLine(updatedContent);
        Assert.True(file.IsModified);
        Assert.NotEqual(content, updatedContent);
        Assert.Contains("!empty($", updatedContent);
    }

    [Theory]
    [InlineData("foreach ($ids as $id) {\r\n\tif ($id != \"\" || $query_filter != null) {\r\n\t\t$query_filter .= $id . \",\";\r\n\t}\r\n}")]
    [InlineData("}\r\n\tif ($query_filter != \"\" || $id != null) {\r\n\t\t$query_filter = substr($query_filter,0,-1);")]
    [InlineData("<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    public void DoesNotUpgradeInvalidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("some-file.php", content);
        _output.WriteLine(file.Content.ToString());

        //Act
        RubiconUpgrader.UpgradeIfEmpty(file);

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine("==================================================");
        _output.WriteLine(updatedContent);
        Assert.False(file.IsModified);
        Assert.Equal(content, updatedContent);
        Assert.DoesNotContain("!empty($", updatedContent);
    }
}

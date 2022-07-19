namespace PhpUpgrader.Tests;

public class TrailingWhitespaceTests : UnitTestWithOutputBase
{
    public TrailingWhitespaceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("<?php\necho(\"some random PHP code\");\n ?>  \t  \n")]
    [InlineData("<?php\necho(\"some random PHP code\");\n ?>  \n \n \t  \n \n\"<?php echo(\\\"some random PHP code\\\"); ?>  \t  \n \n  \n")]
    public void TrailingWhitespaceTest_IsRemoved(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        MonaUpgrader.RemoveTrailingWhitespaceFromEndOfFile(file);

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine("'" + updatedContent + "'");
        Assert.True(file.IsModified);
        Assert.False(updatedContent.EndsWith('\n'));
        Assert.NotEqual(updatedContent, content);
    }
}

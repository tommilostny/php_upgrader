using Xunit;
using Xunit.Abstractions;

namespace PhpUpgrader.Tests
{
    public class EregUnitTest
    {
        private readonly ITestOutputHelper _output;

        public EregUnitTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"if(ereg('.+@.+\..+', $mail))")]
        [InlineData("ereg($this->_fnmatch2regexp(strtolower($pattern)), strtolower($file));")]
        [InlineData(@"ereg_replace(""[\r|\n]\'\""+"",""<br />"",$vypis_gm['text'])")]
        [InlineData(@"ereg_replace ($variable, ""this string"");")]
        [InlineData(@"ereg_replace(""[\r |\n] + "","" < br /> "",$vypis_gm['text']); if(ereg ('.+@.+\..+', 'this string'))")]
        [InlineData(@"ereg (""p\""att'ern"", ""target""); ereg_replace('patt""e\'rn', 'target');")]
        public void PregShouldReplaceEreg(string content)
        {
            var file = new FileWrapper(string.Empty, content);
            _output.WriteLine(content);
            _output.WriteLine(string.Empty);

            MonaUpgrader.UpgradeRegexFunctions(file);

            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
            Assert.DoesNotContain("ereg", file.Content);
        }

        [Fact]
        public void NotContainingEregShoulRemainTheSame()
        {
            var file = new FileWrapper(string.Empty, "mysqli_query($beta, $query);");

            MonaUpgrader.UpgradeRegexFunctions(file);

            Assert.False(file.IsModified);
            Assert.Equal("mysqli_query($beta, $query);", file.Content);
        }

        [Fact]
        public void ReplacesSplitFunction()
        {
            var file = new FileWrapper(string.Empty, "$var = split('pattern', $query);");

            MonaUpgrader.UpgradeRegexFunctions(file);

            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
            Assert.Equal("$var = preg_split('~pattern~', $query);", file.Content);
        }

        [Fact]
        public void PregSplitShouldRemainTheSame()
        {
            var file = new FileWrapper(string.Empty, "preg_split('~pattern~', $query);");

            MonaUpgrader.UpgradeRegexFunctions(file);

            _output.WriteLine(file.Content);
            Assert.False(file.IsModified);
            Assert.Equal("preg_split('~pattern~', $query);", file.Content);
        }

        [Fact]
        public void PregSplitShouldNotReplaceInJavascript()
        {
            var file = new FileWrapper(string.Empty, "<script language=\"javascript\" type=\"text / javascript\">var split_pomlcky = hodnota_polozky.split(\" - \");");
            
            MonaUpgrader.UpgradeRegexFunctions(file);
            
            _output.WriteLine(file.Content);
            Assert.False(file.IsModified);
            Assert.Equal("<script language=\"javascript\" type=\"text / javascript\">var split_pomlcky = hodnota_polozky.split(\" - \");", file.Content);
        }
    }
}

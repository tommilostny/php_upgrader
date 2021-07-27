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
            _output.WriteLine(content);
            _output.WriteLine(string.Empty);

            MonaUpgrader.UpgradeRegexFunctions(ref content);

            _output.WriteLine(content);
            Assert.DoesNotContain("ereg", content);
        }

        [Fact]
        public void NotContainingEregShoulRemainTheSame()
        {
            var content = "mysqli_query($beta, $query);";

            MonaUpgrader.UpgradeRegexFunctions(ref content);
            
            Assert.Equal("mysqli_query($beta, $query);", content);
        }

        [Fact]
        public void ReplacesSplitFunction()
        {
            var content = "$var = split('pattern', $query);";

            MonaUpgrader.UpgradeRegexFunctions(ref content);

            _output.WriteLine(content);
            Assert.Equal("$var = preg_split('~pattern~', $query);", content);
        }

        [Fact]
        public void PregSplitShouldRemainTheSame()
        {
            var content = "preg_split('~pattern~', $query);";

            MonaUpgrader.UpgradeRegexFunctions(ref content);

            _output.WriteLine(content);
            Assert.Equal("preg_split('~pattern~', $query);", content);
        }

        [Fact]
        public void PregSplitShouldNotReplaceInJavascript()
        {
            var content = "<script language=\"javascript\" type=\"text / javascript\">var split_pomlcky = hodnota_polozky.split(\" - \");";

            MonaUpgrader.UpgradeRegexFunctions(ref content);

            _output.WriteLine(content);
            Assert.Equal("<script language=\"javascript\" type=\"text / javascript\">var split_pomlcky = hodnota_polozky.split(\" - \");", content);
        }
    }
}

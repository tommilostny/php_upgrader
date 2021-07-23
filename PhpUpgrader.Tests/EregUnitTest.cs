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

            MonaUpgrader.UpgradeEreg(ref content);

            _output.WriteLine(content);
            Assert.DoesNotContain("ereg", content);
        }

        [Fact]
        public void NotContainingEregShoulRemainTheSame()
        {
            var content = "mysqli_query($beta, $query);";
            var check = content;

            MonaUpgrader.UpgradeEreg(ref content);
            Assert.Equal(check, content);
        }
    }
}

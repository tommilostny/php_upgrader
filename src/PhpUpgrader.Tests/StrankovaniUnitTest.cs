using Xunit;
using Xunit.Abstractions;

namespace PhpUpgrader.Tests
{
    public class StrankovaniUnitTest
    {
        private readonly ITestOutputHelper _output;

        public StrankovaniUnitTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)")]
        [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)")]
        [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)")]
        public void ModifiesKnownVariants(string input)
        {
            var file = new FileWrapper(@"test-site\funkce\strankovani.php", input);

            MonaUpgrader.UpgradeStrankovani(file);

            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
            Assert.NotEqual(input, file.Content);
        }

        [Fact]
        public void DoesNotModifyUnknownVariant()
        {
            string input = "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $vz_vypis)";
            var file = new FileWrapper(@"test-site\funkce\strankovani.php", input);

            MonaUpgrader.UpgradeStrankovani(file);

            _output.WriteLine(file.Content);
            Assert.False(file.IsModified);
            Assert.Equal(input, file.Content);
        }

        [Fact]
        public void DoesNotModifyNotContainingPredchoziDalsi()
        {
            string input = "mysqli_query($beta, $query);";
            var file = new FileWrapper(@"test-site\funkce\strankovani.php", input);

            MonaUpgrader.UpgradeStrankovani(file);

            _output.WriteLine(file.Content);
            Assert.False(file.IsModified);
            Assert.Equal(input, file.Content);
        }

        [Fact]
        public void DoesNotModifyOtherFiles()
        {
            string input = "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)";
            var file = new FileWrapper(@"path-to\admin\table_x_edit.php", input);

            MonaUpgrader.UpgradeStrankovani(file);

            _output.WriteLine(file.Content);
            Assert.False(file.IsModified);
            Assert.Equal(input, file.Content);

        }
    }
}

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
        [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)")]
        [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)")]
        [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)")]
        [InlineData("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $vz_vypis)")]
        public void PredchoziDalsiDebugUpdateTest(string input)
        {
            MonaUpgrader.UpgradeStrankovani("test-site\\funkce\\strankovani.php", ref input);
            _output.WriteLine(input);
        }
    }
}

using Xunit;
using Xunit.Abstractions;

namespace PhpUpgrader.Tests
{
    public class RubiconUpgraderTests
    {
        private readonly ITestOutputHelper _output;

        public RubiconUpgraderTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ConstructorUpgradeTest()
        {
            var file = new FileWrapper("",
                "<?php\necho \"Nějaká blbost před třídou...\"\n\n" +
                "class NejakaMojeTrida\n{\n" +
                "    private function blaBla() { ... }\n\n" +
                "    public function __construct($foo, $bar = null)\n" +
                "    {\n" +
                "        echo \"new class constructor\\n\";\n" +
                "    }\n\n" +
                "    protected function necoDelam() { ... }\n" +
                "}\n\necho \"Nějaká blbost za třídou...\"\n\n" +
                "class JinaTrida extends NejakaMojeTrida\n{\n" +
                "    private function blaBla() { ... }\n\n" +
                "    public function JinaTrida($foo, $bar, $baz = 20)\n" +
                "    {\n" +
                "        echo \"Old Class constructor\\n\";\n" +
                "        $this->shit = \"works\";\n" +
                "    }\n\n" +
                "    protected function necoDelam() { ... }\n" +
                "}\n\necho \"Nějaká blbost za třídou...\"\n?>");

            var upgrader = new RubiconUpgrader(string.Empty, string.Empty);

            upgrader.UpgradeConstructors(file);

            _output.WriteLine($"'{file.Content}'");
            Assert.True(file.IsModified);
            Assert.Contains("function __construct", file.Content);
        }

        [Fact]
        public void RubiconImportTest()
        {
            var file = new FileWrapper("", "");

            var upgrader = new RubiconUpgrader(string.Empty, string.Empty)
            { 
                ConnectionFile = "connect.php",
                RenameBetaWith = "alfa"
            };

            upgrader.UpgradeRubiconImport(file);
        }
    }
}

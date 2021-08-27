using System.IO;
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
            //Arrange
            var file = new FileWrapper("", //File.ReadAllText(@"C:\McRAI\weby\olejemaziva-2\admin\_tiny_mce\plugins\imagemanager\classes\Authenticators\BaseAuthenticator.php"));
                                           //File.ReadAllText(@"C:\McRAI\weby\olejemaziva-2 - Copy\admin\_tiny_mce\plugins\filemanager\classes\FileSystems\LocalFileImpl.php"));
                                           //File.ReadAllText(@"C:\McRAI\weby\olejemaziva-2 - Copy\admin\_tiny_mce\plugins\imagemanager\classes\ManagerEngine.php"));
            "<?php\necho \"Nějaká blbost před třídou... obsahuje slovíčko class hhahahahahha\";\n\n" +
            "class NejakaMojeTrida\n{\n" +
            "    private function blaBla() { /* Tělíčko */ }\n\n" +
            "    public function NejakaMojeTrida($foo, $bar = null)\n" +
            "    {\n" +
            "        echo \"new class constructor\\n\";\n" +
            "    }\n\n" +
            //"    public function NejakaMojeTrida($foo, $bar, $baz = 20)\n    { self::__construct($foo, $bar, $baz); }\n\n" +
            "    protected function necoDelam() { /* Tělo jiné funkce */ }\n" +
            "}\n\necho \"Nějaká blbost za třídou...\";\n\n" +
            "class _LocalCopyDirTreeHandler extends Moxiecode_FileTreeHandler {\n\n" +
            "    var $_handle_as_add_event;\n\n" +
            "\tfunction _LocalCopyDirTreeHandler(&$manager, $from_file, $dest_file, $handle_as_add_event) {\n" +
            "        echo \"Old constructor\\n\";\n" +
            "        $this->shit = \"works\";\n" +
            "    }\n\n" +
            "    protected function necoDelam() { /* Tělo jiné funkce */ }\n" +
            "}\n\necho \"Nějaká blbost za třídou...\";\n?>");

            var upgrader = new RubiconUpgrader(string.Empty, string.Empty);

            //Act
            upgrader.UpgradeConstructors(file);

            //Assert
            _output.WriteLine($"'{file.Content}'");
            //File.WriteAllText(@"C:\McRAI\tst_final.php", file.Content);
            Assert.True(file.IsModified);
            Assert.Contains("function __construct", file.Content);
            Assert.Empty(file.Warnings);
        }

        [Fact]
        public void RubiconImportTest()
        {
            //Arrange
            var file = new FileWrapper("rubicon_import.php", "");
            var upgrader = new RubiconUpgrader(string.Empty, string.Empty)
            { 
                ConnectionFile = "connect.php",
                RenameBetaWith = "alfa"
            };

            //Act, Debug
            upgrader.UpgradeRubiconImport(file);
        }

        [Fact]
        public void FillInDbLoginToSetup()
        {
            //Arrange
            var file = new FileWrapper("test-web\\setup.php",
                                       "\n\n$setup_connect_db = \"olejemaziva\";\n" +
                                       "//$setup_connect_db = \"hasici-pristroje\";\n" +
                                       "$setup_connect_username = \"olejemaziva_use\";\n" +
                                       "$setup_connect_password = \"3_2n7dSj\"; \");\n");

            var upgrader = new RubiconUpgrader(string.Empty, "test-web")
            {
                Username = "myUserName", Password = "myPassword", Database = "myDatabase"
            };

            //Act
            upgrader.UpgradeSetup(file);

            //Assert
            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
        }

        [Fact]
        public void UpdatesBetaHostnameToMcrai2()
        {
            //Arrange
            var file = new FileWrapper("Connections\\beta.php",
                                       "\t$hostname_beta = \"93.185.102.228\";		//server(host)\n" +
	                                   "\t$database_beta = $setup_connect_db;	//databaze\n" +
	                                   "\t$username_beta = $setup_connect_username;	//login(user)\n" +
	                                   "\t$password_beta = $setup_connect_password;		//heslo\n" +
	                                   "\t$connport_beta = \"5432\";			//port");

            var upgrader = new RubiconUpgrader(string.Empty, "test-web")
            {
                Hostname = "mcrai2.vshosting.cz"
            };

            //Act
            upgrader.UpgradeHostnameFromMcrai1IP(file);

            //Assert
            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
        }

        [Fact]
        public void UpgradesDeprecatedSctiptPHP()
        {
            //Arrange
            var file = new FileWrapper(string.Empty, "<script language=\"javascript\">\n" +
                                                     "//some JavaScript\n" +
                                                     "</script>\n"+
                                                     "<script language=\"php\">\n" +
                                                     "\techo 'some PHP code';\n" +
                                                     "</script>\n");
            
            //Act
            RubiconUpgrader.UpgradeScriptLanguagePhp(file);

            //Assert
            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
            Assert.Contains("<?php", file.Content);
            Assert.Contains("?>", file.Content);
            Assert.DoesNotContain("<script language=\"php\">", file.Content);
            Assert.DoesNotContain("<script language=\"PHP\">", file.Content);
        }

        [Fact]
        public void CommentsIncludesInProductDetail()
        {
            //Arrange
            var file = new FileWrapper(@"test-site\templates\amt\product_detail.php",
                "</div>\n<?php include \"rubicon/modules/category/menu1.php\";?>\n" +
                "<div class=\"clear\"></div>\n</div>\n</div>\n" +
                "<!--div class=\"obsah_detail\">\n" +
                "\t<div class=\"obsah_detail_in\">\n" +
                "\t\t<?php include TML_URL.\"/product/product_navigace.php\";?>\n" +
                "\t\t<div class=\"spacer\">&nbsp;</div>\n\n" +
                "\t\t<?php include \"mona/system/head.php\";?>\n" +
                "\t\t<?php include \"rubicon/modules/news/main.php\";//load modul news/aktuality?>\n" +
                "\t</div>\n" +
                "</div>-->\n\n<?php include TML_URL.\"/product/product_detail_detail.php\";?>");

            //Act
            RubiconUpgrader.UpgradeIncludesInHtmlComments(file);

            //Assert
            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
            Assert.Contains("<?php //include", file.Content);
            Assert.Single(file.Warnings);
        }

        [Fact]
        public void ReplacesBreakWithReturnInAegisxDetail()
        {
            //Arrange
            var file = new FileWrapper("test-site\\aegisx\\detail.php", "if ($presmeruj == \"NO\")\n{\n\t\t\tbreak;");

            //Act
            RubiconUpgrader.UpgradeAegisxDetail(file);

            //Assert
            _output.WriteLine(file.Content);
            Assert.True(file.IsModified);
            Assert.DoesNotContain("break;", file.Content);
            Assert.Contains("return;", file.Content);
        }
    }
}

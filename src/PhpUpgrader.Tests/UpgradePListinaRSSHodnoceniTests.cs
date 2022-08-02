using PhpUpgrader.Rubicon.UpgradeExtensions;

namespace PhpUpgrader.Tests;

public class UpgradePListinaRSSHodnoceniTests : UnitTestWithOutputBase
{
    public UpgradePListinaRSSHodnoceniTests(ITestOutputHelper output) : base(output)
    {
    }

    private const string _content = "$setup_connect_db = \"nicom\";\r\n$setup_connect_username = \"nicom_use\";\r\n$setup_connect_password = \"aljjaskvnjnnavajnjks\";\r\n$connport_beta = \"5432\";\r\n$hostname_beta = \"93.185.102.228\";\r\n$beta = pg_connect(\"host=$hostname_beta port=$connport_beta dbname=$setup_connect_db user=$setup_connect_username password=$setup_connect_password\");\r\n\r\n$server_hod = \"localhost\";\r\n$dtb_hod = \"nicom_hod\";\r\n$user_hod = \"nicom_hod_use\";\r\n$pass_hod = \"savasvkkmůmvavkmlamjuvnejwikuwiqhbnbacha\";\r\n$beta_hod = mysqli_connect($server_hod, $user_hod, $pass_hod);\r\nmysql_select_db($dtb_hod, $beta_hod);\r\n\r\nmysqli_select_db($beta_hod, $dtb_hod);\r\n\r\nmysqli_query($beta, \"SET character_set_connection=utf8mb4\");\r\nmysqli_query($beta, \"SET character_set_results=utf8mb4\");\r\nmysqli_query($beta, \"SET character_set_client=utf8mb4\");\r\nmysqli_query($beta, 'SET CHARACTER SET utf8mb4');\r\n\r\n$DPH_ARRAY = array(0,10,15,21);\r\n\r\n$product_id = najdi_v_db(\"product_spec\", \"product_spec_id\", $_GET['psid'], \"product_id\");\r\n$cat_q = \"SELECT category_id FROM product_category WHERE product_id = '\".$product_id.\"'\";\r\n$cat_d = pg_query($cat_q);\r\n$ctg_arr = array();\r\nwhile($cat_r = pg_fetch_assoc($cat_d)) {\r\n\t$ctg_arr[] = $cat_r['category_id'];\r\n}";
    private const string _updatedMysqli = "mysqli_query($beta_hod, ";
    private const string _someWebPath = "some-website";

    [Theory]
    [InlineData("pdf", "p_listina.php")]
    [InlineData("pdf", "p_listina_u.php")]
    [InlineData("rss", "hodnoceni.php")]
    public void UpgradesValidFile(string folder, string fileName)
    {
        //Arrange
        var file = new FileWrapper(Path.Join(_someWebPath, folder, fileName), _content);

        //Act
        file.UpgradeHodnoceniDBCalls();

        //Assert
        _output.WriteLine(file.Path);
        var updatedContent1 = file.Content.ToString();
        _output.WriteLine(updatedContent1);

        Assert.True(file.IsModified);
        Assert.NotEqual(updatedContent1, _content);
        Assert.Contains(_updatedMysqli, updatedContent1);
    }

    [Fact]
    public void DoesNotUpgradeInvalidFile()
    {
        //Arrange
        var file = new FileWrapper(Path.Join(_someWebPath, "other_folder", "other_file.php"), _content);

        //Act
        file.UpgradeHodnoceniDBCalls();

        //Assert
        _output.WriteLine(file.Path);
        var updatedContent1 = file.Content.ToString();
        _output.WriteLine(updatedContent1);

        Assert.False(file.IsModified);
        Assert.Equal(updatedContent1, _content);
        Assert.DoesNotContain(_updatedMysqli, updatedContent1);
    }
}

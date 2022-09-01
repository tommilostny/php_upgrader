using PhpUpgrader.Rubicon.UpgradeExtensions;

namespace PhpUpgrader.Tests;

public class RequiredParameterFollowsOptionalTests : UnitTestWithOutputBase
{
    public RequiredParameterFollowsOptionalTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("private $database = null;\r\n    private $connport = null;\r\n\r\n    public function __construct($domain = 1, $hostname, $username, $password, $database, $connport)\r\n    {\r\n        $this->domain = $domain;\r\n\r\n        $this->hostname = $hostname;\r\n        $this->username = $username;\r\n        $this->password = $password;\r\n        $this->database = $database;\r\n        $this->connport = $connport;\r\n    }\r\n\r\n\r\n    public function GetData($SearchString, $id) {\r\n        $this->DBconn();\r\n\r\n        $data = [];")]
    [InlineData("     * @return Piwik_DataTable_Filter_AddColumnsProcessedMetricsGoal\r\n     */\r\n    public function __construct($table, $enable = true, $processOnlyIdGoal)\r\n    {\r\n        $this->processOnlyIdGoal = $processOnlyIdGoal;\r\n        $this->isEcommerce = $this->processOnlyIdGoal == Piwik_Archive::LABEL_ECOMMERCE_ORDER || $this->processOnlyIdGoal == Piwik_Archive::LABEL_ECOMMERCE_CART;\r\n        parent::__construct($table);\r\n        // Ensure that all rows with no visit but conversions will be displayed\r\n        $this->deleteRowsWithNoVisit = false;\r\n    }\r\n\r\n    /**\r\n     * Filters the given data table\r\n     *\r\n     * @param Piwik_DataTable $table\r\n     */\r\n    public function filter()\r\n    {\r\n        // Add standard processed metrics\r\n        parent::filter($table);\r\n        $roundingPrecision = Piwik_Tracker_GoalManager::REVENUE_PRECISION;\r\n        $expectedColumns = array();")]
    [InlineData("protected static function applyFilter(&$value, $key = null, $filter)\r\n    {\r\n        $callback = $filter[0];\r\n        $options  = $filter[1];\r\n        if (!is_array($options)) {\r\n            $options = array();\r\n        }\r\n        array_unshift($options, $value);\r\n        $value = call_user_func_array($callback, $options);\r\n    }")]
    [InlineData("protected static function applyFilter(&$value, $key = null, $filter, $a = 10)\r\n    {\r\n        $callback = $filter[0];\r\n        $options  = $filter[1];\r\n        if (!is_array($options)) {\r\n            $options = array();\r\n        }\r\n        array_unshift($options, $value);\r\n        $value = call_user_func_array($callback, $options);\r\n    }")]
    [InlineData("protected static function applyFilter(&$value, $key=null, $filter)\r\n    {\r\n        $callback = $filter[0];\r\n        $options  = $filter[1];\r\n        if (!is_array($options)) {\r\n            $options = array();\r\n        }\r\n        array_unshift($options, $value);\r\n        $value = call_user_func_array($callback, $options);\r\n    }")]
    [InlineData("var $dbTable;\r\n\t\r\n    function __construct($y=0, $m, $settings = array())\r\n      {\r\n        $this->todayYear = Date('Y');\r\n\t$this->todayMonth = Date(")]
    [InlineData("var $dbTable;\r\n\t\r\n    function __construct($y=0, $m, $settings = array(), $a, $b=1)\r\n      {\r\n        $this->todayYear = Date('Y');\r\n\t$this->todayMonth = Date(")]
    [InlineData("var $dbTable;\r\n\t\r\n    function __construct($y=\"abc\", $m, $settings = array(), $a, $b=1)\r\n      {\r\n        $this->todayYear = Date('Y');\r\n\t$this->todayMonth = Date(")]
    [InlineData("var $dbTable;\r\n\t\r\n    function __construct($y=\"abc(def)\", $m, $settings = array(), $a, $b=1)\r\n      {\r\n        $this->todayYear = Date('Y');\r\n\t$this->todayMonth = Date(")]
    [InlineData("var $dbTable;\r\n\t\r\n    function __construct($y=\"abc(def)ghijklmnopqrstuvwxyz\", $m, $settings = array(), $a, $b=1)\r\n      {\r\n        $this->todayYear = Date('Y');\r\n\t$this->todayMonth = Date(")]
    [InlineData("     */\r\n    public static function sendHttpRequestBy(\r\n        $method = 'socket',\r\n        $aUrl,\r\n        $timeout,\r\n        $userAgent = null,\r\n        $destinationPath = null,\r\n        $file = null,\r\n        $followDepth = 0,\r\n        $acceptLanguage = false,\r\n        $acceptInvalidSslCertificate = false,\r\n        $byteRange = false,\r\n        $getExtendedInfo = false,\r\n        $httpMethod = 'GET'\r\n    )\r\n    {\r\n        if ($followDepth > 5) {")]
    [InlineData("    public function createAttachment($body,\r\n                                     $mimeType    = Zend_Mime::TYPE_OCTETSTREAM,\r\n                                     $disposition = Zend_Mime::DISPOSITION_ATTACHMENT,\r\n                                     $encoding    = Zend_Mime::ENCODING_BASE64,\r\n                                     $filename    = null)\r\n    {\r\n\r\n        $mp = new Zend_Mime_Part($body);\r\n        $mp->encoding = $encoding;\r\n        $mp->type = $mimeType;\r\n        $mp->disposition = $disposition;\r\n        $mp->filename = $filename;\r\n\r\n        $this->addAttachment($mp);\r\n\r\n        return $mp;\r\n    }")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeRequiredParameterFollowsOptional();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
        Assert.NotEqual(content, updated);
    }

    [Fact]
    public void DoesNotUpgradeInvalidFile()
    {
        //Arrange
        var content = "    public function __construct($domain, $hostname, $username, $password, $database='dbname', $connport = 80)\r\n    {\r\n        $this->domain = $domain;\r\n\r\n        $this->hostname = $hostname;\r\n        $this->username = $username;\r\n        $this->password = $password;\r\n        $this->database = $database;\r\n        $this->connport = $connport;\r\n    }";
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeRequiredParameterFollowsOptional();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.False(file.IsModified);
        Assert.Equal(content, updated);
    }
}

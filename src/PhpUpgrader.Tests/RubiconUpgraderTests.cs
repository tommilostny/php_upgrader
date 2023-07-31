namespace PhpUpgrader.Tests;

public class RubiconUpgraderTests : UnitTestWithOutputBase
{
    public RubiconUpgraderTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("<?php\necho \"Nějaká blbost před třídou... obsahuje slovíčko class hhahahahahha\";\n\nclass NejakaMojeTrida\n{\nprivate function blaBla() { /* Tělíčko */ }\n\n    public function NejakaMojeTrida($foo, $bar = null)\n    {\n        echo \"new class constructor\\n\";\n    }\n\n    protected function necoDelam() { /* Tělo jiné funkce */ }\n}\n\necho \"Nějaká blbost za třídou...\";\n\nclass _LocalCopyDirTreeHandler extends Moxiecode_FileTreeHandler {\n\n    var $_handle_as_add_event;\n\n\tfunction _LocalCopyDirTreeHandler(&$manager, $from_file, $dest_file, $handle_as_add_event) {\n        echo \"Old constructor\\n\";\n        $this->shit = \"works\";\n    }\n\n    protected function necoDelam() { /* Tělo jiné funkce */ }\n}\n\necho \"Nějaká blbost za třídou...\";\n?>")]
    [InlineData("<?php SESSION_START();\r\nrequire('../connect/connection.php');\r\n\r\nDEFINE('DB_USER', $username_beta);\r\nDEFINE('DB_PASSWORD', $password_beta);\r\nDEFINE('DB_DATABASE', $database_beta);\r\nDEFINE('DB_HOST', 'rdesign.cybersales.cz');\r\nDEFINE('DB_TABLE', 'kalendar');\r\n\r\n\r\n\r\n\r\nclass Calendar\r\n  {\r\n    //mesic a rok ktery se ma zobrazit\r\n    var $year;\r\n    var $month;\r\n\t\r\n    //aktualni datum\r\n    var $todayYear;\r\n    var $todayMonth;\r\n    var $todayDay;\r\n\t\r\n    var $settings;\r\n\r\n    //\r\n    var $daysInMonth;\r\n    var $weeksInMonth;\r\n    var $firstDay;\r\n    var $week;\r\n\r\n    //settings\r\n    var $htmlHeader;\r\n    var $htmlFooter;\r\n    var $htmlDays;\r\n\t\r\n    var $dayStartOfWeek;\r\n\r\n    var $cssTable;\r\n    var $cssDays;\r\n    var $cssDaySelected;\r\n    var $cssDayEvent;\r\n    var $cssDayEventSelected;\r\n    var $cssHeader;\r\n    var $cssFooter;\r\n\t\r\n    var $months;\r\n    var $monthScrolling;\r\n    var $monthAjaxScrolling;\r\n    var $monthScrollingGetParam;\r\n    var $ajaxDivCalendar;\r\n    var $ajaxDivEvents;\r\n    var $ajaxDayLinks;\r\n    var $ajaxDays;\r\n    var $dayAjaxScrolling;\r\n\t\r\n    //udalosti z databaze\r\n    var $events;\r\n\t\r\n    var $db;\r\n    var $dbTable;\r\n\t\r\n    function __construct($y=0, $m=0, $settings = array())\r\n      {\r\n        $this->todayYear = Date('Y');\r\n\t$this->todayMonth = Date('n');\r\n\t$this->todayDay = Date('j');\r\n\t\t\r\n\t$this->year = $y==0 ? $this->todayYear : $y;\r\n\t$this->month = $m==0 ? $this->todayMonth : $m;\r\n\t\t\r\n\t//SETTINGS\r\n\t$this->dayStartOfWeek = isset($settings['dayStartOfWeek']) ? $settings['dayStartOfWeek'] : 1;\r\n\t\t\r\n\t$this->htmlDays = isset($settings['htmlDays']) ? $settings['htmlDays'] : array(0=>'Po', 1=>'Út', 2=>'St', 3=>'Čt', 4=>'Pá', 5=>'So', 6=>'Ne');\r\n\t$this->htmlHeader = isset($settings['htmlHeader']) ? $settings['htmlHeader'] : false;\r\n\t$this->htmlFooter = isset($settings['htmlFooter']) ? $settings['htmlFooter'] : false;\r\n\t\t\r\n\t$this->cssTable = isset($settings['cssTable']) ? $settings['cssTable'] : false;\r\n\t$this->cssDays = isset($settings['cssDays']) ? $settings['cssDays'] : false;\r\n\t$this->cssDaySelected = isset($settings['cssDaySelected']) ? $settings['cssDaySelected'] : false;\r\n\t$this->cssDayEvent = isset($settings['cssDayEvent']) ? $settings['cssDayEvent'] : false;\r\n\t$this->cssDayEventSelected = isset($settings['cssDayEventSelected']) ? $settings['cssDayEventSelected'] : false;\r\n\t$this->cssHeader = isset($settings['cssHeader']) ? $settings['cssHeader'] : false;\r\n\t$this->cssFooter = isset($settings['cssFooter']) ? $settings['cssFooter'] : false;\r\n\t\t\r\n\t$this->monthScrolling = isset($settings['monthScrolling']) ? $settings['monthScrolling'] : false;\r\n\t$this->months = isset($settings['months']) ? $settings['months'] : false;\r\n\t$this->monthScrollingGetParam = isset($settings['monthScrollingGetParam']) ? $settings['monthScrollingGetParam'] : false;\r\n\t$this->dayAjaxScrolling = isset($settings['dayAjaxScrolling']) ? $settings['dayAjaxScrolling'] : false;\r\n\t\t\r\n\t$this->monthAjaxScrolling = isset($settings['monthAjaxScrolling']) ? $settings['monthAjaxScrolling'] : false;\r\n\t$this->ajaxDivCalendar = isset($settings['ajaxDivCalendar']) ? $settings['ajaxDivCalendar'] : false;\r\n\t$this->ajaxDivEvents = isset($settings['ajaxDivEvents']) ? $settings['ajaxDivEvents'] : false;\r\n\t$this->ajaxDayLinks = isset($settings['ajaxDayLinks']) ? $settings['ajaxDayLinks'] : false;\r\n\t$this->ajaxDays = isset($settings['ajaxDays']) ? $settings['ajaxDays'] : false;\r\n\t\t\r\n\t//db\r\n\tif(isset($settings['db']))\r\n          {\r\n            $this->db = $settings['db'];\r\n            $this->dbTable = $settings['dbTable'];\r\n          }\r\n\telse\r\n          {\r\n            //sql connect\r\n            $this->db = @pg_connect(DB_HOST, DB_USER, DB_PASSWORD);\r\n\r\n            if(!$this->db)\r\n              {\r\n                return false;\r\n              }\r\n\r\n            pg_select_db(DB_DATABASE, $this->db);\r\n\t\t\tpg_query(\"SET CHARACTER SET utf8\", $this->db);\r\n            $this->dbTable = DB_TABLE;\r\n          }\r\n\t\t\r\n\t$this->week = array();\r\n\t\t\r\n\t$this->daysInMonth = Date(\"t\",mktime(0,0,0,$this->month,1,$this->year));\r\n\t// get first day of the month\r\n\t$this->firstDay = Date(\"w\", mktime(0,0,0,$this->month,1,$this->year)) - $this->dayStartOfWeek;\r\n\t\t\r\n\t$tempDays = $this->firstDay + $this->daysInMonth;\r\n\t$this->weeksInMonth = ceil($tempDays/7);\r\n      }\r\n    \r\n    function Calendar($y=0, $m=0, $settings=array())\r\n      {\r\n        $this->__construct($y, $m, $settings);\r\n      }\r\n    \r\n    function __destruct()\r\n      {\r\n        pg_close($this->db);\r\n      }\r\n\t\r\n    // create a 2-d array\r\n    function fillArray()\r\n      {\r\n        $counter = 0;\r\n\t\r\n        for($j=0;$j<$this->weeksInMonth;$j++)\r\n          {\r\n            for($i=0;$i<7;$i++)\r\n              {\r\n                $counter++;\r\n\t\t$this->week[$j][$i] = $counter; \r\n\t\t// offset the days\r\n\t\t$this->week[$j][$i] -= $this->firstDay;\r\n\t\t\r\n                if(($this->week[$j][$i] < 1) || ($this->week[$j][$i] > $this->daysInMonth))\r\n                  {\r\n                    $this->week[$j][$i] = \"\";\r\n                  }\r\n              }\r\n          }\r\n      }\r\n\t\r\n    function Render()\r\n      {\r\n        $this->fillArray();\r\n\t$this->html = '';\r\n\t\t\r\n\t$this->events = false;\r\n\t$count = $this->LoadEvents($this->year, $this->month);\r\n\t\t\r\n\t$this->CreateHeader();\r\n\t$this->CreateBody();\r\n\t$this->CreateFooter();\r\n\t\t\r\n\treturn $this->html;\r\n      }\r\n\t\r\n    function CreateHeader()\r\n      {\r\n        if($this->cssTable)\r\n          {\r\n            $this->html = \"<table class=\\\"\".$this->cssTable.\"\\\">\\n\";\r\n          }\r\n        else\r\n          {\r\n            $this->html = \"<table>\\n\";\r\n          }\r\n\t\t\r\n\t//htmlHeader\r\n\tif($this->htmlHeader)\r\n          {\r\n            if(eregi('^<tr(.*){0,}</tr>$', $this->htmlHeader))\r\n              {\r\n                $this->html .= $this->htmlHeader.\"\\n\";\r\n              }\r\n            else\r\n              {\r\n\t\tif($this->cssHeader)\r\n                  {\r\n                    $this->html .= \"<tr class=\\\"\".$this->cssHeader.\"\\\"><td colspan=\\\"7\\\">\".$this->htmlHeader.\"</td></tr>\\n\";\r\n                  }\r\n                else\r\n                  {\r\n                    $this->html .= \"<tr><td colspan=\\\"7\\\">\".$this->htmlHeader.\"</td></tr>\\n\";\r\n                  }\r\n              }\r\n          }\r\n\t\t\r\n        //months header\r\n\tif($this->months)\r\n          {\r\n            //scroling\r\n\t\r\n            if($this->monthScrolling)\r\n              {\r\n                //previous\r\n\t\t$year = $this->year;\r\n\t\t$month = $this->month;\r\n\t\t$month--;\r\n\t\t\r\n                if($month==0)\r\n                  {\r\n                    $month=12;$year--;\r\n                  }\r\n\r\n\t\t$previous = '<a href=\"?'.$this->monthScrollingGetParam.'='.$month.'-'.$year.'\"><<</a>';\r\n\t\t\t\t\r\n                //next\r\n\t\t$year = $this->year;\r\n\t\t$month = $this->month;\r\n\t\t$month++;\r\n\r\n\t\tif($month==13)\r\n                  {\r\n                    $month=1;$year++;\r\n                  }\r\n\r\n\t\t$next = '<a href=\"?'.$this->monthScrollingGetParam.'='.$month.'-'.$year.'\">>></a>';\r\n\t\t\t\t\t\t\t\t\r\n                if($this->cssHeader)\r\n                  {\r\n                    $this->html .= \"<tr class=\\\"\".$this->cssHeader.\"\\\"><td>\".$previous.\"</td><td colspan=\\\"5\\\">\".$this->months[$this->month-1].\" \".$this->year.\"</td><td>\".$next.\"</td></tr>\\n\";\r\n                  }\r\n                else\r\n                  {\r\n                    $this->html .= \"<tr><td>\".$previous.\"</td><td colspan=\\\"5\\\">\".$this->months[$this->month-1].\" \".$this->year.\"</td><td>\".$next.\"</td></tr>\\n\";\r\n                  }\r\n              }\r\n\t\t\t\r\n            //ajax scroling\r\n            if($this->monthAjaxScrolling) \r\n              {\r\n                //previous\r\n\t\t$year = $this->year;\r\n\t\t$month = $this->month;\r\n\t\t$month--;\r\n\t\t\r\n                if($month==0)\r\n                  {\r\n                    $month=12;\r\n                    $year--;\r\n                  }\r\n\t\t\r\n                $previous = '<a href=\"#\" onclick=\"CADisplayCalendar('.$year.','.$month.', \\''.$this->ajaxDivCalendar.'\\');return false;\"><<</a>';\r\n\t\t\t\t\r\n\t\t//next\r\n\t\t$year = $this->year;\r\n\t\t$month = $this->month;\r\n\t\t$month++;\r\n\t\t\r\n                if($month==13)\r\n                  {\r\n                    $month=1;\r\n                    $year++;\r\n                  }\r\n\t\t\r\n\t\t$next = '<a href=\"#\" onclick=\"CADisplayCalendar('.$year.','.$month.', \\''.$this->ajaxDivCalendar.'\\');return false;\">>></a>';\r\n\t\t\t\t\t\t\t\t\r\n\t\tif($this->cssHeader)\r\n                  {\r\n                    $this->html .= \"<tr class=\\\"\".$this->cssHeader.\"\\\"><td>\".$previous.\"</td><td colspan=\\\"5\\\">\".$this->months[$this->month-1].\" \".$this->year.\"</td><td>\".$next.\"</td></tr>\\n\";\r\n                  }\r\n                else\r\n                  {\r\n                    $this->html .= \"<tr><td>\".$previous.\"</td><td colspan=\\\"5\\\">\".$this->months[$this->month-1].\" \".$this->year.\"</td><td>\".$next.\"</td></tr>\\n\";\r\n\t\t\t\t\r\n                  }\r\n              }\r\n\t\t\t\r\n\t\t\t//no scrolling\r\n\t\t\tif(!$this->monthScrolling and !$this->monthAjaxScrolling) {\r\n\t\t\t\tif ($this->cssHeader)\r\n\t\t\t\t\t$this->html .= \"<tr class=\\\"\".$this->cssHeader.\"\\\"><td colspan=\\\"7\\\">\".$this->months[$this->month-1].\" \".$this->year.\"</td></tr>\\n\";\r\n\t\t\t\telse\r\n\t\t\t\t\t$this->html .= \"<tr><td colspan=\\\"7\\\">\".$this->months[$this->month-1].\" \".$this->year.\"</td></tr>\\n\";\r\n\t\t\t}\r\n\t\t}\r\n\t\t\r\n\t\t\r\n\t}\r\n\t\r\n\tfunction CreateBody() {\r\n\t\tif ($this->htmlDays)\r\n\t\t\tif ($this->cssDays)\r\n\t\t\t\t$this->html .= \"<tr class=\\\"\".$this->cssDays.\"\\\"><td>\".$this->htmlDays[0].\"</td><td>\".$this->htmlDays[1].\"</td><td>\".$this->htmlDays[2].\"</td><td>\".$this->htmlDays[3].\"</td><td>\".$this->htmlDays[4].\"</td><td>\".$this->htmlDays[5].\"</td><td>\".$this->htmlDays[6].\"</td></tr>\\n\";\r\n\t\t\telse\r\n\t\t\t\t$this->html .= \"<tr><td>\".$this->htmlDays[0].\"</td><td>\".$this->htmlDays[1].\"</td><td>\".$this->htmlDays[2].\"</td><td>\".$this->htmlDays[3].\"</td><td>\".$this->htmlDays[4].\"</td><td>\".$this->htmlDays[5].\"</td><td>\".$this->htmlDays[6].\"</td></tr>\\n\";\r\n\t\t\r\n\t\tforeach ($this->week as $week) {\r\n\t\t\t$this->html.= \"<tr>\";\r\n\t\t\tforeach ($week as $day) {\r\n\t\t\t\t$isEvent = false;\r\n\t\t\t\tif (isset($this->events[$this->year][$this->month][$day]))\r\n\t\t\t\t\t$isEvent = true;\r\n\t\t\t\t\r\n\t\t\t\tif (!$day) {\r\n\t\t\t\t\t$this->html .= \"<td></td>\";\r\n\t\t\t\t\tcontinue;\r\n\t\t\t\t}\r\n\t\t\t\t\t\r\n\t\t\t\t$textDay = $day;\r\n\t\t\t\tif ($this->ajaxDayLinks)\r\n\t\t\t\t\t$textday = \"<a href=\\\"#\\\" onclick=\\\"CADisplayDayEvents(\".$this->year.\", \".$this->month.\", \".$day.\", '\".$this->ajaxDivEvents.\"');return false;\\\">\".$day.\"</a>\";\r\n\t\t\t\t\t\r\n\t\t\t\tif ($isEvent) {\r\n\t\t\t\t\tif ($this->todayYear==$this->year and $this->todayMonth==$this->month and $this->todayDay==$day and $this->cssDaySelected)\r\n\t\t\t\t\t\t$this->html .= \"<td class=\\\"\".$this->cssDayEventSelected.\"\\\">\".$textday.\"</td>\";\r\n\t\t\t\t\telse\r\n\t\t\t\t\t\t$this->html .= \"<td class=\\\"\".$this->cssDayEvent.\"\\\">\".$textday.\"</td>\";\r\n\t\t\t\t}\r\n\t\t\t\telse {\r\n\t\t\t\t\tif ($this->todayYear==$this->year and $this->todayMonth==$this->month and $this->todayDay==$day and $this->cssDaySelected)\r\n\t\t\t\t\t\t$this->html .= \"<td class=\\\"\".$this->cssDaySelected.\"\\\">\".$textday.\"</td>\";\r\n\t\t\t\t\telse\r\n\t\t\t\t\t\t$this->html .= \"<td>\".$textday.\"</td>\";\r\n\t\t\t\t}\r\n\t\t\t\t\r\n\t\t\t}\r\n\t\t\t$this->html.= \"</tr>\\n\";\r\n\t\t}\r\n\t}\r\n\t\r\n\tfunction CreateFooter() {\r\n\t\tif ($this->htmlFooter) {\r\n\t\t\tif (eregi('^<tr(.*){0,}</tr>$', $this->htmlFooter)) {\r\n\t\t\t\t$this->html .= $this->htmlFooter.\"\\n\";\r\n\t\t\t}\r\n\t\t\telse {\r\n\t\t\t\tif ($this->cssFooter) {\r\n\t\t\t\t\t$this->html .= \"<tr class=\\\"\".$this->cssFooter.\"\\\"><td colspan=\\\"7\\\">\".$this->htmlFooter.\"</td></tr>\\n\";\r\n\t\t\t\t}\r\n\t\t\t\telse {\r\n\t\t\t\t\t$this->html .= \"<tr><td colspan=\\\"7\\\">\".$this->htmlFooter.\"</td></tr>\\n\";\r\n\t\t\t\t}\r\n\t\t\t}\r\n\t\t}\r\n\t\t$this->html .= \"</table>\\n\";\r\n\t}\r\n\t\r\n\tfunction LoadEvents($year=0, $month=0, $day=0, $description = false) {\r\n\t\t//slozime sql dotaz\r\n\t\t$sql = $description ? \"SELECT id, year, month, day, name, description FROM \".$this->dbTable : \"SELECT id, year, month, day, name FROM \".DB_TABLE;\r\n\t\t$where = false;\r\n\t\tif ($year>0)\r\n\t\t\t$where .= $where ? \" AND year=\".$year : \" WHERE year=\".$year;\r\n\t\tif ($month>0)\r\n\t\t\t$where .= $where ? \" AND month=\".$month : \" WHERE month=\".$month;\r\n\t\tif ($day>0)\r\n\t\t\t$where .= $where ? \" AND day=\".$day : \" WHERE day=\".$day;\r\n                $where .= \" and jazyk='\".$_SESSION['session_jazykova_mutace'].\"' order by poradi asc\"; \r\n\t\t$sql .= $where;\r\n\r\n                //dota na db\r\n\t\t$res = pg_query($sql);\r\n\t\tif (!$res)\r\n\t\t\treturn false;\r\n\t\t\r\n\t\twhile ($row = pg_fetch_array($res, MYSQL_ASSOC))\r\n\t\t\t$this->events[$row['year']][$row['month']][$row['day']][] = $row;\r\n\t\t\r\n\t\treturn pg_num_rows($res);\r\n\t}\r\n\t\r\n\t\r\n\tfunction getDayEvents($year=0, $month=0, $day=0) {\r\n\t\tif ($year==0 or $month==0 or $day==0) {\r\n\t\t\t$year = $this->todayYear;\r\n\t\t\t$month = $this->todayMonth;\r\n\t\t\t$day = $this->todayDay;\r\n\t\t}\r\n\t\t$this->events = false;\r\n\t\t\r\n\t\t$this->LoadEvents($year, $month, $day, true);\r\n\t\t\r\n\t\tif (isset($this->events[$year][$month][$day]))\r\n\t\t\treturn $this->events[$year][$month][$day];\r\n\t\telse\r\n\t\t\treturn false;\r\n\t}\r\n\t\r\n\t\r\n\tfunction saveEvent($year, $month, $day, $name, $text, $poradi, $jazyk) {\r\n\t\t$sql = \"INSERT INTO `\".$this->dbTable.\"` (`id`, `year`, `month`, `day`, `name`, `description`, `poradi`, `jazyk`) VALUES (0, \".$year.\", \".$month.\", \".$day.\", '\".$name.\"', '\".$text.\"', '\".$poradi.\"', '\".$_SESSION['session_jazykova_mutace'].\"'); \";\r\n\t\tpg_query($sql);\r\n\t}\r\n\t\r\n}\r\n\r\n?>\r\n")]
    [InlineData("/**\r\n * This class is a Drupal CMS authenticator implementation.\r\n *\r\n * @package MCImageManager.Authenticators\r\n */\r\nclass Moxiecode_PHPNukeAuthenticator extends Moxiecode_ManagerPlugin {\r\n    /**#@+\r\n\t * @access public\r\n\t */\r\n\r\n\t/**\r\n\t * Main constructor.\r\n\t */\r\n\tfunction Moxiecode_PHPNukeAuthenticator() {\r\n\t}\r\n\r\n\tfunction onAuthenticate(&$man) {\r\n\t\tglobal $user;\r\n\r\n\t\treturn is_user($user) == 1;\r\n\t}\r\n\r\n\t/**#@-*/\r\n}\r\n\r\n// Add plugin to MCManager\r\n$man->registerPlugin(\"PHPNukeAuthenticator\", new Moxiecode_PHPNukeAuthenticator());")]
    [InlineData("<?php\r\n/**\r\n * Moxiecode JS Compressor.\r\n *\r\n * @version 1.0\r\n * @author Moxiecode\r\n * @site http://www.moxieforge.com/\r\n * @copyright Copyright � 2004-2008, Moxiecode Systems AB, All rights reserved.\r\n * @licence LGPL\r\n * @ignore\r\n */\r\n\r\nclass Moxiecode_ClientResources {\r\n\t/**#@+ @access private */\r\n\r\n\tvar $_path, $_files, $_settings, $_debug;\r\n\r\n\t/**#@-*/\r\n\r\n\tfunction Moxiecode_ClientResources($settings = array()) {\r\n\t\t$default = array(\r\n\t\t);\r\n\r\n\t\t$this->_settings = array_merge($default, $settings);\r\n\t\t$this->_files = array();\r\n\t}\r\n\r\n\tfunction isDebugEnabled() {\r\n\t\treturn $this->_debug;\r\n\t}\r\n\r\n\tfunction getSetting($name, $default = false) {\r\n\t\treturn isset($this->_settings[\"name\"]) ? $this->_settings[\"name\"] : $default;\r\n\t}\r\n\r\n\tfunction getPackageIDs() {\r\n\t\treturn array_keys($this->_files);\r\n\t}\r\n\r\n\tfunction &getFile($package, $file_id) {\r\n\t\t$files = $this->getFiles($package);\r\n\r\n\t\tforeach ($files as $file) {\r\n\t\t\tif ($file->getId() == $file_id)\r\n\t\t\t\treturn $file;\r\n\t\t}\r\n\r\n\t\treturn null;\r\n\t}\r\n\r\n\tfunction getFiles($package) {\r\n\t\treturn isset($this->_files[$package]) ? $this->_files[$package] : array();\r\n\t}\r\n\r\n\tfunction load($xml_file) {\r\n\t\t$this->_path = dirname($xml_file);\r\n\r\n\t\tif (!file_exists($xml_file))\r\n\t\t\treturn;\r\n\r\n\t\t$fp = @fopen($xml_file, \"r\");\r\n\t\tif ($fp) {\r\n\t\t\t$data = '';\r\n\r\n\t\t\twhile (!feof($fp))\r\n\t\t\t\t$data .= fread($fp, 8192);\r\n\r\n\t\t\tfclose($fp);\r\n\r\n\t\t\tif (ini_get(\"magic_quotes_gpc\"))\r\n\t\t\t\t$data = stripslashes($data);\r\n\t\t}\r\n\r\n\t\t$this->_parser = xml_parser_create('UTF-8');\r\n\t\txml_set_object($this->_parser, $this);\r\n\t\txml_set_element_handler($this->_parser, \"_saxStartElement\", \"_saxEndElement\");\r\n\t\txml_parser_set_option($this->_parser, XML_OPTION_TARGET_ENCODING, \"UTF-8\");\r\n\r\n\t\tif (!xml_parse($this->_parser, $data, true))\r\n\t\t\ttrigger_error(sprintf(\"Language pack loading failed, XML error: %s at line %d.\", xml_error_string(xml_get_error_code($this->_parser)), xml_get_current_line_number($this->_parser)), E_USER_ERROR);\r\n\r\n\t\txml_parser_free($this->_parser);\r\n\t}\r\n\r\n\t// * * Private methods\r\n\r\n\tfunction _saxStartElement($parser, $name, $attrs) {\r\n\t\tswitch ($name) {\r\n\t\t\tcase \"RESOURCES\":\r\n\t\t\t\tif (!$this->_debug)\r\n\t\t\t\t\t$this->_debug = isset($attrs[\"DEBUG\"]) && $attrs[\"DEBUG\"] == 'yes';\r\n\r\n\t\t\t\tbreak;\r\n\r\n\t\t\tcase \"PACKAGE\":\r\n\t\t\t\t$this->_packageID = isset($attrs[\"ID\"]) ? $attrs[\"ID\"] : 'noid';\r\n\r\n\t\t\t\tif (!isset($this->_files[$this->_packageID]))\r\n\t\t\t\t\t$this->_files[$this->_packageID] = array();\r\n\t\t\tbreak;\r\n\r\n\t\t\tcase \"FILE\":\r\n\t\t\t\t$this->_files[$this->_packageID][] = new Moxiecode_ClientResourceFile(\r\n\t\t\t\t\tisset($attrs[\"ID\"]) ? $attrs[\"ID\"] : \"\",\r\n\t\t\t\t\tstr_replace(\"\\\\\", DIRECTORY_SEPARATOR, $this->_path . '/' . $attrs[\"PATH\"]),\r\n\t\t\t\t\t!isset($attrs[\"KEEPWHITESPACE\"]) || $attrs[\"KEEPWHITESPACE\"] != \"yes\",\r\n\t\t\t\t\tisset($attrs[\"TYPE\"]) ? $attrs[\"TYPE\"] : ''\r\n\t\t\t\t);\r\n\t\t\tbreak;\r\n\t\t}\r\n\t}\r\n\r\n\tfunction _saxEndElement($parser, $name) {\r\n\t}\r\n}\r\n\r\nclass Moxiecode_ClientResourceFile {\r\n\t/**#@+ @access private */\r\n\r\n\tvar $_id, $_contentType, $_path, $_remove_whitespace;\r\n\r\n\t/**#@-*/\r\n\r\n\tfunction Moxiecode_ClientResourceFile($id, $path, $remove_whitespace, $content_type)\r\n    {\r\n        self::__construct($id, $path, $remove_whitespace, $content_type);\r\n    }\r\n\r\n    public function __construct($id, $path, $remove_whitespace, $content_type) {\r\n\t\t$this->_id = $id;\r\n\t\t$this->_path = $path;\r\n\t\t$this->_remove_whitespace = $remove_whitespace;\r\n\t\t$this->_contentType = $content_type;\r\n\t}\r\n\r\n\tfunction isRemoveWhitespaceEnabled() {\r\n\t\treturn $this->_remove_whitespace;\r\n\t}\r\n\r\n\tfunction getId() {\r\n\t\treturn $this->_id;\r\n\t}\r\n\r\n\tfunction getContentType() {\r\n\t\treturn $this->_contentType;\r\n\t}\r\n\r\n\tfunction getPath() {\r\n\t\treturn $this->_path;\r\n\t}\r\n}\r\n\r\n?>")]
    [InlineData("<?php\r\n/**\r\n * $Id: BaseFileImpl.php 10 2007-05-27 10:55:12Z spocke $\r\n *\r\n * @package MCFileManager.filesystems\r\n * @author Moxiecode\r\n * @copyright Copyright � 2005, Moxiecode Systems AB, All rights reserved.\r\n */\r\n\r\n/**\r\n * Implements some of the basic features of a FileSystem but not specific functionality.\r\n *\r\n * @package MCFileManager.filesystems\r\n */\r\nclass Moxiecode_BaseFileImpl extends Moxiecode_BaseFile {\r\n\t// Private fields\r\n\tvar $_absPath;\r\n\tvar $_type;\r\n\tvar $_manager;\r\n\tvar $_events = true;\r\n\r\n\t/**\r\n\t * Creates a new absolute file.\r\n\t *\r\n\t * @param MCManager $manager MCManager reference.\r\n\t * @param String $absolute_path Absolute path to local file.\r\n\t * @param String $child_name Name of child file (Optional).\r\n\t * @param String $type Optional file type.\r\n\t */\r\n\tfunction Moxiecode_BaseFileImpl(&$manager, $absolute_path, $child_name = \"\", $type = MC_IS_FILE) {\r\n\t\t$this->_manager =& $manager;\r\n\t\t$this->_type = $type;\r\n\r\n\t\tif ($child_name != \"\")\r\n\t\t\t $this->_absPath = $this->_manager->removeTrailingSlash($absolute_path) . \"/\" . $child_name;\r\n\t\telse\r\n\t\t\t$this->_absPath = $absolute_path;\r\n\t}\r\n\r\n\t/**\r\n\t * Set a bool regarding events triggering.\r\n\t *\r\n\t * @param Bool $trigger Trigger or not to trigger.\r\n\t */\r\n\tfunction setTriggerEvents($trigger) {\r\n\t\t$this->_events = $trigger;\r\n\t}\r\n\r\n\t/**\r\n\t * Returns bool if events are to be triggered or not.\r\n\t *\r\n\t * @return Bool bool for triggering events or not.\r\n\t */\r\n\tfunction getTriggerEvents() {\r\n\t\treturn $this->_events;\r\n\t}\r\n\r\n\t/**\r\n\t * Returns the parent files absolute path.\r\n\t *\r\n\t * @return String parent files absolute path.\r\n\t */\r\n\tfunction getParent() {\r\n\t\t$pathAr = explode(\"/\", $this->getAbsolutePath());\r\n\r\n\t\tarray_pop($pathAr);\r\n\t\t$path = implode(\"/\", $pathAr);\r\n\r\n\t\treturn ($path == \"\") ? \"/\" : $path;\r\n\t}\r\n\r\n\t/**\r\n\t * Returns the file name of a file.\r\n\t *\r\n\t * @return string File name of file.\r\n\t */\r\n\tfunction getName() {\r\n\t\treturn basename($this->_absPath);\r\n\t}\r\n\r\n\t/**\r\n\t * Returns the absolute path of the file.\r\n\t *\r\n\t * @return String absolute path of the file.\r\n\t */\r\n\tfunction getAbsolutePath() {\r\n\t\treturn $this->_absPath;\r\n\t}\r\n\r\n\t/**\r\n\t * Returns true if the file is a directory.\r\n\t *\r\n\t * @return boolean true if the file is a directory.\r\n\t */\r\n\tfunction isDirectory() {\r\n\t\tif (!$this->exists())\r\n\t\t\treturn $this->_type == MC_IS_DIRECTORY;\r\n\r\n\t\treturn is_dir($this->_manager->toOSPath($this->_absPath));\r\n\t}\r\n\r\n\t/**\r\n\t * Returns true if the file is a file.\r\n\t *\r\n\t * @return boolean true if the file is a file.\r\n\t */\r\n\tfunction isFile() {\r\n\t\tif (!$this->exists())\r\n\t\t\treturn $this->_type == MC_IS_FILE;\r\n\r\n\t\treturn !$this->isDirectory();\r\n\t}\r\n\r\n\t/**\r\n\t * Returns an array of File instances.\r\n\t *\r\n\t * @return Array array of File instances.\r\n\t */\r\n\tfunction &listFiles() {\r\n\t\t$files = $this->listFilesFiltered(new Moxiecode_DummyFileFilter());\r\n\t\treturn $files;\r\n\t}\r\n\r\n\t/**\r\n\t * Lists the file as an tree and calls the specified FileTreeHandler instance on each file. \r\n\t *\r\n\t * @param FileTreeHandler &$file_tree_handler FileTreeHandler to invoke on each file.\r\n\t */\r\n\tfunction listTree(&$file_tree_handler) {\r\n\t\t$this->_listTree($this, $file_tree_handler, new Moxiecode_DummyFileFilter(), 0);\r\n\t}\r\n\r\n\t/**\r\n\t * Lists the file as an tree and calls the specified FileTreeHandler instance on each file\r\n\t * if the file filter accepts the file.\r\n\t *\r\n\t * @param FileTreeHandler &$file_tree_handler FileTreeHandler to invoke on each file.\r\n\t * @param FileTreeHandler &$file_filter FileFilter instance to filter files by.\r\n\t */\r\n\tfunction listTreeFiltered(&$file_tree_handler, &$file_filter) {\r\n\t\t$this->_listTree($this, $file_tree_handler, $file_filter, 0);\r\n\t}\r\n\r\n\t// * * Private methods\r\n\r\n\t/**\r\n\t * Lists files recursive, and places the files in the specified array.\r\n\t */\r\n\tfunction _listTree($file, &$file_tree_handler, &$file_filter, $level) {\r\n\t\t$state = $file_tree_handler->CONTINUE;\r\n\r\n\t\tif ($file_filter->accept($file)) {\r\n\t\t\t$state = $file_tree_handler->handle($file, $level);\r\n\r\n\t\t\tif ($state == $file_tree_handler->ABORT || $state == $file_tree_handler->ABORT_FOLDER)\r\n\t\t\t\treturn $state;\r\n\t\t}\r\n\r\n\t\t$files = $file->listFiles();\r\n\r\n\t\tforeach ($files as $file) {\r\n\t\t\tif ($file_filter->accept($file)) {\r\n\t\t\t\tif ($file->isFile()) {\r\n\t\t\t\t\t// This is some weird shit!\r\n\t\t\t\t\t//if (!is_object($file_filter))\r\n\t\t\t\t\t\t$state = $file_tree_handler->handle($file, $level);\r\n\t\t\t\t} else {\r\n\t\t\t\t\t$state = $this->_listTree($file, $file_tree_handler, $file_filter, ++$level);\r\n\t\t\t\t\t--$level;\r\n\t\t\t\t}\r\n\t\t\t}\r\n\r\n\t\t\tif ($state == $file_tree_handler->ABORT)\r\n\t\t\t\treturn $state;\r\n\t\t}\r\n\r\n\t\treturn $file_tree_handler->CONTINUE;\r\n\t}\r\n}\r\n\r\n?>")]
    public void ConstructorUpgradeTest(string content)
    {
        //Arrange
        var file = new FileWrapper("somefile.php", content);

        //Act
        file.UpgradeConstructors();

        //Assert
        _output.WriteLine(file.IsModified.ToString());
        _output.WriteLine($"'{file.Content}'");
        Assert.Contains("function __construct", file.Content.ToString());
        Assert.Empty(file.Warnings);
    }

    [Theory]
    [InlineData("rubicon_import.php", "<?php\r\n# FileName=\"Connection_php_mysql.htm\"\r\n# Type=\"MYSQL\"\r\n# HTTP=\"true\"\r\n$hostname_sportmall_import = \"localhost\";\r\n$database_sportmall_import = \"eshop_products\";\r\n$username_sportmall_import = \"eshop_products\";\r\n$password_sportmall_import = \"heslo_k_databazi_:)\";\r\n$sportmall_import = mysql_pconnect($hostname_sportmall_import, $username_sportmall_import, $password_sportmall_import) or trigger_error(mysql_error(),E_USER_ERROR); \r\n\r\nmysql_query(\"SET character_set_connection=cp1250\");\r\nmysql_query(\"SET character_set_results=cp1250\");\r\nmysql_query(\"SET character_set_client=cp1250\");\r\n?>")]
    [InlineData("hodnoceni.php", "<?php\r\n# FileName=\"Connection_php_mysql.htm\"\r\n# Type=\"MYSQL\"\r\n# HTTP=\"true\"\r\n$hostname_hodnoceni_conn = \"localhost\";\r\n$database_hodnoceni_conn = \"nicom_hod\";\r\n$username_hodnoceni_conn = \"nicom_hod_use\";\r\n$password_hodnoceni_conn = \"nvjsnvlsnjlvnhvjslvnjslknjvjskdnjvsvkdnjslnjvsnvjlds\";\r\n//$hodnoceni_conn = mysql_pconnect($hostname_hodnoceni_conn, $username_hodnoceni_conn, $password_hodnoceni_conn) or trigger_error(mysql_error(),E_USER_ERROR); \r\nif (!($hodnoceni_conn = mysql_pconnect($hostname_hodnoceni_conn, $username_hodnoceni_conn, $password_hodnoceni_conn))) {\r\n  DisplayErrMsg(sprintf(\"Chyba při připojování uživatele %s k hostiteli %s\", $username_hodnoceni_conn, $hostname_hodnoceni_conn));\r\n  exit();\r\n}\r\nif (!($hodnoceni_db = mysql_select_db($database_hodnoceni_conn, $hodnoceni_conn))) {\r\n  DisplayErrMsg(sprintf(\"Chyba při připojování databaze %s \", $database_hodnoceni_conn));\r\n  exit();\r\n}\r\n/**/\r\nmysql_query(\"SET character_set_connection=utf8mb4\");\r\nmysql_query(\"SET character_set_results=utf8mb4\");\r\nmysql_query(\"SET character_set_client=utf8mb4\");\r\nmysql_query('SET CHARACTER SET utf8mb4');\r\n\r\n?>")]
    public void MonaLikeConnect_Test(string fileName, string content)
    {
        //Arrange
        var file = new FileWrapper(Path.Join("Connections", fileName), content);
        //var upgrader = new RubiconUpgrader("../../../../..", string.Empty)
        //{ 
        //    ConnectionFile = "connect.php",
        //    RenameBetaWith = "alfa"
        //};
        //
        ////Act, Debug
        ////((RubiconConnectHandler)upgrader.ConnectHandler).UpgradeMonaLikeConnect(file, upgrader, fileName, varName);
        _output.WriteLine(file.Content.ToString());
    }

    [Fact]
    public void FillInDbLoginToSetup()
    {
        //Arrange
        var file = new FileWrapper(Path.Join("test-web", "setup.php"),
                                   "\n\n$setup_connect_db = \"olejemaziva\";\n" +
                                   "//$setup_connect_db = \"hasici-pristroje\";\n" +
                                   "$setup_connect_username = \"olejemaziva_use\";\n" +
                                   "$setup_connect_password = \"3_2n7dSj\"; \");\n");

        var upgrader = new RubiconUpgrader(string.Empty, "test-web")
        {
            Username = "myUserName", Password = "myPassword", Database = "myDatabase"
        };

        //Act
        RubiconConnectHandler.UpgradeSetup(file, upgrader);

        //Assert
        _output.WriteLine(file.Content.ToString());
        Assert.True(file.IsModified);
    }

    [Fact]
    public void UpdatesBetaHostnameToMcrai2()
    {
        //Arrange
        var file = new FileWrapper(Path.Join("Connections", "beta.php"),
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
        RubiconConnectHandler.UpgradeHostname(file, upgrader);

        //Assert
        _output.WriteLine(file.Content.ToString());
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
        file.UpgradeScriptLanguagePhp();

        //Assert
        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.Contains("<?php", contentStr);
        Assert.Contains("?>", contentStr);
        Assert.DoesNotContain("<script language=\"php\">", contentStr);
        Assert.DoesNotContain("<script language=\"PHP\">", contentStr);
    }

    [Fact]
    public void CommentsIncludesInProductDetail()
    {
        //Arrange
        var file = new FileWrapper(Path.Join("test-site", "templates", "amt", "product_detail.php"),
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
        file.UpgradeIncludesInHtmlComments();

        //Assert
        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.Contains("<?php //include", contentStr);
        Assert.Single(file.Warnings);
    }

    [Fact]
    public void ReplacesBreakWithReturnInAegisxDetail()
    {
        //Arrange
        var file = new FileWrapper(Path.Join("test-site", "aegisx", "detail.php"), "if ($presmeruj == \"NO\") {\r\n\t\t\tbreak;");

        //Act
        file.UpgradeAegisxDetail();

        //Assert
        var contentStr = file.Content.ToString();
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.DoesNotContain("break;", contentStr);
        Assert.Contains("return;", contentStr);
    }

    [Fact]
    public void UpdatesHostnameInDatabaseConnect()
    {
        //Arrange
        const string originalContent = "/* some stuff before */\n\n\tDatabase::connect('93.185.102.228', 'safety-jogger', 'Qhc1e2_5', 'safety-jogger', '5432');\n\n/* some stuff after */";
        var file = new FileWrapper(Path.Join("test-site", "index.php"), originalContent);

        //Act
        RubiconConnectHandler.UpgradeDatabaseConnectCall(file, new MonaUpgraderFixture(), oldHost: "93.185.102.228", newHost: "mcrai-upgrade.vshosting.cz");

        //Assert
        var contentStr = file.Content.ToString();
        _output.WriteLine(originalContent);
        _output.WriteLine("==============================");
        _output.WriteLine(contentStr);
        Assert.True(file.IsModified);
        Assert.DoesNotContain("\nDatabase::connect('93.185.102.228', 'safety-jogger', 'Qhc1e2_5', 'safety-jogger', '5432');", contentStr);
        Assert.Contains("//Database::connect('93.185.102.228', 'safety-jogger', 'Qhc1e2_5', 'safety-jogger', '5432');", contentStr);
        Assert.Contains("\n\tDatabase::connect('mcrai-upgrade.vshosting.cz', 'safety-jogger', 'Qhc1e2_5', 'safety-jogger', '5432');", contentStr);
    }

    [Fact]
    public void UpgradeOldUnparsableAlmostEmpty_Test()
    {
        //Arrange
        var content = "<?php\r\n\r\n\r\n\r\npublic XMLDiff\\File::diff ( string  , string $to ) : string";
        var file = new FileWrapper(Path.Join("some-website", "money", "old", "Compare_XML.php"), content);

        //Act
        file.UpgradeOldUnparsableAlmostEmptyFile();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("============================================================================================");
        var updatedContent = file.Content.ToString();
        _output.WriteLine(updatedContent);
        Assert.NotEqual(updatedContent, content);
    }

    [Fact]
    public void ShouldAddToFindReplace()
    {
        //Arrange & Act
        var monaFR = new MonaUpgrader(null, null).FindReplaceHandler;
        var rubiconFR = new RubiconUpgrader(null, null).FindReplaceHandler;

        //Assert
        _output.WriteLine($"Mona\tFR: {monaFR.Replacements.Count}");
        _output.WriteLine($"Rubicon\tFR: {rubiconFR.Replacements.Count}");
        Assert.True(monaFR.Replacements.Count < rubiconFR.Replacements.Count);
        Assert.Equal(nameof(MonaFindReplaceHandler), monaFR.GetType().Name);
        Assert.Equal(nameof(RubiconFindReplaceHandler), rubiconFR.GetType().Name);
    }
}

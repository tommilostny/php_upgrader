namespace PhpUpgrader.Tests;

public class CurlyBraceIndexingTests : UnitTestWithOutputBase
{
    public CurlyBraceIndexingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("\t\t\t\t\tif ($sublen == 6) {\r\n\t\t\t\t\t\t$t = bcmul(''.ord($code{0}), '1099511627776');\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{1}), '4294967296'));\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{2}), '16777216'));\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{3}), '65536'));\r\n\t\t\t\t\t\t$t = bcadd($t, bcmul(''.ord($code{4}), '256'));\r\n\t\t\t\t\t\t$t = bcadd($t, ''.ord($code{5}));\r\n\t\t\t\t\t\tdo {\r\n\t\t\t\t\t\t\t$d = bcmod($t, '900');\r\n\t\t\t\t\t\t\t$t = bcdiv($t, '900');\r\n\t\t\t\t\t\t\tarray_unshift($cw, $d);\r\n\t\t\t\t\t\t} while ($t != '0');\r\n\t\t\t\t\t} else {\r\n\t\t\t\t\t\tfor ($i = 0; $i < $sublen; ++$i) {\r\n\t\t\t\t\t\t\t$cw[] = ord($code{$i});\r\n\t\t\t\t\t\t}\r\n\t\t\t\t\t}\r\n\t\t\t\t\t$code = $rest;")]
    [InlineData("for ($s = 0; $s < $chrlen; $s++){\r\n\t\t\t\t$seq .= $chr[$char_bar]{$s} . $chr[$char_space]{$s};\r\n\t\t\t}\r\n\t\t\t$seqlen = strlen($seq);")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeCurlyBraceIndexing();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
    }

    [Theory]
    [InlineData("// otherwise, create data table & cache it\r\n                $sql = \"SELECT name as 'label', COUNT(*) as 'row_count'$extraCols FROM {$status['Name']} GROUP BY name\";\r\n\r\n                $ta")]
    [InlineData("if (method_exists($this, $this->endpoint))\r\n                return $this->_response($this->{$this->endpoint}($this->parameters));")]
    [InlineData("<?php \r\necho \"<hr />\";\r\n\tif ($last_skupina<>$row_data['skupina']) {  }\r\n\t$last_skupina = $row_data['skupina'];\r\n\t'start' => date('Y-m-d', $row['predpokladana_expedice']),\r\n\t\t\t'title' => \"{$row['doklad_n']} {$row['user_name']} {$row['user_surname']}\",\r\n\t\t\t'url' => \"a_load.php?menu=objednavky&order_id={$row['order_id']}\",")]
    [InlineData("function repeat_pattern($Xf, $re)\r\n{\r\n    return\r\n    str_repeat(\"$Xf{0,65535}\", $re/65535).\"$Xf{0,\".($re%65535).\"}\";\r\n}")]
    [InlineData("                else\r\n                {\r\n                    $guestTalk = false;\r\n\r\n                    $talkId = TalkModel::repo()->getTalkIdForUsers($from, $to);\r\n\r\n                    // Sort ids\r\n\r\n                    $user1 = $from;\r\n                    $user2 = $to;\r\n\r\n                    if($user1 > $user2)\r\n                    {\r\n                        $user1 = $user2;\r\n                        $user2 = $from;\r\n                    }\r\n\r\n                    $userTalkMapping[\"{$user1}_$user2\"] = $talkId;\r\n                }\r\n\r\n                // Make the other user watch the new talk\r\n\r\n                UserModel::repo()->addWatchedTalks($to, array($talkId));\r\n            }")]
    public void DoesNotUpgradeInvalidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeCurlyBraceIndexing();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.False(file.IsModified);
    }
}

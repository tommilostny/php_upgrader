﻿namespace PhpUpgrader.Tests;

public class ImplodeTests : UnitTestWithOutputBase
{
    public ImplodeTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("            $sql = 'UPDATE ' . Piwik_Common::prefixTable('log_conversion_item') . \"\r\n\t\t\t\t\tSET \" . implode($updateParts, ', ') . \"\r\n\t\t\t\t\t\tWHERE idvisit = ?\r\n\t\t\t\t\t\t\tAND idorder = ? \r\n\t\t\t\t\t\t\tAND idaction_sku = ?\";\r\n     return implode($output, \"\\n\") . \"\\n\";\r\n    }")]
    [InlineData("sql = 'UPDATE  ' . Piwik_Common::prefixTable('log_conversion') . \"\r\n\t\t\t\t\tSET \" . implode($updateParts, ', ') . \"\r\n\t\t\t\t\t\tWHERE \" . implode($updateWhereParts, ' AND ');\r\n            Piwik_Tracker::getDatabase()->query($sql, $sqlBind);\r\n            return true;")]
    [InlineData("        if(count($watchedTalksIds) > 0)\r\n        {\r\n            $watchedTalksStr = ' m.talk_id In (' . implode($watchedTalksIds, ',') . ') Or';\r\n        }\r\n\r\n        $getTalksStr = '';\r\n\r\n        if(count($getTalksIds) > 0)\r\n        {\r\n            $getTalksStr = ' m.talk_id In (' . implode($getTalksIds, ',') . ') Or';\r\n        }")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("file.php", content);

        //Act
        file.UpgradeImplode();

        //Assert
        _output.WriteLine(content);
        _output.WriteLine("=========================================================");
        var updated = file.Content.ToString();
        _output.WriteLine(updated);
        Assert.True(file.IsModified);
    }
}

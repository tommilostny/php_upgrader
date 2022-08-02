namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class ResultFuncion
{
    /// <summary>
    /// mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
    /// </summary>
    public static FileWrapper UpgradeResultFunction(this FileWrapper file, MonaUpgrader upgrader)
    {
        if (file.Path.EndsWith(Path.Join("funkce", "secure", "login.php")))
        {
            var content = file.Content.ToString();
            var updated = Regex.Replace(content,
                                        @"\$loginStrGroup\s*=\s*mysql_result\(\$LoginRS,\s*0,\s*'valid'\);\s*\n\s*\$loginUserid\s*=\s*mysql_result\(\$LoginRS,\s*0,\s*'user_id'\);",
                                        "mysqli_field_seek($LoginRS, 0);\n    $field = mysqli_fetch_field($LoginRS);\n    $loginStrGroup = $field->valid;\n    $loginUserid  = $field->user_id;\n    mysqli_free_result($LoginRS);");
            file.Content.Replace(content, updated);
        }

        var (oldResultFunc, newNumRowsFunc) = upgrader switch
        {
            RubiconUpgrader => ("pg_result", "pg_num_rows"),
            MonaUpgrader => ("mysql_result", "mysqli_num_rows")
        };
        if (!file.Content.Contains(oldResultFunc))
        {
            return file;
        }
        var lines = file.Content.Split();
        StringBuilder currentLine;

        for (var i = 0; i < lines.Count; i++)
        {
            if (!(currentLine = lines[i]).Contains(oldResultFunc))
            {
                continue;
            }
            const string countFunc = "COUNT(*)";
            var countIndex = currentLine.IndexOf(countFunc);
            if (countIndex == -1)
            {
                if (upgrader is not RubiconUpgrader)
                {
                    file.Warnings.Add($"Neobvyklé použití {oldResultFunc}!");
                    continue;
                }
                currentLine.Replace(oldResultFunc, "pg_fetch_result");
                continue;
            }
            currentLine.Replace(countFunc, "*", countIndex)
                       .Replace(", 0", string.Empty)
                       .Replace(oldResultFunc, newNumRowsFunc);
        }
        lines.JoinInto(file.Content);
        return file;
    }
}

namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class ResultFunction
{
    private static string[] _oldResultFuncs = new[] { "mysql_result", "pg_result" };
    private static string[] _newNumRowsFuncs = new[] { "mysqli_num_rows", "pg_num_rows" };

    /// <summary>
    /// mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
    /// </summary>
    public static FileWrapper UpgradeResultFunction(this FileWrapper file, MonaUpgrader upgrader)
    {
        if (file.Path.EndsWith(Path.Join("funkce", "secure", "login.php"), StringComparison.Ordinal))
        {
            var content = file.Content.ToString();
            var updated = LoginMysqlResultRegex().Replace(content, "mysqli_field_seek($LoginRS, 0);\n    $field = mysqli_fetch_field($LoginRS);\n    $loginStrGroup = $field->valid;\n    $loginUserid  = $field->user_id;\n    mysqli_free_result($LoginRS);");
            file.Content.Replace(content, updated);
        }
        if (_oldResultFuncs.All(f => !file.Content.Contains(f)))
        {
            return file;
        }
        var lines = file.Content.Split();
        StringBuilder currentLine;

        for (var i = 0; i < lines.Count; i++)
        {
            for (var j = 0; j < _oldResultFuncs.Length; j++)
            {
                var oldResultFunc = _oldResultFuncs[j];
                var newNumRowsFunc = _newNumRowsFuncs[j];

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
        }
        lines.JoinInto(file.Content);
        return file;
    }

    [GeneratedRegex(@"\$loginStrGroup\s*=\s*mysql_result\(\$LoginRS,\s*0,\s*'valid'\);\s*\n\s*\$loginUserid\s*=\s*mysql_result\(\$LoginRS,\s*0,\s*'user_id'\);", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex LoginMysqlResultRegex();
}

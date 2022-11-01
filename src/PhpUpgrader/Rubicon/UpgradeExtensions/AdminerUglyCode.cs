namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class AdminerUglyCode
{
    /// <summary>
    /// Minifikovaný špatně formátovaný kód v souboru 'adminer.php' rozhodí ostatní aktualizační rutiny.
    /// </summary>
    /// <remarks>
    /// Vyžaduje nainstalované PHP 7.4 a composer.
    /// Používá nástroj https://github.com/FriendsOfPHP/PHP-CS-Fixer.
    /// </remarks>
    public static void UpgradeAdminerUglyCode(this RubiconUpgrader upgrader, string filePath)
    {
        if (filePath.EndsWith("adminer.php", StringComparison.Ordinal))
        {
            BackupManager.CreateBackupFile(filePath, upgrader.BaseFolder, upgrader.WebName, modified: true);
            try
            {
                PhpCsFixerWrapper.FormatPhp(filePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Při spouštění php-cs-fixer došlo k chybě. Je správně nainstalovaný a spustitelný PHP composer?");
                Console.Error.WriteLine("Chybová zpráva:");
                Console.Error.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }

    public static FileWrapper UpgradeAdminerMysql(this FileWrapper file)
    {
        if (file.Path.EndsWith($"{Path.DirectorySeparatorChar}adminer.php", StringComparison.Ordinal))
        {
            file.Content.Replace("mysql_get_server_info($this->_link)", "mysqli_get_server_info($this->_link)")
                        .Replace("mysql_set_charset($bb, $this->_link)", "mysqli_set_charset($this->_link, $bb)")
                        .Replace("mysql_set_charset('utf8', $this->_link)", "mysqli_set_charset($this->_link, 'utf8')")
                        .Replace("mysql_select_db($k, $this->_link)", "mysqli_select_db($this->_link, $k)")
                        .Replace("mysql_unbuffered_query($G, $this->_link)", "mysqli_query($this->_link, $G, MYSQLI_USE_RESULT)")
                        .Replace("mysql_errno($this->_link)", "mysqli_errno($this->_link)")
                        .Replace("mysql_error($this->_link)", "mysqli_error($this->_link)")
                        .Replace("mysql_affected_rows($this->_link)", "mysqli_affected_rows($this->_link)")
                        .Replace("mysql_info($this->_link)", "mysqli_info($this->_link)")
                        .Replace("$I=mysql_fetch_field($this->_result, $this->_offset++);",
                                 "mysqli_field_seek($this->_result, $this->_offset++);\n                $I=mysqli_fetch_field($this->_result);");

            var content = file.Content.ToString();
            var evaluator = new MatchEvaluator(MysqlResultEvaluator);
            var updated = ReturnMysqlResultRegex().Replace(content, evaluator);
            file.Content.Replace(content, updated);
        }
        return file;
    }

    private static string MysqlResultEvaluator(Match match)
    {
        var result = match.Groups["result"].Value.Trim();
        var row = match.Groups["row"].Value.Trim();
        var field = match.Groups["field"].Value.Trim();
        const string ws = "                ";

        return $"mysqli_data_seek({result}, {row});\n{ws}mysqli_field_seek({result}, {field});\n{ws}return mysqli_fetch_field({result})";
    }

    [GeneratedRegex(@"return\s+?mysql_result\((?<result>.+?),(?<row>.+?),(?<field>.+?)\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1234)]
    private static partial Regex ReturnMysqlResultRegex();
}

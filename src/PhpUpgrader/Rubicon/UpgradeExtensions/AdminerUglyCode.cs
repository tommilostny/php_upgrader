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
        if (!IsAdminer(filePath))
        {
            return;
        }
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

    public static FileWrapper UpgradeAdminerMysql(this FileWrapper file)
    {
        if (IsAdminer(file.Path))
        {
            file.Content.Replace("mysql_get_server_info($this->_link)", "mysqli_get_server_info($this->_link)")
                        .Replace("mysql_errno($this->_link)", "mysqli_errno($this->_link)")
                        .Replace("mysql_error($this->_link)", "mysqli_error($this->_link)")
                        .Replace("mysql_affected_rows($this->_link)", "mysqli_affected_rows($this->_link)")
                        .Replace("mysql_info($this->_link)", "mysqli_info($this->_link)")
                        .Replace("mysqli_error($beta);", "mysqli_error($this->_link);")
                        .Replace("mysqli_query($beta, ", "mysqli_query($this->_link, ")
                        .Replace(", $this->_link));", "));")
                        .Replace("function_exists('mysql_", "function_exists('mysqli_");
            UpgradeMysqlResult(file);
            UpgradeMysqlTwoVarsSwitch(file);
            UpgradeMysqlFetchField(file);
        }
        return file;
    }

    private static bool IsAdminer(string path)
    {
        return path.EndsWith($"{Path.DirectorySeparatorChar}adminer.php", StringComparison.Ordinal)
            || path.EndsWith(Path.Join("adminer", "index.php"), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"return\s+?mysql_result\((?<result>.+?),(?<row>.+?),(?<field>.+?)\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 6666)]
    private static partial Regex ReturnMysqlResultRegex();

    private static void UpgradeMysqlResult(FileWrapper file)
    {
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(MysqlResultEvaluator);
        var updated = ReturnMysqlResultRegex().Replace(content, evaluator);
        file.Content.Replace(content, updated);

        static string MysqlResultEvaluator(Match match)
        {
            var result = match.Groups["result"].Value.Trim();
            var row = match.Groups["row"].Value.Trim();
            var field = match.Groups["field"].Value.Trim();
            const string ws = "                ";

            return $"mysqli_data_seek({result}, {row});\n{ws}mysqli_field_seek({result}, {field});\n{ws}return mysqli_fetch_field({result})";
        }
    }

    [GeneratedRegex(@"(?<func>(mysql_(set_charset|select_db|unbuffered_query))|mysqli_real_escape_string)\s?\((?<var1>.+?)\s?,\s?(?<var2>.+?)\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 6666)]
    private static partial Regex MysqlTwoVarsSwitchRegex();

    private static void UpgradeMysqlTwoVarsSwitch(FileWrapper file)
    {
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(MysqlTwoVarsSwitchEvaluator);
        var updated = MysqlTwoVarsSwitchRegex().Replace(content, evaluator);
        file.Content.Replace(content, updated);

        static string MysqlTwoVarsSwitchEvaluator(Match match)
        {
            var func = match.Groups["func"].Value.Replace("mysql_", "mysqli_", StringComparison.Ordinal);
            var var1 = match.Groups["var1"].Value.Trim();
            var var2 = match.Groups["var2"].Value.Trim();

            return string.Equals(func, "mysqli_unbuffered_query", StringComparison.Ordinal)
                ? $"mysqli_query({var2}, {var1}, MYSQLI_USE_RESULT)"
                : $"{func}({var2}, {var1})";
        }
    }

    [GeneratedRegex(@"(?<var1>\$.+?)\s?=\s?mysql_fetch_field\s?\((?<invar1>.+?)\s?,\s?(?<invar2>.+?)\);", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 6666)]
    private static partial Regex MysqlFetchFieldRegex();

    private static void UpgradeMysqlFetchField(FileWrapper file)
    {
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(MysqlFetchFieldEvaluator);
        var updated = MysqlFetchFieldRegex().Replace(content, evaluator);
        file.Content.Replace(content, updated);

        static string MysqlFetchFieldEvaluator(Match match)
        {
            var var1 = match.Groups["var1"].Value.Trim();
            var invar1 = match.Groups["invar1"].Value.Trim();
            var invar2 = match.Groups["invar2"].Value.Trim();

            return $"mysqli_field_seek({invar1}, {invar2});\n                {var1}=mysqli_fetch_field({invar1});";
        }
    }
}

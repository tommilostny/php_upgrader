using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PhpUpgrader
{
    /// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
    public class RubiconUpgrader : MonaUpgrader
    {
        /// <summary>  </summary>
        public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
        {
            FindReplace.Add("mysql_select_db($database_beta);", "//mysql_select_db($database_beta);");
            FindReplace.Add("////mysql_select_db($database_beta);", "//mysql_select_db($database_beta);");
            FindReplace.Add("function_exists(\"mysqli_real_escape_string\") ? mysqli_real_escape_string($theValue) : mysql_escape_string($theValue)",
                            "mysqli_real_escape_string($beta, $theValue)");
            FindReplace.Add("mysql_select_db($database_sportmall_import, $sportmall_import);", "mysqli_select_db($sportmall_import, $database_sportmall_import);");
            FindReplace.Add("mysqli_query($beta,$query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))", "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))");
            FindReplace.Add(@"preg_match(""^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$"", $email)", @"preg_match("";^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$;"", $email)");
        }

        /// <summary> Procedura aktualizace Rubicon souborů. </summary>
        /// <remarks> Použita ve volání metody <see cref="MonaUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
        /// <returns> Upravený soubor. </returns>
        protected override FileWrapper UpgradeProcedure(string filePath)
        {
            var file = base.UpgradeProcedure(filePath);

            if (file is not null)
            {
                UpgradeConstructors(file);
                UpgradeScriptLanguagePhp(file);
                UpgradeIncludesInHtmlComments(file);
                UpgradeAegisxDetail(file);
                UpgradeLoadData(file);
                UpgradeHomeTopProducts(file);
            }
            return file;
        }

        /// <summary> Old style constructor function ClassName() => function __construct() </summary>
        public void UpgradeConstructors(FileWrapper file)
        {
            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++) //procházení řádků souboru
            {
                if (!lines[i].Contains("class "))
                    continue;

                int nameStartIndex = lines[i].IndexOf("class ") + 6;

                int nameEndIndex = lines[i].IndexOf(' ', nameStartIndex);
                if (nameEndIndex == -1)
                    nameEndIndex = lines[i].IndexOf('{', nameStartIndex);

                var className = lines[i][nameStartIndex..(nameEndIndex != -1 ? nameEndIndex : lines[i].Length)].Trim();

                int bracketCount = Convert.ToInt32(lines[i].Contains('{'));

                if (bracketCount == 0 && !lines[i + 1].Contains('{'))
                    continue;

                if (_LookAheadFor__construct(bracketCount, i + 1)) //třída obsahuje metodu __construct(), nehledat starý konstruktor
                    continue;

                bool inComment = lines[i].Contains("/*");
                while (++i < lines.Length) //hledání a nahrazení starého konstruktoru uvnitř třídy
                {
                    if (lines[i].Contains("/*")) inComment = true;
                    if (lines[i].Contains("*/")) inComment = false;

                    if (inComment || lines[i].TrimStart().StartsWith("//"))
                        continue;

                    if (lines[i].Contains('{')) bracketCount++;
                    if (lines[i].Contains('}')) bracketCount--;

                    if (bracketCount == 0)
                        break;

                    if (bracketCount > 2 && lines[i].TrimStart().StartsWith("function"))
                    {
                        file.Warnings.Add($"Large bracket count ({bracketCount}), function around line {i + 1}. Check constructor(s) of class {className}.");
                        bracketCount = 2;
                    }
                    if (Regex.IsMatch(lines[i], $@"function {className}\s?\("))
                    {
                        int paramsStartIndex = lines[i].IndexOf('(') + 1;
                        int paramsEndIndex = lines[i].LastIndexOf(')');

                        var parameters = lines[i][paramsStartIndex..paramsEndIndex];

                        lines[i] = lines[i].Replace($"function {className}", "function __construct");
                        lines[i] = $"    public function {className}({parameters})\n" +
                                    "    {\n" +
                                   $"        self::__construct({_ParamsWithoutDefaultValues(parameters)});\n" +
                                   $"    }}\n\n{lines[i]}";
                    }
                }
            }
            file.Content = string.Join('\n', lines);

            static string _ParamsWithoutDefaultValues(string parameters)
            {
                return string.Join(", ", parameters.Split(',').Select(p => p.Split('=')[0].Trim().Replace("&", string.Empty)));
            }

            bool _LookAheadFor__construct(int bracketCount, int linesIndex)
            {
                for (; linesIndex < lines.Length; linesIndex++)
                {
                    if (lines[linesIndex].Contains('{')) bracketCount++;
                    if (lines[linesIndex].Contains('}')) bracketCount--;

                    if (bracketCount == 0)
                        break;

                    if (lines[linesIndex].Contains("function __construct"))
                        return true;
                }
                return false;
            }
        }

        /// <summary> Aktualizace souborů připojení systému Rubicon. </summary>
        public override void UpgradeConnect(FileWrapper file)
        {
            UpgradeRubiconImport(file);
            UpgradeSetup(file);
            UpgradeHostnameBeta(file);
        }

        /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php,  </summary>
        public void UpgradeRubiconImport(FileWrapper file)
        {
            if (!file.Path.Contains("Connections\\rubicon_import.php"))
                return;

            var backup = (ConnectionFile, Username, Password, Database);
            ConnectionFile = "rubicon_import.php";
            Database = "eshop-products_n";
            Username = "eshop-products_u";
            Password = "bZcg386!";
            
            base.UpgradeConnect(file);
            file.Content = RenameBeta(file.Content, "sportmall_import");

            file.Content = file.Content.Replace("mysqli_query($sportmall_import, \"SET CHARACTER SET utf8\");",
                "mysqli_query($sportmall_import, \"SET character_set_connection = cp1250\");\n" +
                "mysqli_query($sportmall_import, \"SET character_set_results = cp1250\");\n" +
                "mysqli_query($sportmall_import, \"SET character_set_client = cp1250\");");

            (ConnectionFile, Username, Password, Database) = backup;
        }

        /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
        public void UpgradeSetup(FileWrapper file)
        {
            switch (file)
            {
                case { Path: var p } when !p.Contains($"{WebName}\\setup.php"):
                case { Content: var c } when c.Contains($"password = '{Password}';"):
                    return;
            }
            bool usernameLoaded = false, passwordLoaded = false, databaseLoaded = false;

            var evaluator = new MatchEvaluator(_NewCredentialAndComment);
            file.Content = Regex.Replace(file.Content, @"\$setup_connect.*= ?"".*"";", evaluator);
            file.Content = file.Content.Replace("////", "//");

            if (!usernameLoaded)
                file.Warnings.Add("setup.php - username not loaded.");
            if (!passwordLoaded)
                file.Warnings.Add("setup.php - password not loaded.");
            if (!databaseLoaded)
                file.Warnings.Add("setup.php - database not loaded.");

            file.Warnings.Add("setup.php - check db connections and such.");

            string _NewCredentialAndComment(Match match)
            {
                if (file.Content[..match.Index].EndsWith("//"))
                    return match.Value;

                var varName = match.Value.Split('=')[0].Trim();
                var credential = varName switch
                {
                    var vn when vn.EndsWith("username") && (usernameLoaded = true) => Username,
                    var vn when vn.EndsWith("password") && (passwordLoaded = true) => Password,
                    var vn when vn.EndsWith("db") && (databaseLoaded = true) => Database,
                    _ => null
                };
                if (credential is null)
                    return match.Value;

                return $"//{match.Value}\n{varName} = '{credential}';";
            }
        }

        /// <summary> Aktualizace hostname v souboru Connections/beta.php na server mcrai2. </summary>
        public void UpgradeHostnameBeta(FileWrapper file)
        {
            if (file.Path.Contains("Connections\\beta.php") && !file.Content.Contains($"$hostname_beta = \"{Hostname}\";"))
            {
                file.Content = file.Content.Replace("$hostname_beta = \"93.185.102.228\";", $"//$hostname_beta = \"93.185.102.228\";\n\t$hostname_beta = \"{Hostname}\";");
            }
            if (!file.Content.Contains("//Database::connect('93.185.102.228'"))
            {
                file.Content = file.Content.Replace("Database::connect('93.185.102.228', $username_beta, $password_beta, $database_beta, '5432');",
                    "//Database::connect('93.185.102.228', $username_beta, $password_beta, $database_beta, '5432');\nDatabase::connect('mcrai2.vshosting.cz', $username_beta, $password_beta, $database_beta, '5432');");
            }
        }

        /// <summary> Přidá funkci pg_close na konec index.php. </summary>
        public override void UpgradeMysqliClose(FileWrapper file)
        {
            if (file.Path.Contains($@"{WebName}\index.php") && !file.Content.Contains("pg_close"))
            {
                file.Content += "\n<?php pg_close($beta); ?>";
            }
        }

        /// <summary> HTML tag &lt;script language="PHP"&gt;&lt;/script> deprecated => &lt;?php ?&gt; </summary>
        public static void UpgradeScriptLanguagePhp(FileWrapper file)
        {
            if (!Regex.IsMatch(file.Content, @"<script language=""PHP"">", RegexOptions.IgnoreCase))
                return;

            var lines = file.Content.Split('\n');
            bool insidePhpScriptTag = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"<script language=""PHP"">", RegexOptions.IgnoreCase))
                {
                    lines[i] = Regex.Replace(lines[i], @"<script language=""PHP"">", "<?php ", RegexOptions.IgnoreCase);
                    insidePhpScriptTag = true;
                }
                if (insidePhpScriptTag && lines[i].Contains("</script>"))
                {
                    lines[i] = lines[i].Replace("</script>", " ?>");
                    insidePhpScriptTag = false;
                }
            }
            file.Content = string.Join('\n', lines);
            file.Warnings.Add("Found <script language=\"PHP\">. Better check JavaScript.");
        }

        /// <summary> templates/.../product_detail.php, zakomentovaný blok HTML stále spouští broken PHP includy, zakomentovat </summary>
        public static void UpgradeIncludesInHtmlComments(FileWrapper file)
        {
            if (!Regex.IsMatch(file.Path, @"\\templates\\.+\\product_detail\.php"))
                return;

            var lines = file.Content.Split('\n');
            bool insideHtmlComment = false;
            bool commentedAtLeastOneInclude = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<!--"))
                    insideHtmlComment = true;

                if (lines[i].Contains("-->"))
                    insideHtmlComment = false;

                if (insideHtmlComment && (commentedAtLeastOneInclude |= lines[i].Contains("<?php include")))
                {
                    lines[i] = lines[i].Replace("<?php include", "<?php //include");
                }
            }
            file.Content = string.Join('\n', lines);

            if (commentedAtLeastOneInclude)
                file.Warnings.Add("check commented includes");
        }

        /// <summary> [Break => Return] v souboru aegisx\detail.php (není ve smyčce, ale included). </summary>
        public static void UpgradeAegisxDetail(FileWrapper file)
        {
            if (!file.Path.Contains(@"aegisx\detail.php"))
                return;

            file.Content = Regex.Replace(file.Content, @"if\s?\(\$presmeruj == ""NO""\)\s*\{\n\s*break;", "if ($presmeruj == \"NO\") {\n\t\t\treturn;");
        }

        /// <summary> Úprava mysql a proměnné $beta v souboru aegisx\import\load_data.php. </summary>
        public static void UpgradeLoadData(FileWrapper file)
        {
            if (!file.Path.Contains(@"aegisx\import\load_data.php"))
                return;

            file.Content = file.Content.Replace("global $beta;", "global $sportmall_import;");
            file.Content = file.Content.Replace("mysqli_real_escape_string($beta,", "mysqli_real_escape_string($sportmall_import,");
        }

        /// <summary> Úprava SQL dotazu na top produkty v souboru aegisx\home.php. </summary>
        public static void UpgradeHomeTopProducts(FileWrapper file)
        {
            if (!file.Path.Contains(@"aegisx\home.php"))
                return;

            file.Content = file.Content.Replace("SELECT product_id, COUNT(orders_data.order_id) AS num_orders FROM orders_data, orders WHERE orders_data.order_id=orders.order_id AND \" . getSQLLimit3Months() . \" GROUP BY product_id ORDER BY num_orders DESC LIMIT 1",
                "SELECT product_id, COUNT(orders_data.order_id) AS num_orders FROM orders_data, orders WHERE orders_data.order_id=orders.order_id AND \" . getSQLLimit3Months() . \" AND product_id IN (SELECT product_id FROM product_info) GROUP BY product_id ORDER BY num_orders DESC LIMIT 1");
        }
    }
}

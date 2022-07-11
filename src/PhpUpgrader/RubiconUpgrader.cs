namespace PhpUpgrader;

/// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
public class RubiconUpgrader : MonaUpgrader
{
    /// <summary> Obsahuje právě aktualizovaný web soubor classes\Object.php? </summary>
    private readonly bool _containsObjectClass;

    /// <summary> Konstruktor Rubicon > Mona upgraderu. </summary>
    /// <remarks> Přidá specifické případy pro Rubicon do <see cref="MonaUpgrader.FindReplace"/>. </remarks>
    public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
    {
        FindReplace.Add("mysql_select_db($database_beta);", "//mysql_select_db($database_beta);");
        FindReplace.Add("////mysql_select_db($database_beta);", "//mysql_select_db($database_beta);");
        FindReplace.Add("function_exists(\"mysqli_real_escape_string\") ? mysqli_real_escape_string($theValue) : mysql_escape_string($theValue)",
                        "mysqli_real_escape_string($beta, $theValue)");
        FindReplace.Add("mysql_select_db($database_sportmall_import, $sportmall_import);", "mysqli_select_db($sportmall_import, $database_sportmall_import);");
        FindReplace.Add("mysqli_query($beta,$query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))", "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))");
        FindReplace.Add(@"preg_match(""^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$"", $email)", @"preg_match("";^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$;"", $email)");
        FindReplace.Add("emptiable(strip_tags($obj->category_name.', '.$obj->style_name)), $title)", "emptiable(strip_tags($obj->category_name.', '.$obj->style_name), $title))");

        _containsObjectClass = File.Exists(Path.Join(BaseFolder, "weby", WebName, "classes", "Object.php"));
    }

    /// <summary> Procedura aktualizace Rubicon souborů. </summary>
    /// <remarks> Použita ve volání metody <see cref="MonaUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
    /// <returns> Upravený soubor. </returns>
    protected override FileWrapper UpgradeProcedure(string filePath)
    {
        var file = base.UpgradeProcedure(filePath);

        if (file is not null)
        {
            UpgradeObjectClass(file);
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
    public static void UpgradeConstructors(FileWrapper file)
    {
        var lines = file.Content.Split();

        for (int i = 0; i < lines.Count; i++) //procházení řádků souboru
        {
            if (!lines[i].Contains("class "))
                continue;

            var line = lines[i];
            var lineStr = lines[i].ToString();

            int nameStartIndex = lineStr.IndexOf("class ") + 6;

            int nameEndIndex = lineStr.IndexOf(' ', nameStartIndex);
            if (nameEndIndex == -1)
                nameEndIndex = lineStr.IndexOf('{', nameStartIndex);

            var className = lineStr[nameStartIndex..(nameEndIndex != -1 ? nameEndIndex : line.Length)].Trim();

            int bracketCount = Convert.ToInt32(line.Contains('{'));

            if (bracketCount == 0 && !lines[i + 1].Contains('{'))
                continue;

            if (_LookAheadFor__construct(bracketCount, i + 1)) //třída obsahuje metodu __construct(), nehledat starý konstruktor
                continue;

            bool inComment = line.Contains("/*");
            while (++i < lines.Count) //hledání a nahrazení starého konstruktoru uvnitř třídy
            {
                line = lines[i];
                lineStr = line.ToString();

                if (line.Contains("/*")) inComment = true;
                if (line.Contains("*/")) inComment = false;

                if (inComment || lineStr.TrimStart().StartsWith("//"))
                    continue;

                if (line.Contains('{')) bracketCount++;
                if (line.Contains('}')) bracketCount--;

                if (bracketCount == 0)
                    break;

                if (bracketCount > 2 && lineStr.TrimStart().StartsWith("function"))
                {
                    file.Warnings.Add($"Neočekávaný počet složených závorek ({bracketCount}), funkce okolo řádku {i + 1}. Zkontrolovat konstruktor(y) třídy {className}.");
                    bracketCount = 2;
                }
                if (Regex.IsMatch(lineStr, $@"function {className}\s?\(.*\)"))
                {
                    int paramsStartIndex = lineStr.IndexOf('(') + 1;
                    int paramsEndIndex = lineStr.LastIndexOf(')');

                    var @params = lineStr[paramsStartIndex..paramsEndIndex];

                    line.Replace($"function {className}", "function __construct");

                    var compatibilityConstructorBuilder = new StringBuilder()
                        .AppendLine($"    public function {className}({@params})")
                        .AppendLine( "    {")
                        .AppendLine($"        self::__construct({_ParamsWithoutDefaultValues(@params)});")
                        .AppendLine( "    }")
                        .AppendLine();

                    line.Insert(0, compatibilityConstructorBuilder.ToString());
                }
            }
        }
        lines.JoinInto(file.Content);

        if (!file.IsModified && file.Warnings.Count > 0)
        {
            file.Warnings.Remove(file.Warnings.FirstOrDefault(w => w.StartsWith("Large bracket count (")));
        }

        static string _ParamsWithoutDefaultValues(string parameters)
        {
            var sb = new StringBuilder(parameters);
            var @params = sb.Split(',');
            for (int i = 0; i < @params.Count; i++)
            {
                var param = @params[i];
                var name = param.Split('=')[0].Replace(" ", string.Empty).Replace("&", string.Empty);
                param.Clear();
                param.Append(name);
            }
            @params.JoinInto(sb, ", ");
            return sb.ToString();
        }

        bool _LookAheadFor__construct(int bracketCount, int linesIndex)
        {
            for (; linesIndex < lines.Count; linesIndex++)
            {
                var line = lines[linesIndex];

                if (line.Contains('{')) bracketCount++;
                if (line.Contains('}')) bracketCount--;

                if (bracketCount == 0)
                    break;

                if (line.Contains("function __construct"))
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
        UpgradeHostnameFromMcrai1IP(file);
    }

    /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php,  </summary>
    public void UpgradeRubiconImport(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("Connections", "rubicon_import.php")))
        {
            return;
        }
        var backup = (ConnectionFile, Username, Password, Database);
        ConnectionFile = "rubicon_import.php";
        Database = "eshop-products_n";
        Username = "eshop-products_u";
        Password = "bZcg386!";
        
        base.UpgradeConnect(file);
        RenameBeta(file.Content, "sportmall_import");

        var replacements = new StringBuilder()
            .AppendLine("mysqli_query($sportmall_import, \"SET character_set_connection = cp1250\");")
            .AppendLine("mysqli_query($sportmall_import, \"SET character_set_results = cp1250\");")
            .Append("mysqli_query($sportmall_import, \"SET character_set_client = cp1250\");");

        file.Content.Replace("mysqli_query($sportmall_import, \"SET CHARACTER SET utf8\");",
                             replacements.ToString());

        (ConnectionFile, Username, Password, Database) = backup;
    }

    /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
    public void UpgradeSetup(FileWrapper file)
    {
        if (Database is null || Username is null || Password is null)
        {
            return;
        }
        switch (file)
        {
            case { Path: var p } when !p.Contains(Path.Join(WebName, "setup.php")):
            case { Content: var c } when c.Contains($"password = '{Password}';"):
                return;
        }

        bool usernameLoaded = false, passwordLoaded = false, databaseLoaded = false;
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(_NewCredentialAndComment);

        content = Regex.Replace(content, @"\$setup_connect.*= ?"".*"";", evaluator);
        file.Content.Clear();
        file.Content.Append(content);
        file.Content.Replace("////", "//");

        if (!usernameLoaded)
            file.Warnings.Add("setup.php - nenačtené přihlašovací jméno.");
        if (!passwordLoaded)
            file.Warnings.Add("setup.php - nenačtené heslo.");
        if (!databaseLoaded)
            file.Warnings.Add("setup.php - nenačtený název databáze.");

        file.Warnings.Add("setup.php - zkontrolovat připojení k databázi atd..");

        string _NewCredentialAndComment(Match match)
        {
            if (content[..match.Index].EndsWith("//"))
            {
                return match.Value;
            }
            var varName = match.Value.Split('=')[0].Trim();
            var credential = varName switch
            {
                var vn when vn.EndsWith("username") && (usernameLoaded = true) => Username,
                var vn when vn.EndsWith("password") && (passwordLoaded = true) => Password,
                var vn when vn.EndsWith("db") && (databaseLoaded = true) => Database,
                _ => null
            };
            return credential is null ? match.Value : $"//{match.Value}\n{varName} = '{credential}';";
        }
    }

    /// <summary> Aktualizace hostname z mcrai1 na server mcrai2. </summary>
    public void UpgradeHostnameFromMcrai1IP(FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("Connections", "beta.php"))
            && !file.Content.Contains($"$hostname_beta = \"{Hostname}\";"))
        {
            file.Content.Replace("$hostname_beta = \"93.185.102.228\";", $"//$hostname_beta = \"93.185.102.228\";\n\t$hostname_beta = \"{Hostname}\";");
            file.Content.Replace("$hostname_beta = \"localhost\";", $"//$hostname_beta = \"localhost\";\n\t$hostname_beta = \"{Hostname}\";");
        }
        if (!file.Content.Contains($"Database::connect('{Hostname}'"))
        {
            file.Content.Replace("Database::connect('93.185.102.228', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');",
                $"//Database::connect('93.185.102.228', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');\n\tDatabase::connect('{Hostname}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');");
            file.Content.Replace("Database::connect('93.185.102.228', $username_beta, $password_beta, $database_beta, '5432');",
                $"//Database::connect('93.185.102.228', $username_beta, $password_beta, $database_beta, '5432');\n\tDatabase::connect('{Hostname}', $username_beta, $password_beta, $database_beta, '5432');");
        }
        if (!file.Content.Contains($"$api = new RubiconAPI($_REQUEST['url'], '{Hostname}'"))
        {
            file.Content.Replace("$api = new RubiconAPI($_REQUEST['url'], '93.185.102.228', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');",
                $"//$api = new RubiconAPI($_REQUEST['url'], '93.185.102.228', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');\n\t$api = new RubiconAPI($_REQUEST['url'], '{Hostname}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');");
        }
    }

    /// <summary> Přidá funkci pg_close na konec index.php. </summary>
    public override void UpgradeMysqliClose(FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join(WebName, "index.php")) && !file.Content.Contains("pg_close"))
        {
            file.Content.AppendLine();
            file.Content.Append("<?php pg_close($beta); ?>");
        }
    }

    /// <summary> HTML tag &lt;script language="PHP"&gt;&lt;/script> deprecated => &lt;?php ?&gt; </summary>
    public static void UpgradeScriptLanguagePhp(FileWrapper file)
    {
        if (!Regex.IsMatch(file.Content.ToString(), @"<script language=""PHP"">", _regexIgnoreCaseOpts))
        {
            return;
        }
        var lines = file.Content.Split();
        bool insidePhpScriptTag = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineStr = line.ToString();
            if (Regex.IsMatch(lineStr, @"<script language=""PHP"">", _regexIgnoreCaseOpts))
            {
                line.Clear();
                line.Append(Regex.Replace(lineStr, @"<script language=""PHP"">", "<?php ", _regexIgnoreCaseOpts));
                insidePhpScriptTag = true;
            }
            if (insidePhpScriptTag && line.Contains("</script>"))
            {
                line.Replace("</script>", " ?>");
                insidePhpScriptTag = false;
            }
        }
        lines.JoinInto(file.Content);
        file.Warnings.Add("Nalezena značka <script language=\"PHP\">. Zkontrolovat možný Javascript.");
    }

    /// <summary> templates/.../product_detail.php, zakomentovaný blok HTML stále spouští broken PHP includy, zakomentovat </summary>
    public static void UpgradeIncludesInHtmlComments(FileWrapper file)
    {
        if (!Regex.IsMatch(file.Path, @"(\\|/)templates(\\|/).+(\\|/)product_detail\.php", _regexCompiledOpts))
        {
            return;
        }
        var lines = file.Content.Split();
        bool insideHtmlComment = false;
        bool commentedAtLeastOneInclude = false;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("<!--"))
            {
                insideHtmlComment = true;
            }
            if (lines[i].Contains("-->"))
            {
                insideHtmlComment = false;
            }
            if (insideHtmlComment && (commentedAtLeastOneInclude |= lines[i].Contains("<?php include")))
            {
                lines[i].Replace("<?php include", "<?php //include");
            }
        }
        lines.JoinInto(file.Content);

        if (commentedAtLeastOneInclude)
        {
            file.Warnings.Add("Zkontrolovat HTML zakomentované '<?php include'.");
        }
    }

    /// <summary> [Break => Return] v souboru aegisx\detail.php (není ve smyčce, ale included). </summary>
    public static void UpgradeAegisxDetail(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("aegisx", "detail.php")))
        {
            return;
        }
        var contentStr = file.Content.ToString();
        file.Content.Clear();
        file.Content.Append(Regex.Replace(contentStr, @"if\s?\(\$presmeruj == ""NO""\)\s*\{\n\s*break;", "if ($presmeruj == \"NO\") {\n\t\t\treturn;"));
    }

    /// <summary> Úprava mysql a proměnné $beta v souboru aegisx\import\load_data.php. </summary>
    public static void UpgradeLoadData(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("aegisx", "import", "load_data.php")))
        {
            return;
        }
        file.Content.Replace("global $beta;", "global $sportmall_import;");
        file.Content.Replace("mysqli_real_escape_string($beta,", "mysqli_real_escape_string($sportmall_import,");
    }

    /// <summary> Úprava SQL dotazu na top produkty v souboru aegisx\home.php. </summary>
    public static void UpgradeHomeTopProducts(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("aegisx", "home.php")))
        {
            return;
        }
        file.Content.Replace("SELECT product_id, COUNT(orders_data.order_id) AS num_orders FROM orders_data, orders WHERE orders_data.order_id=orders.order_id AND \" . getSQLLimit3Months() . \" GROUP BY product_id ORDER BY num_orders DESC LIMIT 1",
            "SELECT product_id, COUNT(orders_data.order_id) AS num_orders FROM orders_data, orders WHERE orders_data.order_id=orders.order_id AND \" . getSQLLimit3Months() . \" AND product_id IN (SELECT product_id FROM product_info) GROUP BY product_id ORDER BY num_orders DESC LIMIT 1");
    }

    /// <summary> Aktualizace třídy Object => ObjectBase. Provádí se pouze pokud existuje soubor classes\Object.php. </summary>
    /// <remarks> + extends Object, @param Object, @property Object </remarks>
    public void UpgradeObjectClass(FileWrapper file)
    {
        if (!_containsObjectClass)
        {
            return;
        }
        if (file.Path.EndsWith(Path.Join("classes", "Object.php")) && file.Content.Contains("abstract class Object"))
        {
            file.Content.Replace("abstract class Object", "abstract class ObjectBase");
            var contentStr = file.Content.ToString();
            file.Content.Clear();
            file.Content.Append(Regex.Replace(contentStr, @"function\s+Object\s*\(", "function ObjectBase("));
            file.MoveOnSavePath = file.Path.Replace(Path.Join("classes", "Object.php"),
                                                    Path.Join("classes", "ObjectBase.php"));
        }
        file.Content.Replace("extends Object", "extends ObjectBase");
        file.Content.Replace("@param Object", "@param ObjectBase");
        file.Content.Replace("@property  Object", "@property  ObjectBase");
    }
}

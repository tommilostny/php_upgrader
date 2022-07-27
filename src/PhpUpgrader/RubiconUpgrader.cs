using System.Text.RegularExpressions;

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
        _containsObjectClass = File.Exists(Path.Join(baseFolder, "weby", webName, "classes", "Object.php"));

        //Přidat do FindReplace dvojice pro nahrazení specifické pro Rubicon.
        new List<KeyValuePair<string, string>>
        {
            new("mysql_select_db($database_beta);",
                "//mysql_select_db($database_beta);"
            ),
            new("////mysql_select_db($database_beta);",
                "//mysql_select_db($database_beta);"
            ),
            new("function_exists(\"mysqli_real_escape_string\") ? mysqli_real_escape_string($theValue) : mysql_escape_string($theValue)",
                "mysqli_real_escape_string($beta, $theValue)"
            ),
            new("mysql_select_db($database_sportmall_import, $sportmall_import);",
                "mysqli_select_db($sportmall_import, $database_sportmall_import);"
            ),
            new("mysql_select_db($database_iviki_mysql, $iviki_mysql);",
                "mysqli_select_db($iviki_mysql, $database_iviki_mysql);"
            ),
            new("mysqli_query($beta, $query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
                "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
            ),
            new("mysqli_query($beta, $query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
            ),
            new("mysqli_query($beta, $query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
            ),
            new("mysqli_query($beta,$query_import_univarzal, $sportmall_import) or die(mysqli_error($beta))",
                "mysqli_query($sportmall_import, $query_import_univarzal) or die(mysqli_error($sportmall_import))"
            ),
            new("mysqli_query($beta,$query_data_iviki, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_iviki) or die(mysqli_error($iviki_mysql))"
            ),
            new("mysqli_query($beta,$query_data_druh, $iviki_mysql) or die(mysqli_error($beta))",
                "mysqli_query($iviki_mysql, $query_data_druh) or die(mysqli_error($iviki_mysql))"
            ),
            new("emptiable(strip_tags($obj->category_name.', '.$obj->style_name)), $title)",
                "emptiable(strip_tags($obj->category_name.', '.$obj->style_name), $title))"
            ),
            new(@"preg_match(""^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$"", $email)",
                @"preg_match("";^$atom+(\\.$atom+)*@($domain?\\.)+$domain\$;"", $email)"
            ),
            new("preg_match(\"ID\", $nazev)",
                "preg_match('~ID~', $nazev)"
            ),
            new("MySQL_query($query, $DBLink)",
                "mysqli_query($DBLink, $query)"
            ),
            new("MySQL_errno()",
                "mysqli_errno($DBLink)"
            ),
            new("MySQL_errno($DBLink)",
                "mysqli_errno($DBLink)"
            ),
            new("MySQL_error()",
                "mysqli_error($DBLink)"
            ),
            //Použití <? ... ?> způsobuje, že kód neprojde PHP parserem, který vyhodí chybu.
            new("<? ", "<?php "),
            new("<?\n", "<?php\n"),
            new("<?\r", "<?php\r"),
            new("<?\t", "<?php\t"),
            //PHPStan: Undefined variable: $PHP_SELF
            new("<?php $PHP_SELF.\"#", "<?= $_SERVER['PHP_SELF'].\"#"),
            new("<?= $PHP_SELF.\"#", "<?= $_SERVER['PHP_SELF'].\"#"),
            new("$PHP_SELF", "$_SERVER['PHP_SELF']"),
            //PHPStan: Function pg_select_db not found
            new("pg_connect(DB_HOST, DB_USER, DB_PASSWORD)",
                "pg_connect(\"host=\".DB_HOST.\" dbname=\".DB_DATABASE.\" user=\".DB_USER.\" password=\".DB_PASSWORD)"
            ),
            new("pg_select_db(DB_DATABASE, $this->db)",
                "//pg_select_db(DB_DATABASE, $this->db)"
            ),
            new("pg_query(\"SET CHARACTER SET utf8\", $this->db)",
                "pg_query($this->db, \"SET CHARACTER SET utf8\")"
            ),
        }
        .ForEach(afr => FindReplace[afr.Key] = afr.Value);
    }

    /// <summary> Procedura aktualizace Rubicon souborů. </summary>
    /// <remarks> Použita ve volání metody <see cref="MonaUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
    /// <returns> Upravený soubor. </returns>
    protected override FileWrapper? UpgradeProcedure(string filePath)
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
            UpgradeUrlPromenne(file);
            UpgradeOldDbConnect(file);
            UpgradeDuplicateArrayKeys(file);
        }
        return file;
    }

    /// <summary> Old style constructor function ClassName() => function __construct() </summary>
    /// <remarks> Deprecated: Methods with the same name as their class will not be constructors in a future version of PHP; </remarks>
    public static void UpgradeConstructors(FileWrapper file)
    {
        if (!file.Content.Contains("class"))
        {
            return;
        }
        var contentStr = file.Content.ToString();
        var contentAhead = contentStr;
        var initialContent = contentStr;
        //procházíme soubor po znacích.
        for (var i = 0; i < file.Content.Length; i++)
        {
            //nalézt další třídu v souboru a přesunout se na její index.
            var classMatch = Regex.Match(contentAhead, @"class\s.+\s*\{", RegexOptions.Multiline | RegexOptions.Compiled);
            if (!classMatch.Success) //skončit, pokud kód neobsahuje další třídu.
            {
                break;
            }
            i += classMatch.Index + classMatch.Value.Length;
            contentAhead = contentStr[i..];

            //jméno třídy, jejíž konstruktory hledáme a upravujeme.
            var className = _LoadClassName(classMatch.ValueSpan).ToString();
            //parametry všech konstruktorů (jak nového __construct, který se neupravuje, tak {className})
            //a uloží jejich příznak upravenosti (funkce je __construct).
            var constructors = _LoadContructorsParameters(contentAhead, className);
            //není nutné procházet třídu znovu, pokud jsou všechny její konstruktory již aktualizovány.
            if (constructors.All(x => x.Value))
            {
                continue;
            }
            //prochází třídu, dokud nenarazí na funkci, zde se zavolá následující akce,
            //která zkontroluje, zda se jedná o konstruktor a případně jej aktualizuje.
            _GoThroughClass(contentStr, i, onFunctionFindAction: (int j) =>
            {
                var lowerHalf = contentStr.AsSpan(0, j + 2);
                var higherHalf = contentStr[(j + 2)..];
                //jedná se o funkci {className}, aka starý konstruktor?
                var match = Regex.Match(higherHalf, $@"^{className}\s?\(.*\)\s");
                if (match.Success)
                {
                    //ano, jedná se o starý konstruktor. Pokud neexistuje jeho aktualizovaná varianta __construct, doplň.
                    var @params = _LoadParameters(match.Value);
                    if (!constructors[_ParametersKey(@params)])
                    {
                        var oldConstructor = $"{className}({@params})";
                        higherHalf = $"__construct{higherHalf.AsSpan(className.Length)}";

                        var compatibilityConstructorBuilder = new StringBuilder(oldConstructor)
                            .AppendLine()
                            .AppendLine("    {")
                            .AppendLine($"        self::__construct({_ParametersWithoutDefaultValues(@params)});")
                            .AppendLine("    }")
                            .AppendLine();

                        contentStr = $"{lowerHalf}{compatibilityConstructorBuilder}    public function {higherHalf}";
                    }
                }
            });
            //uložit aktualizovaný kód třídy do "souboru" před přesunem na další.
            file.Content.Replace(initialContent, contentStr);
            initialContent = contentStr;
        }

        static ReadOnlySpan<char> _LoadClassName(ReadOnlySpan<char> classMatchVal)
        {
            var afterClassKW = classMatchVal[6..];
            for (var i = 0; i < afterClassKW.Length; i++)
            {
                var currentChar = afterClassKW[i];
                if (char.IsWhiteSpace(currentChar) || currentChar == '{')
                {
                    return afterClassKW[..i];
                }
            }
            return afterClassKW;
        }

        static IReadOnlyDictionary<string, bool> _LoadContructorsParameters(string content, string className)
        {
            var result = new Dictionary<string, bool>();

            _GoThroughClass(content, 0, onFunctionFindAction: (int i) =>
            {
                var match = Regex.Match(content[(i + 2)..], $@"^(__construct|{className})\s?\(.*\)\s");
                if (match.Success)
                {
                    var @params = _LoadParameters(match.Value);
                    var key = _ParametersKey(@params);

                    if (match.Value.StartsWith("__construct"))
                    {
                        result[key] = true;
                        return;
                    }
                    result[key] = result.ContainsKey(key);
                }
            });
            return result;
        }

        static string _LoadParameters(string functionMatch)
        {
            var paramsStartIndex = functionMatch.IndexOf('(') + 1;
            var paramsEndIndex = functionMatch.LastIndexOf(')');
            return functionMatch[paramsStartIndex..paramsEndIndex];
        }

        static string _ParametersKey(string parameters) => parameters.Replace(" ", string.Empty);

        static void _GoThroughClass(string content, int startIndex, Action<int> onFunctionFindAction)
        {
            short scope = 1;
            byte functionCursor = 0;
            bool functionFlag = false, inBlockComment = false, inLineComment = false;

            for (var i = startIndex; i < content.Length; i++)
            {
                var currentChar = content[i];
                //přeskočit komentáře.
                if (inLineComment && currentChar == '\n')
                {
                    inLineComment = false;
                    continue;
                }
                if (i < content.Length - 2)
                {
                    var twoCharSlice = $"{currentChar}{content[i + 1]}";
                    if (_CommentCheck(twoCharSlice, ref inBlockComment, ref i, "/*")
                        || _CommentCheck(twoCharSlice, ref inBlockComment, ref i, "*/", matchValue: false)
                        || inBlockComment
                        || _CommentCheck(twoCharSlice, ref inLineComment, ref i, "//", increment: false))
                    {
                        continue;
                    }
                }
                //hlídání zda jsme uvnitř třídy na scope minimálně 1.
                switch (currentChar)
                {
                    case '{': scope++; break;
                    case '}': scope--; break;
                }
                if (scope == 0)
                {
                    break;
                }
                //jsme ve třídě mimo funkci, hledání řetězce "function".
                if (scope == 1 && !functionFlag)
                {
                    functionFlag = _InFunction(currentChar, ref functionCursor);
                }
                //nalezena funkce, načteme její jméno a zkontrolujeme, jestli se nejedná {className}.
                if (functionFlag)
                {
                    onFunctionFindAction(i);
                    functionFlag = false;
                }
            }

            static bool _InFunction(char current, ref byte functionCursor)
            {
                //kurzor se posouvá po řetězci "function" dle aktuálního vstupního znaku.
                if (functionCursor < 8)
                {
                    if (current != "function"[functionCursor++])
                    {
                        functionCursor = 0;
                    }
                    if (functionCursor != 8)
                    {
                        return false;
                    }
                }
                functionCursor = 0;
                return true;
            }

            static bool _CommentCheck(ReadOnlySpan<char> twoCharSlice, ref bool inComment, ref int i, ReadOnlySpan<char> commentStartSequence, bool matchValue = true, bool increment = true)
            {
                if (twoCharSlice.SequenceEqual(commentStartSequence))
                {
                    inComment = matchValue;
                    if (increment)
                    {
                        i++;
                    }
                    return true;
                }
                return false;
            }
        }

        static string _ParametersWithoutDefaultValues(ReadOnlySpan<char> parameters)
        {
            var sb = new StringBuilder().Append(parameters);
            var @params = sb.Split(',');
            for (var i = 0; i < @params.Count; i++)
            {
                var param = @params[i];
                var name = param.Split('=')[0].Replace(" ", string.Empty).Replace("&", string.Empty);
                param.Clear();
                param.Append(name);
            }
            @params.JoinInto(sb, ", ");
            return sb.ToString();
        }
    }

    /// <summary> Aktualizace souborů připojení systému Rubicon. </summary>
    public override void UpgradeConnect(FileWrapper file)
    {
        UpgradeRubiconImport(file);
        UpgradeSetup(file);
        UpgradeHostname(file);
    }

    /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php,  </summary>
    public void UpgradeRubiconImport(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("Connections", "rubicon_import.php")))
        {
            return;
        }
        var backup = ConnectionFile;
        ConnectionFile = "rubicon_import.php";        
        base.UpgradeConnect(file);
        ConnectionFile = backup;

        RenameVar(file.Content, "sportmall_import");

        var replacements = new StringBuilder()
            .AppendLine("mysqli_query($sportmall_import, \"SET character_set_connection = cp1250\");")
            .AppendLine("mysqli_query($sportmall_import, \"SET character_set_results = cp1250\");")
            .Append("mysqli_query($sportmall_import, \"SET character_set_client = cp1250\");");

        file.Content.Replace("mysqli_query($sportmall_import, \"SET CHARACTER SET utf8\");",
                             replacements.ToString());
    }

    /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
    public void UpgradeSetup(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join(WebName, "setup.php")))
        {
            return;
        }
        file.Content.Replace("$_SERVER[HTTP_HOST]", "$_SERVER['HTTP_HOST']");

        if (Database is null || Username is null || Password is null
            || file.Content.Contains($"password = '{Password}';"))
        {
            return;
        }
        bool usernameLoaded = false, passwordLoaded = false, databaseLoaded = false;
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(_NewCredentialAndComment);

        var updated = Regex.Replace(content, @"\$setup_connect.*= ?"".*"";", evaluator, RegexOptions.Compiled);

        file.Content.Replace(content, updated);
        file.Content.Replace("////", "//");

        if (!usernameLoaded)
        {
            file.Warnings.Add("setup.php - nenačtené přihlašovací jméno.");
        }
        if (!passwordLoaded)
        {
            file.Warnings.Add("setup.php - nenačtené heslo.");
        }
        if (!databaseLoaded)
        {
            file.Warnings.Add("setup.php - nenačtený název databáze.");
        }
        file.Warnings.Add("setup.php - zkontrolovat připojení k databázi..");

        string _NewCredentialAndComment(Match match)
        {
            if (content.AsSpan(0, match.Index).EndsWith("//"))
            {
                return match.Value;
            }
            var eqIndex = match.ValueSpan.IndexOf('=');
            var varName = match.ValueSpan[..eqIndex].Trim(); 
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

    /// <summary> Hodnoty <b>$hostname_beta</b>, které nahradit <see cref="MonaUpgrader.Hostname"/>. </summary>
    private readonly string[] _hostnamesToReplace =
    {
        "93.185.102.228", "mcrai.vshosting.cz", "217.16.184.116", "mcrai2.vshosting.cz", "localhost"
    };

    /// <summary> Aktualizace hostname z mcrai1 na server mcrai2. </summary>
    public void UpgradeHostname(FileWrapper file)
    {
        var connBeta = file.Path.EndsWith(Path.Join("Connections", "beta.php"));
        foreach (var hn in _hostnamesToReplace)
        {
            if (Hostname == hn)
            {
                continue;
            }
            if (connBeta && !file.Content.Contains($"//$hostname_beta = \"{hn}\";"))
            {
                file.Content.Replace($"$hostname_beta = \"{hn}\";",
                    $"//$hostname_beta = \"{hn}\";\n\t$hostname_beta = \"{Hostname}\";");
            }
            if (!file.Content.Contains($"//$api = new RubiconAPI($_REQUEST['url'], '{hn}'"))
            {
                file.Content.Replace($"$api = new RubiconAPI($_REQUEST['url'], '{hn}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');",
                    $"//$api = new RubiconAPI($_REQUEST['url'], '{hn}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');\n\t$api = new RubiconAPI($_REQUEST['url'], '{Hostname}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');");
            }
            UpgradeDatabaseConnectCall(file, hn, Hostname);
        }
    }

    /// <summary> Aktualizace Database::connect. </summary>
    public static void UpgradeDatabaseConnectCall(FileWrapper file, string oldHost, string newHost)
    {
        var lookingFor = $"Database::connect('{oldHost}'";
        var commented = $"//{lookingFor}";

        if (file.Content.Contains(lookingFor) && !file.Content.Contains(commented))
        {
            var content = file.Content.ToString();
            var evaluator = new MatchEvaluator(_DCMatchEvaluator);
            var updated = Regex.Replace(content, @$"( |\t)*Database::connect\('{oldHost}'.+\);", evaluator);

            file.Content.Replace(content, updated);
        }

        string _DCMatchEvaluator(Match match)
        {
            var startIndex = match.ValueSpan.IndexOf('(') + oldHost.Length + 2;
            var afterOldHost = match.ValueSpan[startIndex..];

            var spaces = match.ValueSpan[..match.ValueSpan.IndexOf('D')];

            return $"{spaces}//{match.ValueSpan.TrimStart()}\n{spaces}Database::connect('{newHost}{afterOldHost}";
        }
    }

    /// <summary> Přidá funkci pg_close na konec index.php. </summary>
    public override void UpgradeCloseIndex(FileWrapper file)
    {
        UpgradeCloseIndex(file, "pg_close");
    }

    /// <summary> HTML tag &lt;script language="PHP"&gt;&lt;/script> deprecated => &lt;?php ?&gt; </summary>
    public static void UpgradeScriptLanguagePhp(FileWrapper file)
    {
        const string oldScriptTagStart = @"<script language=""PHP"">";
        const string oldScriptTagEnd = "</script>";

        if (!file.Content.Contains(oldScriptTagStart, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        var lines = file.Content.Split();
        var insidePhpScriptTag = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains(oldScriptTagStart, StringComparison.OrdinalIgnoreCase))
            {
                var lineStr = line.ToString();
                var updated = Regex.Replace(lineStr, oldScriptTagStart, "<?php ", RegexOptions.IgnoreCase);
                line.Replace(lineStr, updated);
                insidePhpScriptTag = true;
            }
            if (insidePhpScriptTag && line.Contains(oldScriptTagEnd))
            {
                line.Replace(oldScriptTagEnd, " ?>");
                insidePhpScriptTag = false;
            }
        }
        lines.JoinInto(file.Content);
        file.Warnings.Add($"Nalezena značka {oldScriptTagStart}. Zkontrolovat možný Javascript.");
    }

    /// <summary> templates/.../product_detail.php, zakomentovaný blok HTML stále spouští broken PHP includy, zakomentovat </summary>
    public static void UpgradeIncludesInHtmlComments(FileWrapper file)
    {
        if (!Regex.IsMatch(file.Path, @"(\\|/)templates(\\|/).+(\\|/)product_detail\.php", RegexOptions.Compiled))
        {
            return;
        }
        var lines = file.Content.Split();
        var insideHtmlComment = false;
        var commentedAtLeastOneInclude = false;

        for (var i = 0; i < lines.Count; i++)
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

    /// <summary> [Break => Return] v souboru aegisx\detail.php (není ve smyčce, ale v if). </summary>
    public static void UpgradeAegisxDetail(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("aegisx", "detail.php")))
        {
            return;
        }
        var content = file.Content.ToString();
        var updated = Regex.Replace(content, @"if\s?\(\$presmeruj == ""NO""\)\s*\{\s*break;",
                                              "if ($presmeruj == \"NO\") {\n\t\t\treturn;");
        file.Content.Replace(content, updated);
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
            var content = file.Content.ToString();

            var updated = Regex.Replace(content, @"function\s+Object\s*\(", "function ObjectBase(");
            file.Content.Replace(content, updated);

            file.MoveOnSavePath = file.Path.Replace(Path.Join("classes", "Object.php"),
                                                    Path.Join("classes", "ObjectBase.php"));
        }
        file.Content.Replace("extends Object", "extends ObjectBase");
        file.Content.Replace("@param Object", "@param ObjectBase");
        file.Content.Replace("@property  Object", "@property  ObjectBase");
    }

    /// <summary> Opravuje chybně zapsanou proměnnou $modul v souboru funkce/url_promenne.php </summary>
    public static void UpgradeUrlPromenne(FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("funkce", "url_promene.php")))
        {
            file.Content.Replace("if($url != \"0\" AND modul != \"obsah\"):",
                                 "if($url != \"0\" AND $modul != \"obsah\"):");
        }
    }

    /// <summary>
    /// Nalezeno v importy/_importy_old/DB_connect.php.
    /// (Raději také aktualizovat. Stejný soubor se někde může ještě používat.)
    /// </summary>
    public void UpgradeOldDbConnect(FileWrapper file)
    {
        if (file.Path.EndsWith("DB_connect.php"))
        {
            file.Content.Replace("$DBLink = mysqli_connect ($host,$user,$pass) or mysql_errno() + mysqli_error($beta);",
                                 "$DBLink = mysqli_connect($host, $user, $pass);");
            file.Content.Replace("if (!mysql_select_db( $DBname, $DBLink ))",
                                 "mysqli_select_db($DBLink, $DBname);\nif (mysqli_connect_errno())");
            if (!file.Content.Contains("exit()"))
            {
                file.Content.Replace("echo \"ERROR\";", "echo \"ERROR\";\nexit();");
            }
            RenameVar(file.Content, "DBLink");
        }
    }

    /// <summary> PHPStan: Array has 2 duplicate keys </summary>
    public static void UpgradeDuplicateArrayKeys(FileWrapper file)
    {
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(_ArrayKeyValueEvaluator);
        var updated = Regex.Replace(content,
                                    @"\$(cz_)?osetreni(_url)?\s?=\s?array\((""([^""]|\\""){0,9}""\s?=>\s?""([^""]|\\""){0,9}"",? ?)+\)",
                                    evaluator,
                                    RegexOptions.Compiled);

        file.Content.Replace(content, updated);

        static string _ArrayKeyValueEvaluator(Match match)
        {
            var keys = new HashSet<string>();
            var kvExpressions = new Stack<string>();
            var matches = Regex.Matches(match.Value,
                                        @"""([^""]|\\""){0,9}""\s?=>\s?""([^""]|\\""){0,9}""",
                                        RegexOptions.Compiled);

            foreach (var kv in matches.Reverse())
            {
                var keyEndIndex = kv.ValueSpan[1..].IndexOf('"') + 1;
                var key = kv.Value[1..keyEndIndex];

                if (keys.Contains(key))
                {
                    continue;
                }
                keys.Add(key);
                kvExpressions.Push(kv.Value);
            }
            var bracketIndex = match.ValueSpan.IndexOf('(') + 1;
            return $"{match.ValueSpan[..bracketIndex]}{string.Join(", ", kvExpressions)})";
        }
    }
}

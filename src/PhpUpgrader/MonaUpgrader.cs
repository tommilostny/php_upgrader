namespace PhpUpgrader;

/// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
public class MonaUpgrader
{
    /// <summary> Výchozí nastavení pro regulární výrazy. </summary>
    protected const RegexOptions _regexCompiled = RegexOptions.Compiled;

    /// <summary> Výchozí nastavení pro regulární výrazy ignorující velikost písmen. </summary>
    protected const RegexOptions _regexIgnoreCase = RegexOptions.IgnoreCase | _regexCompiled;

    /// <summary> Seznam souborů, které se nepodařilo aktualizovat a stále obsahují mysql_ funkce. </summary>
    public List<string> FilesContainingMysql { get; } = new();

    /// <summary> Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'. </summary>
    public string BaseFolder { get; }

    /// <summary> Název webu ve složce 'weby'. </summary>
    public string WebName { get; }

    /// <summary> Složky obsahující administraci RS Mona (null => 1 složka 'admin') </summary>
    public string[] AdminFolders
    {
        get => _adminFolders ??= new string[] { "admin" };
        set => _adminFolders = value ?? new string[] { "admin" };
    }
    private string[] _adminFolders;

    /// <summary> URL k databázovému serveru. </summary>
    public string? Hostname { get; set; }

    /// <summary> Nová databáze na serveru hostname. </summary>
    public string? Database { get; set; }

    /// <summary> Nové uživatelské jméno k databázi. </summary>
    public string? Username { get; set; }

    /// <summary> Nové heslo k databázi. </summary>
    public string? Password { get; set; }

    /// <summary> Název souboru ve složce 'connect'. </summary>
    public string ConnectionFile { get; set; }

    /// <summary> Přejmenovat proměnnou $beta tímto názvem (null => nepřejmenovávat). </summary>
    public string? RenameBetaWith
    {
        get => _replaceBetaWith;
        set
        {
            if ((_replaceBetaWith = value) is not null)
                RenameVariableInFindReplace("beta", value);
        }
    }
    private string? _replaceBetaWith;

    /// <summary> Co a čím to nahradit. </summary>
    public Dictionary<string, string> FindReplace { get; } = new()
    {
        { "=& new", "= new" },
        { "mysql_num_rows", "mysqli_num_rows" },
        { "mysql_error()", "mysqli_error($beta)" },
        { "mysql_connect", "mysqli_connect" },
        { "mysql_close", "mysqli_close" },
        { "MySQL_Close", "mysqli_close" },
        { "mysql_fetch_row", "mysqli_fetch_row" },
        { "mysql_Fetch_Row", "mysqli_fetch_row" },
        { "mysql_fetch_array", "mysqli_fetch_array" },
        { "mysql_fetch_assoc", "mysqli_fetch_assoc" },
        { "MYSQL_ASSOC", "MYSQLI_ASSOC" },
        { "mysql_select_db(DB_DATABASE, $this->db)", "mysqli_select_db($this->db, DB_DATABASE)" },
        { "mysql_select_db($database_beta, $beta)", "mysqli_select_db($beta, $database_beta)" },
        { "mysql_query(", "mysqli_query($beta, " },
        { "mysql_query (", "mysqli_query($beta, " },
        { "MySQL_Query(", "mysqli_query($beta, " },
        { "MySQL_Query (", "mysqli_query($beta, " },
        { ", $beta)", ")" },
        { ",$beta)", ")" },
        { "eregi(", "preg_match(" },
        { "eregi (", "preg_match(" },
        { "preg_match('^<tr(.*){0,}</tr>$'", "preg_match('/^<tr(.*){0,}< \\/tr>$/'" },
        { "unlink", "@unlink" },
        { "@@unlink", "@unlink" },
        { "mysql_data_seek", "mysqli_data_seek" },
        { "mysql_real_escape_string", "mysqli_real_escape_string" },
        { "mysql_free_result", "mysqli_free_result" },
        { "mysql_list_tables($database_beta);", "mysqli_query($beta, \"SHOW TABLES FROM `$database_beta`\");" },
        { "$table_all .= \"`\".mysql_tablename($result, $i).\"`\";", "$table_all .= \"`\".mysqli_fetch_row($result)[0].\"`\";" },
        { "<?php/", "<?php /" }
    };

    /// <summary> Počet modifikovaných souborů během procesu aktualizace. </summary>
    public uint ModifiedFilesCount { get; private set; } = 0;

    /// <summary> Celkový počet zpracovaných souborů. </summary>
    public uint TotalFilesCount { get; private set; } = 0;

    /// <summary> Inicializace povinných atributů. </summary>
    public MonaUpgrader(string baseFolder, string webName)
    {
        BaseFolder = baseFolder;
        WebName = webName;
        Regex.CacheSize = 20;
    }

    /// <summary> Rekurzivní upgrade .php souborů ve všech podadresářích. </summary>
    /// <param name="directoryPath">Cesta k adresáři, kde hledat .php soubory.</param>
    public void UpgradeAllFilesRecursively(string directoryPath)
    {
        //rekurzivní aktualizace podsložek
        foreach (var subdir in Directory.GetDirectories(directoryPath))
        {
            UpgradeAllFilesRecursively(subdir);
        }
        //aktualizace aktuální složky
        foreach (var filePath in Directory.GetFiles(directoryPath, "*.php"))
        {
            TotalFilesCount++;
            var file = UpgradeProcedure(filePath);

            if (file is null)
                continue;

            //upraveno, zapsat do souboru
            file.WriteStatus();
            file.Save(WebName);
            ModifiedFilesCount += Convert.ToUInt32(file.IsModified);

            //po dodelani nahrazeni nize projit na retezec - mysql_
            if (Regex.IsMatch(file.Content.ToString(), "[^//]mysql_", _regexIgnoreCase))
            {
                FilesContainingMysql.Add(filePath);
            }
        }
    }

    /// <summary> Procedura aktualizace zadaného souboru. </summary>
    /// <returns> Upravený soubor, null v případě TinyAjaxBehavior. </returns>
    protected virtual FileWrapper UpgradeProcedure(string filePath)
    {
        if (UpgradeTinyAjaxBehavior(filePath))
            return null;

        var file = new FileWrapper(filePath);

        if (!filePath.Contains("tiny_mce"))
        {
            UpgradeConnect(file);
            UpgradeMysqlResult(file);
            UpgradeClanekVypis(file);
            UpgradeFindReplace(file);
            UpgradeMysqliQueries(file);
            UpgradeMysqliClose(file);
            UpgradeAnketa(file);
            UpgradeChdir(file);
            UpgradeTableAddEdit(file);
            UpgradeStrankovani(file);
            UpgradeXmlFeeds(file);
            UpgradeSitemapSave(file);
            UpgradeGlobalBeta(file);
            RenameBeta(file);
        }
        else
        {
            UpgradeFindReplace(file);
            UpgradeTinyMceUploaded(file);
        }
        UpgradeRegexFunctions(file);

        if (file.Content.Contains("93.185.102.228"))
        {
            file.Warnings.Add("Soubor obsahuje IP adresu mcrai1 (93.185.102.228).");
        }
        return file;
    }

    /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
    public virtual void UpgradeConnect(FileWrapper file)
    {
        //konec, pokud aktuální soubor nepatří mezi validní connection soubory
        switch (file.Path)
        {
            case var p0 when p0.Contains(Path.Join("connect", ConnectionFile)):
            case var p1 when p1.Contains(Path.Join("system", ConnectionFile)):
            case var p2 when p2.Contains(Path.Join("Connections", ConnectionFile)):
                break;
            default: return;
        }
        //načtení hlavičky connect souboru.
        _LoadConnectHeader();
        //generování nových údajů k databázi, pokud jsou všechny zadány
        _GenerateNewCredentials();
        //na konec přidání obsahu předpřipraveného souboru
        file.Content.Append(File.ReadAllText(Path.Join(BaseFolder, "important", "connection.txt")));

        void _LoadConnectHeader()
        {
            bool inComment, hostLoaded, dbnameLoaded, usernameLoaded, passwdLoaded;
            inComment = hostLoaded = dbnameLoaded = usernameLoaded = passwdLoaded = false;
            var lines = file.Content.Split();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line.Contains("/*")) inComment = true;
                if (line.Contains("*/")) inComment = false;

                if (!inComment)
                {
                    if (line.Contains("$hostname_beta") && !line.Contains("//$hostname_beta"))
                    {
                        hostLoaded = true;
                    }
                    else if (line.Contains("$database_beta") && !line.Contains("//$database_beta"))
                    {
                        dbnameLoaded = true;
                    }
                    else if (line.Contains("$username_beta") && !line.Contains("//$username_beta"))
                    {
                        usernameLoaded = true;
                    }
                    else if (line.Contains("$password_beta") && !line.Contains("//$password_beta"))
                    {
                        passwdLoaded = true;
                    }
                    if (hostLoaded && dbnameLoaded && usernameLoaded && passwdLoaded)
                    {
                        lines.RemoveRange(i + 1, lines.Count - i - 1);
                        break;
                    }
                }
            }
            lines.JoinInto(file.Content);
            if (file.Content[file.Content.Length - 1] != '\n')
            {
                file.Content.AppendLine();
            }
        }

        void _GenerateNewCredentials()
        {
            Lazy<string> hostCreds = new(() => $"$hostname_beta = '{Hostname}';");
            Lazy<string> dbCreds = new(() => $"$database_beta = '{Database}';");
            Lazy<string> userCreds = new(() => $"$username_beta = '{Username}';");
            Lazy<string> passwdCreds = new(() => $"$password_beta = '{Password}';");

            if (Hostname is not null && !file.Content.Contains(hostCreds.Value))
            {
                file.Content.Replace("\n$hostname_beta", "\n//$hostname_beta");
                file.Content.AppendLine(hostCreds.Value);
            }
            if (Database is not null && !file.Content.Contains(dbCreds.Value))
            {
                file.Content.Replace("\n$database_beta", "\n//$database_beta");
                file.Content.AppendLine(dbCreds.Value);
            }
            if (Username is not null && !file.Content.Contains(userCreds.Value))
            {
                file.Content.Replace("\n$username_beta", "\n//$username_beta");
                file.Content.AppendLine(userCreds.Value);
            }
            if (Password is not null && !file.Content.Contains(passwdCreds.Value))
            {
                file.Content.Replace("\n$password_beta", "\n//$password_beta");
                file.Content.AppendLine(passwdCreds.Value);
            }
            file.Content.Replace("////", "//"); //smazat zbytečná lomítka
            file.Content.Replace("\r\r", "\r"); //smazat zbytečné \r
        }
    }

    /// <summary>
    /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
    /// </summary>
    public bool UpgradeTinyAjaxBehavior(string filePath)
    {
        var file = new FileWrapper(filePath, string.Empty);

        if (AdminFolders.Any(af => filePath.Contains(Path.Join(af, "include", "TinyAjaxBehavior.php"))))
        {
            var tabPath = Path.Join(BaseFolder, "important", "TinyAjaxBehavior.txt");
            if (File.GetLastWriteTime(tabPath) == File.GetLastWriteTime(file.Path))
            {
                file.WriteStatus(false);
            }
            else
            {
                File.Copy(tabPath, file.Path, overwrite: true);
                ModifiedFilesCount++;
                file.WriteStatus(true);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
    /// </summary>
    public static void UpgradeMysqlResult(FileWrapper file)
    {
        if (!file.Content.Contains("mysql_result"))
            return;

        var lines = file.Content.Split();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("mysql_result"))
            {
                line.Replace("COUNT(*)", "*");
                line.Replace(", 0", string.Empty);
                line.Replace("mysql_result", "mysqli_num_rows");
            }
        }
        lines.JoinInto(file.Content);
    }

    /// <summary>
    /// upravit soubory system/clanek.php a system/vypis.php - pokud je sdileni fotogalerii pridat nad podminku $vypis_table_clanek["sdileni_fotogalerii"] kod $p_sf = array();
    /// </summary>
    public static void UpgradeClanekVypis(FileWrapper file)
    {
        switch (file.Content)
        {
            case var c0 when !c0.Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"):
            case var c1 when c1.Contains("$p_sf = array();"):
                return;
        }
        var lines = file.Content.Split();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
            {
                line.Insert(0, "        $p_sf = array();\n");
            }
        }
        lines.JoinInto(file.Content);
    }

    /// <summary>
    /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
    /// </summary>
    public void UpgradeFindReplace(FileWrapper file)
    {
        foreach (var fr in FindReplace)
        {
            file.Content.Replace(fr.Key, fr.Value);
        }
    }

    /// <summary>
    /// po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta
    /// (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db);
    /// predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
    /// </summary>
    public void UpgradeMysqliQueries(FileWrapper file)
    {
        if (file.Content.Contains("$this->db"))
        {
            file.Content.Replace("mysqli_query($beta, \"SET CHARACTER SET utf8\", $this->db);", "mysqli_query($this->db, \"SET CHARACTER SET utf8\");");
            RenameBeta(file.Content, "this->db");
        }
    }

    /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
    public virtual void UpgradeMysqliClose(FileWrapper file)
    {
        if (file.Path.Contains(Path.Join(WebName, "index.php")) && !file.Content.Contains("mysqli_close"))
        {
            file.Content.AppendLine();
            file.Content.Append("<?php mysqli_close($beta); ?>");
        }
    }

    /// <summary>
    /// upravit soubor anketa/anketa.php - r.3 (odmazat ../)
    ///     - include_once "../setup.php"; na include_once "setup.php";
    /// </summary>
    public static void UpgradeAnketa(FileWrapper file)
    {
        if (file.Path.Contains(Path.Join("anketa", "anketa.php")))
        {
            file.Content.Replace(@"include_once(""../setup.php"")", @"include_once(""setup.php"")");
        }
    }

    /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
    public void UpgradeChdir(FileWrapper file)
    {
        if (!AdminFolders.Any(af => file.Path.Contains(Path.Join(af, "funkce", "vytvoreni_adr.php"))))
        {
            return;
        }
        if (!file.Content.Contains("//chdir"))
        {
            file.Content.Replace("chdir", "//chdir");
        }
    }

    /// <summary>
    /// upravit soubor admin/table_x_add.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 47-55 - $pocet_text_all = mysqli_num_rows….
    /// upravit soubor admin/table_x_edit.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 53-80 - $pocet_text_all = mysqli_num_rows….
    /// </summary>
    public void UpgradeTableAddEdit(FileWrapper file)
    {
        switch (AdminFolders)
        {
            case var afs0 when afs0.Any(af => file.Path.Contains(Path.Join(af, "table_x_add.php"))):
            case var afs1 when afs1.Any(af => file.Path.Contains(Path.Join(af, "table_x_edit.php"))):
                break;
            default: return;
        }
        if (!file.Content.Contains("@$pocet_text_all"))
        {
            file.Content.Replace("$pocet_text_all = mysqli_num_rows", "@$pocet_text_all = mysqli_num_rows");
        }
    }

    /// <summary>
    /// upravit soubor funkce/strankovani.php
    ///     >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
    /// </summary>
    public static void UpgradeStrankovani(FileWrapper file)
    {
        switch (file)
        {
            case { Path: var p } when !p.Contains(Path.Join("funkce", "strankovani.php")):
            case { Content: var c } when !c.Contains("function predchozi_dalsi"):
                return;
        }
        foreach (var (old, updated) in _PredchoziDalsiVariants())
        {
            file.Content.Replace(old, updated);

            if (file.Content.Contains(updated))
                return;
        }
        //zahlásit chybu při nalezení další varianty funkce predchozi_dalsi
        file.Warnings.Add("Nalezena neznámá varianta funkce predchozi_dalsi.");

        //iterátor dvojic 'co hledat?', 'čím to nahradit?' pro varianty funkce predchozi_dalsi
        static IEnumerable<(string old, string updated)> _PredchoziDalsiVariants()
        {
            yield return ("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)",
                          "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)"
            );
            yield return ("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)",
                          "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $prenext_2 = null)"
            );
            yield return ("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)",
                          "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $pre = null, $next = null)"
            );
            yield return ("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $filter)",
                          "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $filter = null)"
            );
        }
    }

    /// <summary>
    /// Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
    /// </summary>
    public static void UpgradeXmlFeeds(FileWrapper file)
    {
        if (Regex.IsMatch(file.Path, "xml_feeds_[^edit]", _regexCompiled))
        {
            file.Content.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");
        }
    }

    /// <summary>
    /// upravit soubor admin/sitemap_save.php cca radek 84
    ///     - pridat podminku „if($query_text_all !== FALSE)“
    ///     a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
    /// </summary>
    public void UpgradeSitemapSave(FileWrapper file)
    {
        if (!AdminFolders.Any(af => file.Path.Contains(Path.Join(af, "sitemap_save.php"))))
            return;

        switch (file.Content)
        {
            case var c0 when !c0.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"):
            case var c1 when c1.Contains("if($query_text_all !== FALSE)"):
                return;
        }
        bool sfBracket = false;
        var lines = file.Content.Split();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"))
            {
                line.Insert(0, "          if($query_text_all !== FALSE)\n          {\n");
                sfBracket = true;
            }
            if (line.Contains('}') && sfBracket)
            {
                line.Append($"\n{line}");
                line.Insert(0, "    ");
                sfBracket = false;
            }
        }
        lines.JoinInto(file.Content);
    }

    /// <summary>
    /// pro všechny funkce které v sobe mají dotaz na db pridat na zacatek
    ///     - global $beta; >>> hledat v netbeans - (?s)^(?=.*?function )(?=.*?mysqli_) - regular
    /// </summary>
    public static void UpgradeGlobalBeta(FileWrapper file)
    {
        switch (file.Content)
        {
            case var c0 when !Regex.IsMatch(c0.ToString(), "(?s)^(?=.*?function )(?=.*?mysqli_)", _regexCompiled):
            case var c1 when c1.Contains("$this"):
                return;
        }
        bool javascript = false;
        var lines = file.Content.Split();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("<script")) javascript = true;
            if (line.Contains("</script")) javascript = false;

            if (Regex.IsMatch(line.ToString(), @"function\s", _regexCompiled)
                && !javascript
                && _MysqliAndBetaInFunction(i))
            {
                lines[++i].Append("\n    global $beta;\n\n");
            }
        }
        lines.JoinInto(file.Content);

        bool _MysqliAndBetaInFunction(int startIndex)
        {
            bool javascript = false, inComment = false, foundMysqli = false, foundBeta = false;
            int bracketCount = 0;

            for (int i = startIndex; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Contains("<script")) javascript = true;
                if (line.Contains("</script")) javascript = false;

                if (javascript)
                    continue;

                if (line.Contains("/*")) inComment = true;
                if (line.Contains("*/")) inComment = false;

                if (!inComment && !line.ToString().TrimStart().StartsWith("//"))
                {
                    if (line.Contains("mysqli_")) foundMysqli = true;
                    if (line.Contains("$beta")) foundBeta = true;

                    if (foundBeta && foundMysqli)
                        return true;
                }
                if (line.Contains('{')) bracketCount++;
                if (line.Contains('}')) bracketCount--;

                if ((line.Contains("global $beta;") || bracketCount <= 0) && i > startIndex)
                    break;
            }
            return false;
        }
    }

    /// <summary> Přejmenuje proměnnou $beta na přednastavenou hodnotu v instanci <see cref="StringBuilder"/>. </summary>
    /// <param name="newVarName">null => použít vlastnost RenameBetaWith.</param>
    /// <param name="oldVarName"></param>
    /// <param name="content"></param>
    public void RenameBeta(StringBuilder content, string? newVarName = null, string oldVarName = "beta")
    {
        if ((newVarName ??= RenameBetaWith) is not null)
        {
            content.Replace($"${oldVarName}", $"${newVarName}");
            content.Replace($"_{oldVarName}", $"_{newVarName}");
        }
    }

    /// <summary> Přejmenuje proměnnou $beta na přednastavenou hodnotu. </summary>
    /// <param name="newVarName">null => použít vlastnost RenameBetaWith.</param>
    /// <param name="oldVarName"></param>
    /// <param name="content"></param>
    public string RenameBeta(string content, string? newVarName = null, string oldVarName = "beta")
    {
        if ((newVarName ??= RenameBetaWith) is not null)
        {
            content = content.Replace($"${oldVarName}", $"${newVarName}");
            content = content.Replace($"_{oldVarName}", $"_{newVarName}");
        }
        return content;
    }

    /// <summary> Přejmenovat proměnnou $beta v souboru. </summary>
    public void RenameBeta(FileWrapper file) => RenameBeta(file.Content);

    /// <summary>
    /// - funkci ereg nebo ereg_replace doplnit do prvního parametru delimetr na začátek a nakonec (if(ereg('.+@.+..+', $retezec))
    /// // puvodni, jiz nefunkcni >>> if(preg_match('#.+@.+..+#', $retezec)) // upravene - delimiter zvolen #)
    /// </summary>
    public static void UpgradeRegexFunctions(FileWrapper file)
    {
        var evaluator = new MatchEvaluator(_PregMatchEvaluator);
        _UpgradeEreg();
        _UpgradeSplit();

        void _UpgradeEreg()
        {
            if (!file.Content.Contains("ereg"))
                return;

            string content = file.Content.ToString();

            content = Regex.Replace(content, @"ereg(_replace)? ?\('(\\'|[^'])*'", evaluator, _regexCompiled);
            content = Regex.Replace(content, @"ereg(_replace)? ?\(""(\\""|[^""])*""", evaluator, _regexCompiled);

            content = Regex.Replace(content, @"ereg ?\( ?\$", "preg_match($", _regexCompiled);
            content = Regex.Replace(content, @"ereg_replace ?\( ?\$", "preg_replace($", _regexCompiled);

            if (content.Contains("ereg"))
            {
                file.Warnings.Add("Nemodifikovaná funkce ereg!");
            }
            file.Content.Clear();
            file.Content.Append(content);
        }

        void _UpgradeSplit()
        {
            if (!file.Content.Contains("split") || file.Content.Contains("preg_split"))
                return;

            bool javascript = false;
            var lines = file.Content.Split();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Contains("<script")) javascript = true;
                if (line.Contains("</script")) javascript = false;

                if (!javascript && !line.Contains(".split"))
                {
                    var lineStr = line.ToString();
                    lineStr = Regex.Replace(lineStr, @"\bsplit ?\('(\\'|[^'])*'", evaluator, _regexCompiled);
                    lineStr = Regex.Replace(lineStr, @"\bsplit ?\(""(\\""|[^""])*""", evaluator, _regexCompiled);
                    line.Clear();
                    line.Append(lineStr);
                }
            }
            lines.JoinInto(file.Content);
            if (Regex.IsMatch(file.Content.ToString(), @"[^_\.]split ?\(", _regexCompiled))
            {
                file.Warnings.Add("Nemodifikovaná funkce split!");
            }
        }

        static string _PregMatchEvaluator(Match match)
        {
            int bracketIndex = match.Value.IndexOf('(');

            string pregFunction = match.Value[..bracketIndex].TrimEnd() switch
            {
                "ereg_replace" => "preg_replace",
                "split" => "preg_split",
                _ => "preg_match"
            };
            char quote = match.Value[++bracketIndex];
            string insidePattern = match.Value[++bracketIndex..(match.Value.Length - 1)];

            return $"{pregFunction}({quote}~{insidePattern}~{quote}";
        }
    }

    ///<summary> PHP Parse error:  syntax error, unexpected '&amp;' on line 49` </summary>
    public static void UpgradeTinyMceUploaded(FileWrapper file)
    {
        if (!file.Path.Contains(Path.Join("plugins", "imagemanager", "plugins", "Uploaded", "Uploaded.php")))
        {
            return;
        }
        file.Content.Replace("$this->_uploadedFile(&$man, $file1);", "$this->_uploadedFile($man, $file1);");
    }

    /// <summary> Přejmenovat proměnnou ve slovníku <see cref="FindReplace"/>. </summary>
    protected void RenameVariableInFindReplace(string oldVarName, string newVarName)
    {
        var renamedItems = new Stack<(string, string, string)>();
        foreach (var fr in FindReplace)
        {
            if (fr.Key.Contains(oldVarName) || fr.Value.Contains(oldVarName))
            {
                var newKey = RenameBeta(fr.Key, newVarName, oldVarName);
                var newValue = RenameBeta(fr.Value, newVarName, oldVarName);
                renamedItems.Push((fr.Key, newKey, newValue));
            }
        }
        while (renamedItems.Count > 0)
        {
            var (oldKey, newKey, newValue) = renamedItems.Pop();
            FindReplace.Remove(oldKey);
            FindReplace.Add(newKey, newValue);
        }
    }
}

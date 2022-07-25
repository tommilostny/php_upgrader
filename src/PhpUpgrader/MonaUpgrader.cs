using System;

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

    /// <summary> Root složky obsahující index.php, do kterého vložit mysqli_close na konec. </summary>
    public string[]? OtherRootFolders { get; set; }

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
        { "MySQL_num_rows", "mysqli_num_rows" },
        { "mysql_error()", "mysqli_error($beta)" },
        { "mysql_connect", "mysqli_connect" },
        { "mysql_close", "mysqli_close" },
        { "MySQL_Close", "mysqli_close" },
        { "MySQL_close", "mysqli_close" },
        { "mysqli_close()", "mysqli_close($beta)" },
        { "mysql_fetch_row", "mysqli_fetch_row" },
        { "mysql_Fetch_Row", "mysqli_fetch_row" },
        { "mysql_fetch_array", "mysqli_fetch_array" },
        { "mysql_fetch_assoc", "mysqli_fetch_assoc" },
        { "mysql_fetch_object", "mysqli_fetch_object" },
        { "MySQL_fetch_object", "mysqli_fetch_object" },
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
        Regex.CacheSize = 25;
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
            FileWrapper? file;
            if ((file = UpgradeProcedure(filePath)) is null)
            {
                continue;
            }
            //upraveno, zapsat do souboru
            file.WriteStatus();
            file.Save(WebName);
            if (file.IsModified)
            {
                ModifiedFilesCount++;
            }
            //po dodelani nahrazeni nize projit na retezec - mysql_
            if (Regex.IsMatch(file.Content.ToString(), "[^//]mysql_", _regexIgnoreCase))
            {
                FilesContainingMysql.Add(filePath);
            }
        }
    }

    /// <summary> Procedura aktualizace zadaného souboru. </summary>
    /// <returns> Upravený soubor, null v případě TinyAjaxBehavior. </returns>
    protected virtual FileWrapper? UpgradeProcedure(string filePath)
    {
#if DEBUG
        Console.Write("   ");
        Console.Write(filePath);
        Console.Write('\r');
#endif
        if (UpgradeTinyAjaxBehavior(filePath))
        {
            return null;
        }
        var file = new FileWrapper(filePath);

        if (!filePath.Contains("tiny_mce"))
        {
            UpgradeConnect(file);
            UpgradeResultFunc(file);
            UpgradeClanekVypis(file);
            UpgradeFindReplace(file);
            UpgradeMysqliQueries(file);
            UpgradeCloseIndex(file);
            UpgradeAnketa(file);
            UpgradeChdir(file);
            UpgradeTableAddEdit(file);
            UpgradeStrankovani(file);
            UpgradeXmlFeeds(file);
            UpgradeSitemapSave(file);
            UpgradeGlobalBeta(file);
            RenameBeta(file);
            UpgradeFloatExplodeConversions(file);
        }
        else
        {
            UpgradeFindReplace(file);
            UpgradeTinyMceUploaded(file);
        }
        UpgradeRegexFunctions(file);
        RemoveTrailingWhitespaceFromEndOfFile(file);
        UpgradeIfEmpty(file);
        UpgradeGetMagicQuotesGpc(file);

        if (file.Content.Contains("93.185.102.228"))
        {
            file.Warnings.Add("Soubor obsahuje IP adresu mcrai1 (93.185.102.228).");
        }
        return file;
    }

    /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
    public virtual void UpgradeConnect(FileWrapper file)
    {
        const string hostnameVarPart = "$hostname_";
        const string databaseVarPart = "$database_";
        const string usernameVarPart = "$username_";
        const string passwordVarPart = "$password_";

        //konec, pokud aktuální soubor nepatří mezi validní connection soubory
        if (_ConnectionPaths().Any(cf => file.Path.EndsWith(cf)))
        {
            //načtení hlavičky connect souboru.
            _LoadConnectHeader();

            //generování nových údajů k databázi, pokud jsou všechny zadány
            _GenerateNewCredential(hostnameVarPart, Hostname);
            _GenerateNewCredential(databaseVarPart, Database);
            _GenerateNewCredential(usernameVarPart, Username);
            _GenerateNewCredential(passwordVarPart, Password);

            //smazat zbytečné znaky
            file.Content.Replace("////", "//");
            file.Content.Replace("\r\r", "\r");

            //na konec přidání obsahu předpřipraveného souboru
            file.Content.Append(File.ReadAllText(Path.Join(BaseFolder, "important", "connection.txt")));
        }

        IEnumerable<string> _ConnectionPaths()
        {
            yield return Path.Join("connect", ConnectionFile);
            yield return Path.Join("system", ConnectionFile);
            yield return Path.Join("Connections", ConnectionFile);
        }

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
                    if (line.Contains(hostnameVarPart) && !line.Contains($"//{hostnameVarPart}"))
                    {
                        hostLoaded = true;
                    }
                    else if (line.Contains(databaseVarPart) && !line.Contains($"//{databaseVarPart}"))
                    {
                        dbnameLoaded = true;
                    }
                    else if (line.Contains(usernameVarPart) && !line.Contains($"//{usernameVarPart}"))
                    {
                        usernameLoaded = true;
                    }
                    else if (line.Contains(passwordVarPart) && !line.Contains($"//{passwordVarPart}"))
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
            if (file.Content[^1] != '\n')
            {
                file.Content.AppendLine();
            }
        }

        void _GenerateNewCredential(string varPart, string? varValue)
        {
            Lazy<string> cred = new(() => $"{varPart}beta = '{varValue}';");

            if (varValue is not null && !file.Content.Contains(cred.Value))
            {
                file.Content.Replace($"\n{varPart}", $"\n//{varPart}");
                file.Content.AppendLine(cred.Value);
            }
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
    public void UpgradeResultFunc(FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("funkce", "secure", "login.php")))
        {
            var content = file.Content.ToString();
            var updated = Regex.Replace(content,
                                        @"\$loginStrGroup\s*=\s*mysql_result\(\$LoginRS,\s*0,\s*'valid'\);\s*\n\s*\$loginUserid\s*=\s*mysql_result\(\$LoginRS,\s*0,\s*'user_id'\);",
                                        "mysqli_field_seek($LoginRS, 0);\n    $field = mysqli_fetch_field($LoginRS);\n    $loginStrGroup = $field->valid;\n    $loginUserid  = $field->user_id;\n    mysqli_free_result($LoginRS);");
            file.Content.Replace(content, updated);
        }

        var (oldResultFunc, newNumRowsFunc) = this switch
        {
            RubiconUpgrader => ("pg_result", "pg_num_rows"),
            MonaUpgrader => ("mysql_result", "mysqli_num_rows")
        };
        if (!file.Content.Contains(oldResultFunc))
        {
            return;
        }
        var lines = file.Content.Split();
        StringBuilder currentLine;

        for (int i = 0; i < lines.Count; i++)
        {
            if (!(currentLine = lines[i]).Contains(oldResultFunc))
            {
                continue;
            }
            const string countFunc = "COUNT(*)";
            var countIndex = currentLine.IndexOf(countFunc);
            if (countIndex == -1)
            {
                if (this is not RubiconUpgrader)
                {
                    file.Warnings.Add($"Neobvyklé použití {oldResultFunc}!");
                    continue;
                }
                currentLine.Replace(oldResultFunc, "pg_fetch_result");
                continue;
            }
            currentLine.Replace(countFunc, "*", countIndex);
            currentLine.Replace(", 0", string.Empty);
            currentLine.Replace(oldResultFunc, newNumRowsFunc);
        }
        lines.JoinInto(file.Content);
    }

    /// <summary>
    /// upravit soubory system/clanek.php a system/vypis.php - pokud je sdileni fotogalerii pridat nad podminku $vypis_table_clanek["sdileni_fotogalerii"] kod $p_sf = array();
    /// </summary>
    public static void UpgradeClanekVypis(FileWrapper file)
    {
        const string lookingFor = "$vypis_table_clanek[\"sdileni_fotogalerii\"]";
        const string adding = "$p_sf = array();";
        const string addLine = $"        {adding}\n";

        if (file.Content.Contains(lookingFor) && !file.Content.Contains(adding))
        {
            var lines = file.Content.Split();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Contains(lookingFor))
                {
                    line.Insert(0, addLine);
                }
            }
            lines.JoinInto(file.Content);
        }
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
        const string thisDB = "$this->db";
        if (file.Content.Contains(thisDB))
        {
            file.Content.Replace($"mysqli_query($beta, \"SET CHARACTER SET utf8\", {thisDB});", $"mysqli_query({thisDB}, \"SET CHARACTER SET utf8\");");
            RenameVar(file.Content, thisDB);
        }
    }

    /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
    public virtual void UpgradeCloseIndex(FileWrapper file)
    {
        UpgradeCloseIndex(file, "mysqli_close");
    }

    /// <summary> Přidá "{closeFunction}($beta);" na konec soubor index.php. </summary>
    protected void UpgradeCloseIndex(FileWrapper file, string closeFunction)
    {
        if (_IsInRootFolder(file.Path) && !file.Content.Contains(closeFunction))
        {
            file.Content.AppendLine();
            file.Content.Append($"<?php {closeFunction}($beta); ?>");
        }

        bool _IsInRootFolder(string path)
        {
            const string indexFile = "index.php";
            return path.EndsWith(Path.Join(WebName, indexFile))
                || OtherRootFolders?.Any(rf => path.EndsWith(Path.Join(WebName, rf, indexFile))) == true;
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
        const string chdir = "chdir";
        const string commentedChdir = $"//{chdir}";
        if (!file.Content.Contains(commentedChdir))
        {
            file.Content.Replace(chdir, commentedChdir);
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
        const string variable = "$pocet_text_all";
        const string variableWithAtSign = $"@{variable}";

        if (!AdminFolders.Any(af => file.Path.Contains(Path.Join(af, "table_x_add.php"))
                                 || file.Path.Contains(Path.Join(af, "table_x_edit.php")))
            || file.Content.Contains(variableWithAtSign))
        {
            return;
        }
        file.Content.Replace($"{variable} = mysqli_num_rows", $"{variableWithAtSign} = mysqli_num_rows");
    }

    /// <summary>
    /// upravit soubor funkce/strankovani.php
    ///     >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
    /// </summary>
    public static void UpgradeStrankovani(FileWrapper file)
    {
        const string pdFunc = "function predchozi_dalsi";
        switch (file)
        {
            case { Path: var p } when !p.Contains(Path.Join("funkce", "strankovani.php")):
            case { Content: var c } when !c.Contains(pdFunc):
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
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)"
            );
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $prenext_2 = null)"
            );
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $pre = null, $next = null)"
            );
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $filter)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $filter = null)"
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
        const string lookingFor = "while($data_stranky_text_all = mysqli_fetch_array($query_text_all))";
        const string adding = "if($query_text_all !== FALSE)";
        const string addingLine = $"          {adding}\n          {{\n";

        if (!AdminFolders.Any(af => file.Path.EndsWith(Path.Join(af, "sitemap_save.php")))
            || !file.Content.Contains(lookingFor) || file.Content.Contains(adding))
        {
            return;
        }
        bool sfBracket = false;
        var lines = file.Content.Split();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains(lookingFor))
            {
                line.Insert(0, addingLine);
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
        if (file.Content.Contains("$this")
            || !Regex.IsMatch(file.Content.ToString(), "(?s)^(?=.*?function )(?=.*?mysqli_)", _regexCompiled))
        {
            return;
        }
        var javascript = false;
        var lines = file.Content.Split();
        const string globalBeta = "\n\n    global $beta;\n";

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("<script")) javascript = true;
            if (line.Contains("</script")) javascript = false;

            if (Regex.IsMatch(line.ToString(), @"function\s", _regexCompiled)
                && !javascript
                && _MysqliAndBetaInFunction(i, lines))
            {
                if ((line = lines[++i]).Contains('{'))
                {
                    line.Append(globalBeta);
                    continue;
                }
                line.Insert(0, globalBeta);
            }
        }
        lines.JoinInto(file.Content);

        static bool _MysqliAndBetaInFunction(int startIndex, IReadOnlyList<StringBuilder> lines)
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
                bracketCount += line.Count('{');
                bracketCount -= line.Count('}');

                if ((line.Contains("global $beta;") || bracketCount <= 0) && i > startIndex)
                    break;
            }
            return false;
        }
    }

    /// <summary> Přejmenuje proměnnou $<paramref name="oldVarName"/> v instanci <see cref="StringBuilder"/>. </summary>
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="RenameBetaWith"/>. </param>
    /// <param name="oldVarName"> Jmené původní proměnné, která se bude přejmenovávat. </param>
    /// <param name="content"> Obsah, ve kterém se proměnná přejmenovává. </param>
    public void RenameVar(StringBuilder content, string? newVarName = null, string oldVarName = "beta")
    {
        if ((newVarName ??= RenameBetaWith) is not null)
        {
            content.Replace($"${oldVarName}", $"${newVarName}");
            if (!newVarName.Contains("->"))
            {
                content.Replace($"_{oldVarName}", $"_{newVarName}");
            }
        }
    }

    /// <summary> Přejmenuje proměnnou $<paramref name="oldVarName"/>. </summary>
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="RenameBetaWith"/>. </param>
    /// <param name="oldVarName"> Jmené původní proměnné, která se bude přejmenovávat. </param>
    /// <param name="content"> Obsah, ve kterém se proměnná přejmenovává. </param>
    /// <returns> Upravený <paramref name="content"/>. </returns>
    public string RenameVar(string content, string? newVarName = null, string oldVarName = "beta")
    {
        var csb = new StringBuilder(content);
        RenameVar(csb, newVarName, oldVarName);
        return csb.ToString();
    }

    /// <summary> Přejmenovat proměnnou $beta v souboru. </summary>
    public void RenameBeta(FileWrapper file) => RenameVar(file.Content);

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

            var content = file.Content.ToString();

            var updated = Regex.Replace(content, @"ereg(_replace)? ?\('(\\'|[^'])*'", evaluator, _regexCompiled);
            updated = Regex.Replace(updated, @"ereg(_replace)? ?\(""(\\""|[^""])*""", evaluator, _regexCompiled);

            updated = Regex.Replace(updated, @"ereg ?\( ?\$", "preg_match($", _regexCompiled);
            updated = Regex.Replace(updated, @"ereg_replace ?\( ?\$", "preg_replace($", _regexCompiled);

            if (updated.Contains("ereg"))
            {
                file.Warnings.Add("Nemodifikovaná funkce ereg!");
            }
            file.Content.Replace(content, updated);
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

                if (!javascript && !line.Contains(".split") && line.Length > 7)
                {
                    var lineStr = line.ToString();
                    var updated = Regex.Replace(lineStr, @"\bsplit ?\('(\\'|[^'])*'", evaluator, _regexCompiled);
                    updated = Regex.Replace(updated, @"\bsplit ?\(""(\\""|[^""])*""", evaluator, _regexCompiled);
                    
                    line.Replace(lineStr, updated);
                }
            }
            lines.JoinInto(file.Content);

            if (!file.Path.EndsWith(Path.Join("facebook", "src", "Facebook", "SignedRequest.php"))
                && !file.Path.EndsWith(Path.Join("funkce", "qrkod", "qrsplit.php"))
                && !file.Path.EndsWith(Path.Join("funkce", "qrkod", "phpqrcode.php"))
                && Regex.IsMatch(file.Content.ToString(), @"[^_\.]split ?\(", _regexCompiled))
            {
                file.Warnings.Add("Nemodifikovaná funkce split!");
            }
        }

        static string _PregMatchEvaluator(Match match)
        {
            var bracketIndex = match.ValueSpan.IndexOf('(');

            var pregFunction = match.ValueSpan[..bracketIndex].TrimEnd() switch
            {
                var x when x.SequenceEqual("ereg_replace") => "preg_replace",
                var x when x.SequenceEqual("split") => "preg_split",
                _ => "preg_match"
            };
            var quote = match.ValueSpan[++bracketIndex];
            var insidePattern = match.ValueSpan[++bracketIndex..(match.ValueSpan.Length - 1)];

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
                var newKey = RenameVar(fr.Key, newVarName, oldVarName);
                var newValue = RenameVar(fr.Value, newVarName, oldVarName);
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

    /// <summary>
    /// PHPStan: File ends with a trailing whitespace.
    /// This may cause problems when running the code in the web browser.
    /// Remove the closing ?> mark or remove the whitespace.
    /// </summary>
    public static void RemoveTrailingWhitespaceFromEndOfFile(FileWrapper file)
    {
        while (char.IsWhiteSpace(file.Content[^1]))
        {
            file.Content.Remove(file.Content.Length - 1, 1);
        }
    }

    /// <summary> PHPStan: Right side of || is always false. </summary>
    /// <remarks> if ($id != "" || $id != null) </remarks>
    public static void UpgradeIfEmpty(FileWrapper file)
    {
        var evaluator = new MatchEvaluator(_IfEmptyMatchEvaluator);
        var content = file.Content.ToString();
        var updated = Regex.Replace(content,
                                    @"if\s?\(\$\w+\s?!=\s?""""\s?\|\|\s?\$\w+\s?!=\s?null\)",
                                    evaluator,
                                    _regexIgnoreCase);
        file.Content.Replace(content, updated);

        static string _IfEmptyMatchEvaluator(Match match)
        {
            var varStartIndex = match.ValueSpan.IndexOf('$');
            var varEndIndex = match.ValueSpan.IndexOf('!') - 1;
            var varValue1 = match.ValueSpan[varStartIndex..varEndIndex];

            varStartIndex = match.ValueSpan.LastIndexOf('|') + 2;
            varEndIndex = match.ValueSpan.LastIndexOf('!') - 1;
            var varValue2 = match.ValueSpan[varStartIndex..varEndIndex];

            return varValue1.SequenceEqual(varValue2) ? $"if (!empty({varValue1}))" : match.Value;
        }
    }

    /// <summary> PHPStan: Parameter #2 $str of function explode expects string, float|int&lt;0, max&gt; given. </summary>
    public static void UpgradeFloatExplodeConversions(FileWrapper file)
    {
        if (!file.Content.Contains("$stranka_end = explode"))
        {
            return;
        }
        var content = file.Content.ToString();
        var updated = Regex.Replace(content,
                                    @"\s\$stranka_end = \$stranka_pocet \/ 10;\s+\$stranka_end = explode\(""\."", \$stranka_end\);\s+\$stranka_end = \$stranka_end\[0\];\s+\$stranka_end = \$stranka_end \* 10 \+ 10;",
                                    "\n$stranka_end = (int)($stranka_pocet / 10);\n$stranka_end = $stranka_end * 10 + 10;");
        file.Content.Replace(content, updated);
    }

    /// <summary>
    /// 
    /// </summary>
    public static void UpgradeGetMagicQuotesGpc(FileWrapper file)
    {
        Lazy<string> contentStr = new(() => file.Content.ToString());
        if (!file.Content.Contains("get_magic_quotes_gpc()") || _Is_GMQG_Commented(contentStr.Value))
        {
            return;
        }

        //Zpracování výrazu s ternárním operátorem.
        var evaluator = new MatchEvaluator(_GetMagicQuotesGpcTernaryEvaluator);
        var updated = Regex.Replace(contentStr.Value,
                                    @"\(?!?get_magic_quotes_gpc\(\)\)?\s{0,5}\?\s{0,5}(/\*.*\*/)?\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))\s{0,5}:\s{0,5}(\$\w+(\[('|"")\w+('|"")\])?|(add|strip)slashes\(\$\w+(\[('|"")\w+('|"")\])?\))",
                                    evaluator,
                                    _regexCompiled);
        //Pokud výraz s get_magic_quotes_gpc nebyl aktualizován, jedná se pravděpodobně o variantu s if else.
        if (!_Is_GMQG_Commented(updated))
        {
            evaluator = new MatchEvaluator(_GetMagicQuotesGpcIfElseEvaluator);
            updated = Regex.Replace(contentStr.Value,
                                    @"if\s?\(\s?get_magic_quotes_gpc\(\)\s?\)(\n|.){0,236}else(\n|.){0,236};",
                                    evaluator);

            if (!_Is_GMQG_Commented(updated))
            {
                file.Warnings.Add("Nezakomentovaná funkce get_magic_quotes_gpc().");
                return;
            }
        }
        file.Content.Replace(contentStr.Value, updated);

        static bool _Is_GMQG_Commented(string str)
        {
            return Regex.IsMatch(str, @"/\*.{0,6}get_magic_quotes_gpc\(\)(\n|.){0,236}\*/", _regexCompiled);
        }

        //get_magic_quotes_gpc vždy vrací false, tu vybere a zakomentuje zbytek.
        static string _GetMagicQuotesGpcTernaryEvaluator(Match match)
        {
            var colonIndex = match.ValueSpan.LastIndexOf(':') + 1;
            var afterColon = match.ValueSpan[colonIndex..];

            //negovaný výraz, vybrat true část mezi '?' a ':'.
            if (match.ValueSpan.Contains("!get_", StringComparison.Ordinal))
            {
                var qmarkIndex = match.ValueSpan.IndexOf('?') + 1;
                var beforeQMark = match.ValueSpan[..qmarkIndex];
                var afterQMark = match.ValueSpan[qmarkIndex..(colonIndex - 1)];
                return $"/*{beforeQMark}*/ {afterQMark} /*:{afterColon}*/";
            }

            //běžný podmíněný výraz, vybrat false část za ':'.
            var beforeColon = match.ValueSpan.Contains("*/", StringComparison.Ordinal)
                ? match.Value[..colonIndex].Replace("*/", "*//*") //volat string replace jen pokud je opravdu potřeba
                : match.ValueSpan[..colonIndex];

            return $"/*{beforeColon}*/{afterColon}";
        }

        static string _GetMagicQuotesGpcIfElseEvaluator(Match match)
        {
            //zakomentovat if else s get_magic_quotes_gpc a ponechat pouze else část.
            var elseIndex = match.ValueSpan.IndexOf("else") + 4;
            var beforeElse = match.ValueSpan[..elseIndex];
            var afterElse = match.ValueSpan[elseIndex..];

            return $"/*{beforeElse}*/{afterElse}";
        }
    }
}

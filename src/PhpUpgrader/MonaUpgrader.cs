using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PhpUpgrader
{
    /// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
    public class MonaUpgrader
    {
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
            { "mysql_query(", "mysqli_query($beta," },
            { "mysql_query (", "mysqli_query($beta," },
            { "MySQL_Query(", "mysqli_query($beta," },
            { "MySQL_Query (", "mysqli_query($beta," },
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
                file.Save();
                ModifiedFilesCount += Convert.ToUInt32(file.IsModified);

                //po dodelani nahrazeni nize projit na retezec - mysql_
                if (Regex.IsMatch(file.Content, "[^//]mysql_", RegexOptions.IgnoreCase))
                    FilesContainingMysql.Add(filePath);
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
                file.Warnings.Add("Soubor obsahuje IP adresu mcrai1 (93.185.102.228).");

            return file;
        }

        /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
        public virtual void UpgradeConnect(FileWrapper file)
        {
            //konec, pokud aktuální soubor nepatří mezi validní connection soubory
            switch (file.Path)
            {
                case var p0 when p0.Contains($@"\connect\{ConnectionFile}"):
                case var p1 when p1.Contains($@"\system\{ConnectionFile}"):
                case var p2 when p2.Contains($@"\Connections\{ConnectionFile}"):
                    break;
                default: return;
            }
            string connectHead = string.Empty;
            bool inComment = false;
            using var sr = new StreamReader(file.Path);

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                connectHead += $"{line}\n";

                if (line.Contains("/*"))
                    inComment = true;

                if (line.Contains("*/"))
                {
                    inComment = false;
                    if (line.TrimStart().StartsWith("$password_beta"))
                        continue;
                }
                if (line.Contains("$password_") && !inComment && !line.Contains("//$password_"))
                    break;
            }
            //generování nových údajů k databázi, pokud jsou všechny zadány
            if (Database is not null && Username is not null && Password is not null && Hostname is not null
                && !Regex.IsMatch(file.Content, $@"\$password_.* = '{Password}'"))
            {
                connectHead = connectHead.Replace("\n", "\n//"); //zakomentovat původní řádky
                connectHead = connectHead.Replace("////", "//"); //smazat zbytečná lomítka
                connectHead += '\n';
                connectHead = connectHead.Replace("//\n", "\n");
                connectHead += $"$hostname_beta = '{Hostname}';\n$database_beta = '{Database}';\n$username_beta = '{Username}';\n$password_beta = '{Password}';\n";
            }
            file.Content = connectHead + File.ReadAllText($"{BaseFolder}important\\connection.txt");
        }

        /// <summary>
        /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
        /// </summary>
        public bool UpgradeTinyAjaxBehavior(string filePath)
        {
            var file = new FileWrapper(filePath, string.Empty);

            if (file.OverwriteModificationFlag(AdminFolders.Any(af => filePath.Contains($@"\{af}\include\TinyAjaxBehavior.php"))))
            {
                File.Copy($"{BaseFolder}important\\TinyAjaxBehavior.txt", file.Path, overwrite: true);
                file.WriteStatus();
                ModifiedFilesCount++;
            }
            return file.IsModified;
        }

        /// <summary>
        /// mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
        /// </summary>
        public static void UpgradeMysqlResult(FileWrapper file)
        {
            if (!file.Content.Contains("mysql_result"))
                return;

            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("mysql_result"))
                {
                    lines[i] = lines[i].Replace("COUNT(*)", "*");
                    lines[i] = lines[i].Replace(", 0", string.Empty);
                    lines[i] = lines[i].Replace("mysql_result", "mysqli_num_rows");
                }
            }
            file.Content = string.Join('\n', lines);
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
            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
                {
                    lines[i] = $"        $p_sf = array();\n{lines[i]}";
                }
            }
            file.Content = string.Join('\n', lines);
        }

        /// <summary>
        /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
        /// </summary>
        public void UpgradeFindReplace(FileWrapper file)
        {
            foreach (var fr in FindReplace)
            {
                file.Content = file.Content.Replace(fr.Key, fr.Value);
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
                file.Content = file.Content.Replace("mysqli_query($beta, \"SET CHARACTER SET utf8\", $this->db);", "mysqli_query($this->db, \"SET CHARACTER SET utf8\");");
                file.Content = RenameBeta(file.Content, "this->db");
            }
        }

        /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
        public virtual void UpgradeMysqliClose(FileWrapper file)
        {
            if (file.Path.Contains($@"{WebName}\index.php") && !file.Content.Contains("mysqli_close"))
            {
                file.Content += "\n<?php mysqli_close($beta); ?>";
            }
        }

        /// <summary>
        /// upravit soubor anketa/anketa.php - r.3 (odmazat ../)
        ///     - include_once "../setup.php"; na include_once "setup.php";
        /// </summary>
        public static void UpgradeAnketa(FileWrapper file)
        {
            if (file.Path.Contains(@"\anketa\anketa.php"))
            {
                file.Content = file.Content.Replace("include_once(\"../setup.php\")", "include_once(\"setup.php\")");
            }
        }

        /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
        public void UpgradeChdir(FileWrapper file)
        {
            if (!AdminFolders.Any(af => file.Path.Contains($@"\{af}\funkce\vytvoreni_adr.php")))
                return;

            if (!file.Content.Contains("//chdir"))
                file.Content = file.Content.Replace("chdir", "//chdir");
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
                case var afs0 when afs0.Any(af => file.Path.Contains($@"\{af}\table_x_add.php")):
                case var afs1 when afs1.Any(af => file.Path.Contains($@"\{af}\table_x_edit.php")):
                    break;
                default: return;
            }
            if (!file.Content.Contains("@$pocet_text_all"))
                file.Content = file.Content.Replace("$pocet_text_all = mysqli_num_rows", "@$pocet_text_all = mysqli_num_rows");
        }

        /// <summary>
        /// upravit soubor funkce/strankovani.php
        ///     >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
        /// </summary>
        public static void UpgradeStrankovani(FileWrapper file)
        {
            switch (file)
            {
                case { Path: var p } when !p.Contains(@"\funkce\strankovani.php"):
                case { Content: var c } when !c.Contains("function predchozi_dalsi"):
                    return;
            }
            foreach (var variant in _PredchoziDalsiVariants())
            {
                file.Content = file.Content.Replace(variant.Item1, variant.Item2);

                if (file.Content.Contains(variant.Item2))
                    return;
            }
            //zahlásit chybu při nalezení další varianty funkce predchozi_dalsi
            file.Warnings.Add("Nalezena neznámá varianta funkce predchozi_dalsi.");

            //iterátor dvojic 'co hledat?', 'čím to nahradit?' pro varianty funkce predchozi_dalsi
            static IEnumerable<(string, string)> _PredchoziDalsiVariants()
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
            }
        }

        /// <summary>
        /// Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
        /// </summary>
        public static void UpgradeXmlFeeds(FileWrapper file)
        {
            if (Regex.IsMatch(file.Path, "xml_feeds_[^edit]"))
            {
                file.Content = file.Content.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");
            }
        }

        /// <summary>
        /// upravit soubor admin/sitemap_save.php cca radek 84
        ///     - pridat podminku „if($query_text_all !== FALSE)“
        ///     a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
        /// </summary>
        public void UpgradeSitemapSave(FileWrapper file)
        {
            if (!AdminFolders.Any(af => file.Path.Contains($@"\{af}\sitemap_save.php")))
                return;

            switch (file.Content)
            {
                case var c0 when !c0.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"):
                case var c1 when c1.Contains("if($query_text_all !== FALSE)"):
                    return;
            }
            bool sfBracket = false;
            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"))
                {
                    lines[i] = $"          if($query_text_all !== FALSE)\n          {{\n{lines[i]}";
                    sfBracket = true;
                }
                if (lines[i].Contains('}') && sfBracket)
                {
                    lines[i] = $"    {lines[i]}\n{lines[i]}";
                    sfBracket = false;
                }
            }
            file.Content = string.Join('\n', lines);
        }

        /// <summary>
        /// pro všechny funkce které v sobe mají dotaz na db pridat na zacatek
        ///     - global $beta; >>> hledat v netbeans - (?s)^(?=.*?function )(?=.*?mysqli_) - regular
        /// </summary>
        public static void UpgradeGlobalBeta(FileWrapper file)
        {
            switch (file.Content)
            {
                case var c0 when !Regex.IsMatch(c0, "(?s)^(?=.*?function )(?=.*?mysqli_)"):
                case var c1 when c1.Contains("$this"):
                    return;
            }
            bool javascript = false;
            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<script")) javascript = true;
                if (lines[i].Contains("</script")) javascript = false;

                if (Regex.IsMatch(lines[i], @"function\s") && !javascript && _MysqliAndBetaInFunction(i))
                {
                    lines[++i] += $"\n    global $beta;\n\n";
                }
            }
            file.Content = string.Join('\n', lines);

            bool _MysqliAndBetaInFunction(int startIndex)
            {
                bool javascript = false, inComment = false, foundMysqli = false, foundBeta = false;
                int bracketCount = 0;

                for (int i = startIndex; i < lines.Length; i++)
                {
                    if (lines[i].Contains("<script")) javascript = true;
                    if (lines[i].Contains("</script")) javascript = false;

                    if (javascript)
                        continue;

                    if (lines[i].Contains("/*")) inComment = true;
                    if (lines[i].Contains("*/")) inComment = false;

                    if (!inComment && !lines[i].TrimStart().StartsWith("//"))
                    {
                        if (lines[i].Contains("mysqli_")) foundMysqli = true;
                        if (lines[i].Contains("$beta")) foundBeta = true;

                        if (foundBeta && foundMysqli)
                            return true;
                    }
                    if (lines[i].Contains('{')) bracketCount++;
                    if (lines[i].Contains('}')) bracketCount--;

                    if ((lines[i].Contains("global $beta;") || bracketCount <= 0) && i > startIndex)
                        break;
                }
                return false;
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
        public void RenameBeta(FileWrapper file) => file.Content = RenameBeta(file.Content);

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

                file.Content = Regex.Replace(file.Content, @"ereg(_replace)? ?\('(\\'|[^'])*'", evaluator);
                file.Content = Regex.Replace(file.Content, @"ereg(_replace)? ?\(""(\\""|[^""])*""", evaluator);

                file.Content = Regex.Replace(file.Content, @"ereg ?\( ?\$", "preg_match($");
                file.Content = Regex.Replace(file.Content, @"ereg_replace ?\( ?\$", "preg_replace($");

                if (file.Content.Contains("ereg"))
                {
                    file.Warnings.Add("Nemodifikovaná funkce ereg!");
                }
            }

            void _UpgradeSplit()
            {
                if (!file.Content.Contains("split") || file.Content.Contains("preg_split"))
                    return;

                bool javascript = false;
                var lines = file.Content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("<script")) javascript = true;
                    if (lines[i].Contains("</script")) javascript = false;

                    if (!javascript && !lines[i].Contains(".split"))
                    {
                        lines[i] = Regex.Replace(lines[i], @"\bsplit ?\('(\\'|[^'])*'", evaluator);
                        lines[i] = Regex.Replace(lines[i], @"\bsplit ?\(""(\\""|[^""])*""", evaluator);
                    }
                }
                if (Regex.IsMatch(file.Content = string.Join('\n', lines), @"[^_\.]split ?\("))
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
            if (!file.Path.Contains(@"\plugins\imagemanager\plugins\Uploaded\Uploaded.php"))
                return;

            file.Content = file.Content.Replace("$this->_uploadedFile(&$man, $file1);", "$this->_uploadedFile($man, $file1);");
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
                (var oldKey, var newKey, var newValue) = renamedItems.Pop();
                FindReplace.Remove(oldKey);
                FindReplace.Add(newKey, newValue);
            }
        }
    }
}

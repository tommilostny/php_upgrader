using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PhpUpgrader
{
    /// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
    public class MonaUpgrader
    {
        /// <summary> Seznam souborů, které se nepodařilo aktualizovat a stále obsahují mysql_ funkce. </summary>
        public List<string> FilesContainingMysql { get; } = new();

        private readonly string[] _findWhat;
        private readonly string[] _replaceWith;
        private readonly string _baseFolder;
        private readonly string[] _adminFolders;
        private readonly string _webName;
        private readonly string? _hostname;
        private readonly string? _database;
        private readonly string? _username;
        private readonly string? _password;
        private readonly string? _replaceBetaWith;
        private readonly string _connectionFile;

        /// <summary>
        /// Inicializace PHP upgraderu.
        /// </summary>
        /// <param name="baseFolder">Absolutní cesta základní složky (př. default C:\McRAI\), kde jsou složky 'weby' a 'important'.</param>
        /// <param name="webName">Název webu ve složce 'weby'.</param>
        /// <param name="adminFolders">Složky obsahující administraci RS Mona (default null => 1 složka admin)</param>
        /// <param name="database">Nová databáze na serveru hostname.</param>
        /// <param name="username">Nové uživatelské jméno k databázi.</param>
        /// <param name="password">Nové heslo k databázi.</param>
        /// <param name="hostname">URL k databázovému serveru (př. default mcrai2.vshosting.cz)</param>
        /// <param name="replaceBetaWith"></param>
        /// <param name="connectionFile"></param>
        public MonaUpgrader(string baseFolder, string webName, string[]? adminFolders, string? database, string? username, string? password, string? hostname, string? replaceBetaWith, string connectionFile)
        {
            _findWhat = File.ReadAllLines($@"{baseFolder}important\find_what.txt");
            _replaceWith = File.ReadAllLines($@"{baseFolder}important\replace_with.txt");
            _baseFolder = baseFolder;
            _webName = webName;
            _adminFolders = adminFolders ?? new string[] { "admin" };
            _database = database;
            _username = username;
            _password = password;
            _hostname = hostname;
            _connectionFile = connectionFile;

            if ((_replaceBetaWith = replaceBetaWith) is not null)
            {
                for (int i = 0; i < _findWhat.Length; i++)
                {
                    RenameBeta(ref _findWhat[i]);
                    RenameBeta(ref _replaceWith[i]);
                }
            }
        }

        /// <summary>
        /// Rekurzivní upgrade .php souborů ve všech podadresářích.
        /// </summary>
        /// <param name="directoryName">Cesta k adresáři, kde hledat .php soubory.</param>
        public void UpgradeAllFilesRecursively(string directoryName)
        {
            foreach (var subdir in Directory.GetDirectories(directoryName))
            {
                if (Directory.GetDirectories(subdir).Length > 0)
                    UpgradeAllFilesRecursively(subdir);
                UpgradeFiles(subdir);
            }
            UpgradeFiles(directoryName);
        }

        /// <summary> Upgrade všech .php souborů v jednom adresáři. </summary>
        public void UpgradeFiles(string directoryName)
        {
            foreach (var fileName in Directory.GetFiles(directoryName, "*.php"))
            {
                Console.WriteLine(fileName);
                string fileContent = File.ReadAllText(fileName);
                string originalContent = fileContent;

                if (UpgradeTinyAjaxBehavior(fileName))
                    continue;

                if (!fileName.Contains("tiny_mce"))
                {
                    UpgradeConnect(fileName, ref fileContent);
                    UpgradeMysqlResult(ref fileContent);
                    UpgradeClanekVypis(ref fileContent);
                    UpgradeFindReplace(ref fileContent);
                    UpgradeMysqliQueries(ref fileContent);
                    UpgradeMysqliClose(fileName, ref fileContent);
                    UpgradeAnketa(fileName, ref fileContent);
                    UpgradeChdir(fileName, ref fileContent);
                    UpgradeTableAddEdit(fileName, ref fileContent);
                    UpgradeStrankovani(fileName, ref fileContent);
                    UpgradeXmlFeeds(fileName, ref fileContent);
                    UpgradeSitemapSave(fileName, ref fileContent);
                    UpgradeGlobalBeta(ref fileContent);
                    RenameBeta(ref fileContent);
                }
                UpgradeEreg(ref fileContent);

                //upraveno, zapsat do souboru
                if (fileContent != originalContent)
                    File.WriteAllText(fileName, fileContent);

                //po dodelani nahrazeni nize projit na retezec - mysql_
                if (fileContent.ToLower().Contains("mysql_") && !FilesContainingMysql.Contains(fileName))
                    FilesContainingMysql.Add(fileName);
            }
        }

        /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
        public void UpgradeConnect(string fileName, ref string fileContent)
        {
            if (!fileName.Contains($@"\connect\{_connectionFile}") && !fileName.Contains($@"\system\{_connectionFile}"))
            {
                return;
            }
            string connectHead = string.Empty;
            bool inComment = false;
            using var sr = new StreamReader(fileName);

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                connectHead += $"{line}\n";

                if (line.Contains("/*"))
                {
                    inComment = true;
                }
                if (line.Contains("*/"))
                {
                    inComment = false;
                    if (line.TrimStart().StartsWith("$password_beta"))
                        continue;
                }

                if (line.Contains("$password_beta") && !inComment && !line.Contains("//$password_beta"))
                    break;
            }

            //generování nových údajů k databázi, pokud jsou všechny zadány
            if (_database is not null && _username is not null && _password is not null && _hostname is not null)
            {
                connectHead = connectHead.Replace("\n", "\n//"); //zakomentovat původní řádky
                connectHead = connectHead.Replace("////", "//"); //smazat zbytečná lomítka
                connectHead += '\n';
                connectHead = connectHead.Replace("//\n", "\n");
                connectHead += $"$hostname_beta = \"{_hostname}\";\n$database_beta = \"{_database}\";\n$username_beta = \"{_username}\";\n$password_beta = \"{_password}\";\n";
            }
            fileContent = connectHead + File.ReadAllText($"{_baseFolder}important\\connection.txt");
        }

        /// <summary>
        /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
        /// </summary>
        public bool UpgradeTinyAjaxBehavior(string fileName)
        {
            bool foundTAB = false;
            foreach (var adminFolder in _adminFolders)
            {
                if (fileName.Contains($@"\{adminFolder}\include\TinyAjaxBehavior.php"))
                {
                    File.Copy($"{_baseFolder}important\\TinyAjaxBehavior.txt", fileName, overwrite: true);
                    foundTAB = true;
                }
            }
            return foundTAB;
        }

        /// <summary>
        /// mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
        /// </summary>
        public static void UpgradeMysqlResult(ref string fileContent)
        {
            if (!fileContent.Contains("mysql_result"))
            {
                return;
            }
            var lines = fileContent.Split('\n');
            fileContent = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("mysql_result"))
                {
                    lines[i] = lines[i].Replace("COUNT(*)", "*");
                    lines[i] = lines[i].Replace(", 0", string.Empty);
                    lines[i] = lines[i].Replace("mysql_result", "mysqli_num_rows");
                }
                fileContent += $"{lines[i]}\n";
            }
        }

        /// <summary>
        /// upravit soubory system/clanek.php a system/vypis.php - pokud je sdileni fotogalerii pridat nad podminku $vypis_table_clanek["sdileni_fotogalerii"] kod $p_sf = array();
        /// </summary>
        public static void UpgradeClanekVypis(ref string fileContent)
        {
            if (!fileContent.Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]") || fileContent.Contains("$p_sf = array();"))
            {
                return;
            }
            var lines = fileContent.Split('\n');
            fileContent = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
                {
                    fileContent += "        $p_sf = array();\n";
                }
                fileContent += $"{lines[i]}\n";
            }
        }

        /// <summary>
        /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
        /// </summary>
        public void UpgradeFindReplace(ref string fileContent)
        {
            for (int i = 0; i < _findWhat.Length; i++)
            {
                fileContent = fileContent.Replace(_findWhat[i], _replaceWith[i]);
            }
        }

        /// <summary>
        /// po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta
        /// (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db);
        /// predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
        /// </summary>
        public static void UpgradeMysqliQueries(ref string fileContent)
        {
            if (fileContent.Contains("$this->db"))
            {
                fileContent = fileContent.Replace("mysqli_query($beta, \"SET CHARACTER SET utf8\", $this->db);", "mysqli_query($this->db, \"SET CHARACTER SET utf8\");");
                fileContent = fileContent.Replace("$beta", "$this->db");
            }
        }

        /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
        public void UpgradeMysqliClose(string fileName, ref string fileContent)
        {
            if (fileName.Contains($@"{_webName}\index.php") && !fileContent.Contains("mysqli_close"))
            {
                fileContent += "\n<?php mysqli_close($beta); ?>";
            }
        }

        /// <summary>
        /// upravit soubor anketa/anketa.php - r.3 (odmazat ../)
        ///     - include_once "../setup.php"; na include_once "setup.php";
        /// </summary>
        public static void UpgradeAnketa(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\anketa\anketa.php"))
            {
                fileContent = fileContent.Replace("include_once(\"../setup.php\")", "include_once(\"setup.php\")");
            }
        }

        /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
        public void UpgradeChdir(string fileName, ref string fileContent)
        {
            foreach (var adminFolder in _adminFolders)
            {
                if (fileName.Contains($@"\{adminFolder}\funkce\vytvoreni_adr.php") && !fileContent.Contains("//chdir"))
                {
                    fileContent = fileContent.Replace("chdir", "//chdir");
                }
            }
        }

        /// <summary>
        /// upravit soubor admin/table_x_add.php
        ///     - potlacit chybova hlasku znakem „@“ na radku cca 47-55 - $pocet_text_all = mysqli_num_rows….
        /// upravit soubor admin/table_x_edit.php
        ///     - potlacit chybova hlasku znakem „@“ na radku cca 53-80 - $pocet_text_all = mysqli_num_rows….
        /// </summary>
        public void UpgradeTableAddEdit(string fileName, ref string fileContent)
        {
            foreach (var adminFolder in _adminFolders)
            {
                if ((fileName.Contains($@"\{adminFolder}\table_x_add.php")
                    || fileName.Contains($@"\{adminFolder}\table_x_edit.php"))
                    && !fileContent.Contains("@$pocet_text_all"))
                {
                    fileContent = fileContent.Replace("$pocet_text_all = mysqli_num_rows", "@$pocet_text_all = mysqli_num_rows");
                }
            }
        }

        /// <summary>
        /// upravit soubor funkce/strankovani.php
        ///     >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
        /// </summary>
        public static void UpgradeStrankovani(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\funkce\strankovani.php"))
            {
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)");
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $prenext_2 = null)");
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $pre = null, $next = null)");
            }
        }

        /// <summary>
        /// Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
        /// </summary>
        public static void UpgradeXmlFeeds(string fileName, ref string fileContent)
        {
            if (fileName.Contains("xml_feeds_") && !fileName.Contains("xml_feeds_edit"))
            {
                fileContent = fileContent.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");
            }
        }

        /// <summary>
        /// upravit soubor admin/sitemap_save.php cca radek 84
        ///     - pridat podminku „if($query_text_all !== FALSE)“
        ///     a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
        /// </summary>
        public void UpgradeSitemapSave(string fileName, ref string fileContent)
        {
            foreach (var adminFolder in _adminFolders)
            {
                if (!fileName.Contains($"{adminFolder}\\sitemap_save.php")
                    || !fileContent.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))")
                    || fileContent.Contains("if($query_text_all !== FALSE)"))
                {
                    continue;
                }
                bool sfBracket = false;
                var lines = fileContent.Split('\n');
                fileContent = string.Empty;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"))
                    {
                        fileContent += "          if($query_text_all !== FALSE)\n          {\n";
                        sfBracket = true;
                    }
                    if (lines[i].Contains("}") && sfBracket)
                    {
                        fileContent += $"    {lines[i]}\n";
                        sfBracket = false;
                    }
                    fileContent += $"{lines[i]}\n";
                }
            }
        }

        /// <summary>
        /// pro všechny funkce které v sobe mají dotaz na db pridat na zacatek
        ///     - global $beta; >>> hledat v netbeans - (?s)^(?=.*?function )(?=.*?mysqli_) - regular
        /// </summary>
        public static void UpgradeGlobalBeta(ref string fileContent)
        {
            if (!Regex.IsMatch(fileContent, "(?s)^(?=.*?function )(?=.*?mysqli_)") || fileContent.Contains("$this"))
            {
                return;
            }
            bool javascript = false;
            var lines = fileContent.Split('\n');
            fileContent = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<script")) javascript = true;
                if (lines[i].Contains("</script")) javascript = false;

                fileContent += $"{lines[i]}\n";

                if (lines[i].Contains("function") && !javascript && CheckForMysqli_BeforeAnotherFunction(lines, i))
                {
                    fileContent += $"{lines[++i]}\n\n    global $beta;\n\n";
                }
            }

            static bool CheckForMysqli_BeforeAnotherFunction(string[] lines, int startIndex)
            {
                bool javascript = false;
                bool inComment = false;
                int bracketCount = 0;

                for (int i = startIndex; i < lines.Length; i++)
                {
                    if (lines[i].Contains("<script")) javascript = true;
                    if (lines[i].Contains("</script")) javascript = false;

                    if (javascript)
                        continue;

                    if (lines[i].Contains("/*")) inComment = true;
                    if (lines[i].Contains("*/")) inComment = false;

                    if (lines[i].Contains("mysqli_") && !inComment && !lines[i].TrimStart().StartsWith("//"))
                        return true;

                    if (lines[i].Contains("{")) bracketCount++;
                    if (lines[i].Contains("}")) bracketCount--;

                    if ((lines[i].Contains("global $beta;") || bracketCount <= 0) && i > startIndex)
                        break;
                }
                return false;
            }
        }

        /// <summary> Přejmenuje proměnnou $beta na přednastavenou hodnotu. </summary>
        public void RenameBeta(ref string fileContent)
        {
            if (_replaceBetaWith is not null)
            {
                fileContent = fileContent.Replace("$beta", $"${_replaceBetaWith}");
            }
        }

        /// <summary>
        /// - funkci ereg nebo ereg_replace doplnit do prvního parametru delimetr na začátek a nakonec (if(ereg('.+@.+..+', $retezec))
        /// // puvodni, jiz nefunkcni >>> if(preg_match('#.+@.+..+#', $retezec)) // upravene - delimiter zvolen #)
        /// </summary>
        public static void UpgradeEreg(ref string fileContent)
        {
            var evaluator = new MatchEvaluator(EregToPreg);
            fileContent = Regex.Replace(fileContent, @"ereg(_replace)? ?\('(\\'|[^'])*'", evaluator);
            fileContent = Regex.Replace(fileContent, @"ereg(_replace)? ?\(""(\\""|[^""])*""", evaluator);

            fileContent = Regex.Replace(fileContent, @"ereg ?\( ?\$", "preg_match($");
            fileContent = Regex.Replace(fileContent, @"ereg_replace ?\( ?\$", "preg_replace($");

            if (fileContent.Contains("ereg"))
                Console.Error.WriteLine("- ereg alert!");

            static string EregToPreg(Match match)
            {
                int bracketIndex = match.Value.IndexOf('(');
                char quote = match.Value[bracketIndex + 1];

                string insidePattern = match.Value[(bracketIndex + 2)..(match.Value.Length - 1)];

                string pregFunction = match.Value.StartsWith("ereg_replace") ? "preg_replace" : "preg_match";

                return $"{pregFunction}({quote}~{insidePattern}~{quote}";
            }
        }
    }
}

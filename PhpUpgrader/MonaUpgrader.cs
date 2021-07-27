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
        public HashSet<string> FilesContainingMysql { get; } = new();

        /// <summary>
        /// Co nahradit? (načteno ze souboru '{<see cref="BaseFolder"/>}important/find_what.txt').
        /// </summary>
        public string[] FindWhat { get; private set; }

        /// <summary>
        /// Čím to nahradit? (načteno ze souboru '{<see cref="BaseFolder"/>}important/replace_with.txt').
        /// </summary>
        public string[] ReplaceWith { get; private set; }

        /// <summary> Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'. </summary>
        public string BaseFolder
        {
            get => _baseFolder;
            init
            {
                FindWhat = File.ReadAllLines($@"{value}important\find_what.txt");
                ReplaceWith = File.ReadAllLines($@"{value}important\replace_with.txt");
                _baseFolder = value;
            }
        }
        private string _baseFolder;

        /// <summary> Název webu ve složce 'weby'. </summary>
        public string WebName { get; init; }

        /// <summary> Složky obsahující administraci RS Mona (null => 1 složka 'admin') </summary>
        public string[] AdminFolders
        {
            get => _adminFolders;
            init => _adminFolders = value ?? new string[] { "admin" };
        }
        private string[] _adminFolders;

        /// <summary> URL k databázovému serveru. </summary>
        public string? Hostname { get; init; }

        /// <summary> Nová databáze na serveru hostname. </summary>
        public string? Database { get; init; }

        /// <summary> Nové uživatelské jméno k databázi. </summary>
        public string? Username { get; init; }

        /// <summary> Nové heslo k databázi. </summary>
        public string? Password { get; init; }

        /// <summary> Přejmenovat proměnnou $beta tímto názvem (null => nepřejmenovávat). </summary>
        /// <remarks> Mělo by být nastaveno po <see cref="BaseFolder"/>, která načte <see cref="FindWhat"/> a <see cref="ReplaceWith"/>, které můžou obsahovat proměnnou $beta. </remarks>
        public string? RenameBetaWith
        {
            get => _replaceBetaWith;
            init
            {
                if ((_replaceBetaWith = value) is not null)
                {
                    for (int i = 0; i < FindWhat?.Length; i++)
                    {
                        RenameBeta(ref FindWhat[i]);
                        RenameBeta(ref ReplaceWith[i]);
                    }
                }
            }
        }
        private string? _replaceBetaWith;

        /// <summary> Název souboru ve složce 'connect'. </summary>
        public string ConnectionFile { get; init; }


        /// <summary> Rekurzivní upgrade .php souborů ve všech podadresářích. </summary>
        /// <param name="directoryPath">Cesta k adresáři, kde hledat .php soubory.</param>
        public void UpgradeAllFilesRecursively(string directoryPath)
        {
            foreach (var subdir in Directory.GetDirectories(directoryPath))
            {
                if (Directory.GetDirectories(subdir).Length > 0)
                    UpgradeAllFilesRecursively(subdir);
                UpgradeFiles(subdir);
            }
            UpgradeFiles(directoryPath);
        }

        /// <summary> Upgrade všech .php souborů v jednom adresáři. </summary>
        public void UpgradeFiles(string directoryPath)
        {
            foreach (var filePath in Directory.GetFiles(directoryPath, "*.php"))
            {
                Console.WriteLine(filePath);

                if (UpgradeTinyAjaxBehavior(filePath))
                    continue;

                string fileContent = File.ReadAllText(filePath);
                string originalContent = fileContent;

                if (!filePath.Contains("tiny_mce"))
                {
                    UpgradeConnect(filePath, ref fileContent);
                    UpgradeMysqlResult(ref fileContent);
                    UpgradeClanekVypis(ref fileContent);
                    UpgradeFindReplace(ref fileContent);
                    UpgradeMysqliQueries(ref fileContent);
                    UpgradeMysqliClose(filePath, ref fileContent);
                    UpgradeAnketa(filePath, ref fileContent);
                    UpgradeChdir(filePath, ref fileContent);
                    UpgradeTableAddEdit(filePath, ref fileContent);
                    UpgradeStrankovani(filePath, ref fileContent);
                    UpgradeXmlFeeds(filePath, ref fileContent);
                    UpgradeSitemapSave(filePath, ref fileContent);
                    UpgradeGlobalBeta(ref fileContent);
                    RenameBeta(ref fileContent);
                }
                UpgradeRegexFunctions(ref fileContent);

                //upraveno, zapsat do souboru
                if (fileContent != originalContent)
                    File.WriteAllText(filePath, fileContent);

                //po dodelani nahrazeni nize projit na retezec - mysql_
                if (fileContent.ToLower().Contains("mysql_"))
                    FilesContainingMysql.Add(filePath);
            }
        }

        /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
        public void UpgradeConnect(string filePath, ref string fileContent)
        {
            if (!filePath.Contains($@"\connect\{ConnectionFile}") && !filePath.Contains($@"\system\{ConnectionFile}"))
            {
                return;
            }
            string connectHead = string.Empty;
            bool inComment = false;
            using var sr = new StreamReader(filePath);

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
            if (Database is not null && Username is not null && Password is not null && Hostname is not null)
            {
                connectHead = connectHead.Replace("\n", "\n//"); //zakomentovat původní řádky
                connectHead = connectHead.Replace("////", "//"); //smazat zbytečná lomítka
                connectHead += '\n';
                connectHead = connectHead.Replace("//\n", "\n");
                connectHead += $"$hostname_beta = \"{Hostname}\";\n$database_beta = \"{Database}\";\n$username_beta = \"{Username}\";\n$password_beta = \"{Password}\";\n";
            }
            fileContent = connectHead + File.ReadAllText($"{BaseFolder}important\\connection.txt");
        }

        /// <summary>
        /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
        /// </summary>
        public bool UpgradeTinyAjaxBehavior(string filePath)
        {
            bool foundTAB = false;
            foreach (var adminFolder in AdminFolders)
            {
                if (filePath.Contains($@"\{adminFolder}\include\TinyAjaxBehavior.php"))
                {
                    File.Copy($"{BaseFolder}important\\TinyAjaxBehavior.txt", filePath, overwrite: true);
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
            for (int i = 0; i < FindWhat.Length; i++)
            {
                fileContent = fileContent.Replace(FindWhat[i], ReplaceWith[i]);
            }
        }

        /// <summary>
        /// po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta
        /// (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db);
        /// predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
        /// </summary>
        public void UpgradeMysqliQueries(ref string fileContent)
        {
            if (fileContent.Contains("$this->db"))
            {
                fileContent = fileContent.Replace("mysqli_query($beta, \"SET CHARACTER SET utf8\", $this->db);", "mysqli_query($this->db, \"SET CHARACTER SET utf8\");");
                RenameBeta(ref fileContent, "this->db");
            }
        }

        /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
        public void UpgradeMysqliClose(string filePath, ref string fileContent)
        {
            if (filePath.Contains($@"{WebName}\index.php") && !fileContent.Contains("mysqli_close"))
            {
                fileContent += "\n<?php mysqli_close($beta); ?>";
            }
        }

        /// <summary>
        /// upravit soubor anketa/anketa.php - r.3 (odmazat ../)
        ///     - include_once "../setup.php"; na include_once "setup.php";
        /// </summary>
        public static void UpgradeAnketa(string filePath, ref string fileContent)
        {
            if (filePath.Contains(@"\anketa\anketa.php"))
            {
                fileContent = fileContent.Replace("include_once(\"../setup.php\")", "include_once(\"setup.php\")");
            }
        }

        /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
        public void UpgradeChdir(string filePath, ref string fileContent)
        {
            foreach (var adminFolder in AdminFolders)
            {
                if (filePath.Contains($@"\{adminFolder}\funkce\vytvoreni_adr.php") && !fileContent.Contains("//chdir"))
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
        public void UpgradeTableAddEdit(string filePath, ref string fileContent)
        {
            foreach (var adminFolder in AdminFolders)
            {
                if ((filePath.Contains($@"\{adminFolder}\table_x_add.php")
                    || filePath.Contains($@"\{adminFolder}\table_x_edit.php"))
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
        public static void UpgradeStrankovani(string filePath, ref string fileContent)
        {
            if (filePath.Contains(@"\funkce\strankovani.php"))
            {
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)");
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $prenext_2 = null)");
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $pre = null, $next = null)");

                //zahlásit chybu při nalezení další varianty funkce predchozi_dalsi
                if (!fileContent.Contains("$texta = null") && fileContent.Contains("function predchozi_dalsi"))
                    Console.Error.WriteLine("- predchozi_dalsi error!");
            }
        }

        /// <summary>
        /// Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
        /// </summary>
        public static void UpgradeXmlFeeds(string filePath, ref string fileContent)
        {
            if (filePath.Contains("xml_feeds_") && !filePath.Contains("xml_feeds_edit"))
            {
                fileContent = fileContent.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");
            }
        }

        /// <summary>
        /// upravit soubor admin/sitemap_save.php cca radek 84
        ///     - pridat podminku „if($query_text_all !== FALSE)“
        ///     a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
        /// </summary>
        public void UpgradeSitemapSave(string filePath, ref string fileContent)
        {
            foreach (var adminFolder in AdminFolders)
            {
                if (!filePath.Contains($"{adminFolder}\\sitemap_save.php")
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

                if (lines[i].Contains("function") && !javascript && _CheckForMysqliBeforeAnotherFunction(lines, i))
                {
                    fileContent += $"{lines[++i]}\n\n    global $beta;\n\n";
                }
            }

            static bool _CheckForMysqliBeforeAnotherFunction(string[] lines, int startIndex)
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
        /// <param name="replacement">null => použít vlastnost RenameBetaWith.</param>
        /// <param name="fileContent"></param>
        public void RenameBeta(ref string fileContent, string? replacement = null)
        {
            if ((replacement ??= RenameBetaWith) is not null)
            {
                fileContent = fileContent.Replace("$beta", $"${replacement}");
            }
        }

        /// <summary>
        /// - funkci ereg nebo ereg_replace doplnit do prvního parametru delimetr na začátek a nakonec (if(ereg('.+@.+..+', $retezec))
        /// // puvodni, jiz nefunkcni >>> if(preg_match('#.+@.+..+#', $retezec)) // upravene - delimiter zvolen #)
        /// </summary>
        public static void UpgradeRegexFunctions(ref string fileContent)
        {
            var evaluator = new MatchEvaluator(_PregMatchEvaluator);

            //funkce ereg
            fileContent = Regex.Replace(fileContent, @"ereg(_replace)? ?\('(\\'|[^'])*'", evaluator);
            fileContent = Regex.Replace(fileContent, @"ereg(_replace)? ?\(""(\\""|[^""])*""", evaluator);

            fileContent = Regex.Replace(fileContent, @"ereg ?\( ?\$", "preg_match($");
            fileContent = Regex.Replace(fileContent, @"ereg_replace ?\( ?\$", "preg_replace($");

            if (fileContent.Contains("ereg"))
                Console.Error.WriteLine("- ereg alert!");

            //funkce split
            if (!fileContent.Contains("split"))
                return;

            if (fileContent.Contains("script") && fileContent.Contains(".split"))
            {
                //soubor obsahuje Javascript i funkci split, zkontrolovat manuálně
                Console.Error.WriteLine("- split Javascript alert!");
                return;
            }
            fileContent = Regex.Replace(fileContent, @"\bsplit ?\('(\\'|[^'])*'", evaluator);
            fileContent = Regex.Replace(fileContent, @"\bsplit ?\(""(\\""|[^""])*""", evaluator);

            if (Regex.IsMatch(fileContent, @"[^preg_]split ?\("))
                Console.Error.WriteLine("- unmodified split alert!");

            static string _PregMatchEvaluator(Match match)
            {
                int bracketIndex = match.Value.IndexOf('(');
                char quote = match.Value[bracketIndex + 1];

                string insidePattern = match.Value[(bracketIndex + 2)..(match.Value.Length - 1)];

                string pregFunction = match.Value[0..bracketIndex].TrimEnd() switch
                {
                    "ereg_replace" => "preg_replace",
                    "split" => "preg_split",
                    _ => "preg_match"
                };
                return $"{pregFunction}({quote}~{insidePattern}~{quote}";
            }
        }
    }
}

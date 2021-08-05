﻿using System;
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
            get => _adminFolders ??= new string[] { "admin" };
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
                if ((_replaceBetaWith = value) is null)
                    return;

                for (int i = 0; i < FindWhat?.Length; i++)
                {
                    FindWhat[i] = RenameBeta(FindWhat[i], value);
                    ReplaceWith[i] = RenameBeta(ReplaceWith[i], value);
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
            //rekurzivní aktualizace podsložek
            foreach (var subdir in Directory.GetDirectories(directoryPath))
            {
                UpgradeAllFilesRecursively(subdir);
            }
            //aktualizace aktuální složky
            foreach (var filePath in Directory.GetFiles(directoryPath, "*.php"))
            {
                Console.WriteLine(filePath.Replace($"{BaseFolder}weby\\", string.Empty));

                if (UpgradeTinyAjaxBehavior(filePath))
                    continue;

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
                UpgradeRegexFunctions(file);

                //upraveno, zapsat do souboru
                file.Save();

                //po dodelani nahrazeni nize projit na retezec - mysql_
                if (Regex.IsMatch(file.Content, "mysql_", RegexOptions.IgnoreCase))
                    FilesContainingMysql.Add(filePath);
            }
        }

        /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
        public void UpgradeConnect(FileWrapper file)
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
            file.Content = connectHead + File.ReadAllText($"{BaseFolder}important\\connection.txt");
        }

        /// <summary>
        /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
        /// </summary>
        public bool UpgradeTinyAjaxBehavior(string filePath)
        {
            if (!AdminFolders.Any(af => filePath.Contains($@"\{af}\include\TinyAjaxBehavior.php")))
                return false;

            File.Copy($"{BaseFolder}important\\TinyAjaxBehavior.txt", filePath, overwrite: true);
            return true;
        }

        /// <summary>
        /// mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
        /// </summary>
        public static void UpgradeMysqlResult(FileWrapper file)
        {
            if (!file.Content.Contains("mysql_result"))
                return;

            var lines = file.Content.Split('\n');
            var newContent = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("mysql_result"))
                {
                    lines[i] = lines[i].Replace("COUNT(*)", "*");
                    lines[i] = lines[i].Replace(", 0", string.Empty);
                    lines[i] = lines[i].Replace("mysql_result", "mysqli_num_rows");
                }
                newContent += $"{lines[i]}\n";
            }
            file.Content = newContent;
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
            var newContent = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
                {
                    newContent += "        $p_sf = array();\n";
                }
                newContent += $"{lines[i]}\n";
            }
            file.Content = newContent;
        }

        /// <summary>
        /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
        /// </summary>
        public void UpgradeFindReplace(FileWrapper file)
        {
            for (int i = 0; i < FindWhat?.Length; i++)
            {
                file.Content = file.Content.Replace(FindWhat[i], ReplaceWith[i]);
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
        public void UpgradeMysqliClose(FileWrapper file)
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
            Console.Error.WriteLine("- predchozi_dalsi error!");

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
            var newContent = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"))
                {
                    newContent += "          if($query_text_all !== FALSE)\n          {\n";
                    sfBracket = true;
                }
                if (lines[i].Contains("}") && sfBracket)
                {
                    newContent += $"    {lines[i]}\n";
                    sfBracket = false;
                }
                newContent += $"{lines[i]}\n";
            }
            file.Content = newContent;
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
            var newContent = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<script")) javascript = true;
                if (lines[i].Contains("</script")) javascript = false;

                newContent += $"{lines[i]}\n";

                if (lines[i].Contains("function") && !javascript && _MysqliInFunction(i))
                {
                    newContent += $"{lines[++i]}\n\n    global $beta;\n\n";
                }
            }
            file.Content = newContent;

            bool _MysqliInFunction(int startIndex)
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
        /// <param name="content"></param>
        public string RenameBeta(string content, string? replacement = null)
        {
            if ((replacement ??= RenameBetaWith) is not null)
            {
                content = content.Replace("$beta", $"${replacement}");
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
                    Console.Error.WriteLine("- ereg alert!");
            }

            void _UpgradeSplit()
            {
                if (!file.Content.Contains("split") || file.Content.Contains("preg_split"))
                    return;

                if (file.Content.Contains("script") && file.Content.Contains(".split"))
                {
                    //soubor obsahuje Javascript i funkci split, zkontrolovat manuálně
                    Console.Error.WriteLine("- split Javascript alert!");
                    return;
                }
                file.Content = Regex.Replace(file.Content, @"\bsplit ?\('(\\'|[^'])*'", evaluator);
                file.Content = Regex.Replace(file.Content, @"\bsplit ?\(""(\\""|[^""])*""", evaluator);

                if (Regex.IsMatch(file.Content, @"[^preg_]split ?\("))
                    Console.Error.WriteLine("- unmodified split alert!");
            }

            static string _PregMatchEvaluator(Match match)
            {
                int bracketIndex = match.Value.IndexOf('(');

                string pregFunction = match.Value[0..bracketIndex].TrimEnd() switch
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
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace php_upgrader
{
    /// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
    public class PhpUpgrader
    {
        /// <summary>
        /// Seznam souborů, které se nepodařilo aktualizovat a stále obsahují mysql_ funkce.
        /// </summary>
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

        /// <summary>
        /// Inicializace PHP upgraderu.
        /// </summary>
        /// <param name="findWhat">Co nahradit.</param>
        /// <param name="replaceWith">Čím to nahradit.</param>
        /// <param name="baseFolder">Absolutní cesta základní složky (př. default C:\McRAI\), kde jsou složky 'weby' a 'important'.</param>
        /// <param name="webName">Název webu ve složce 'weby'.</param>
        /// <param name="adminFolders">Složky obsahující administraci RS Mona (default null => 1 složka admin)</param>
        /// <param name="database">Nová databáze na serveru hostname.</param>
        /// <param name="username">Nové uživatelské jméno k databázi.</param>
        /// <param name="password">Nové heslo k databázi.</param>
        /// <param name="hostname">URL k databázovému serveru (př. default mcrai2.vshosting.cz)</param>
        public PhpUpgrader(
            string[] findWhat, string[] replaceWith, string baseFolder, string webName,
            string[]? adminFolders, string? database, string? username, string? password, string? hostname)
        {
            _findWhat = findWhat;
            _replaceWith = replaceWith;
            _baseFolder = baseFolder;
            _webName = webName;
            _adminFolders = adminFolders ?? new string[] { "admin" };
            _database = database;
            _username = username;
            _password = password;
            _hostname = hostname;
        }

        /// <summary>
        /// Rekurzivní upgrade .php souborů ve všech podadresářích.
        /// </summary>
        /// <param name="directoryName">Cesta k adresáři, kde hledat .php soubory.</param>
        public void UpgradeAllFilesRecursively(string directoryName)
        {
            foreach (var subdir in Directory.GetDirectories(directoryName))
            {
                if (Directory.GetDirectories(subdir).Length > 0 && !subdir.Contains("tiny_mce"))
                    UpgradeAllFilesRecursively(subdir);
                UpgradeFiles(subdir);
            }
            UpgradeFiles(directoryName);
        }

        /// <summary> Upgrade všech .php souborů v jednom adresáři. </summary>
        private void UpgradeFiles(string directoryName)
        {
            foreach (var fileName in Directory.GetFiles(directoryName, "*.php"))
            {
                Console.WriteLine(fileName);
                var fileContent = File.ReadAllText(fileName);

                if (UpgradeTinyAjaxBehavior(fileName))
                    continue;

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
                UpgradeSitemapSave(fileName, ref fileContent);
                UpgradeGlobalBeta(ref fileContent);

                File.WriteAllText(fileName, fileContent);

                //po dodelani nahrazeni nize projit na retezec - mysql_
                if (fileContent.ToLower().Contains("mysql_"))
                    FilesContainingMysql.Add(fileName);
            }
        }

        /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
        private void UpgradeConnect(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\connect\connection.php"))
            {
                var connectHead = string.Empty;
                using (var sr = new StreamReader(fileName))
                {
                    var inComment = false;
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();

                        if (line.Contains("/*")) inComment = true;
                        if (line.Contains("*/")) inComment = false;

                        connectHead += $"{line}\n";

                        if (line.Contains("$password_beta") && !inComment && !line.Contains("//$password_beta"))
                            break;
                    }
                }
                //generování nových údajů k databázi, pokud jsou všechny zadány
                if (_database is not null && _username is not null && _password is not null && _hostname is not null)
                {
                    connectHead = connectHead.Replace("\n", "\n//");
                    connectHead = connectHead.Replace("////", "//"); 
                    connectHead += $"\n\n$hostname_beta = \"{_hostname}\";\n$database_beta = \"{_database}\";\n$username_beta = \"{_username}\";\n$password_beta = \"{_password}\";\n";
                }
                fileContent = connectHead + File.ReadAllText($"{_baseFolder}important\\connection.txt");
            }
        }

        /// <summary>
        /// predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
        /// </summary>
        private bool UpgradeTinyAjaxBehavior(string fileName)
        {
            var foundTAB = false;

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
        private static void UpgradeMysqlResult(ref string fileContent)
        {
            if (fileContent.Contains("mysql_result"))
            {
                var lines = fileContent.Split('\n');
                fileContent = string.Empty;

                for (var i = 0; i < lines.Length; i++)
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
        }

        /// <summary>
        /// upravit soubory system/clanek.php a system/vypis.php - pokud je sdileni fotogalerii pridat nad podminku $vypis_table_clanek["sdileni_fotogalerii"] kod $p_sf = array();
        /// </summary>
        private static void UpgradeClanekVypis(ref string fileContent)
        {
            if (fileContent.Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
            {
                var lines = fileContent.Split('\n');
                fileContent = string.Empty;

                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]")
                        && !lines[i - 1].Contains("$p_sf = array();"))
                    {
                        fileContent += "        $p_sf = array();\n";
                    }
                    fileContent += $"{lines[i]}\n";
                }
            }
        }

        /// <summary>
        /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
        /// </summary>
        private void UpgradeFindReplace(ref string fileContent)
        {
            for (var i = 0; i < _findWhat.Length; i++)
            {
                fileContent = fileContent.Replace(_findWhat[i], _replaceWith[i]);
            }
        }

        /// <summary>
        /// po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta
        /// (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db);
        /// predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
        /// </summary>
        private static void UpgradeMysqliQueries(ref string fileContent)
        {
            if (fileContent.Contains("$this->db"))
            {
                fileContent = fileContent.Replace("mysqli_query($beta, \"SET CHARACTER SET utf8\", $this->db);", "mysqli_query($this->db, \"SET CHARACTER SET utf8\");");
                fileContent = fileContent.Replace("$beta", "$this->db");
            }
        }

        /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
        private void UpgradeMysqliClose(string fileName, ref string fileContent)
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
        private static void UpgradeAnketa(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\anketa\anketa.php"))
            {
                fileContent = fileContent.Replace("include_once(\"../setup.php\")", "include_once(\"setup.php\")");
            }
        }

        /// <summary> zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php </summary>
        private void UpgradeChdir(string fileName, ref string fileContent)
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
        private void UpgradeTableAddEdit(string fileName, ref string fileContent)
        {
            foreach (var adminFolder in _adminFolders)
            {
                if (fileName.Contains($@"\{adminFolder}\table_x_add.php")
                    || fileName.Contains($@"\{adminFolder}\table_x_edit.php")
                    && !fileContent.Contains("@pocet_text_all"))
                {
                    fileContent = fileContent.Replace("$pocet_text_all = mysqli_num_rows", "@$pocet_text_all = mysqli_num_rows");
                }
            }
        }

        /// <summary>
        /// upravit soubor funkce/strankovani.php
        ///     >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
        /// </summary>
        private static void UpgradeStrankovani(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\funkce\strankovani.php"))
            {
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)");
            }
        }

        /// <summary>
        /// upravit soubor admin/sitemap_save.php cca radek 84
        ///     - pridat podminku „if($query_text_all !== FALSE)“
        ///     a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
        /// </summary>
        private void UpgradeSitemapSave(string fileName, ref string fileContent)
        {
            foreach (var adminFolder in _adminFolders)
            {
                if (fileName.Contains($"{adminFolder}\\sitemap_save.php")
                    && fileContent.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))")
                    && !fileContent.Contains("if($query_text_all !== FALSE)"))
                {
                    var lines = fileContent.Split('\n');
                    var sfBracket = false;
                    fileContent = string.Empty;

                    for (var i = 0; i < lines.Length; i++)
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
        }

        /// <summary>
        /// pro všechny funkce které v sobe mají dotaz na db pridat na zacatek
        ///     - global $beta; >>> hledat v netbeans - (?s)^(?=.*?function )(?=.*?mysqli_) - regular
        /// </summary>
        private static void UpgradeGlobalBeta(ref string fileContent)
        {
            if (Regex.IsMatch(fileContent, "(?s)^(?=.*?function )(?=.*?mysqli_)") && !fileContent.Contains("$this"))
            {
                var lines = fileContent.Split('\n');
                var javascript = false;
                fileContent = string.Empty;

                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("<script")) javascript = true;
                    if (lines[i].Contains("</script")) javascript = false;

                    fileContent += $"{lines[i]}\n";
                    if (lines[i].Contains("function") && !javascript)
                    {
                        if (CheckForMysqli_BeforeAnotherFunction(lines, i))
                        {
                            fileContent += $"{lines[++i]}\n\n    global $beta;\n\n";
                            Console.WriteLine(" - global $beta; added");
                        }
                    }
                }
            }
        }

        /// <summary> Kontrola funkce zda obsahuje mysqli_ (pro přidávání global $beta;). </summary>
        private static bool CheckForMysqli_BeforeAnotherFunction(string[] lines, int startIndex)
        {
            var javascript = false;
            var bracketCount = 0;

            for (var i = startIndex; i < lines.Length; i++)
            {
                if (lines[i].Contains("<script")) javascript = true;
                if (lines[i].Contains("</script")) javascript = false;

                if (!javascript)
                {
                    if (lines[i].Contains("mysqli_") && !lines[i].TrimStart(' ').StartsWith("//"))
                        return true;

                    if (lines[i].Contains("{")) bracketCount++;
                    if (lines[i].Contains("}")) bracketCount--;

                    if ((lines[i].Contains("global $beta;") || bracketCount <= 0) && i > startIndex)
                        break;
                }
            }
            return false;
        }
    }
}

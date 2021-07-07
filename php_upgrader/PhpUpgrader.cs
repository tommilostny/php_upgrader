﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace php_upgrader
{
    /// <summary>
    /// PHP upgrader pro RS Mona z verze 5 na verzi 7.
    /// </summary>
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

        /// <summary>
        /// Inicializace PHP upgraderu.
        /// </summary>
        /// <param name="findWhat">Co nahradit.</param>
        /// <param name="replaceWith">Čím to nahradit.</param>
        /// <param name="baseFolder">Absolutní cesta základní složky (př. default C:\McRAI\), kde jsou složky 'weby' a 'important'.</param>
        /// <param name="webName">Název webu ve složce 'weby'.</param>
        /// <param name="adminFolders">Složky obsahující administraci RS Mona (default: 1 složka admin)</param>
        public PhpUpgrader(string[] findWhat, string[] replaceWith, string baseFolder, string webName, string[]? adminFolders)
        {
            _findWhat = findWhat;
            _replaceWith = replaceWith;
            _baseFolder = baseFolder;
            _webName = webName;
            _adminFolders = adminFolders ?? new string[] { "admin" };
        }

        /// <summary>
        /// Upgrade všech .php souborů v jednom adresáři.
        /// </summary>
        /// <param name="directoryName">Cesta k adresáři, kde hledat .php soubory.</param>
        public void UpgradeFiles(string directoryName)
        {
            foreach (var fileName in Directory.GetFiles(directoryName, "*.php"))
            {
                Console.WriteLine(fileName);
                string fileContent = File.ReadAllText(fileName);

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

        /// <summary>
        /// Rekurzivní upgrade .php souborů ve všech podadresářích.
        /// </summary>
        /// <param name="directoryName">Cesta k adresáři, kde hledat .php soubory.</param>
        public void UpgradeFilesInFolders(string directoryName)
        {
            foreach (var subdir in Directory.GetDirectories(directoryName))
            {
                if (Directory.GetDirectories(subdir).Length > 0 && !subdir.Contains("tiny_mce"))
                    UpgradeFilesInFolders(subdir);
                UpgradeFiles(subdir);
            }
        }

        //predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona
        private void UpgradeConnect(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\connect\connection.php"))
            {
                using var sr = new StreamReader(fileName);
                string connectHead = "";
                bool inComment = false;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine() ?? "";

                    if (line.Contains("/*")) inComment = true;
                    if (line.Contains("*/")) inComment = false;

                    connectHead += $"{line}\n";
                    if (line.Contains("$password_beta") && !inComment && !line.Contains("//$password_beta"))
                        break;
                }
                fileContent = connectHead + File.ReadAllText(_baseFolder + "important\\connection.txt");
            }
        }

        //predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
        private bool UpgradeTinyAjaxBehavior(string fileName)
        {
            bool foundTAB = false;

            foreach (var adminFolder in _adminFolders)
            {
                if (fileName.Contains($@"\{adminFolder}\include\TinyAjaxBehavior.php"))
                {
                    File.Copy(_baseFolder + "important\\TinyAjaxBehavior.txt", fileName, overwrite: true);
                    foundTAB = true;
                }
            }
            return foundTAB;
        }

        //mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
        private static void UpgradeMysqlResult(ref string fileContent)
        {
            if (fileContent.Contains("mysql_result"))
            {
                string[] r = fileContent.Split('\n');
                fileContent = "";
                for (int i = 0; i < r.Length; i++)
                {
                    if (r[i].Contains("mysql_result"))
                    {
                        r[i] = r[i].Replace("COUNT(*)", "*");
                        r[i] = r[i].Replace(", 0", "");
                        r[i] = r[i].Replace("mysql_result", "mysqli_num_rows");
                    }
                    fileContent += r[i] + "\n";
                }
            }
        }

        //upravit soubory system/clanek.php a system/vypis.php - pokud je sdileni fotogalerii pridat nad podminku $vypis_table_clanek["sdileni_fotogalerii"] kod $p_sf = array();
        private static void UpgradeClanekVypis(ref string fileContent)
        {
            if (fileContent.Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
            {
                string[] r = fileContent.Split('\n');
                fileContent = "";
                for (int i = 0; i < r.Length; i++)
                {
                    if (r[i].Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]") && !r[i - 1].Contains("$p_sf = array();"))
                    {
                        fileContent += "        $p_sf = array();\n";
                    }
                    fileContent += r[i] + "\n";
                }
            }
        }

        //predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
        private void UpgradeFindReplace(ref string fileContent)
        {
            for (int i = 0; i < _findWhat.Length; i++)
            {
                fileContent = fileContent.Replace(_findWhat[i], _replaceWith[i]);
            }
        }

        //po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db); predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
        private static void UpgradeMysqliQueries(ref string fileContent)
        {
            if (fileContent.Contains("$this->db"))
            {
                fileContent = fileContent.Replace("mysqli_query($beta, \"SET CHARACTER SET utf8\", $this->db);", "mysqli_query($this->db, \"SET CHARACTER SET utf8\");");
                fileContent = fileContent.Replace("$beta", "$this->db");
            }
        }

        //pridat mysqli_close($beta); do indexu nakonec
        private void UpgradeMysqliClose(string fileName, ref string fileContent)
        {
            if (fileName.Contains(_webName + @"\index.php") && !fileContent.Contains("mysqli_close"))
            {
                fileContent += "\n<?php mysqli_close($beta); ?>";
            }
        }

        //upravit soubor anketa/anketa.php - r.3 (odmazat ../) - include_once "../setup.php"; na include_once "setup.php";
        private static void UpgradeAnketa(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\anketa\anketa.php"))
            {
                fileContent = fileContent.Replace("include_once(\"../setup.php\")", "include_once(\"setup.php\")");
            }
        }

        //zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php
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

        //upravit soubor admin/table_x_add.php - potlacit chybova hlasku znakem „@“ na radku cca 47-55 - $pocet_text_all = mysqli_num_rows….
        //upravit soubor admin/table_x_edit.php - potlacit chybova hlasku znakem „@“ na radku cca 53-80 - $pocet_text_all = mysqli_num_rows….
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

        //Upravit soubor funkce/strankovani.php >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
        private static void UpgradeStrankovani(string fileName, ref string fileContent)
        {
            if (fileName.Contains(@"\funkce\strankovani.php"))
            {
                fileContent = fileContent.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)");
            }
        }

        //upravit soubor admin/sitemap_save.php cca radek 84 - pridat podminku „if($query_text_all !== FALSE)“ a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
        private void UpgradeSitemapSave(string fileName, ref string fileContent)
        {
            foreach (var adminFolder in _adminFolders)
            {
                if (fileName.Contains($"{adminFolder}\\sitemap_save.php")
                    && fileContent.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))")
                    && !fileContent.Contains("if($query_text_all !== FALSE)"))
                {
                    string[] splitLine = fileContent.Split('\n');
                    fileContent = "";
                    bool sfBracket = false;
                    for (int i = 0; i < splitLine.Length; i++)
                    {
                        if (splitLine[i].Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"))
                        {
                            fileContent += "          if($query_text_all !== FALSE)\n          {\n";
                            sfBracket = true;
                        }
                        if (splitLine[i].Contains("}") && sfBracket)
                        {
                            fileContent += "    " + splitLine[i] + "\n";
                            sfBracket = false;
                        }
                        fileContent += splitLine[i] + "\n";
                    }
                }
            }
        }

        private static void UpgradeGlobalBeta(ref string fileContent)
        {
            //pro všechny funkce které v sobe mají dotaz na db pridat na zacatek - global $beta; >>> hledat v netbeans - (?s)^(?=.*?function )(?=.*?mysqli_) - regular
            if (Regex.IsMatch(fileContent, "(?s)^(?=.*?function )(?=.*?mysqli_)") && !fileContent.Contains("$this"))
            {
                var splitLine = fileContent.Split('\n');
                fileContent = "";
                var javascript = false;
                for (int i = 0; i < splitLine.Length; i++)
                {
                    if (splitLine[i].Contains("<script")) javascript = true;
                    if (splitLine[i].Contains("</script")) javascript = false;

                    fileContent += splitLine[i] + "\n";
                    if (splitLine[i].Contains("function") && !javascript)
                    {
                        if (CheckForMysqli_BeforeAnotherFunction(splitLine, i))
                        {
                            fileContent += splitLine[++i] + "\n\n    global $beta;\n\n";
                            Console.WriteLine(" - global $beta; added");
                        }
                    }
                }
            }
        }

        //kontrola funkce zda obsahuje mysqli_ (pro přidávání global $beta;)
        private static bool CheckForMysqli_BeforeAnotherFunction(string[] splitLine, int splitStartIndex)
        {
            bool javascript = false;
            int bracketCount = 0;
            for (int i = splitStartIndex; i < splitLine.Length; i++)
            {
                if (splitLine[i].Contains("<script")) javascript = true;
                if (splitLine[i].Contains("</script")) javascript = false;

                if (!javascript)
                {
                    if (splitLine[i].Contains("mysqli_") && !splitLine[i].TrimStart(' ').StartsWith("//"))
                        return true;

                    if (splitLine[i].Contains("{")) bracketCount++;
                    if (splitLine[i].Contains("}")) bracketCount--;

                    if ((splitLine[i].Contains("global $beta;") || bracketCount <= 0) && i > splitStartIndex)
                        break;
                }
            }
            return false;
        }
    }
}
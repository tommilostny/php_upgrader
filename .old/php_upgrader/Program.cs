using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace php_upgrader
{
    class Program
    {
        private static string base_folder = @"C:\McRAI\";
        private static string web_name;
        private static List<string> files_containing_mysql_ = new List<string>();
        private static string[] find_what;
        private static string[] replace_with;

        /// <summary>
        /// Kontrola funkce zda obsahuje mysqli_ (pro přidávání global $beta;).
        /// </summary>
        /// <param name="r">Pole s řádky souboru</param>
        /// <param name="i">Index řádku v poli</param>
        /// <returns></returns>
        private static bool CheckForMysqli_BeforeAnotherFunction(string[] r, int i)
        {
            bool javascript = false;
            bool poznamka = false;
            int bracket_count = 0;
            for (int y = i; y < r.Count(); y++)
            {
                if (r[y].Contains("<script")) javascript = true;
                if (r[y].Contains("</script")) javascript = false;

                if (!javascript)
                {
                    if (r[y].Contains("/*")) poznamka = true;
                    if (r[y].Contains("*/")) poznamka = false;

                    if (r[y].Contains("mysqli_") && !poznamka && !r[y].TrimStart(' ').StartsWith("//")) return true;

                    if (r[y].Contains("{")) bracket_count++;
                    if (r[y].Contains("}")) bracket_count--;

                    if ((r[y].Contains("global $beta;") || bracket_count <= 0) && y > i) break;
                }
            }
            return false;
        }

        /// <summary>
        /// Update všech souborů složky podle instrukcí (mysql-mysqli, atd...).
        /// </summary>
        /// <param name="dir"></param>
        private static void UpgradeFiles(string dir)
        {
            foreach (string file in Directory.GetFiles(dir, "*.php"))
            {
                Console.WriteLine(file);
                string up_cont = File.ReadAllText(file);

                //predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona
                if (file.Contains(@"\connect\connection.php") || file.Contains(@"\system\connection.php"))
                {
                    StreamReader sr = new StreamReader(file);
                    string connect_head = "";
                    bool poznamka = false;
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        connect_head += line + "\n";

                        if (line.Contains("/*")) poznamka = true;
                        if (line.Contains("*/"))
                        {
                            poznamka = false;
                            if (line.TrimStart(' ').StartsWith("$password_beta")) continue;
                        }
                        if (line.Contains("$password_beta") && !poznamka && !line.Contains("//$password_beta")) break;
                    }
                    sr.Close();
                    up_cont = connect_head + File.ReadAllText(base_folder + "important\\connection.txt");
                }
                //predelat soubor (TinyAjaxBehavior.php) v adresari admin/include >>> prekopirovat soubor ze vzoru rs mona
                if (file.Contains(@"\admin\include\TinyAjaxBehavior.php"))
                {
                    File.Copy(base_folder + "important\\TinyAjaxBehavior.txt", file, true);
                    continue;
                }

                //mysql_result >>> mysqli_num_rows + odmazat druhy parametr (vetsinou - , 0) + predelat COUNT(*) na *
                if (up_cont.Contains("mysql_result"))
                {
                    string[] r = up_cont.Split('\n');
                    up_cont = "";
                    for (int i = 0; i < r.Count(); i++)
                    {
                        if (r[i].Contains("mysql_result"))
                        {
                            r[i] = r[i].Replace("COUNT(*)", "*");
                            r[i] = r[i].Replace(", 0", "");
                            r[i] = r[i].Replace("mysql_result", "mysqli_num_rows");
                        }
                        up_cont += r[i] + "\n";
                    }
                }
                //upravit soubory system/clanek.php a system/vypis.php - pokud je sdileni fotogalerii pridat nad podminku $vypis_table_clanek["sdileni_fotogalerii"] kod $p_sf = array();
                if (up_cont.Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]") && !up_cont.Contains("$p_sf = array();"))
                {
                    string[] r = up_cont.Split('\n');
                    up_cont = "";
                    for (int i = 0; i < r.Count(); i++)
                    {
                        if (r[i].Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
                        {
                            up_cont += "        $p_sf = array();\n";
                        }
                        up_cont += r[i] + "\n";
                    }
                }

                //predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
                for (int i = 0; i < find_what.Count(); i++)
                {
                    up_cont = up_cont.Replace(find_what[i], replace_with[i]);
                }

                //po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db); predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
                if (up_cont.Contains("$this->db"))
                {
                    up_cont = up_cont.Replace("mysqli_query($beta, \"SET CHARACTER SET utf8\", $this->db);", "mysqli_query($this->db, \"SET CHARACTER SET utf8\");");
                    up_cont = up_cont.Replace("$beta", "$this->db");
                }

                //pridat mysqli_close($beta); do indexu nakonec
                if (file.Contains(web_name + @"\index.php") && !up_cont.Contains("mysqli_close"))
                    up_cont += "\n<?php mysqli_close($beta); ?>";

                //upravit soubor anketa/anketa.php - r.3 (odmazat ../) - include_once "../setup.php"; na include_once "setup.php";
                if (file.Contains(@"\anketa\anketa.php"))
                    up_cont = up_cont.Replace("include_once(\"../setup.php\")", "include_once(\"setup.php\")");

                //zakomentovat radky s funkci chdir v souboru admin/funkce/vytvoreni_adr.php
                if (file.Contains(@"\admin\funkce\vytvoreni_adr.php") && !up_cont.Contains("//chdir"))
                    up_cont = up_cont.Replace("chdir", "//chdir");

                //upravit soubor admin/table_x_add.php - potlacit chybova hlasku znakem „@“ na radku cca 47-55 - $pocet_text_all = mysqli_num_rows….
                //upravit soubor admin/table_x_edit.php - potlacit chybova hlasku znakem „@“ na radku cca 53-80 - $pocet_text_all = mysqli_num_rows….
                if ((file.Contains(@"\admin\table_x_add.php") || file.Contains(@"\admin\table_x_edit.php")) && !up_cont.Contains("@$pocet_text_all"))
                    up_cont = up_cont.Replace("$pocet_text_all = mysqli_num_rows", "@$pocet_text_all = mysqli_num_rows");

                //Upravit soubor funkce/strankovani.php >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
                if (file.Contains(@"\funkce\strankovani.php"))
                    up_cont = up_cont.Replace("function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)", "function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)");

                //Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
                if (file.Contains("xml_feeds_") && !file.Contains("xml_feeds_edit"))
                    up_cont = up_cont.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");

                //upravit soubor admin/sitemap_save.php cca radek 84 - pridat podminku „if($query_text_all !== FALSE)“ a obalit ji „while($data_stranky_text_all = mysqli_fetch_array($query_text_all))“
                if (file.Contains("admin\\sitemap_save.php") && up_cont.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))") && !up_cont.Contains("if($query_text_all !== FALSE)"))
                {
                    string[] r = up_cont.Split('\n');
                    up_cont = "";
                    bool sf_bracket = false;
                    for (int i = 0; i < r.Count(); i++)
                    {
                        if (r[i].Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"))
                        {
                            up_cont += "          if($query_text_all !== FALSE)\n          {\n";
                            sf_bracket = true;
                        }
                        if (r[i].Contains("}") && sf_bracket)
                        {
                            up_cont += "    " + r[i] + "\n";
                            sf_bracket = false;
                        }
                        up_cont += r[i] + "\n";
                    }
                }

                //pro všechny funkce které v sobe mají dotaz na db pridat na zacatek - global $beta; >>> hledat v netbeans - (?s)^(?=.*?function )(?=.*?mysqli_) - regular
                if (Regex.IsMatch(up_cont, "(?s)^(?=.*?function )(?=.*?mysqli_)") && !up_cont.Contains("$this"))
                {
                    string[] r = up_cont.Split('\n');
                    up_cont = "";
                    bool javascript = false;
                    for (int i = 0; i < r.Count(); i++)
                    {
                        if (r[i].Contains("<script")) javascript = true;
                        else if (r[i].Contains("</script")) javascript = false;

                        up_cont += r[i] + "\n";
                        if (r[i].Contains("function") && !javascript)
                        {
                            if (CheckForMysqli_BeforeAnotherFunction(r, i))
                            {
                                up_cont += r[++i] + "\n\n    global $beta;\n\n";
                                Console.WriteLine(" - global $beta; added");
                            }
                        }
                    }
                }

                File.WriteAllText(file, up_cont);

                //po dodelani nahrazeni nize projit na retezec - mysql_
                if (up_cont.ToLower().Contains("mysql_")) files_containing_mysql_.Add(file);
            }
        }

        /// <summary>
        /// Rekurzivní procházení podadresářů v adresáři.
        /// </summary>
        /// <param name="dir"></param>
        private static void GetFolders(string dir)
        {
            foreach (string subdir in Directory.GetDirectories(dir))
            {
                if (Directory.GetDirectories(subdir).Count() > 0 && !subdir.Contains("tiny_mce")) GetFolders(subdir);
                UpgradeFiles(subdir);
            }
        }

        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                web_name = args[0];
                string dir = base_folder + "weby\\" + web_name;
                if (Directory.Exists(dir))
                {
                    find_what = File.ReadAllLines(base_folder + @"\important\find_what.txt");
                    replace_with = File.ReadAllLines(base_folder + @"\important\replace_with.txt");

                    Console.WriteLine("\nProcessed files:\n");
                    GetFolders(dir);
                    UpgradeFiles(dir);

                    Console.WriteLine("\nAutomatic PHP upgrade of " + web_name + " is complete!");
                    Console.WriteLine("Files containing mysql_: " + files_containing_mysql_.Count().ToString());
                    foreach (string item in files_containing_mysql_) Console.WriteLine(item);
                }
                else Console.WriteLine("Folder " + dir + " does not exist.");
            }
            else Console.WriteLine("php_upgrader [WEB_FOLDER_NAME]\n\nweb folder name from " + base_folder + "weby\\.");
        }
    }
}
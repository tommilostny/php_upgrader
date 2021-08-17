﻿using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PhpUpgrader
{
    /// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
    public class RubiconUpgrader : MonaUpgrader
    {
        /// <summary>  </summary>
        public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
        {
            ConnectionFile = null;
        }

        /// <summary> Procedura aktualizace Rubicon souborů. </summary>
        /// <remarks> Použita ve volání metody <see cref="MonaUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
        /// <returns> Upravený soubor. </returns>
        protected override FileWrapper UpgradeProcedure(string filePath)
        {
            var file = base.UpgradeProcedure(filePath);

            if (file is not null)
            {
                UpgradeConstructors(file);
                UpgradeRubiconImport(file);
                UpgradeSetup(file);
                UpgradeHostnameBeta(file);
            }
            return file;
        }

        /// <summary> Old style constructor function ClassName() => function __construct() </summary>
        public void UpgradeConstructors(FileWrapper file)
        {
            var lines = file.Content.Split('\n');
            var newContent = string.Empty;

            for (int i = 0; i < lines.Length; i++) //procházení řádků souboru
            {
                newContent += $"{lines[i]}\n";

                if (!lines[i].Contains("class "))
                    continue;

                int nameStartIndex = lines[i].IndexOf("class ") + 6;

                int nameEndIndex = lines[i].IndexOf(' ', nameStartIndex);
                if (nameEndIndex == -1)
                    nameEndIndex = lines[i].IndexOf('{', nameStartIndex);

                var className = lines[i][nameStartIndex..(nameEndIndex != -1 ? nameEndIndex : lines[i].Length)].Trim();

                int bracketCount = Convert.ToInt32(nameEndIndex != -1);

                if (bracketCount == 0 && !lines[i + 1].Contains('{'))
                    continue;

                if (_LookAheadFor__construct(bracketCount, i + 1)) //třída obsahuje metodu __construct(), nehledat starý konstruktor
                    continue;

                while (++i < lines.Length) //hledání konstruktoru uvnitř třídy
                {
                    if (lines[i].Contains('{')) bracketCount++;
                    if (lines[i].Contains('}')) bracketCount--;

                    if (bracketCount == 0)
                    {
                        newContent += $"{lines[i]}\n";
                        break;
                    }
                    if (Regex.IsMatch(lines[i], $@"function {className}\s?\("))
                    {
                        int paramsStartIndex = lines[i].IndexOf('(', lines[i].IndexOf($"function {className}")) + 1;
                        int paramsEndIndex = lines[i].IndexOf(')', paramsStartIndex);

                        var parameters = lines[i][paramsStartIndex..paramsEndIndex];

                        lines[i] = lines[i].Replace($"function {className}", "function __construct");
                        lines[i] = $"    public function {className}({parameters})\n" +
                                    "    {\n" +
                                   $"        self::__construct({_ParamsWithoutDefaultValues(parameters)});\n" +
                                   $"    }}\n\n{lines[i]}";
                    }
                    newContent += $"{lines[i]}\n";
                }
            }
            file.Content = newContent;

            static string _ParamsWithoutDefaultValues(string parameters)
            {
                return string.Join(", ", parameters.Split(',').Select(p => p.Split('=').First().Trim()));
            }

            bool _LookAheadFor__construct(int bracketCount, int linesIndex)
            {
                for (; linesIndex < lines.Length; linesIndex++)
                {
                    if (lines[linesIndex].Contains('{')) bracketCount++;
                    if (lines[linesIndex].Contains('}')) bracketCount--;

                    if (bracketCount == 0)
                        break;

                    if (lines[linesIndex].Contains("function __construct"))
                        return true;
                }
                return false;
            }
        }

        /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php,  </summary>
        public void UpgradeRubiconImport(FileWrapper file)
        {
            if (!file.Path.Contains("rubicon_import.php"))
                return;

            var backup = (ConnectionFile, RenameBetaWith);
            (ConnectionFile, RenameBetaWith) = ("rubicon_import.php", "sportmall_import");
            
            UpgradeConnect(file);
            RenameBeta(file);
            file.Content = file.Content.Replace("mysqli_query($sportmall_import, \"SET CHARACTER SET utf8\");",
                "mysqli_query($sportmall_import, \"SET character_set_connection = cp1250\");\n" +
                "mysqli_query($sportmall_import, \"SET character_set_results = cp1250\");\n" +
                "mysqli_query($sportmall_import, \"SET character_set_client = cp1250\");");

            (ConnectionFile, RenameBetaWith) = backup;
        }

        /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
        public void UpgradeSetup(FileWrapper file)
        {
            if (!file.Path.Contains($"{WebName}\\setup.php"))
                return;

            bool usernameLoaded = false, passwordLoaded = false, databaseLoaded = false;

            var evaluator = new MatchEvaluator(_NewCredentialAndComment);
            file.Content = Regex.Replace(file.Content, @"\$setup_connect.*= ?"".*"";", evaluator);
            file.Content = file.Content.Replace("////", "//");

            if (!usernameLoaded)
                file.Warnings.Add("setup.php - username not loaded.");
            if (!passwordLoaded)
                file.Warnings.Add("setup.php - password not loaded.");
            if (!databaseLoaded)
                file.Warnings.Add("setup.php - database not loaded.");

            file.Warnings.Add("setup.php - check db connections and such.");

            string _NewCredentialAndComment(Match match)
            {
                if (file.Content[..match.Index].EndsWith("//"))
                    return match.Value;

                var varName = match.Value.Split('=').First().Trim();
                var credential = varName switch
                {
                    var vn when vn.Contains("username") && (usernameLoaded = true) => Username,
                    var vn when vn.Contains("password") && (passwordLoaded = true) => Password,
                    _ => Database
                };
                if (!usernameLoaded && !passwordLoaded)
                    databaseLoaded = true;

                return $"//{match.Value}\n{varName} = \"{credential}\";";
            }
        }

        /// <summary> Aktualizace hostname v souboru Connections/beta.php na server mcrai2. </summary>
        public void UpgradeHostnameBeta(FileWrapper file)
        {
            if (file.Path.Contains("Connections\\beta.php"))
            {
                file.Content = file.Content.Replace("$hostname_beta = \"93.185.102.228\";", $"//$hostname_beta = \"93.185.102.228\";\n\t$hostname_beta = \"{Hostname}\";");
            }
        }
    }
}

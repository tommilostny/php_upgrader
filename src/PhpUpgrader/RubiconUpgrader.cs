using System;
using System.Linq;

namespace PhpUpgrader
{
    /// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
    public class RubiconUpgrader : MonaUpgrader
    {
        /// <summary>  </summary>
        public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
        {
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

                int nameEndIndex = lines[i].IndexOf('{', nameStartIndex + 1);
                if (nameEndIndex == -1)
                    nameEndIndex = lines[i].IndexOf(' ', nameStartIndex + 1);

                var className = lines[i][nameStartIndex..(nameEndIndex != -1 ? nameEndIndex : lines[i].Length)].Trim();

                int bracketCount = Convert.ToInt32(nameEndIndex != -1);

                int lookAheadIndex = _LookAheadFor__construct(bracketCount, i);
                if (lookAheadIndex != -1) //třída obsahuje metodu __construct(), přeskočit ji a hledat dál od indexu
                {
                    i = lookAheadIndex;
                    continue;
                }
                while (++i < lines.Length) //hledání konstruktoru uvnitř třídy
                {
                    if (lines[i].Contains('{')) bracketCount++;
                    if (lines[i].Contains('}')) bracketCount--;

                    if (bracketCount == 0)
                    {
                        newContent += $"{lines[i]}\n";
                        break;
                    }
                    if (lines[i].Contains($"function {className}"))
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

            int _LookAheadFor__construct(int bracketCount, int linesIndex)
            {
                bool foundConstruct = false;
                for (; linesIndex < lines.Length; linesIndex++)
                {
                    if (lines[linesIndex].Contains('{')) bracketCount++;
                    if (lines[linesIndex].Contains('}')) bracketCount--;

                    if (bracketCount == 0)
                        break;

                    if (lines[linesIndex].Contains("function __construct"))
                        foundConstruct = true;
                }
                return foundConstruct ? linesIndex : -1;
            }
        }

        /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php,  </summary>
        public void UpgradeRubiconImport(FileWrapper file)
        {
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
    }
}

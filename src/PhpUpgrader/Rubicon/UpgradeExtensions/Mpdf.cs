﻿namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class Mpdf
{
    private static readonly string _functionsPath = Path.Join("mpdf", "includes", "functions.php");
    private static readonly string _functions57Path = Path.Join("MPDF57", "includes", "functions.php");
    private static readonly string _mpdfPath = Path.Join("mpdf", "mpdf.php");
    private static readonly string _mpdf57Path = Path.Join("MPDF57", "mpdf.php");
    private static readonly string _mpdfSourcePath = Path.Join("mpdf", "mpdf_source.php");

    public static bool UpgradeMpdf(this RubiconUpgrader upgrader, string filePath)
    {
        if (filePath.EndsWith(_mpdfSourcePath, StringComparison.Ordinal)
            || filePath.EndsWith(_mpdfPath, StringComparison.Ordinal)
            || filePath.EndsWith(_mpdf57Path, StringComparison.OrdinalIgnoreCase))
        {
            BackupManager.CreateBackupFile(filePath, upgrader.BaseFolder, upgrader.WebName, modified: true);

            var newMpdfPath = Path.Join(upgrader.BaseFolder, "important", "mpdf.php");
            if (!File.Exists(newMpdfPath))
            {
                newMpdfPath = Path.Join(upgrader.BaseFolder, "important", "mpdf.txt");
            }
            File.WriteAllText(filePath, File.ReadAllText(newMpdfPath));

            var file = new FileWrapper(filePath, content: null);
            upgrader.ModifiedFiles.Add(file.Path);
            file.PrintStatus(modified: true);
            return true;
        }
        return false;
    }

    public static FileWrapper UpgradeMpdfFunctions(this FileWrapper file)
    {
        if (file.Path.EndsWith(_functionsPath, StringComparison.Ordinal)
            || file.Path.EndsWith(_functions57Path, StringComparison.Ordinal))
        {
            file.Content.Replace("$str = preg_replace('/\\&\\#([0-9]+)\\;/me', \"code2utf('\\\\1',{$lo})\",$str);",
                                 "$str = preg_replace_callback(\n\t\t'/\\&\\#([0-9]+)\\;/m',\n\t\tfunction($matches) use ($lo) {\n\t\t\treturn code2utf($matches[1], $lo);\n\t\t},\n\t\t$str\n\t);")
                        .Replace("$str = preg_replace('/\\&\\#x([0-9a-fA-F]+)\\;/me', \"codeHex2utf('\\\\1',{$lo})\",$str);",
                                 "$str = preg_replace_callback(\n\t\t'/\\&\\#x([0-9a-fA-F]+)\\;/m',\n\t\tfunction($matches) use ($lo) {\n\t\t\treturn codeHex2utf($matches[1], $lo);\n\t\t},\n\t\t$str\n\t);");
            if (!file.Content.Contains("urldecode_parts"))
            {
                var endTagIndex = file.Content.LastIndexOf("?>");
                file.Content.Insert(endTagIndex,
                    "// mPDF 5.7.4 URLs\nif (!function_exists('urldecode_parts')) {\n  function urldecode_parts($url) {\n\t$file=$url;\n\t$query='';\n\tif (preg_match('/[?]/',$url)) {\n\t\t$bits = preg_split('/[?]/',$url,2);\n\t\t$file=$bits[0];\n\t\t$query='?'.$bits[1];\n\t}\n\t$file = rawurldecode($file);\n\t$query = urldecode($query);\n\treturn $file.$query;\n  }\n}\n");
            }
        }
        return file;
    }
}

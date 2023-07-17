namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class Mpdf
{
    public static FileWrapper UpgradeMpdf(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("mpdf", "mpdf.php"), StringComparison.Ordinal)
            || file.Path.EndsWith(Path.Join("mpdf", "mpdf_source.php"), StringComparison.Ordinal))
        {
            UpgradeMpdfSwitchWith2Defaults(file);
        }
        else if (file.Path.EndsWith(Path.Join("mpdf", "includes", "functions.php"), StringComparison.Ordinal))
        {
            file.Content.Replace("$str = preg_replace('/\\&\\#([0-9]+)\\;/me', \"code2utf('\\\\1',{$lo})\",$str);",
                                 "$str = preg_replace_callback(\n\t\t'/\\&\\#([0-9]+)\\;/m',\n\t\tfunction($matches) use ($lo) {\n\t\t\treturn code2utf($matches[1], $lo);\n\t\t},\n\t\t$str\n\t);")
                        .Replace("$str = preg_replace('/\\&\\#x([0-9a-fA-F]+)\\;/me', \"codeHex2utf('\\\\1',{$lo})\",$str);",
                                 "$str = preg_replace_callback(\n\t\t'/\\&\\#x([0-9a-fA-F]+)\\;/m',\n\t\tfunction($matches) use ($lo) {\n\t\t\treturn codeHex2utf($matches[1], $lo);\n\t\t},\n\t\t$str\n\t);");
        }
        return file;
    }

    private static void UpgradeMpdfSwitchWith2Defaults(FileWrapper file)
    {
        var lines = file.Content.Split();
        var formatFuncFound = false;
        foreach (var line in lines)
        {
            if (line.Contains("function _getPageFormat($format)"))
            {
                formatFuncFound = true;
            }
            if (formatFuncFound && line.Contains("default: $format = false;"))
            {
                line.Replace("default: $format = false;", "//default: $format = false;");
                break;
            }
        }
        lines.JoinInto(file.Content);
    }
}

namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class FtpPut
{
    private static readonly string _createXMLPHP = Path.Join("money", "createXML.php");
    private static readonly string _importToPohodaPHP = Path.Join("rss", "import_to_pohoda.php");

    public static FileWrapper UpgradeFtpPut(this FileWrapper file)
    {
        if (file.Path.EndsWith(_createXMLPHP, StringComparison.Ordinal))
        {
            AddPassiveMode("ftp_spojeni");
        }
        else if (file.Path.EndsWith(_importToPohodaPHP, StringComparison.Ordinal))
        {
            AddPassiveMode("spojeni");
        }
        return file;

        void AddPassiveMode(string connVarName)
        {
            var i = file.Content.IndexOf($"if(!ftp_put(${connVarName},");
            string? ftpPasv = null;
            while (i != -1)
            {
                using var spaces = ZString.CreateStringBuilder();
                for (var j = i - 1; j >= 0; j--)
                {
                    var current = file.Content[j];
                    if (current is ' ' or '\t')
                    {
                        spaces.Append(current);
                        continue;
                    }
                    break;
                }
                ftpPasv ??= $"ftp_pasv(${connVarName}, true);{Environment.NewLine}{spaces}";
                file.Content.Insert(i, ftpPasv);
                i = file.Content.IndexOf($"if(!ftp_put(${connVarName},", i + ftpPasv.Length + 1);
            }
        }
    }
}

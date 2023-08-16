namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class MoneyCreateXmlFtp
{
    private static readonly string _createXMLPHP = Path.Join("money", "createXML.php");

    public static FileWrapper UpgradeMoneyCreateXmlFtp(this FileWrapper file)
    {
        if (file.Path.EndsWith(_createXMLPHP, StringComparison.Ordinal))
        {
            var i = file.Content.IndexOf("if(!ftp_put($ftp_spojeni,");
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
                ftpPasv ??= $"ftp_pasv($ftp_spojeni, true);{Environment.NewLine}{spaces}";
                file.Content.Insert(i, ftpPasv);
                i = file.Content.IndexOf("if(!ftp_put($ftp_spojeni,", i + ftpPasv.Length + 1);
            }
        }
        return file;
    }
}

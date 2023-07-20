namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class PclZipLib
{
    private static readonly string _pclZipLibPhp = Path.Join("piwika", "libs", "PclZip", "pclzip.lib.php");

    public static FileWrapper UpgradePclZipLib(this FileWrapper file)
    {
        if (file.Path.EndsWith(_pclZipLibPhp, StringComparison.Ordinal))
        {
            file.Content.Replace("$v_filedescr_list[]", "$v_filedescr_list");
        }
        return file;
    }
}

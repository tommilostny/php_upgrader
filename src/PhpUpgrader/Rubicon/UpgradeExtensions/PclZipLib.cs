namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class PclZipLib
{
    public static FileWrapper UpgradePclZipLib(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("piwika", "libs", "PclZip", "pclzip.lib.php"), StringComparison.Ordinal))
        {
            file.Content.Replace("$v_filedescr_list[]", "$v_filedescr_list");
        }
        return file;
    }
}

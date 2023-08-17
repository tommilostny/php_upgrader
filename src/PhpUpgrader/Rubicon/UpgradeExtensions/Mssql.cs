namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class Mssql
{
    /// <summary>
    /// PhpStorm:
    /// 'mssql_query' was removed in 7.0 PHP version.<br />
    /// (Zatím neřešit, nepoužívá se (pouze nějaký ruční import)).
    /// </summary>
    /// <remarks>
    /// 1. Obsahuje mssql funkce?<br />
    /// 2. Nahoru doplnit require_once("mssql_overwrite.php"); se správnou cestou k souboru,
    ///    který bych dal do root složky (aka doplnit příslušný počet "../").<br />
    /// 3. Zkopírovat soubor mssql_overwrite.php ze složky important.<br />
    /// 4. Profit.
    /// </remarks>
    public static FileWrapper UpgradeMssql(this FileWrapper file)
    {
        if (file.Content.Contains("$ms_hostname_beta ="))
        {
            file.Content.Replace("$ms_hostname_beta =", "return;\t$ms_hostname_beta =");
        }
        return file;
    }
}

//TODO: mssql_pconnect rewite to mssql_connect

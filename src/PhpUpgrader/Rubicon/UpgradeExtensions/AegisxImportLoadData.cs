namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class AegisxImportLoadData
{
    private static readonly string _importLoadDataPhp = Path.Join("aegisx", "import", "load_data.php");

    /// <summary> Úprava mysql a proměnné $beta v souboru aegisx\import\load_data.php. </summary>
    public static FileWrapper UpgradeLoadData(this FileWrapper file)
    {
        if (file.Path.EndsWith(_importLoadDataPhp, StringComparison.Ordinal))
        {
            file.Content.Replace("global $beta;", "global $sportmall_import;")
                        .Replace("mysqli_real_escape_string($beta,", "mysqli_real_escape_string($sportmall_import,");
        }
        return file;
    }
}

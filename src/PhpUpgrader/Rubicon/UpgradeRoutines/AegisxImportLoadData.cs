namespace PhpUpgrader.Rubicon.UpgradeRoutines;

public static class AegisxImportLoadData
{
    /// <summary> Úprava mysql a proměnné $beta v souboru aegisx\import\load_data.php. </summary>
    public static FileWrapper UpgradeLoadData(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("aegisx", "import", "load_data.php")))
        {
            file.Content.Replace("global $beta;", "global $sportmall_import;")
                        .Replace("mysqli_real_escape_string($beta,", "mysqli_real_escape_string($sportmall_import,");
        }
        return file;
    }
}

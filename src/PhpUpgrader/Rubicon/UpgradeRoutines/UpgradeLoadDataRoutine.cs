namespace PhpUpgrader.Rubicon.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeLoadDataRoutine
{
    /// <summary> Úprava mysql a proměnné $beta v souboru aegisx\import\load_data.php. </summary>
    public static void UpgradeLoadData(this FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("aegisx", "import", "load_data.php")))
        {
            return;
        }
        file.Content.Replace("global $beta;", "global $sportmall_import;");
        file.Content.Replace("mysqli_real_escape_string($beta,", "mysqli_real_escape_string($sportmall_import,");
    }
}

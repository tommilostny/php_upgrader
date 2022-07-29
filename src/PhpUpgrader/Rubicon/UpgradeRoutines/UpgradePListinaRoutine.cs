namespace PhpUpgrader.Rubicon.UpgradeRoutines;

public static class UpgradePListinaRoutine
{
    public static FileWrapper UpgradePListina(this FileWrapper file)
    {
        if (Regex.IsMatch(file.Path, @"pdf(\\|/)p_listina(_u)?\.php$", RegexOptions.Compiled))
        {
            file.Content.Replace("mysqli_query($beta, ", "mysqli_query($beta_hod, ");
            file.Content.Replace("mysql_select_db($dtb_hod, $beta_hod);", "mysqli_select_db($beta_hod, $dtb_hod);");
        }
        return file;
    }
}

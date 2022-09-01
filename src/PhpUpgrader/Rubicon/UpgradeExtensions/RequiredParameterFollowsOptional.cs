namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class RequiredParameterFollowsOptional
{
    /// <summary>
    /// PHPStan: Deprecated in PHP 8.0: Required parameter ${...} follows optional parameter $domain.
    /// </summary>
    public static FileWrapper UpgradeRequiredParameterFollowsOptional(this FileWrapper file)
    {
        foreach (var (filename, find, replace) in UpgradableFiles())
        {
            if (file.Path.EndsWith(filename, StringComparison.Ordinal))
            {
                file.Content.Replace(find, replace);
            }
        }
        return file;
    }

    //TODO: zobecnit, moc případů
    private static IEnumerable<(string filename, string find, string replace)> UpgradableFiles()
    {
        yield return (filename: Path.Join("classes", "McBalikovna.class.php"),
                      find:     "public function __construct($domain = 1, $hostname, $username, $password, $database, $connport)",
                      replace:  "public function __construct($domain, $hostname, $username, $password, $database, $connport)");

        yield return (filename: Path.Join("DataTable", "Filter", "AddColumnsProcessedMetricsGoal.php"),
                      find:     "public function __construct($table, $enable = true, $processOnlyIdGoal)",
                      replace:  "public function __construct($table, $enable, $processOnlyIdGoal)");
    }
}

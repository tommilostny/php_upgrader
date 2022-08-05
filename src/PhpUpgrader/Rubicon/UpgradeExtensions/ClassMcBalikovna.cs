namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class ClassMcBalikovna
{
    /// <summary>
    /// PHPStan: Deprecated in PHP 8.0: Required parameter ${...} follows optional parameter $domain.
    /// </summary>
    public static FileWrapper UpgradeClassMcBalikovna(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("classes", "McBalikovna.class.php"), StringComparison.Ordinal))
        {
            file.Content.Replace("public function __construct($domain = 1, $hostname, $username, $password, $database, $connport)",
                                 "public function __construct($domain, $hostname, $username, $password, $database, $connport)");
        }
        return file;
    }
}

namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class McPayment
{
    public static FileWrapper UpgradeMcPayment(this FileWrapper file)
    {
        if (file.Path.Contains("McPayment", StringComparison.OrdinalIgnoreCase))
        {
            file.Content.Replace("public function __construct($domain = 1, $hostname, $username, $password, $database, $connport)",
                                 "public function __construct($domain = 1, $hostname = null, $username = null, $password = null, $database = null, $connport = null)");
        }
        return file;
    }
}

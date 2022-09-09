namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class AdminerUglyCode
{
    /// <summary>
    /// Minifikovaný špatně formátovaný kód v souboru 'adminer.php' rozhodí ostatní aktualizační rutiny.
    /// </summary>
    /// <remarks>
    /// Vyžaduje nainstalované PHP 7.4 a composer.
    /// Používá nástroj https://github.com/FriendsOfPHP/PHP-CS-Fixer.
    /// </remarks>
    public static void UpgradeAdminerUglyCode(this RubiconUpgrader upgrader, string filePath)
    {
        if (filePath.EndsWith("adminer.php", StringComparison.Ordinal))
        {
            BackupManager.CreateBackupFile(filePath, upgrader.BaseFolder, upgrader.WebName, modified: true);
            try
            {
                PhpCsFixerWrapper.FormatPhp(filePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Při spouštění php-cs-fixer došlo k chybě. Je správně nainstalovaný a spustitelný composer?");
                Console.Error.WriteLine("Chybová zpráva:");
                Console.Error.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }

    public static void UpgradeAdminerUglyCode(this RubiconUpgrader upgrader, string filePath, string content)
    {
        File.WriteAllText(filePath, content);
        UpgradeAdminerUglyCode(upgrader, filePath, content);
    }
}

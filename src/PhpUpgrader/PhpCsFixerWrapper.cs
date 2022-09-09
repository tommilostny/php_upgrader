using System.Diagnostics;

namespace PhpUpgrader;

/// <summary>
/// <seealso cref="Rubicon.UpgradeExtensions.AdminerUglyCode"/>
/// </summary>
internal static class PhpCsFixerWrapper
{
    private static bool _installed = false;

    public static void FormatPhp(string filePath, string rules = "@PSR2")
    {
        if (!_installed)
        {
            RunComposer();
        }
        var phpCsFixer = new ProcessStartInfo
        {
            FileName = "php-cs-fixer",
            Arguments = $"--verbose fix {filePath} --rules={rules}",
            UseShellExecute = true,
        };
        using var phpCsFixerProcess = Process.Start(phpCsFixer);
        phpCsFixerProcess.WaitForExit();
    }

    private static void RunComposer()
    {
        var composer = new ProcessStartInfo
        {
            FileName = "composer",
            Arguments = "global require friendsofphp/php-cs-fixer",
            UseShellExecute = true,
        };
        using var composerProcess = Process.Start(composer);
        composerProcess.WaitForExit();
        _installed = true;
    }
}

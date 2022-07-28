namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeFindReplaceRoutine
{
    /// <summary>
    /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
    /// </summary>
    public static void UpgradeFindReplace(this FileWrapper file, MonaUpgrader upgrader)
    {
        foreach (var fr in upgrader.FindReplace)
        {
            file.Content.Replace(fr.Key, fr.Value);
        }
    }
}

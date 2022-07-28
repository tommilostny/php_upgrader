namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeCloseIndexRoutine
{
    /// <summary> Přidá "{closeFunction}($beta);" na konec soubor index.php. </summary>
    /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
    public static void UpgradeCloseIndex(this FileWrapper file, MonaUpgrader upgrader)
    {
        if (!IsRootIndexFile(file.Path, upgrader))
        {
            return;
        }
        var closeFunction = upgrader is RubiconUpgrader ? "pg_close" : "mysqli_close";

        if (!file.Content.Contains(closeFunction))
        {
            file.Content.AppendLine();
            file.Content.Append($"<?php {closeFunction}($beta); ?>");
        }
    }

    private static bool IsRootIndexFile(string path, MonaUpgrader upgrader)
    {
        const string indexFile = "index.php";
        return path.EndsWith(Path.Join(upgrader.WebName, indexFile))
            || upgrader.OtherRootFolders?.Any(rf => path.EndsWith(Path.Join(upgrader.WebName, rf, indexFile))) == true;
    }
}

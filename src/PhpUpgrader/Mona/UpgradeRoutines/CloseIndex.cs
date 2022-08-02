namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class CloseIndex
{
    /// <summary> Přidá mysqli_close nebo pg_close na konec soubor index.php. </summary>
    public static FileWrapper UpgradeCloseIndex(this FileWrapper file, MonaUpgrader upgrader)
    {
        if (!IsRootIndexFile(file.Path, upgrader))
        {
            return file;
        }
        var closeFunction = upgrader is RubiconUpgrader ? "pg_close" : "mysqli_close";

        if (!file.Content.Contains(closeFunction))
        {
            file.Content.AppendLine();
            file.Content.Append($"<?php {closeFunction}($beta); ?>");
        }
        return file;
    }

    private static bool IsRootIndexFile(string path, MonaUpgrader upgrader)
    {
        const string indexFile = "index.php";
        return path.EndsWith(Path.Join(upgrader.WebName, indexFile))
            || upgrader.OtherRootFolders?.Any(rf => path.EndsWith(Path.Join(upgrader.WebName, rf, indexFile))) == true;
    }
}

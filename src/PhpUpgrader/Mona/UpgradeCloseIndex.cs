namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary> pridat mysqli_close($beta); do indexu nakonec </summary>
    public virtual void UpgradeCloseIndex(FileWrapper file)
    {
        UpgradeCloseIndex(file, "mysqli_close");
    }

    /// <summary> Přidá "{closeFunction}($beta);" na konec soubor index.php. </summary>
    protected void UpgradeCloseIndex(FileWrapper file, string closeFunction)
    {
        if (_IsInRootFolder(file.Path) && !file.Content.Contains(closeFunction))
        {
            file.Content.AppendLine();
            file.Content.Append($"<?php {closeFunction}($beta); ?>");
        }

        bool _IsInRootFolder(string path)
        {
            const string indexFile = "index.php";
            return path.EndsWith(Path.Join(WebName, indexFile))
                || OtherRootFolders?.Any(rf => path.EndsWith(Path.Join(WebName, rf, indexFile))) == true;
        }
    }
}

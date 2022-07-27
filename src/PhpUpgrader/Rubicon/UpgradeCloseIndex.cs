namespace PhpUpgrader.Rubicon;

public partial class RubiconUpgrader
{
    /// <summary> Přidá funkci pg_close na konec index.php. </summary>
    public override void UpgradeCloseIndex(FileWrapper file)
    {
        UpgradeCloseIndex(file, "pg_close");
    }
}

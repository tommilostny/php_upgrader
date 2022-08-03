using PhpUpgrader.Rubicon.UpgradeExtensions;
using PhpUpgrader.Rubicon.UpgradeHandlers;

namespace PhpUpgrader.Rubicon;

/// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
public sealed class RubiconUpgrader : MonaUpgrader
{
    /// <summary> Konstruktor Rubicon > Mona upgraderu. </summary>
    public RubiconUpgrader(string baseFolder, string webName)
        : base(baseFolder, webName,
               new RubiconFindReplaceHandler(),
               new RubiconConnectHandler())
    {
    }

    /// <summary> Procedura aktualizace Rubicon souborů. </summary>
    /// <remarks> Použita ve volání metody <see cref="PhpUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
    /// <returns> Upravený soubor. </returns>
    protected override FileWrapper? UpgradeProcedure(string filePath)
    {
        return base.UpgradeProcedure(filePath) switch
        {
            //MonaUpgrader končí s null, také hned skončit.
            null => null,
            //jinak máme soubor k aktualizaci dalšími metodami specifickými pro Rubicon.
            var file => file.UpgradeObjectClass(this)
                            .UpgradeConstructors()
                            .UpgradeScriptLanguagePhp()
                            .UpgradeIncludesInHtmlComments()
                            .UpgradeAegisxDetail()
                            .UpgradeLoadData()
                            .UpgradeHomeTopProducts()
                            .UpgradeUrlPromenne()
                            .UpgradeDuplicateArrayKeys()
                            .UpgradeOldUnparsableAlmostEmptyFile()
                            .UpgradeHodnoceniDBCalls()
                            .UpgradeLibDbMysql()
                            .UpgradeArrayMissingKeyValue()
        };
    }
}

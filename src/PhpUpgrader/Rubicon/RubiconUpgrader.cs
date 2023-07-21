using PhpUpgrader.Rubicon.UpgradeExtensions;
using PhpUpgrader.Rubicon.UpgradeHandlers;

namespace PhpUpgrader.Rubicon;

/// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
public sealed class RubiconUpgrader : MonaUpgrader
{
    private readonly ObjectClassHandler _objectClassHandler;

    /// <summary> Konstruktor Rubicon > Mona upgraderu. </summary>
    public RubiconUpgrader(string baseFolder, string webName)
        : base(baseFolder, webName,
               new RubiconFindReplaceHandler(),
               new RubiconConnectHandler())
    {
        _objectClassHandler = new(this);
    }

    /// <summary> Procedura aktualizace Rubicon souborů. </summary>
    /// <remarks> Použita ve volání metody <see cref="PhpUpgraderBase.UpgradeAllFilesRecursively"/>. </remarks>
    /// <returns> Upravený soubor. </returns>
    protected override FileWrapper? UpgradeProcedure(string filePath)
    {
        if (this.UpgradeMpdf(filePath))
            return null;

        this.UpgradeAdminerUglyCode(filePath);

        switch (base.UpgradeProcedure(filePath))
        {
            //MonaUpgrader končí s null, také hned skončit.
            case null: return null;

            //jinak máme soubor k aktualizaci dalšími metodami specifickými pro Rubicon.
            case var file:
                _objectClassHandler.UpgradeObjectClass(file);
                file.UpgradeConstructors()
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
                    .UpgradePiwikaLibsPearRaiseError()
                    .UpgradeRequiredParameterFollowsOptional()
                    .UpgradeNullByteInRegex()
                    .UpgradePclZipLib()
                    .UpgradeAdminerMysql()
                    .UpgradeNajdiVDb()
                    .UpgradeUnparenthesizedPlus()
                    .UpgradeMssql()
                    .UpgradeMpdfFunctions()
                    .UpgradeMissingCurlyBracket()
                    .UpgradeUnexpectedEOF();
                return file;
        }
    }
}

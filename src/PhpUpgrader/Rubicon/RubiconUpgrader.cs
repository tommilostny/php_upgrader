using PhpUpgrader.Rubicon.UpgradeExtensions;
using PhpUpgrader.Rubicon.UpgradeHandlers;

namespace PhpUpgrader.Rubicon;

/// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
public sealed class RubiconUpgrader : MonaUpgrader
{
    public string? OutsideRubiconFolder { get; set; }

    public bool HasRubiconOutside => OutsideRubiconFolder is not null;

    public string? OutsideWebUsersFolder { get; set; }

    public bool HasWebUsersOutside => OutsideWebUsersFolder is not null;

    public string? DevDatabase { get; set; }

    public string? DevUsername { get; set; }

    public string? DevPassword { get; set; }

    private readonly ObjectClassHandler _objectClassHandler;

    /// <summary> Konstruktor Rubicon > Mona upgraderu. </summary>
    public RubiconUpgrader(string baseFolder, string webName)
        : base(baseFolder, webName,
               new RubiconFindReplaceHandler(),
               new RubiconConnectHandler())
    {
        OutsideRubiconFolder = Path.Join(BaseFolder, "weby", $"{WebName}-rubicon");
        if (!Directory.Exists(OutsideRubiconFolder))
        {
            OutsideRubiconFolder = null;
        }
        OutsideWebUsersFolder = Path.Join(BaseFolder, "weby", $"{WebName}-web_users");
        if (!Directory.Exists(OutsideWebUsersFolder))
        {
            OutsideWebUsersFolder = null;
        }
        _objectClassHandler = new(this);
    }

    public override void RunUpgrade(string directoryPath)
    {
        UpgradeAllFilesRecursively(directoryPath);
        string oldWebName = WebName;
        if (HasRubiconOutside)
        {
            WebName = $"{WebName}-rubicon";
            UpgradeAllFilesRecursively(OutsideRubiconFolder);
        }
        if (HasWebUsersOutside)
        {
            WebName = $"{oldWebName}-web_users";
            UpgradeAllFilesRecursively(OutsideWebUsersFolder);
        }
        WebName = oldWebName;
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
                    .UpgradeUnexpectedEOF()
                    .UpgradeSetyFormImplodes()
                    .UpgradeSetupIncludes()
                    .UpgradePHPMailer(this)
                    .UpgradeMktime()
                    .UpgradeMoneyCreateXmlFtp()
                    .UpgradeCountableWarning()
                    .UpgradeVipClub();
                return file;
        }
    }
}

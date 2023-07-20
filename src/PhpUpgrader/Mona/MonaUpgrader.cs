using PhpUpgrader.Mona.UpgradeExtensions;
using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Mona;

/// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
public partial class MonaUpgrader : PhpUpgraderBase
{
    /// <summary> Složky obsahující administraci RS Mona (null => 1 složka 'admin') </summary>
    public string[] AdminFolders
    {
        get => _adminFolders ??= new[] { "admin" };
        set => _adminFolders = value ?? new[] { "admin" };
    }
    private string[] _adminFolders;

    /// <summary> Root složky obsahující index.php, do kterého vložit mysqli_close na konec. </summary>
    public string[]? OtherRootFolders { get; set; }

    /// <summary> Přejmenovat proměnnou $beta tímto názvem (null => nepřejmenovávat). </summary>
    public string? RenameBetaWith
    {
        get => _replaceBetaWith;
        set
        {
            if ((_replaceBetaWith = value) is not null)
            {
                this.RenameVarInFindReplace("beta", value);
            }
        }
    }
    private string? _replaceBetaWith;

    public MonaUpgrader(string baseFolder, string webName)
        : this(baseFolder, webName,
               new MonaFindReplaceHandler(),
               new MonaConnectHandler())
    {
    }

    protected MonaUpgrader(string baseFolder, string webName, IFindReplaceHandler findReplaceHandler, IConnectHandler connectHandler)
        : base(baseFolder, webName, findReplaceHandler, connectHandler)
    {
    }

    protected override FileWrapper? UpgradeProcedure(string filePath)
    {
        FileWrapper? file = this.UpgradeTinyAjaxBehavior(filePath) ? null : new(filePath);
        switch (file)
        {
            case null or { Content.Length: 0 }:
                return null;

            //pro tiny_mce pouze find=>replace a speciální případy.
            case { Path: var p } when p.Contains("tiny_mce", StringComparison.Ordinal):
                FindReplaceHandler.UpgradeFindReplace(file);
                file.UpgradeTinyMceUploaded();
                break;

            default:
                ConnectHandler.UpgradeConnect(file, this);
                FindReplaceHandler.UpgradeFindReplace(file);
                file.UpgradeResultFunction(this)
                    .UpgradeMysqliQueries(this)
                    .UpgradeCloseIndex(this)
                    .UpgradeAnketa()
                    .UpgradeClanekVypis()
                    .UpgradeChdir(AdminFolders)
                    .UpgradeTableXAddEdit(AdminFolders)
                    .UpgradeStrankovani()
                    .UpgradeXmlFeeds()
                    .UpgradeSitemapSave(AdminFolders)
                    .UpgradeGlobalBeta()
                    .RenameBeta(this)
                    .UpgradeFloatExplodeConversions();
                break;
        }
        file.UpgradeUnlink()
            .UpgradeRegexFunctions()
            .UpgradeIfEmpty()
            .UpgradeGetMagicQuotesGpc()
            .UpgradeWhileListEach(WebFolder)
            .UpgradeCreateFunction()
            .UpgradeImplode()
            .UpgradeCurlyBraceIndexing();

        //Zahlásit IP adresu serveru mcrai1, pokud není zakomentovaná.
        if (file.Content.Contains("93.185.102.228")
            && !CommentedMcrai1IPRegex().IsMatch(file.Content.ToString()))
        {
            file.Warnings.Add("Soubor obsahuje IP adresu mcrai1 (93.185.102.228).");
        }
        return file;
    }

    protected override void AfterUpgradeProcedure(FileWrapper file)
    {
        if (file.IsModified)
        {
            file.RemoveTrailingWhitespaces();
        }
    }

    [GeneratedRegex(@"//.*93\.185\.102\.228", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex CommentedMcrai1IPRegex();
}

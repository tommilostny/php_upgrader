using PhpUpgrader.Mona.UpgradeExtensions;
using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Mona;

/// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
public class MonaUpgrader : PhpUpgraderBase
{
    public MonaUpgrader(string baseFolder, string webName)
        : this(baseFolder, webName,
               new MonaFindReplaceHandler(),
               new MonaConnectHandler())
    {
    }

    protected MonaUpgrader(string baseFolder, string webName, FindReplaceHandler findReplaceHandler, ConnectHandler connectHandler)
        : base(baseFolder, webName, findReplaceHandler, connectHandler)
    {
    }

    public override string? RenameBetaWith
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

    protected override FileWrapper? UpgradeProcedure(string filePath)
    {
#if DEBUG
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("   ");
        Console.WriteLine(filePath);
        Console.ResetColor();
#endif
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
            .RemoveTrailingWhitespaces()
            .UpgradeIfEmpty()
            .UpgradeGetMagicQuotesGpc();

        //Zahlásit IP adresu serveru mcrai1, pokud není zakomentovaná.
        if (file.Content.Contains("93.185.102.228")
            && !Regex.IsMatch(file.Content.ToString(), @"//.*93\.185\.102\.228", RegexOptions.None, TimeSpan.FromSeconds(5)))
        {
            file.Warnings.Add("Soubor obsahuje IP adresu mcrai1 (93.185.102.228).");
        }
        return file;
    }
}

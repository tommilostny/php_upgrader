using PhpUpgrader.Mona.UpgradeExtensions;
using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Mona;

/// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
public class MonaUpgrader
{
    /// <summary> Seznam souborů, které se nepodařilo aktualizovat a stále obsahují mysql_ funkce. </summary>
    public List<UnmodifiedMysql_File> FilesContainingMysql { get; } = new();

    /// <summary> Handler zajišťující část aktualizace najít >> nahradit. </summary>
    public MonaFindReplaceHandler FindReplaceHandler { get; protected set; } = new();

    /// <summary> Handler zajišťující část aktualizace připojení k databázi. </summary>
    public MonaConnectHandler ConnectHandler { get; protected set; } = new();

    /// <summary> Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'. </summary>
    public string BaseFolder { get; }

    /// <summary> Název webu ve složce 'weby'. </summary>
    public string WebName { get; }

    /// <summary> Složky obsahující administraci RS Mona (null => 1 složka 'admin') </summary>
    public string[] AdminFolders
    {
        get => _adminFolders ??= new string[] { "admin" };
        set => _adminFolders = value ?? new string[] { "admin" };
    }
    private string[] _adminFolders;

    /// <summary> Root složky obsahující index.php, do kterého vložit mysqli_close na konec. </summary>
    public string[]? OtherRootFolders { get; set; }

    /// <summary> URL k databázovému serveru. </summary>
    public string? Hostname { get; set; }

    /// <summary> Nová databáze na serveru hostname. </summary>
    public string? Database { get; set; }

    /// <summary> Nové uživatelské jméno k databázi. </summary>
    public string? Username { get; set; }

    /// <summary> Nové heslo k databázi. </summary>
    public string? Password { get; set; }

    /// <summary> Název souboru ve složce 'connect'. </summary>
    public string ConnectionFile { get; set; }

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

    /// <summary> Počet modifikovaných souborů během procesu aktualizace. </summary>
    public uint ModifiedFilesCount { get; internal set; } = 0;

    /// <summary> Celkový počet zpracovaných souborů. </summary>
    public uint TotalFilesCount { get; private set; } = 0;

    /// <summary> Inicializace povinných atributů. </summary>
    public MonaUpgrader(string baseFolder, string webName)
    {
        BaseFolder = baseFolder;
        WebName = webName;
        Regex.CacheSize = 30;
    }

    /// <summary> Rekurzivní upgrade .php souborů ve všech podadresářích. </summary>
    /// <param name="directoryPath">Cesta k adresáři, kde hledat .php soubory.</param>
    public void UpgradeAllFilesRecursively(string directoryPath)
    {
        //rekurzivní aktualizace podsložek
        foreach (var subdir in Directory.GetDirectories(directoryPath))
        {
            UpgradeAllFilesRecursively(subdir);
        }
        //aktualizace aktuální složky
        foreach (var filePath in Directory.GetFiles(directoryPath, "*.php"))
        {
            TotalFilesCount++;
            FileWrapper? file;
            if ((file = UpgradeProcedure(filePath)) is null)
            {
                continue;
            }
            //upraveno, zapsat do souboru
            file.WriteStatus();
            file.Save(WebName, BaseFolder);
            if (file.IsModified)
            {
                ModifiedFilesCount++;
            }
            //po dodelani nahrazeni nize projit na retezec - mysql_
            var mysql_FileRecord = UnmodifiedMysql_File.Create(file);
            if (mysql_FileRecord is not null)
            {
                //soubor se přidá do kolekce, pokud obsahuje funkce "mysql_".
                FilesContainingMysql.Add(mysql_FileRecord);
            }
        }
    }

    /// <summary> Procedura aktualizace zadaného souboru. </summary>
    /// <returns> Upravený soubor, null v případě TinyAjaxBehavior nebo prázdného souboru. </returns>
    protected virtual FileWrapper? UpgradeProcedure(string filePath)
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
            case { Path: var p } when p.Contains("tiny_mce"):
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
            && !Regex.IsMatch(file.Content.ToString(), @"//.*93\.185\.102\.228"))
        {
            file.Warnings.Add("Soubor obsahuje IP adresu mcrai1 (93.185.102.228).");
        }
        return file;
    }
}

using PhpUpgrader.Mona.UpgradeRoutines;

namespace PhpUpgrader.Mona;

/// <summary> PHP upgrader pro RS Mona z verze 5 na verzi 7. </summary>
public class MonaUpgrader
{
    /// <summary> Seznam souborů, které se nepodařilo aktualizovat a stále obsahují mysql_ funkce. </summary>
    public List<UnmodifiedMysql_Tracker> FilesContainingMysql { get; } = new();

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

    /// <summary> Co a čím to nahradit. </summary>
    public Dictionary<string, string> FindReplace { get; } = new()
    {
        { "=& new", "= new" },
        { "mysql_num_rows", "mysqli_num_rows" },
        { "MySQL_num_rows", "mysqli_num_rows" },
        { "mysql_error()", "mysqli_error($beta)" },
        { "mysql_connect", "mysqli_connect" },
        { "mysql_close", "mysqli_close" },
        { "MySQL_Close", "mysqli_close" },
        { "MySQL_close", "mysqli_close" },
        { "mysqli_close()", "mysqli_close($beta)" },
        { "mysql_fetch_row", "mysqli_fetch_row" },
        { "mysql_Fetch_Row", "mysqli_fetch_row" },
        { "mysql_fetch_array", "mysqli_fetch_array" },
        { "mysql_fetch_assoc", "mysqli_fetch_assoc" },
        { "mysql_fetch_object", "mysqli_fetch_object" },
        { "MySQL_fetch_object", "mysqli_fetch_object" },
        { "MYSQL_ASSOC", "MYSQLI_ASSOC" },
        { "mysql_select_db(DB_DATABASE, $this->db)", "mysqli_select_db($this->db, DB_DATABASE)" },
        { "mysql_select_db($database_beta, $beta)", "mysqli_select_db($beta, $database_beta)" },
        { "mysql_query(", "mysqli_query($beta, " },
        { "mysql_query (", "mysqli_query($beta, " },
        { "MySQL_Query(", "mysqli_query($beta, " },
        { "MySQL_Query (", "mysqli_query($beta, " },
        { ", $beta)", ")" },
        { ",$beta)", ")" },
        { "eregi(", "preg_match(" },
        { "eregi (", "preg_match(" },
        { "preg_match('^<tr(.*){0,}</tr>$'", "preg_match('/^<tr(.*){0,}< \\/tr>$/'" },
        { "unlink", "@unlink" },
        { "@@unlink", "@unlink" },
        { "mysql_data_seek", "mysqli_data_seek" },
        { "mysql_real_escape_string", "mysqli_real_escape_string" },
        { "mysql_free_result", "mysqli_free_result" },
        { "mysql_list_tables($database_beta);", "mysqli_query($beta, \"SHOW TABLES FROM `$database_beta`\");" },
        { "$table_all .= \"`\".mysql_tablename($result, $i).\"`\";", "$table_all .= \"`\".mysqli_fetch_row($result)[0].\"`\";" },
        { "<?php/", "<?php /" },
        { "<?PHP/", "<?PHP /" },
    };

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
            try //po dodelani nahrazeni nize projit na retezec - mysql_
            {
                //soubor se přidá do kolekce, pokud obsahuje funkce "mysql_".
                FilesContainingMysql.Add(new UnmodifiedMysql_Tracker(file));
            }
            catch (DoesNotContainMysql_Exception)
            {
                //soubor neobsahuje "myqsl_", ok, pokračujeme.
                continue;
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
                file.UpgradeFindReplace(this)
                    .UpgradeTinyMceUploaded();
                break;

            default:
                file.UpgradeConnect(this)
                    .UpgradeResultFunc(this)
                    .UpgradeClanekVypis()
                    .UpgradeFindReplace(this)
                    .UpgradeMysqliQueries(this)
                    .UpgradeCloseIndex(this)
                    .UpgradeAnketa()
                    .UpgradeChdir(AdminFolders)
                    .UpgradeTableAddEdit(AdminFolders)
                    .UpgradeStrankovani()
                    .UpgradeXmlFeeds()
                    .UpgradeSitemapSave(AdminFolders)
                    .UpgradeGlobalBeta()
                    .RenameBeta(this)
                    .UpgradeFloatExplodeConversions();
                break;
        }
        file.UpgradeRegexFunctions()
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

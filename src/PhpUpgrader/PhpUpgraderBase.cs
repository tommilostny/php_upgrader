namespace PhpUpgrader;

public abstract class PhpUpgraderBase
{
    /// <summary> Seznam souborů, které se nepodařilo aktualizovat a stále obsahují mysql_ funkce. </summary>
    public ICollection<UnmodifiedMysql_File> FilesContainingMysql { get; } = new List<UnmodifiedMysql_File>();

    /// <summary> Seznam souborů modifikovaných během procesu aktualizace. </summary>
    public ISet<string> ModifiedFiles { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary> Handler zajišťující část aktualizace najít >> nahradit. </summary>
    public IFindReplaceHandler FindReplaceHandler { get; }

    /// <summary> Handler zajišťující část aktualizace připojení k databázi. </summary>
    public IConnectHandler ConnectHandler { get; }

    /// <summary> Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'. </summary>
    public string BaseFolder { get; }

    /// <summary> Název webu ve složce 'weby'. </summary>
    public string WebName { get; }

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

    /// <summary> Celkový počet zpracovaných souborů. </summary>
    public uint TotalFilesCount { get; private set; } = 0;

    /// <summary> Inicializace povinných atributů. </summary>
    protected PhpUpgraderBase(string baseFolder, string webName, IFindReplaceHandler findReplaceHandler, IConnectHandler connectHandler)
    {
        BaseFolder = baseFolder;
        WebName = webName;
        FindReplaceHandler = findReplaceHandler;
        ConnectHandler = connectHandler;
        Regex.CacheSize = 42;
    }

    /// <summary> Procedura aktualizace zadaného souboru. </summary>
    /// <returns> Upravený soubor, null v případě TinyAjaxBehavior nebo prázdného souboru. </returns>
    protected abstract FileWrapper? UpgradeProcedure(string filePath);

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
                ModifiedFiles.Add(file.MoveOnSavePath is null ? file.Path : file.MoveOnSavePath);
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
}

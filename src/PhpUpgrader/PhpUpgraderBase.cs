﻿namespace PhpUpgrader;

public abstract class PhpUpgraderBase
{
    /// <summary> Seznam souborů, které se nepodařilo aktualizovat a stále obsahují mysql_ funkce. </summary>
    public UnmodifiedMysql_FilesCollection FilesContainingMysql { get; } = new();

    /// <summary> Seznam souborů modifikovaných během procesu aktualizace. </summary>
    public ISet<string> ModifiedFiles { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary> Handler zajišťující část aktualizace najít >> nahradit. </summary>
    public IFindReplaceHandler FindReplaceHandler { get; }

    /// <summary> Handler zajišťující část aktualizace připojení k databázi. </summary>
    public IConnectHandler ConnectHandler { get; }

    /// <summary> Absolutní cesta základní složky, kde jsou složky 'weby' a 'important'. </summary>
    public string BaseFolder { get; }

    /// <summary> Název webu ve složce 'weby'. </summary>
    public string WebName { get; protected set; }

    private string? _webFolder = null;
    /// <summary> Absolutní cesta k složce webu. </summary>
    public string WebFolder => _webFolder ??= Path.Join(BaseFolder, "weby", WebName);

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

    /// <summary> Kód, který se spustí po úspěšné <see cref="UpgradeProcedure(string)"/>. </summary>
    protected abstract void AfterUpgradeProcedure(FileWrapper file);

    /// <summary> Spustí aktualizaci všech souborů ve složce. </summary>
    public virtual void RunUpgrade(string directoryPath) => UpgradeAllFilesRecursively(directoryPath);

    /// <summary> Rekurzivní upgrade .php souborů ve všech podadresářích. </summary>
    /// <param name="directoryPath">Cesta k adresáři, kde hledat .php soubory.</param>
    protected void UpgradeAllFilesRecursively(string directoryPath)
    {
        //rekurzivní aktualizace podsložek
        foreach (var subdir in Directory.GetDirectories(directoryPath))
        {
            UpgradeAllFilesRecursively(subdir);
        }
        //aktualizace aktuální složky
        foreach (var filePath in Directory.GetFiles(directoryPath, "*.php"))
        {
            if (filePath.EndsWith("mssql_overwrite.php", StringComparison.Ordinal))
                continue;

            TotalFilesCount++;
            FileWrapper.PrintFile(filePath, "🔃");
            Console.Write('\r');

            FileWrapper? file;
            if ((file = UpgradeProcedure(filePath)) is null)
                continue;

            AfterUpgradeProcedure(file);
            //upraveno, zapsat do souboru
            file.PrintStatus();
            file.Save(WebName, BaseFolder);
            if (file.IsModified)
            {
                ModifiedFiles.Add(file.MoveOnSavePath is null ? file.Path : file.MoveOnSavePath);
            }
            //po dodelani nahrazeni nize projit na retezec - mysql_
            FilesContainingMysql.CheckAdd(file);
        }
    }
}

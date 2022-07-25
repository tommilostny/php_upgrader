namespace PhpUpgrader;

/// <summary> Třída udržující informace o souboru (obsah, cesta, příznak modifikace). </summary>
public class FileWrapper
{
    /// <summary> Cesta k souboru. </summary>
    public string Path { get; }

    /// <summary> Obsah souboru. </summary>
    public StringBuilder Content { get; }

    /// <summary> Původní obsah souboru pro porovnání. </summary>
    private readonly string _initialContent;

    /// <summary> Příznak modifikace obsahu souboru. </summary>
    public bool IsModified => !Content.Equals(_initialContent);

    /// <summary> Symbol značící nemodifikovaný soubor (černá). </summary>
    public const string UnmodifiedSymbol = "⚫";

    /// <summary> Symbol značící modifikovaný soubor (modrá). </summary>
    public const string ModifiedSymbol = "🔵";

    /// <summary> Symbol varování o možné chybě. </summary>
    public const string WarningSymbol = "⚠️";

    /// <summary> Seznam varování o možných chybách. Zobrazí se za výpisem stavu o souboru. </summary>
    public List<string> Warnings { get; } = new();

    /// <summary> Přejmenování/přesunutí souboru na tuto cestu při ukládání, pokud není null. </summary>
    public string? MoveOnSavePath { get; set; } = null;

    /// <summary> Obsah souboru je zadán parametrem. </summary>
    /// <param name="path"> Cesta k souboru. </param>
    /// <param name="content"> Obsah souboru. </param>
    public FileWrapper(string path, string content)
    {
        Path = path;
        Content = new(content);
        _initialContent = content;
    }

    /// <summary> Obsah souboru je načten z disku na zadané cestě. </summary>
    /// <param name="path"> Cesta k souboru. </param>
    public FileWrapper(string path) : this(path, content: File.ReadAllText(path))
    {
    }

    /// <summary> Uložit modifikovaný obsah souboru. </summary>
    /// <remarks> Přesune soubor na cestu <see cref="MoveOnSavePath"/>, pokud není null. </remarks>
    public void Save(string webName, string baseFolder)
    {
        if (!IsModified) //Nezapisovat, pokud neproběhly žádné změny.
        {
            return;
        }
        //Vytvořit backup soubor.
        var s = System.IO.Path.DirectorySeparatorChar;
        var backupFile = new FileInfo(Path.Replace($"{baseFolder}{s}weby{s}{webName}{s}",
                                                   $"{baseFolder}{s}weby{s}_backup{s}{webName}{s}"));
        backupFile.Directory.Create();
        if (!backupFile.Exists)
        {
            File.Copy(Path, backupFile.FullName);
        }
        //Zapsat změny.
        File.WriteAllText(Path, Content.ToString());
        
        if (MoveOnSavePath is not null) //Přesunout soubor, pokud je potřeba změnit jméno.
        {
            File.Move(Path, MoveOnSavePath, overwrite: true);
        }
    }

    /// <summary> Vypíše název souboru a stav modifikace. </summary>
    /// <param name="modified">Který symbol modifikace vybrat?</param>
    public void WriteStatus(bool modified)
    {
        var s = System.IO.Path.DirectorySeparatorChar;
        var webyIndex = Path.IndexOf($"{s}weby{s}");

        var displayName = webyIndex != -1 ? Path.AsSpan(webyIndex + 6) : Path;

        var symbol = modified ? ModifiedSymbol : UnmodifiedSymbol;

        Console.WriteLine($"{symbol} {displayName}");

        if (!modified) //Výpis varování k souboru, pouze pokud je soubor nějak upraven.
        {
            return;
        }
        foreach (var warning in Warnings)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Error.WriteLine($"{WarningSymbol} {warning}");
            Console.ResetColor();
        }
    }

    /// <summary> Vypíše název souboru a stav modifikace. </summary>
    /// <remarks> Pro zjištění modifikace použije <see cref="IsModified"/>. </remarks>
    public void WriteStatus() => WriteStatus(IsModified);
}

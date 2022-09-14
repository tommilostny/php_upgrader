using System.Collections;

namespace PhpUpgrader;

/// <summary>
/// Třída, která je kolekcí <seealso cref="UnmodifiedMysql_File"/> pouze pro čtení
/// nebo přidání metodou <see cref="CheckAdd(FileWrapper)"/>, která napřed zkontroluje daný soubor.
/// </summary>
public sealed class UnmodifiedMysql_FilesCollection : IReadOnlyCollection<UnmodifiedMysql_File>
{
    /// <summary> Datová struktura na pozadí, do které lze přidávat. </summary>
    private readonly HashSet<UnmodifiedMysql_File> _files = new();

    public int Count => _files.Count;

    /// <summary> Zjistí, zda soubor obsahuje funkce "mysql_". A případně jej přidá do kolekce. </summary>
    public void CheckAdd(FileWrapper file)
    {
        var matches = Regex.Matches(file.Content.ToString(),
                                    @"(?<!(//.*)|(/\*((.|\n)(?!\*/))*)|\$|->|_|PDO::|'|"")mysql_[^( )]+",
                                    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                                    TimeSpan.FromSeconds(4));
        if (matches.Count > 0)
        {
            //soubor se přidá do kolekce, pokud obsahuje funkce "mysql_".
            _files.Add(new UnmodifiedMysql_File(file, matches));
        }
    }

    public IEnumerator<UnmodifiedMysql_File> GetEnumerator() => _files.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _files.GetEnumerator();
}

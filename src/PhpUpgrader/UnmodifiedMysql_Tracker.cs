namespace PhpUpgrader;

/// <summary>
/// Vyhazuje konstruktor třídy <see cref="UnmodifiedMysql_Tracker"/>, pokud v souboru nenajde žádnou z funkcí "mysql_".
/// </summary>
public class DoesNotContainMysql_Exception : Exception
{
    public override string Message => "Tento soubor neobsahuje funkce 'mysql_'.";
}

/// <summary>
/// Záznam o funkcích "mysql_" nalezených v souboru <see cref="FileName"/>.
/// </summary>
public record UnmodifiedMysql_Tracker
{
    public string FileName { get; }

    public IReadOnlyCollection<(uint line, string function)> Matches { get; }

    /// <summary>
    /// Vytvoří záznam o souboru, který obsahuje funkce mysql_.
    /// </summary>
    /// <param name="file"> Soubor, ve kterém hledat funkce "mysql_". </param>
    /// <exception cref="DoesNotContainMysql_Exception"> Soubor neobsahuje žádné funkce "mysql_". </exception>
    public UnmodifiedMysql_Tracker(FileWrapper file)
    {
        var matches = Regex.Matches(file.Content.ToString(),
                                    @"(?<!(//.*)|\$|->|_)mysql_[^(]+",
                                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
        if (matches.Count == 0)
        {
            throw new DoesNotContainMysql_Exception();
        }
        FileName = file.Path;
        Matches = LoadMatchesWithLineNumbers(file, matches);
    }

    public void Deconstruct(out string fileName, out IReadOnlyCollection<(uint line, string function)> matches)
    {
        fileName = FileName;
        matches = Matches;
    }

    private static List<(uint line, string function)> LoadMatchesWithLineNumbers(FileWrapper file, IEnumerable<Match> matches)
    {
        var matchesList = new List<(uint, string)>();
        foreach (var match in matches)
        {
            uint line = 1;
            for (var i = 0; i < match.Index; i++)
            {
                if (file.Content[i] == '\n')
                {
                    line++;
                }
            }
            matchesList.Add((line, match.Value));
        }
        return matchesList;
    }
}

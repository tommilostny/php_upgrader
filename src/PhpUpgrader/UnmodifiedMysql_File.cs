namespace PhpUpgrader;

/// <summary>
/// Záznam o funkcích "mysql_" nalezených v souboru <see cref="FileName"/>.
/// </summary>
public sealed record UnmodifiedMysql_File
{
    public string FileName { get; }

    public IReadOnlyCollection<(uint line, string function)> Matches { get; }

    public UnmodifiedMysql_File(FileWrapper file, MatchCollection matches)
    {
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
        var i = 0;
        uint line = 1;
        
        foreach (var match in matches)
        {
            while (i < match.Index)
            {
                if (file.Content[i++] == '\n')
                {
                    line++;
                }
            }
            matchesList.Add((line, match.Value));
        }
        return matchesList;
    }
}

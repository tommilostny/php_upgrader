namespace PhpUpgrader;

/// <summary>
/// Záznam o funkcích "mysql_" nalezených v souboru <see cref="FileName"/>.
/// </summary>
public sealed record UnmodifiedMysql_File
{
    public string FileName { get; }

    public IReadOnlyCollection<(uint line, string function)> Matches { get; }

    /// <summary>
    /// Zjistí, zda soubor obsahuje funkce "mysql_".
    /// </summary>
    /// <param name="file"> Soubor, ve kterém hledat funkce "mysql_". </param>
    /// <returns>
    /// Novou instanci záznamu <see cref="UnmodifiedMysql_File"/> nebo <b>null</b>,
    /// pokud <paramref name="file"/> neobsahuje "mysql_" funkce.
    /// </returns>
    public static UnmodifiedMysql_File? Create(FileWrapper file)
    {
        var matches = Regex.Matches(file.Content.ToString(),
                                    @"(?<!(//.*)|(/\*((.|\n)(?!\*/))*)|\$|->|_|PDO::)mysql_[^( )]+",
                                    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                                    TimeSpan.FromSeconds(4));
        
        return matches.Count == 0 ? null : new(file, matches);
    }

    public void Deconstruct(out string fileName, out IReadOnlyCollection<(uint line, string function)> matches)
    {
        fileName = FileName;
        matches = Matches;
    }

    private UnmodifiedMysql_File(FileWrapper file, MatchCollection matches)
    {
        FileName = file.Path;
        Matches = LoadMatchesWithLineNumbers(file, matches);
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

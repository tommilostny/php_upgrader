namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class OldUnparsableAlmostEmptyFile
{
    private static readonly (string file, string eofCode)[] _knownOccurences = new[]
    {
        (Path.Join("money", "old", "Compare_XML.php"),
         "public XMLDiff\\File::diff ( string  , string $to ) : string"
        ),
    };

    /// <summary> "money/old/Compare_XML.php" je téměř prázdný a obsahuje kód, který nedává smysl. </summary>
    public static FileWrapper UpgradeOldUnparsableAlmostEmptyFile(this FileWrapper file)
    {
        foreach (var (path, code) in _knownOccurences)
        {
            if (file.Path.EndsWith(path, StringComparison.Ordinal))
            {
                var commented = $"/* {code} */";
                if (!file.Content.Contains(commented))
                {
                    file.Content.Replace(code, commented);
                }
                if (file.Content[^2] != '?' && file.Content[^1] != '>')
                {
                    file.Content.Append("\n?>");
                }
                break;
            }
        }
        return file;
    }
}

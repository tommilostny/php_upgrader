namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class OldUnparsableAlmostEmptyFile
{
    /// <summary> "money/old/Compare_XML.php" je téměř prázdný a obsahuje kód, který nedává smysl. </summary>
    public static FileWrapper UpgradeOldUnparsableAlmostEmptyFile(this FileWrapper file)
    {
        foreach (var (path, code) in KnownOccurences())
        {
            if (file.Path.EndsWith(path))
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

    private static IEnumerable<(string file, string eofCode)> KnownOccurences()
    {
        yield return (Path.Join("money", "old", "Compare_XML.php"), "public XMLDiff\\File::diff ( string  , string $to ) : string");
    }
}

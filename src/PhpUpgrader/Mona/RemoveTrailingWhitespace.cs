namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// PHPStan: File ends with a trailing whitespace.
    /// This may cause problems when running the code in the web browser.
    /// Remove the closing ?> mark or remove the whitespace.
    /// </summary>
    public static void RemoveTrailingWhitespace(FileWrapper file)
    {
        while (char.IsWhiteSpace(file.Content[^1]))
        {
            file.Content.Remove(file.Content.Length - 1, 1);
        }
    }
}

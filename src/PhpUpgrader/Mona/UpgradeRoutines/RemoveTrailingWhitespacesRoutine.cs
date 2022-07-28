namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// PHPStan: File ends with a trailing whitespace.
/// This may cause problems when running the code in the web browser.
/// Remove the closing ?> mark or remove the whitespace.
/// </summary>
public static class RemoveTrailingWhitespacesRoutine
{
    /// <summary> Removes the trailing whitespaces at the end of a file. </summary>
    /// <exception cref="IndexOutOfRangeException">Empty file.</exception>
    public static void RemoveTrailingWhitespaces(this FileWrapper file)
    {
        while (char.IsWhiteSpace(file.Content[^1]))
        {
            file.Content.Remove(file.Content.Length - 1, 1);
        }
    }
}

﻿namespace PhpUpgrader.Mona.UpgradeExtensions;

/// <summary>
/// PHPStan: File ends with a trailing whitespace.
/// This may cause problems when running the code in the web browser.
/// Remove the closing ?> mark or remove the whitespace.
/// </summary>
public static class TrailingWhiteSpace
{
    /// <summary> Odstraní bílé znaky z konce souboru. </summary>
    /// <exception cref="IndexOutOfRangeException"> Prázdný soubor. </exception>
    public static FileWrapper RemoveTrailingWhitespaces(this FileWrapper file)
    {
        while (char.IsWhiteSpace(file.Content[^1]))
        {
            file.Content.Remove(file.Content.Length - 1, 1);
        }
        return file;
    }
}

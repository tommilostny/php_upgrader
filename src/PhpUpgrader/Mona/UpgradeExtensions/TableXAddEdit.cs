namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class TableXAddEdit
{
    private static string[]? _afTableXAddEditFiles = null;

    /// <summary>
    /// upravit soubor admin/table_x_add.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 47-55 - $pocet_text_all = mysqli_num_rows….
    /// upravit soubor admin/table_x_edit.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 53-80 - $pocet_text_all = mysqli_num_rows….
    /// </summary>
    public static FileWrapper UpgradeTableXAddEdit(this FileWrapper file, string[] adminFolders)
    {
        const string variable = "$pocet_text_all";
        const string variableWithAtSign = $"@{variable}";

        if (_afTableXAddEditFiles is null)
        {
            _afTableXAddEditFiles = new string[adminFolders.Length << 1];
            for (var i = 0; i < adminFolders.Length; i++)
            {
                var af = adminFolders[i];
                _afTableXAddEditFiles[i] = Path.Join(af, "table_x_add.php");
                _afTableXAddEditFiles[i + adminFolders.Length] = Path.Join(af, "table_x_edit.php");
            }
        }
        if (_afTableXAddEditFiles.Any(af => file.Path.EndsWith(af, StringComparison.Ordinal))
            && !file.Content.Contains(variableWithAtSign))
        {
            file.Content.Replace($"{variable} = mysqli_num_rows", $"{variableWithAtSign} = mysqli_num_rows");
        }
        return file;
    }
}

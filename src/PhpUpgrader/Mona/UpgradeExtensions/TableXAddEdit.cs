namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class TableXAddEdit
{
    /// <summary>
    /// upravit soubor admin/table_x_add.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 47-55 - $pocet_text_all = mysqli_num_rows….
    /// upravit soubor admin/table_x_edit.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 53-80 - $pocet_text_all = mysqli_num_rows….
    /// </summary>
    public static FileWrapper UpgradeTableXAddEdit(this FileWrapper file, IEnumerable<string> adminFolders)
    {
        const string variable = "$pocet_text_all";
        const string variableWithAtSign = $"@{variable}";

        if (adminFolders.Any(af => file.Path.Contains(Path.Join(af, "table_x_add.php"), StringComparison.Ordinal)
                                 || file.Path.Contains(Path.Join(af, "table_x_edit.php"), StringComparison.Ordinal))
            && !file.Content.Contains(variableWithAtSign))
        {
            file.Content.Replace($"{variable} = mysqli_num_rows", $"{variableWithAtSign} = mysqli_num_rows");
        }
        return file;
    }
}

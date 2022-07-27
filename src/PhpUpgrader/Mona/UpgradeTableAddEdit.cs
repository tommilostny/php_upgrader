namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// upravit soubor admin/table_x_add.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 47-55 - $pocet_text_all = mysqli_num_rows….
    /// upravit soubor admin/table_x_edit.php
    ///     - potlacit chybova hlasku znakem „@“ na radku cca 53-80 - $pocet_text_all = mysqli_num_rows….
    /// </summary>
    public void UpgradeTableAddEdit(FileWrapper file)
    {
        const string variable = "$pocet_text_all";
        const string variableWithAtSign = $"@{variable}";

        if (!AdminFolders.Any(af => file.Path.Contains(Path.Join(af, "table_x_add.php"))
                                 || file.Path.Contains(Path.Join(af, "table_x_edit.php")))
            || file.Content.Contains(variableWithAtSign))
        {
            return;
        }
        file.Content.Replace($"{variable} = mysqli_num_rows", $"{variableWithAtSign} = mysqli_num_rows");
    }
}

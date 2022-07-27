namespace PhpUpgrader.Rubicon;

public partial class RubiconUpgrader
{
    /// <summary> [Break => Return] v souboru aegisx\detail.php (není ve smyčce, ale v if). </summary>
    public static void UpgradeAegisxDetail(FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("aegisx", "detail.php")))
        {
            return;
        }
        var content = file.Content.ToString();
        var updated = Regex.Replace(content, @"if\s?\(\$presmeruj == ""NO""\)\s*\{\s*break;",
                                              "if ($presmeruj == \"NO\") {\n\t\t\treturn;");
        file.Content.Replace(content, updated);
    }
}

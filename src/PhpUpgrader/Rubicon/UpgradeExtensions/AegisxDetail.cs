namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class AegisxDetail
{
    /// <summary> [Break => Return] v souboru aegisx\detail.php (není ve smyčce, ale v if). </summary>
    public static FileWrapper UpgradeAegisxDetail(this FileWrapper file)
    {
        if (!file.Path.EndsWith(Path.Join("aegisx", "detail.php"), StringComparison.Ordinal))
        {
            return file;
        }
        var content = file.Content.ToString();
        var updated = IfPresmerujRegex().Replace(content, "if ($presmeruj == \"NO\") {\n\t\t\treturn;");
        file.Content.Replace(content, updated);
        return file;
    }

    [GeneratedRegex(@"if\s?\(\$presmeruj == ""NO""\)\s*\{\s*break;", RegexOptions.None, matchTimeoutMilliseconds: 6666)]
    private static partial Regex IfPresmerujRegex();
}

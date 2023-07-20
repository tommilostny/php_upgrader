namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class AegisxDetail
{
    private static readonly string _detailPhp = Path.Join("aegisx", "detail.php");

    /// <summary> [Break => Return] v souboru aegisx\detail.php (není ve smyčce, ale v if). </summary>
    public static FileWrapper UpgradeAegisxDetail(this FileWrapper file)
    {
        if (file.Path.EndsWith(_detailPhp, StringComparison.Ordinal))
        {
            file.Content.Replace(
                IfPresmerujRegex().Replace(
                    file.Content.ToString(),
                    "if ($presmeruj == \"NO\") {\n\t\t\treturn;"
                )
            );
        }
        return file;
    }

    [GeneratedRegex(@"if\s?\(\$presmeruj == ""NO""\)\s*\{\s*break;", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex IfPresmerujRegex();
}

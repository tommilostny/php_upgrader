namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class ProductListFix
{
    public static FileWrapper FixProductList(this FileWrapper file)
    {
        if (file.Path.EndsWith("product_prehled_detail.php", StringComparison.Ordinal))
        {
            file.Content.Replace(PriceDivRegex().Replace(file.Content.ToString(), m => $"{m}</div>"));
        }
        return file;
    }

    [GeneratedRegex(@"<div class=""price"">.*?<\?.+?CURRENCY\s\?>(?!\s*<\/div>)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex PriceDivRegex();
}

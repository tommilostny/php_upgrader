namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class MissingCurlyBracket
{
    private static readonly string _getDeliveryPricePhp = Path.Join("card", "getDeliveryPrice.php");
    private static readonly string _getDeliveryPriceZalohaPhp = Path.Join("card_zaloha", "getDeliveryPrice.php");

    public static FileWrapper UpgradeMissingCurlyBracket(this FileWrapper file)
    {
        if (file.Path.EndsWith(_getDeliveryPricePhp, StringComparison.Ordinal)
            || file.Path.EndsWith(_getDeliveryPriceZalohaPhp, StringComparison.Ordinal))
        {
            var openingBracketsCount = file.Content.Count('{');
            var closingBracketsCount = file.Content.Count('}');
            if (openingBracketsCount > closingBracketsCount)
            {
                file.Content.Replace("?>", "} ?>");
            }
        }
        return file;
    }
}

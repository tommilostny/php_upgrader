namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class MissingCurlyBracket
{
    public static FileWrapper UpgradeMissingCurlyBracket(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("card", "getDeliveryPrice.php"), StringComparison.Ordinal)
            || file.Path.EndsWith(Path.Join("card_zaloha", "getDeliveryPrice.php"), StringComparison.Ordinal))
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

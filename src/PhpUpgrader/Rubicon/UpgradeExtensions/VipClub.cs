namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class VipClub
{
    public static FileWrapper UpgradeVipClub(this FileWrapper file)
    {
        int i;
        if (file.Path.EndsWith("vip-club.php", StringComparison.Ordinal)
            && (i = file.Content.IndexOf("$VIP_R5_TEXT->")) != -1)
        {
            var nl = Environment.NewLine;
            file.Content.Insert(i, $"if (!isset($VIP_R5_TEXT)) {{{nl}\t$VIP_R5_TEXT = new stdClass();{nl}}}{nl}");
        }
        return file;
    }
}

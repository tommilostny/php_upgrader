namespace PhpUpgrader.Mona.UpgradeExtensions;

public static partial class XmlFeeds
{
    /// <summary>
    /// Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
    /// </summary>
    public static FileWrapper UpgradeXmlFeeds(this FileWrapper file)
    {
        if (XmlFeedsRegex().IsMatch(file.Path))
        {
            file.Content.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");
        }
        return file;
    }

    [GeneratedRegex("xml_feeds_[^edit]", RegexOptions.None, matchTimeoutMilliseconds: 6666)]
    private static partial Regex XmlFeedsRegex();
}

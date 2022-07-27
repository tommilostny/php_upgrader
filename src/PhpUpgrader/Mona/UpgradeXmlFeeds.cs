namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
    /// </summary>
    public static void UpgradeXmlFeeds(FileWrapper file)
    {
        if (Regex.IsMatch(file.Path, "xml_feeds_[^edit]", RegexOptions.Compiled))
        {
            file.Content.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");
        }
    }
}

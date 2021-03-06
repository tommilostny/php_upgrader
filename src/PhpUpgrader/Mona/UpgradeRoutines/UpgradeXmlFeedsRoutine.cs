namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class UpgradeXmlFeedsRoutine
{
    /// <summary>
    /// Xml_feeds_ if($query_podmenu_all["casovani"] == 1) -> if($data_podmenu_all["casovani"] == 1)
    /// </summary>
    public static FileWrapper UpgradeXmlFeeds(this FileWrapper file)
    {
        if (Regex.IsMatch(file.Path, "xml_feeds_[^edit]", RegexOptions.Compiled))
        {
            file.Content.Replace("if($query_podmenu_all[\"casovani\"] == 1)", "if($data_podmenu_all[\"casovani\"] == 1)");
        }
        return file;
    }
}

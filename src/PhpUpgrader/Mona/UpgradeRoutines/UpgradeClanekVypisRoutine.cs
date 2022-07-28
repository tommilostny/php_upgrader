namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeClanekVypisRoutine
{
    /// <summary>
    /// upravit soubory system/clanek.php a system/vypis.php - pokud je sdileni fotogalerii pridat nad podminku $vypis_table_clanek["sdileni_fotogalerii"] kod $p_sf = array();
    /// </summary>
    public static void UpgradeClanekVypis(this FileWrapper file)
    {
        const string lookingFor = "$vypis_table_clanek[\"sdileni_fotogalerii\"]";
        const string adding = "$p_sf = array();";
        const string addLine = $"        {adding}\n";

        if (file.Content.Contains(lookingFor) && !file.Content.Contains(adding))
        {
            var lines = file.Content.Split();
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Contains(lookingFor))
                {
                    line.Insert(0, addLine);
                }
            }
            lines.JoinInto(file.Content);
        }
    }
}

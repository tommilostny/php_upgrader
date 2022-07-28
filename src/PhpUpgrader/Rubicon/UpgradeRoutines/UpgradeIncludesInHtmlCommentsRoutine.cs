namespace PhpUpgrader.Rubicon.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeIncludesInHtmlCommentsRoutine
{
    /// <summary> templates/.../product_detail.php, zakomentovaný blok HTML stále spouští broken PHP includy, zakomentovat </summary>
    public static void UpgradeIncludesInHtmlComments(this FileWrapper file)
    {
        if (!Regex.IsMatch(file.Path, @"(\\|/)templates(\\|/).+(\\|/)product_detail\.php", RegexOptions.Compiled))
        {
            return;
        }
        var lines = file.Content.Split();
        var insideHtmlComment = false;
        var commentedAtLeastOneInclude = false;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("<!--"))
            {
                insideHtmlComment = true;
            }
            if (lines[i].Contains("-->"))
            {
                insideHtmlComment = false;
            }
            if (insideHtmlComment && (commentedAtLeastOneInclude |= lines[i].Contains("<?php include")))
            {
                lines[i].Replace("<?php include", "<?php //include");
            }
        }
        lines.JoinInto(file.Content);

        if (commentedAtLeastOneInclude)
        {
            file.Warnings.Add("Zkontrolovat HTML zakomentované '<?php include'.");
        }
    }
}

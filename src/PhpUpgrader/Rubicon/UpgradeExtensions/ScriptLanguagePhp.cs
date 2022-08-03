namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class ScriptLanguagePhp
{
    /// <summary> HTML tag &lt;script language="PHP"&gt;&lt;/script> deprecated => &lt;?php ?&gt; </summary>
    public static FileWrapper UpgradeScriptLanguagePhp(this FileWrapper file)
    {
        const string oldScriptTagStart = @"<script language=""PHP"">";
        const string oldScriptTagEnd = "</script>";

        if (!file.Content.Contains(oldScriptTagStart, StringComparison.OrdinalIgnoreCase))
        {
            return file;
        }
        var lines = file.Content.Split();
        var insidePhpScriptTag = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains(oldScriptTagStart, StringComparison.OrdinalIgnoreCase))
            {
                var lineStr = line.ToString();
                var updated = Regex.Replace(lineStr,
                                            oldScriptTagStart,
                                            "<?php ",
                                            RegexOptions.IgnoreCase,
                                            TimeSpan.FromSeconds(5));

                line.Replace(lineStr, updated);
                insidePhpScriptTag = true;
            }
            if (insidePhpScriptTag && line.Contains(oldScriptTagEnd))
            {
                line.Replace(oldScriptTagEnd, " ?>");
                insidePhpScriptTag = false;
            }
        }
        lines.JoinInto(file.Content);
        file.Warnings.Add($"Nalezena značka {oldScriptTagStart}. Zkontrolovat možný Javascript.");
        return file;
    }
}

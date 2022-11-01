namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class ScriptLanguagePhp
{
    private const string _oldScriptTagStart = @"<script language=""PHP"">";
    private const string _oldScriptTagEnd = "</script>";

    /// <summary> HTML tag &lt;script language="PHP"&gt;&lt;/script> deprecated => &lt;?php ?&gt; </summary>
    public static FileWrapper UpgradeScriptLanguagePhp(this FileWrapper file)
    {

        if (!file.Content.Contains(_oldScriptTagStart, StringComparison.OrdinalIgnoreCase))
        {
            return file;
        }
        var lines = file.Content.Split();
        var insidePhpScriptTag = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains(_oldScriptTagStart, StringComparison.OrdinalIgnoreCase))
            {
                var lineStr = line.ToString();
                var updated = OldScriptTagRegex().Replace(lineStr, "<?php ");

                line.Replace(lineStr, updated);
                insidePhpScriptTag = true;
            }
            if (insidePhpScriptTag && line.Contains(_oldScriptTagEnd))
            {
                line.Replace(_oldScriptTagEnd, " ?>");
                insidePhpScriptTag = false;
            }
        }
        lines.JoinInto(file.Content);
        file.Warnings.Add($"Nalezena značka {_oldScriptTagStart}. Zkontrolovat možný Javascript.");
        return file;
    }

    [GeneratedRegex(_oldScriptTagStart, RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1234)]
    private static partial Regex OldScriptTagRegex();
}

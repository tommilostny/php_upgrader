namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class FloatExplodeConversions
{
    /// <summary> PHPStan: Parameter #2 $str of function explode expects string, float|int&lt;0, max&gt; given. </summary>
    public static FileWrapper UpgradeFloatExplodeConversions(this FileWrapper file)
    {
        if (file.Content.Contains("$stranka_end = explode"))
        {
            var content = file.Content.ToString();
            var updated = Regex.Replace(content,
                                        @"\s\$stranka_end = \$stranka_pocet \/ 10;\s+\$stranka_end = explode\(""\."", \$stranka_end\);\s+\$stranka_end = \$stranka_end\[0\];\s+\$stranka_end = \$stranka_end \* 10 \+ 10;",
                                        "\n$stranka_end = (int)($stranka_pocet / 10);\n$stranka_end = $stranka_end * 10 + 10;");
            file.Content.Replace(content, updated);
        }
        return file;
    }
}

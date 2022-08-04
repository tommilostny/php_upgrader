namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class PiwikaLibsPearRaiseError
{
    /// <summary>
    /// V souboru piwika/libs/PEAR.php projít třídu PEAR
    /// a aktualizovat její (statickou?) metodu &amp;raiseError,
    /// aby neobsahovala referenci na $this.
    /// Místo toho doplnit parametr $pear_object (inspirováno novější verzí této knihovny).
    /// </summary>
    public static FileWrapper UpgradePiwikaLibsPearRaiseError(this FileWrapper file)
    {
        var isPear = file.Path.EndsWith(Path.Join("piwika", "libs", "PEAR.php"), StringComparison.Ordinal);
        var isTar = file.Path.EndsWith(Path.Join("piwika", "libs", "Archive_Tar", "Tar.php"), StringComparison.Ordinal);
        if (isPear || isTar)
        {
            file.Content.Replace("PEAR::raiseError()", "PEAR::raiseError(null)")
                        .Replace("PEAR::raiseError($message", "PEAR::raiseError(null, $message")
                        .Replace("PEAR::raiseError(\"", "PEAR::raiseError(null, \"")
                        .Replace("PEAR::raiseError('", "PEAR::raiseError(null, '")
                        .Replace("$this->raiseError(", "PEAR::raiseError($this, ");
        }
        if (isPear)
        {
            var wholeContent = file.Content.ToString();
            var initialContent = wholeContent;

            var index = wholeContent.IndexOf("class PEAR", StringComparison.Ordinal) + 10;
            index = wholeContent.IndexOf('{', index) + 1;

            ClassConstructors.GoThroughClass(wholeContent, index, onFunctionFindAction: (int i) =>
            {
                var lowerHalf = wholeContent.AsSpan(0, i += 2);
                var higherHalf = wholeContent.AsSpan(i);

                const string functionStart = "&raiseError($message = null,";
                const string replacement = "&raiseError(?PEAR $pear_object,\n                         $message = null,";

                if (higherHalf.StartsWith(functionStart, StringComparison.Ordinal))
                {
                    var sb = new StringBuilder().Append(higherHalf)
                                                .Replace(functionStart, replacement, 0, replacement.Length);
                    char currentChar;
                    ushort scope = 2;
                    i = sb.IndexOf('{') + 1;

                    while (++i < sb.Length)
                    {
                        switch (currentChar = sb[i])
                        {
                            case '{': scope++; break;
                            case '}': scope--; break;
                        }
                        if (scope < 2) //>=2: uvnitř funkce, 1: třída
                        {
                            break;
                        }
                        UpgradeCurrentLine(sb, i);
                    }
                    wholeContent = $"{lowerHalf}{sb}";
                }
            });
            file.Content.Replace(initialContent, wholeContent);
        }
        return file;
    }

    /// <summary>
    /// Získá a aktualizuje ($this >> $pear_object) aktuální řádek relativně k indexu v celém obsahu.
    /// </summary>
    private static void UpgradeCurrentLine(StringBuilder content, int index)
    {
        int? startIndex = null, endIndex = null;
        for (int i = index; i >= 0 ; i--)
        {
            if (content[i] == '\n')
            {
                startIndex = i + 1;
                break;
            }
        }
        for (int i = index + 1; i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                endIndex = i;
                break;
            }
        }
        startIndex ??= 0;
        endIndex ??= content.Length - 1;

        var lineStr = content.ToString()[startIndex.Value..endIndex.Value];
        if (lineStr.Contains("$this", StringComparison.Ordinal))
        {
            var updatedLine = new StringBuilder(lineStr).Replace("isset($this)", "$pear_object !== null")
                                                        .Replace("$this", "$pear_object");

            content.Replace(lineStr, updatedLine.ToString(), startIndex.Value, updatedLine.Length);
        }
    }
}

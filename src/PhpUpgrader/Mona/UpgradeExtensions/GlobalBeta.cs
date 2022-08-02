namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class GlobalBeta
{
    /// <summary>
    /// pro všechny funkce které v sobe mají dotaz na db pridat na zacatek
    ///     - global $beta; >>> hledat v netbeans - (?s)^(?=.*?function )(?=.*?mysqli_) - regular
    /// </summary>
    public static FileWrapper UpgradeGlobalBeta(this FileWrapper file)
    {
        if (file.Content.Contains("$this")
            || !Regex.IsMatch(file.Content.ToString(), "(?s)^(?=.*?function )(?=.*?mysqli_)", RegexOptions.Compiled))
        {
            return file;
        }
        var javascript = false;
        var lines = file.Content.Split();
        const string globalBeta = "\n\n    global $beta;\n";

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("<script")) javascript = true;
            if (line.Contains("</script")) javascript = false;

            if (Regex.IsMatch(line.ToString(), @"function\s", RegexOptions.Compiled)
                && !javascript
                && MysqliAndBetaInFunction(i, lines))
            {
                if ((line = lines[++i]).Contains('{'))
                {
                    line.Append(globalBeta);
                    continue;
                }
                line.Insert(0, globalBeta);
            }
        }
        lines.JoinInto(file.Content);
        return file;
    }

    private static bool MysqliAndBetaInFunction(int startIndex, IReadOnlyList<StringBuilder> lines)
    {
        bool javascript = false, inComment = false, foundMysqli = false, foundBeta = false;
        var bracketCount = 0;

        for (var i = startIndex; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("<script")) javascript = true;
            if (line.Contains("</script")) javascript = false;

            if (javascript)
                continue;

            if (line.Contains("/*")) inComment = true;
            if (line.Contains("*/")) inComment = false;

            if (!inComment && !line.ToString().TrimStart().StartsWith("//"))
            {
                if (line.Contains("mysqli_")) foundMysqli = true;
                if (line.Contains("$beta")) foundBeta = true;

                if (foundBeta && foundMysqli)
                    return true;
            }
            bracketCount += line.Count('{');
            bracketCount -= line.Count('}');

            if ((line.Contains("global $beta;") || bracketCount <= 0) && i > startIndex)
                break;
        }
        return false;
    }
}

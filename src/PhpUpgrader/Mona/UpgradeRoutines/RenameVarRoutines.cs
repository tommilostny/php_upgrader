namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// Metody pro přejmenování proměnné v souboru.
/// </summary>
public static class RenameVarRoutines
{
    /// <summary> Přejmenuje proměnnou $<paramref name="oldVarName"/> v instanci <see cref="StringBuilder"/>. </summary>
    /// <param name="upgrader"></param>
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="MonaUpgrader.RenameBetaWith"/>. </param>
    /// <param name="oldVarName"> Jmené původní proměnné, která se bude přejmenovávat. </param>
    /// <param name="content"> Obsah, ve kterém se proměnná přejmenovává. </param>
    public static void RenameVar(this MonaUpgrader upgrader, StringBuilder content, string? newVarName = null, string oldVarName = "beta")
    {
        if ((newVarName ??= upgrader.RenameBetaWith) is not null)
        {
            content.Replace($"${oldVarName}", $"${newVarName}");
            if (!newVarName.Contains("->"))
            {
                content.Replace($"_{oldVarName}", $"_{newVarName}");
            }
        }
    }

    /// <summary> Přejmenuje proměnnou $<paramref name="oldVarName"/>. </summary>
    /// <param name="upgrader"></param>
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="MonaUpgrader.RenameBetaWith"/>. </param>
    /// <param name="oldVarName"> Jmené původní proměnné, která se bude přejmenovávat. </param>
    /// <param name="content"> Obsah, ve kterém se proměnná přejmenovává. </param>
    /// <returns> Upravený <paramref name="content"/>. </returns>
    public static string RenameVar(this MonaUpgrader upgrader, string content, string? newVarName = null, string oldVarName = "beta")
    {
        var csb = new StringBuilder(content);
        RenameVar(upgrader, csb, newVarName, oldVarName);
        return csb.ToString();
    }

    /// <summary> Přejmenovat proměnnou $beta v souboru. </summary>
    public static FileWrapper RenameBeta(this FileWrapper file, MonaUpgrader upgrader)
    {
        upgrader.RenameVar(file.Content);
        return file;
    }

    /// <summary> Přejmenovat proměnnou ve slovníku <see cref="MonaUpgrader.FindReplace"/>. </summary>
    public static void RenameVarInFindReplace(this MonaUpgrader upgrader, string oldVarName, string newVarName)
    {
        var renamedItems = new Stack<(string, string, string)>();
        foreach (var fr in upgrader.FindReplace)
        {
            if (fr.Key.Contains(oldVarName) || fr.Value.Contains(oldVarName))
            {
                var newKey = RenameVar(upgrader, fr.Key, newVarName, oldVarName);
                var newValue = RenameVar(upgrader, fr.Value, newVarName, oldVarName);
                renamedItems.Push((fr.Key, newKey, newValue));
            }
        }
        while (renamedItems.Count > 0)
        {
            var (oldKey, newKey, newValue) = renamedItems.Pop();
            upgrader.FindReplace.Remove(oldKey);
            upgrader.FindReplace.Add(newKey, newValue);
        }
    }
}

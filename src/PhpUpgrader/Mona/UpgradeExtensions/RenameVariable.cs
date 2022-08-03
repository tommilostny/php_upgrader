using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Mona.UpgradeExtensions;

/// <summary>
/// Metody pro přejmenování proměnné v souboru.
/// </summary>
public static class RenameVariable
{
    /// <summary> Přejmenuje proměnnou $<paramref name="oldVarName"/> v instanci <see cref="StringBuilder"/>. </summary>
    /// <param name="upgrader"></param>
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="PhpUpgrader.RenameBetaWith"/>. </param>
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
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="PhpUpgrader.RenameBetaWith"/>. </param>
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

    /// <summary> Přejmenovat proměnnou ve slovníku <see cref="MonaFindReplaceHandler.Replacements"/>. </summary>
    public static void RenameVarInFindReplace(this MonaUpgrader upgrader, string oldVarName, string newVarName)
    {
        var renamedItems = new Stack<(string, string, string, string)>();
        foreach (var (find, replace) in upgrader.FindReplaceHandler.Replacements)
        {
            if (find.Contains(oldVarName) || replace.Contains(oldVarName))
            {
                var newKey = RenameVar(upgrader, find, newVarName, oldVarName);
                var newValue = RenameVar(upgrader, replace, newVarName, oldVarName);
                renamedItems.Push((find, replace, newKey, newValue));
            }
        }
        while (renamedItems.Count > 0)
        {
            var (oldKey, oldValue, newKey, newValue) = renamedItems.Pop();
            upgrader.FindReplaceHandler.Replacements.Remove((oldKey, oldValue));
            upgrader.FindReplaceHandler.Replacements.Add((newKey, newValue));
        }
    }
}

namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary> Přejmenuje proměnnou $<paramref name="oldVarName"/> v instanci <see cref="StringBuilder"/>. </summary>
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="RenameBetaWith"/>. </param>
    /// <param name="oldVarName"> Jmené původní proměnné, která se bude přejmenovávat. </param>
    /// <param name="content"> Obsah, ve kterém se proměnná přejmenovává. </param>
    public void RenameVar(StringBuilder content, string? newVarName = null, string oldVarName = "beta")
    {
        if ((newVarName ??= RenameBetaWith) is not null)
        {
            content.Replace($"${oldVarName}", $"${newVarName}");
            if (!newVarName.Contains("->"))
            {
                content.Replace($"_{oldVarName}", $"_{newVarName}");
            }
        }
    }

    /// <summary> Přejmenuje proměnnou $<paramref name="oldVarName"/>. </summary>
    /// <param name="newVarName"> Nové jméno proměnné. null => použít vlastnost <see cref="RenameBetaWith"/>. </param>
    /// <param name="oldVarName"> Jmené původní proměnné, která se bude přejmenovávat. </param>
    /// <param name="content"> Obsah, ve kterém se proměnná přejmenovává. </param>
    /// <returns> Upravený <paramref name="content"/>. </returns>
    public string RenameVar(string content, string? newVarName = null, string oldVarName = "beta")
    {
        var csb = new StringBuilder(content);
        RenameVar(csb, newVarName, oldVarName);
        return csb.ToString();
    }

    /// <summary> Přejmenovat proměnnou $beta v souboru. </summary>
    public void RenameBeta(FileWrapper file) => RenameVar(file.Content);

    /// <summary> Přejmenovat proměnnou ve slovníku <see cref="FindReplace"/>. </summary>
    protected void RenameVariableInFindReplace(string oldVarName, string newVarName)
    {
        var renamedItems = new Stack<(string, string, string)>();
        foreach (var fr in FindReplace)
        {
            if (fr.Key.Contains(oldVarName) || fr.Value.Contains(oldVarName))
            {
                var newKey = RenameVar(fr.Key, newVarName, oldVarName);
                var newValue = RenameVar(fr.Value, newVarName, oldVarName);
                renamedItems.Push((fr.Key, newKey, newValue));
            }
        }
        while (renamedItems.Count > 0)
        {
            var (oldKey, newKey, newValue) = renamedItems.Pop();
            FindReplace.Remove(oldKey);
            FindReplace.Add(newKey, newValue);
        }
    }
}

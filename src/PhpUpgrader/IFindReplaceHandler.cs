namespace PhpUpgrader;

public interface IFindReplaceHandler
{
    /// <summary> Co a čím to nahradit. </summary>
    ISet<(string find, string replace)> Replacements { get; }

    /// <summary>
    /// predelat soubory nahrazenim viz. >>> část Hledat >>> Nahradit
    /// </summary>
    sealed void UpgradeFindReplace(FileWrapper file)
    {
        foreach (var (find, replace) in Replacements)
        {
            file.Content.Replace(find, replace);
        }
    }
}

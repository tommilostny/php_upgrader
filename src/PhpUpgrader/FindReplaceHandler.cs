namespace PhpUpgrader;

public abstract class FindReplaceHandler
{
    public abstract HashSet<(string find, string replace)> Replacements { get; }

    public abstract void UpgradeFindReplace(FileWrapper file);
}

namespace PhpUpgrader;

public abstract class FindReplaceHandler
{
    public abstract ISet<(string find, string replace)> Replacements { get; }

    public abstract void UpgradeFindReplace(FileWrapper file);
}

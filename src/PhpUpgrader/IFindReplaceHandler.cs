namespace PhpUpgrader;

public interface IFindReplaceHandler
{
    ISet<(string find, string replace)> Replacements { get; }

    void UpgradeFindReplace(FileWrapper file);
}

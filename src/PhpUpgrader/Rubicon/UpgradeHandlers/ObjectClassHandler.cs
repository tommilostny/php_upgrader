namespace PhpUpgrader.Rubicon.UpgradeHandlers;

public sealed partial class ObjectClassHandler
{
    private static readonly string[] _objectFileNames = { "Object.php", "Object.class.php" };
    private readonly bool _containsObjectClass;

    public ObjectClassHandler(RubiconUpgrader upgrader)
    {
        _containsObjectClass = !string.IsNullOrWhiteSpace(upgrader.BaseFolder)
                            && !string.IsNullOrWhiteSpace(upgrader.WebName) 
                            && LookForObjectPhpFile(upgrader);
    }

    /// <summary>
    /// Aktualizace třídy Object => ObjectBase.
    /// "Object" je v novějším PHP rezervované slovo.
    /// Provádí se pouze pokud existuje nějaký soubor ".../Object.php".
    /// </summary>
    /// <remarks> + extends Object, @param Object, @property Object </remarks>
    public FileWrapper UpgradeObjectClass(FileWrapper file)
    {
        if (_containsObjectClass)
        {
            if (IsObjectFile(file.Path) && file.Content.Contains("class Object"))
            {
                file.Content.Replace("class Object", "class ObjectBase")
                            .Replace(
                                ObjectOldConstructorRegex().Replace(
                                    file.Content.ToString(),
                                    "function ObjectBase("
                ));
            }
            file.Content.Replace("extends Object", "extends ObjectBase")
                        .Replace("@param Object", "@param ObjectBase")
                        .Replace("@property  Object", "@property  ObjectBase")
                        .Replace("parent::Object", "parent::ObjectBase");
        }
        return file;
    }

    private static bool LookForObjectPhpFile(RubiconUpgrader upgrader)
    {
        return _RecursiveSearch(upgrader.WebFolder);

        static bool _RecursiveSearch(string dir)
        {
            foreach (var subdir in Directory.GetDirectories(dir))
            {
                if (_RecursiveSearch(subdir))
                {
                    return true;
                }
            }
            return Directory.GetFiles(dir, "*.php")
                .Any(f => _objectFileNames
                    .Any(of => f.EndsWith(of, StringComparison.Ordinal)));
        }
    }

    private static bool IsObjectFile(string path)
    {
        foreach (var of in _objectFileNames)
        {
            if (path.EndsWith($"{Path.DirectorySeparatorChar}{of}", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    [GeneratedRegex(@"function\s+Object\s*?\(", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex ObjectOldConstructorRegex();
}

namespace PhpUpgrader.Rubicon.UpgradeHandlers;

public sealed class ObjectClassHandler
{
    private static readonly string[] _objectFileNames = new[]
    {
        "Object.php",
        "Object.class.php",
    };
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
            if (IsObjectFile(file.Path, out var objectFileName) && file.Content.Contains("class Object"))
            {
                file.Content.Replace("class Object", "class ObjectBase");
                var content = file.Content.ToString();
                var updated = Regex.Replace(content,
                                            @"function\s+Object\s*?\(",
                                            "function ObjectBase(",
                                            RegexOptions.None,
                                            TimeSpan.FromSeconds(4));

                file.Content.Replace(content, updated);
                file.MoveOnSavePath = file.Path.Replace(objectFileName, $"{Path.DirectorySeparatorChar}ObjectBase.php", StringComparison.Ordinal);
            }
            foreach (var of in _objectFileNames)
            {
                file.Content.Replace($@"\{of}'", @"\ObjectBase.php'")
                            .Replace($@"/{of}'", @"/ObjectBase.php'")
                            .Replace($@"\{of}\""", @"\ObjectBase.php""")
                            .Replace($@"/{of}\""", @"/ObjectBase.php""");
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
        return RecursiveSearch(Path.Join(upgrader.BaseFolder, "weby", upgrader.WebName));

        static bool RecursiveSearch(string dir)
        {
            foreach (var subdir in Directory.GetDirectories(dir))
            {
                if (RecursiveSearch(subdir))
                {
                    return true;
                }
            }
            return Directory.GetFiles(dir, "*.php").Any(f => _objectFileNames.Any(of => f.EndsWith(of, StringComparison.Ordinal)));
        }
    }

    private static bool IsObjectFile(string path, out string? objectFileName)
    {
        foreach (var of in _objectFileNames)
        {
            if (path.EndsWith($"{Path.DirectorySeparatorChar}{of}", StringComparison.Ordinal))
            {
                objectFileName = of;
                return true;
            }
        }
        objectFileName = null;
        return false;
    }
}

namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class ObjectClass
{
    private static bool? _containsObjectClass;
    private static readonly string _objectFileName = $"{Path.DirectorySeparatorChar}Object.php";

    /// <summary>
    /// Aktualizace třídy Object => ObjectBase.
    /// "Object" je v novějším PHP rezervované slovo.
    /// Provádí se pouze pokud existuje nějaký soubor ".../Object.php".
    /// </summary>
    /// <remarks> + extends Object, @param Object, @property Object </remarks>
    public static FileWrapper UpgradeObjectClass(this FileWrapper file, RubiconUpgrader upgrader)
    {
        if (!(_containsObjectClass ??= LookForObjectPhpFile(upgrader)))
        {
            return file;
        }
        if (file.Path.EndsWith(_objectFileName, StringComparison.Ordinal) && file.Content.Contains("class Object"))
        {
            file.Content.Replace("class Object", "class ObjectBase");
            var content = file.Content.ToString();
            var updated = Regex.Replace(content,
                                        @"function\s+Object\s*?\(",
                                        "function ObjectBase(",
                                        RegexOptions.None,
                                        TimeSpan.FromSeconds(4));

            file.Content.Replace(content, updated);
            file.MoveOnSavePath = file.Path.Replace(_objectFileName, $"{Path.DirectorySeparatorChar}ObjectBase.php", StringComparison.Ordinal);
        }
        file.Content.Replace("extends Object", "extends ObjectBase")
                    .Replace("@param Object", "@param ObjectBase")
                    .Replace("@property  Object", "@property  ObjectBase")
                    .Replace("\\Object.php'", "\\ObjectBase.php'")
                    .Replace("/Object.php'", "/ObjectBase.php'")
                    .Replace("\\Object.php\"", "\\ObjectBase.php\"")
                    .Replace("/Object.php\"", "/ObjectBase.php\"")
                    .Replace("parent::Object", "parent::ObjectBase");
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
            return Directory.GetFiles(dir, "*.php").Any(f => f.EndsWith(_objectFileName, StringComparison.Ordinal));
        }
    }
}

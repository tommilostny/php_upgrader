namespace PhpUpgrader.Rubicon.UpgradeRoutines;

public static class ObjectClass
{
    private static bool? _containsObjectClass;

    /// <summary> Aktualizace třídy Object => ObjectBase. Provádí se pouze pokud existuje soubor classes\Object.php. </summary>
    /// <remarks> + extends Object, @param Object, @property Object </remarks>
    public static FileWrapper UpgradeObjectClass(this FileWrapper file, RubiconUpgrader upgrader)
    {
        if (!(_containsObjectClass ??= File.Exists(Path.Join(upgrader.BaseFolder, "weby", upgrader.WebName, "classes", "Object.php"))))
        {
            return file;
        }
        if (file.Path.EndsWith(Path.Join("classes", "Object.php")) && file.Content.Contains("abstract class Object"))
        {
            file.Content.Replace("abstract class Object", "abstract class ObjectBase");
            var content = file.Content.ToString();

            var updated = Regex.Replace(content, @"function\s+Object\s*\(", "function ObjectBase(");
            file.Content.Replace(content, updated);

            file.MoveOnSavePath = file.Path.Replace(Path.Join("classes", "Object.php"),
                                                    Path.Join("classes", "ObjectBase.php"));
        }
        file.Content.Replace("extends Object", "extends ObjectBase")
                    .Replace("@param Object", "@param ObjectBase")
                    .Replace("@property  Object", "@property  ObjectBase");
        return file;
    }
}

namespace PhpUpgrader.Rubicon;

public partial class RubiconUpgrader
{
    private bool? _containsObjectClass;

    /// <summary> Aktualizace třídy Object => ObjectBase. Provádí se pouze pokud existuje soubor classes\Object.php. </summary>
    /// <remarks> + extends Object, @param Object, @property Object </remarks>
    public void UpgradeObjectClass(FileWrapper file)
    {
        if (!(_containsObjectClass ??= File.Exists(Path.Join(BaseFolder, "weby", WebName, "classes", "Object.php"))))
        {
            return;
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
        file.Content.Replace("extends Object", "extends ObjectBase");
        file.Content.Replace("@param Object", "@param ObjectBase");
        file.Content.Replace("@property  Object", "@property  ObjectBase");
    }
}

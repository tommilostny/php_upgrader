namespace PhpUpgrader.Mona.UpgradeRoutines;

/// <summary>
/// 
/// </summary>
public static class UpgradeTinyMceUploadedRoutine
{
    ///<summary> PHP Parse error:  syntax error, unexpected '&amp;' on line 49` </summary>
    public static void UpgradeTinyMceUploaded(this FileWrapper file)
    {
        if (!file.Path.Contains(Path.Join("plugins", "imagemanager", "plugins", "Uploaded", "Uploaded.php")))
        {
            return;
        }
        file.Content.Replace("$this->_uploadedFile(&$man, $file1);", "$this->_uploadedFile($man, $file1);");
    }
}

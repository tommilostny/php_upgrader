namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class UpgradeTinyMceUploadedRoutine
{
    ///<summary> PHP Parse error:  syntax error, unexpected '&amp;' on line 49` </summary>
    public static FileWrapper UpgradeTinyMceUploaded(this FileWrapper file)
    {
        if (file.Path.Contains(Path.Join("plugins", "imagemanager", "plugins", "Uploaded", "Uploaded.php")))
        {
            file.Content.Replace("$this->_uploadedFile(&$man, $file1);", "$this->_uploadedFile($man, $file1);");
        }
        return file;
    }
}

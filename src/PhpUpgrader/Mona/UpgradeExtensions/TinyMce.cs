namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class TinyMce
{
    private static readonly string _uploadedPhp = Path.Join("plugins", "Uploaded", "Uploaded.php");
    private static readonly string _fileFilterPhp = Path.Join("classes", "FileSystems", "FileFilter.php");


    ///<summary>
    /// PHP Parse error:  syntax error, unexpected '&amp;' on line 49`<br />
    /// Declaration of Moxiecode_DummyFileFilter::accept(&amp;$file) must be compatible with Moxiecode_FileFilter::accept($file)
    /// </summary>
    public static FileWrapper UpgradeTinyMceUploaded(this FileWrapper file)
    {
        if (file.Path.EndsWith(_uploadedPhp, StringComparison.Ordinal))
        {
            file.Content.Replace("$this->_uploadedFile(&$man, $file1);", "$this->_uploadedFile($man, $file1);");
        }
        else if (file.Path.EndsWith(_fileFilterPhp, StringComparison.Ordinal)
                && file.Content.Contains("function accept($file)", StringComparison.Ordinal))
        {
            file.Content.Replace("function accept(&$file)", "function accept($file)");
        }
        return file;
    }
}

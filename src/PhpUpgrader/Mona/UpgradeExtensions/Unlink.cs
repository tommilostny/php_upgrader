namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class Unlink
{
    /// <summary>
    /// najit nahradi v souborech systemu ale ne v externich app a funkcich
    /// (nedelat napr. v tinymce, swiper, fancybox .. atd)
    /// - unlink >>> @unlink
    /// </summary>
    public static FileWrapper UpgradeUnlink(this FileWrapper file)
    {
        if (InvalidPathParts().All(ipp => !file.Path.Contains(ipp)))
        {
            file.Content.Replace("unlink", "@unlink")
                        .Replace("@@unlink", "@unlink");
        }
        return file;
    }

    private static IEnumerable<string> InvalidPathParts()
    {
        yield return "tiny_mce";
        yield return "swiper";
        yield return "fancybox";
        yield return "piwika";
    }
}

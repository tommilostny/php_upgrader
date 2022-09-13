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
        if (ExternalAppPathParts().All(eapp => !file.Path.Contains(eapp, StringComparison.Ordinal)))
        {
            file.Content.Replace("unlink", "@unlink")
                        .Replace("@@unlink", "@unlink");
        }
        return file;
    }

    private static IEnumerable<string> ExternalAppPathParts()
    {
        yield return "tiny_mce";
        yield return "swiper";
        yield return "fancybox";
        yield return "piwika";
        yield return "_foxydesk";
        yield return "_foxydesk_zaloha";
        yield return "foxydesk";
    }
}

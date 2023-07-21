namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class UnexpectedEOF
{
    public static FileWrapper UpgradeUnexpectedEOF(this FileWrapper file)
    {
        if (file.Path.EndsWith("_index.php", StringComparison.Ordinal))
        {
            file.Content.Replace(
                UnexpectedEOFRegex().Replace(
                    file.Content.ToString(), "<?php } ?>"
                )
            );
        }
        return file;
    }

    [GeneratedRegex(@"<\?p?h?$", RegexOptions.ExplicitCapture | RegexOptions.RightToLeft, matchTimeoutMilliseconds: 66666)]
    private static partial Regex UnexpectedEOFRegex();
}

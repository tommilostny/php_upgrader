namespace FtpSync;

internal sealed class PhpFtpRule : FtpRule
{
    public override bool IsAllowed(FtpListItem result)
    {
        return result.Name.EndsWith(".php", StringComparison.OrdinalIgnoreCase);
    }
}

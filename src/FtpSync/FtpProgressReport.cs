namespace FtpSync;

internal sealed class FtpProgressReport : IProgress<FtpProgress>
{
    private readonly string? _lineStartStr;
    private readonly FtpOp _operation;
    private string _prevPath = string.Empty;

    public FtpProgressReport(FtpOp operation)
    {
        _operation = operation;
        _lineStartStr = _operation == FtpOp.Upload ? "\r🔼 Probíhá upload\t" : "\r🔽 Probíhá download\t";
    }

    public void Report(FtpProgress value)
    {
        var path = _operation == FtpOp.Download ? value.RemotePath : value.LocalPath;
        if (!string.Equals(_prevPath, path, StringComparison.Ordinal))
        {
            _prevPath = path;
            ColoredConsole.Write(_lineStartStr).SetColor(ConsoleColor.DarkGray).Write(path).ResetColor().WriteLine("...");
        }
    }
}

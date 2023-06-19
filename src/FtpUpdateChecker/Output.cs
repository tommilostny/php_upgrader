namespace FtpUpdateChecker;

/// <summary> Internal console write methods and <seealso cref="FtpChecker"/> extensions. </summary>
internal class Output : IDisposable
{
    public StreamWriter? Writer { get; }

    public Output(string? writePath)
    {
        Writer = writePath is not null ? new StreamWriter(writePath) : null;
    }

    public void Dispose()
    {
        Writer?.Dispose();
    }

    internal async Task WriteLineToFileAsync(string message)
    {
        try
        {
            await Writer?.WriteLineAsync(message);
        }
        catch (InvalidOperationException)
        {
            await Task.Yield();
            await WriteLineToFileAsync(message);
        }
    }

    internal async Task WriteToFileAsync(string message)
    {
        try
        {
            await Writer?.WriteAsync(message);
        }
        catch (InvalidOperationException)
        {
            await Task.Yield();
            await WriteToFileAsync(message);
        }
    }

    /// <summary> Outputs formatted message to stderr. </summary>
    internal async Task WriteErrorAsync(FtpOperation? ftp, string message)
    {
        await ftp?.PrintMessageAsync(this, $"{ConsoleColor.Red}❌ {message}");

        Console.Error.WriteLine("Tip: Spusťte s parametrem --help k zobrazení nápovědy.");
        Console.Error.WriteLine("     Nebo více informací na https://github.com/tommilostny/php_upgrader/blob/master/README.md");
        Console.Error.WriteLine();
    }

    /// <summary> Outputs process completition message to stdout. </summary>
    internal async Task WriteCompletedAsync(FtpOperation ftp, string hostname)
    {
        await ftp.PrintMessageAsync(this, $"{ConsoleColor.Green}✅ Proces kontroly FTP '{hostname}' dokončen.");
    }

    /// <summary>
    /// Prints current status of given <seealso cref="FtpChecker"/>.
    /// </summary>
    /// <returns> Length of the message string. </returns>
    internal static int WriteStatus(FtpChecker fc)
    {
        var messageBuilder = new StringBuilder()
            .Append($"Zkontrolováno {fc.FileCount} souborů v {fc.FolderCount} adresářích.")
            .Append($" Nalezeno {fc.FoundCount} souborů modifikovaných po {fc.FromDate}")
            .Append($" ({fc.PhpFoundCount} z nich je PHP).");

        Console.Write(messageBuilder);
        return messageBuilder.Length;
    }

    /// <summary>
    /// Outputs formatted message about new found file.
    /// </summary>
    /// <param name="fc">Running FTP checker intance.</param>
    /// <param name="fileInfo">WinSCP file info.</param>
    /// <param name="isPhp">PHP files are printed to console in cyan, others in default color.</param>
    internal async Task WriteFoundFileAsync(FtpChecker? fc, RemoteFileInfo fileInfo, bool isPhp)
    {
        if (fc is not null)
        {
            await fc.PrintMessageAsync(this, string.Empty);

            var numberStr = $"{fc.FoundCount}.";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(numberStr);
            await WriteToFileAsync(numberStr);
            await OutputSpacesAsync(numberStr.Length, 6);
        }
        var timeStr = fileInfo.LastWriteTime.ToString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(timeStr);
        await WriteToFileAsync(timeStr);
        await OutputSpacesAsync(timeStr.Length + 6, 29);

        if (isPhp)
            Console.ForegroundColor = ConsoleColor.Cyan;
        else
            Console.ResetColor();

        Console.WriteLine(fileInfo.FullName);
        await WriteLineToFileAsync(fileInfo.FullName);

        if (isPhp)
            Console.ResetColor();
    }

    internal async Task WriteFilesDiffAsync(FtpChecker fc, string host1, string host2, RemoteFileInfo file1, RemoteFileInfo? file2)
    {
        var hostnameEndIndex = (host1.Length > host2.Length ? host1.Length : host2.Length) + 8;

        await fc.PrintMessageAsync(this, string.Empty);
        Console.Write(host1);
        await WriteToFileAsync(host1);
        await OutputSpacesAsync(host1.Length + 5, hostnameEndIndex);
        await WriteFoundFileAsync(null, file1, isPhp: false);

        const string spaces = "     ";
        Console.Write(spaces);
        Console.Write(host2);
        await WriteToFileAsync(spaces);
        await WriteToFileAsync(host2);
        await OutputSpacesAsync(host2.Length + 5, hostnameEndIndex);

        if (file2 is not null)
        {
            await WriteFoundFileAsync(null, file2, isPhp: false);
            return;
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        var doesntExistMessage = $"{file1.Name} neexistuje na {host2}.";
        Console.WriteLine(doesntExistMessage);
        Console.ResetColor();
        await WriteLineToFileAsync(doesntExistMessage);
    }

    internal static int WriteStatusNoDate(FtpChecker fc, int totalFilesCount)
    {
        var message = $"Zkontrolováno {fc.FileCount}/{totalFilesCount} souborů (liší se {fc.FoundCount} z nich).";
        Console.Write(message);
        return message.Length;
    }

    private async Task OutputSpacesAsync(int fromIndex, int toIndex)
    {
        for (var i = fromIndex; i < toIndex; i++)
        {
            Console.Write(' ');
            await WriteToFileAsync(" ");
        }
    }
}

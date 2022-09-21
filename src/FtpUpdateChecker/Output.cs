namespace FtpUpdateChecker;

/// <summary> Internal console write methods and <seealso cref="FtpChecker"/> extensions. </summary>
internal static class Output
{
    /// <summary> Outputs formatted message to stderr. </summary>
    internal static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"❌ {message}");
        Console.ResetColor();
        Console.Error.WriteLine("Tip: Spusťte s parametrem --help k zobrazení nápovědy.");
        Console.Error.WriteLine("     Nebo více informací na https://github.com/tommilostny/php_upgrader/blob/master/README.md");
        Console.Error.WriteLine();
    }

    /// <summary> Outputs process completition message to stdout. </summary>
    internal static void WriteCompleted(FtpOperation ftp, string hostname, string? phpLogFilePath = null, uint phpFoundCount = 0)
    {
        Console.WriteLine();
        ftp.PrintName();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Proces kontroly FTP '{hostname}' dokončen.");
        Console.ResetColor();
        if (phpFoundCount > 0 && phpLogFilePath is not null)
        {
            ftp.PrintName();
            Console.WriteLine("Nalezené PHP soubory byly zaznamenány do souboru:");
            Console.WriteLine(new FileInfo(phpLogFilePath).FullName);
        }
        Console.WriteLine();
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
    /// <param name="phpLogFilePath">If <paramref name="isPhp"/> then write line to this file.</param>
    internal static void WriteFoundFile(FtpChecker? fc, RemoteFileInfo fileInfo, bool isPhp, string? phpLogFilePath)
    {
        StreamWriter? sw = isPhp && phpLogFilePath is not null ? new(phpLogFilePath, append: true) : null;

        if (fc is not null)
        {
            fc.PrintName();

            var numberStr = $"{fc.FoundCount}.";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(numberStr);
            sw?.Write(numberStr);

            OutputSpaces(numberStr.Length, 6, sw);
        }
        var timeStr = fileInfo.LastWriteTime.ToString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(timeStr);
        sw?.Write(timeStr);

        OutputSpaces(timeStr.Length + 6, 29, sw);

        if (isPhp)
            Console.ForegroundColor = ConsoleColor.Cyan;
        else
            Console.ResetColor();

        Console.WriteLine(fileInfo.FullName);
        sw?.WriteLine(fileInfo.FullName);
        sw?.Close();

        if (isPhp)
            Console.ResetColor();
    }

    internal static void WriteFilesDiff(FtpChecker fc, string host1, string host2, RemoteFileInfo file1, RemoteFileInfo? file2)
    {
        var hostnameEndIndex = (host1.Length > host2.Length ? host1.Length : host2.Length) + 8;

        fc.PrintName();
        Console.Write(host1);
        OutputSpaces(host1.Length + 5, hostnameEndIndex, null);
        WriteFoundFile(null, file1, isPhp: false, null);

        Console.Write("     ");
        Console.Write(host2);
        OutputSpaces(host2.Length + 5, hostnameEndIndex, null);

        if (file2 is not null)
        {
            WriteFoundFile(null, file2, isPhp: false, null);
            return;
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"{file1.Name} neexistuje na {host2}.");
        Console.ResetColor();
    }

    internal static int WriteStatusNoDate(FtpChecker fc, int totalFilesCount)
    {
        var message = $"Zkontrolováno {fc.FileCount}/{totalFilesCount} souborů (liší se {fc.FoundCount} z nich).";
        Console.Write(message);
        return message.Length;
    }

    private static void OutputSpaces(int fromIndex, int toIndex, StreamWriter? sw)
    {
        for (var i = fromIndex; i < toIndex; i++)
        {
            Console.Write(' ');
            sw?.Write(' ');
        }
    }
}

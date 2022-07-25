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
        Console.Error.WriteLine("Spusťte s parametrem --help k zobrazení nápovědy.\n");
    }

    /// <summary> Outputs process completition message to stdout. </summary>
    internal static void WriteCompleted(string phpLogFilePath, uint phpFoundCount)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n\n\r✅ Proces dokončen.");
        Console.ResetColor();
        if (phpFoundCount > 0)
        {
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
    /// <param name="messageLength">The lenghth of space printed message needs to overwrite with spaces.</param>
    /// <param name="isPhp">PHP files are printed to console in cyan, others in default color.</param>
    /// <param name="phpLogFilePath">If <paramref name="isPhp"/> then write line to this file.</param>
    internal static void WriteFoundFile(FtpChecker fc, RemoteFileInfo fileInfo, int messageLength, bool isPhp, string phpLogFilePath)
    {
        StreamWriter? sw = isPhp ? new(phpLogFilePath, append: true) : null;

        var numberStr = $"{fc.FoundCount}.";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(numberStr);
        sw?.Write(numberStr);

        _OutputSpaces(numberStr.Length, 6, sw);

        var timeStr = fileInfo.LastWriteTime.ToString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(timeStr);
        sw?.Write(timeStr);

        _OutputSpaces(timeStr.Length + 6, 29, sw);

        if (isPhp)
            Console.ForegroundColor = ConsoleColor.Cyan;
        else
            Console.ResetColor();

        Console.Write(fileInfo.FullName);
        sw?.WriteLine(fileInfo.FullName);
        sw?.Close();

        if (isPhp)
            Console.ResetColor();

        _OutputSpaces(fileInfo.FullName.Length + 27, messageLength, null);
        Console.WriteLine();

        static void _OutputSpaces(int fromIndex, int toIndex, StreamWriter? sw)
        {
            for (var i = fromIndex; i < toIndex; i++)
            {
                Console.Write(' ');
                sw?.Write(' ');
            }
        }
    }
}

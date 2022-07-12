namespace FtpUpdateChecker;

/// <summary> Internal console write methods and <seealso cref="FtpChecker"/> extensions. </summary>
internal static class ConsoleOutput
{
    /// <summary> Outputs formatted message to stderr. </summary>
    internal static void WriteErrorMessage(string message)
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"❌ {message}");
        Console.ForegroundColor = defaultColor;
        Console.Error.WriteLine("Spusťte s parametrem --help k zobrazení nápovědy.\n");
    }

    /// <summary> Outputs process completition message to stdout. </summary>
    internal static void WriteCompletedMessage()
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n\n\r✅ Proces dokončen.\n");
        Console.ForegroundColor = defaultColor;
    }

    /// <summary>
    /// <seealso cref="FtpChecker"/> extension method that prints its current status.
    /// </summary>
    /// <returns> Length of the message string. </returns>
    internal static int WriteStatus(this FtpChecker fc)
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
    /// <param name="isPhp">PHP files are printed to console in cyan, others in <see cref="FtpChecker.DefaultColor"/>.</param>
    internal static void WriteFoundFile(this FtpChecker fc, RemoteFileInfo fileInfo, int messageLength, bool isPhp)
    {
        var numberStr = $"{fc.FoundCount}.";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(numberStr);

        _OutputSpaces(numberStr.Length, 5);

        var timeStr = fileInfo.LastWriteTime.ToString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(timeStr);

        _OutputSpaces(timeStr.Length + 6, 28);

        Console.ForegroundColor = isPhp ? ConsoleColor.Cyan : fc.DefaultColor;
        Console.Write(fileInfo.FullName);
        Console.ForegroundColor = fc.DefaultColor;

        _OutputSpaces(fileInfo.FullName.Length + 27, messageLength);
        Console.WriteLine();

        static void _OutputSpaces(int fromIndex, int toIndex)
        {
            for (int i = fromIndex; i < toIndex; i++)
                Console.Write(' ');
        }
    }
}

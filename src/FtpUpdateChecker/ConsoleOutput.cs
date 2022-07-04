namespace FtpUpdateChecker;

/// <summary>  </summary>
internal static class ConsoleOutput
{
    /// <summary>  </summary>
    internal static void WriteErrorMessage(string message)
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"\n❌ {message}");
        Console.ForegroundColor = defaultColor;
        Console.Error.WriteLine("\tSpusťte s parametrem --help k zobrazení nápovědy.");
    }

    /// <summary>  </summary>
    internal static void WriteCompletedMessage()
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n\n✔️ Proces dokončen.");
        Console.ForegroundColor = defaultColor;
    }
}

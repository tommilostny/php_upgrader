using System;

namespace FtpUpdateChecker
{
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
            Console.Error.WriteLine("   Run with --help to display additional information.");
        }

        /// <summary>  </summary>
        internal static void WriteCompletedMessage()
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n✔️ Process completed.");
            Console.ForegroundColor = defaultColor;
        }
    }
}

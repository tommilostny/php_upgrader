using System;
using System.Collections.Generic;
using WinSCP;

namespace FtpUpdateChecker
{
    class Program
    {
        static void WriteStatus(uint fileCount, uint folderCount, uint foundCount, string displayDate)
        {
            Console.Write($"Checked {fileCount} file(s) in {folderCount} folder(s). ");
            Console.Write($"Found {foundCount} file(s) modified after {displayDate}.");
        }

        static void WriteFoundFile(RemoteFileInfo fileInfo, ref uint foundCount, ConsoleColor defaultColor)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{++foundCount}. ");

            Console.ForegroundColor = defaultColor;
            Console.Write(fileInfo.FullName);

            for (int i = fileInfo.FullName.Length; i < 95; i++) Console.Write(" ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n\t{fileInfo.LastWriteTime}");
            Console.ForegroundColor = defaultColor;
        }

        /// <summary>
        /// Nástroj pro kontrolu nových souborů na FTP serveru po určitém datu.
        /// </summary>
        /// <param name="username">Uživatelské jméno na FTP.</param>
        /// <param name="password">Heslo pro přístup na FTP.</param>
        /// <param name="host">Url serveru.</param>
        /// <param name="path">Root složka skenovaného webu (musí být správně začínající lomítkem).</param>
        /// <param name="year">Soubory nad tímto rokem se zobrazí jako aktualizované.</param>
        /// <param name="month">Soubory nad tímto měsícem se zobrazí jako aktualizované.</param>
        /// <param name="day">Soubory nad tímto dnem se zobrazí jako aktualizované.</param>
        static void Main(string? username = null, string? password = null, string host = "mcrai.vshosting.cz",
            string path = "/httpdocs", int year = 2021, int month = 7, int day = 8)
        {
            if (username is null || password is null)
            {
                Console.Error.WriteLine("Missing arguments --username or --password argument.");
                Console.Error.WriteLine("Run with --help to display additional information.");
                return;
            }
            var defaultColor = Console.ForegroundColor;
            var date = new DateTime(year, month, day);
            var displayDate = date.ToShortDateString();

            var sessionOptions = new SessionOptions //Setup session options
            {
                Protocol = Protocol.Ftp,
                HostName = host,
                UserName = username,
                Password = password,
                FtpSecure = FtpSecure.Explicit
            };
            using var session = new Session();
            Console.WriteLine($"Connecting to {username}@{host} ...");

            try //Connect
            {
                session.Open(sessionOptions);
            }
            catch (SessionRemoteException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("❌ Unable to open session with entered username and password.");
                Console.ForegroundColor = defaultColor;
                return;
            }

            var enumerationOptions = EnumerationOptions.EnumerateDirectories | EnumerationOptions.AllDirectories;
            var fileInfos = session.EnumerateRemoteFiles(path, null, enumerationOptions);

            Console.WriteLine($"Connection successful! Checking all files in {path} for updates after {displayDate}.\n");
            uint foundCount = 0;
            uint fileCount = 0;
            uint folderCount = 0;

            foreach (var fileInfo in fileInfos)
            {
                Console.Write("\r");

                if (fileInfo.IsDirectory)
                {
                    WriteStatus(fileCount, ++folderCount, foundCount, displayDate);
                    continue;
                }
                if (fileInfo.LastWriteTime >= date)
                {
                    WriteFoundFile(fileInfo, ref foundCount, defaultColor);
                }
                WriteStatus(++fileCount, folderCount, foundCount, displayDate);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✔️ Process completed.");
            Console.ForegroundColor = defaultColor;
        }
    }
}

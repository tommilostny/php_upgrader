using System;
using WinSCP;

namespace FtpUpdateChecker
{
    class Program
    {
        /// <summary>
        /// Nástroj pro kontrolu nových souborů na FTP serveru po určitém datu.
        /// </summary>
        /// <param name="username">Uživatelské jméno na FTP.</param>
        /// <param name="password">Heslo pro přístup na FTP.</param>
        /// <param name="host">Url serveru.</param>
        /// <param name="path">Root složka skenovaného webu.</param>
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

            //Connect
            session.Open(sessionOptions);

            //Enumerate files
            var options = EnumerationOptions.EnumerateDirectories | EnumerationOptions.AllDirectories;
            var fileInfos = session.EnumerateRemoteFiles(path, null, options);

            Console.WriteLine($"Connection successful! Checking all PHP files in {path} for updates after {displayDate}.\n");
            int foundCount = 0;
            int fileCount = 0;
            int folderCount = 0;

            foreach (var fileInfo in fileInfos)
            {
                Console.Write("\r");
                if (!fileInfo.IsDirectory)
                {
                    if (fileInfo.LastWriteTime >= date && fileInfo.Name.TrimEnd().EndsWith(".php"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"{++foundCount}. ");

                        Console.ForegroundColor = defaultColor;
                        Console.Write(fileInfo.FullName);

                        for (int i = fileInfo.FullName.Length; i < 95; i++) Console.Write(" ");
                        Console.Write($"\n{fileInfo.LastWriteTime}\n");
                    }
                    fileCount++;
                }
                else folderCount++;
                
                Console.Write($"Checked {fileCount} file(s) in {folderCount} folder(s). Found {foundCount} PHP file(s) modified after {displayDate}.");
            }
            Console.WriteLine("\nProcess completed.");
        }
    }
}

using System;
using System.IO;

namespace FtpUpdateChecker
{
    class Program
    {
        static void WriteErrorMessage(string message)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"\n❌ {message}");
            Console.ForegroundColor = defaultColor;
            Console.Error.WriteLine("   Run with --help to display additional information.");
        }

        static void WriteCompletedMessage()
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n✔️ Process completed.");
            Console.ForegroundColor = defaultColor;
        }

        static bool LoadLoginInfo(string username, ref string password, bool useLoginsFile, string baseFolder)
        {
            if (useLoginsFile)
            {
                try
                {
                    password = _LoadPasswordFromFile(baseFolder, username);
                }
                catch (Exception exception)
                {
                    WriteErrorMessage(exception.Message);
                    return false;
                }
            }
            else if (username is null || password is null)
            {
                WriteErrorMessage("Missing arguments --username or --password argument.");
                return false;
            }
            return true;

            static string _LoadPasswordFromFile(string baseFolder, string? username)
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentNullException(nameof(username), "Argument is required while in --use-logins-file mode.");

                using var sr = new StreamReader($"{baseFolder}ftp_logins.txt");

                while (!sr.EndOfStream)
                {
                    var login = sr.ReadLine().Split(" : ");

                    if (login[0].Trim() == username)
                        return login[1].Trim();
                }
                throw new InvalidOperationException($"Unable to load password from {baseFolder}ftp_logins.txt for user {username}.");
            }
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
        /// <param name="useLoginsFile">Použít heslo ze souboru ftp_logins.txt k zadanému uživatelskému jménu.</param>
        /// <param name="baseFolder">Kde je soubor ftp_logins.txt?</param>
        /// <param name="webName">Název složky v '{baseFolder}\weby'. Získá datum vytvoření.</param>
        static void Main(string? username = null, string? password = null, string host = "mcrai.vshosting.cz",
            string path = "/httpdocs", int year = 2021, int month = 7, int day = 8, bool useLoginsFile = false,
            string baseFolder = @"C:\McRAI\", string? webName = null)
        {
            if (!LoadLoginInfo(username, ref password, useLoginsFile, baseFolder))
                return;

            var date = webName switch
            {
                null => new(year, month, day),
                _ => Directory.GetCreationTime($@"{baseFolder}\weby\{webName}")
            };
            var checker = new FtpChecker(username, password, host, date);

            try //Run update check
            {
                checker.Run(path);
            }
            catch (InvalidOperationException exc)
            {
                WriteErrorMessage(exc.Message);
                return;
            }
            WriteCompletedMessage();
        }
    }
}

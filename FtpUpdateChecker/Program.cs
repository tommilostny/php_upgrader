﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            for (int i = fileInfo.FullName.Length; i < 95; i++)
                Console.Write(" ");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n\t{fileInfo.LastWriteTime}");
            Console.ForegroundColor = defaultColor;
        }

        static void WriteErrorMessage(string message, ConsoleColor defaultColor)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"\n❌ {message}");
            Console.ForegroundColor = defaultColor;
            Console.Error.WriteLine("   Run with --help to display additional information.");
        }

        static string LoadPasswordFromFile(string baseFolder, string username)
        {
            var lines = File.ReadAllLines($"{baseFolder}ftp_logins.txt");
            var allLogins = new List<string[]>();

            foreach (var line in lines)
            {
                allLogins.Add(line.Split(" : ").Select(login => login.Trim()).ToArray());
            }
            return allLogins.First(login => login[0] == username)[1];
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
        static void Main(string? username = null, string? password = null, string host = "mcrai.vshosting.cz",
            string path = "/httpdocs", int year = 2021, int month = 7, int day = 8, bool useLoginsFile = false, string baseFolder = @"C:\McRAI\")
        {
            var defaultColor = Console.ForegroundColor;

            if (useLoginsFile)
            {
                try
                {
                    password = LoadPasswordFromFile(baseFolder, username ?? throw new ArgumentNullException(nameof(username), "Argument is required while in --use-logins-file mode."));
                }
                catch (InvalidOperationException)
                {
                    WriteErrorMessage($"Unable to load password from {baseFolder}ftp_logins.txt for user {username}.", defaultColor);
                    return;
                }
                catch (ArgumentNullException exception)
                {
                    WriteErrorMessage(exception.Message, defaultColor);
                    return;
                }
            }
            else if (username is null || password is null)
            {
                WriteErrorMessage("Missing arguments --username or --password argument.", defaultColor);
                return;
            }

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
                WriteErrorMessage("Unable to open session with entered username and password.", defaultColor);
                return;
            }

            Console.WriteLine($"Connection successful! Checking all files in {path} for updates after {displayDate}.\n");
            var enumerationOptions = WinSCP.EnumerationOptions.EnumerateDirectories | WinSCP.EnumerationOptions.AllDirectories;
            var fileInfos = session.EnumerateRemoteFiles(path, null, enumerationOptions);

            uint foundCount = 0;
            uint fileCount = 0;
            uint folderCount = 0;

            try //Enumerate files
            {
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
            }
            catch (SessionRemoteException)
            {
                WriteErrorMessage($"Entered path \"{path}\" doesn't exist on the server.", defaultColor);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n✔️ Process completed.");
            Console.ForegroundColor = defaultColor;
        }
    }
}
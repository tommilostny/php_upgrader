using System;
using System.Collections.Generic;
using System.Linq;
using WinSCP;

namespace ftp_update_checker
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            string host = "mcrai2.vshosting.cz";
            DateTime date = new DateTime(2019, 7, 1);

            if (args.Count() == 2)
            {
                // Setup session options
                var sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = host,
                    UserName = args[0],
                    Password = args[1],
                    FtpSecure = FtpSecure.Explicit
                };

                Console.WriteLine(string.Format("Connecting to {0}@{1} ...", args[0], host));
                using (var session = new Session())
                {
                    // Connect
                    session.Open(sessionOptions);

                    // Enumerate files
                    var options = EnumerationOptions.EnumerateDirectories | EnumerationOptions.AllDirectories;

                    IEnumerable<RemoteFileInfo> fileInfos = session.EnumerateRemoteFiles("/", null, options);

                    Console.WriteLine("Connection successful! Checking all PHP files for updates after " + date.ToShortDateString() + ".\n");
                    int found_count = 0;
                    int file_count = 0;
                    int folder_count = 0;

                    foreach (var fileInfo in fileInfos)
                    {
                        Console.Write("\r");
                        if (!fileInfo.IsDirectory)
                        {
                            if (fileInfo.LastWriteTime >= date && fileInfo.Name.TrimEnd().EndsWith(".php"))
                            {
                                found_count++;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(found_count.ToString() + ". ");

                                Console.ForegroundColor = defaultColor;
                                Console.Write(fileInfo.FullName);
                                for (int i = fileInfo.FullName.Length; i < 95; i++) Console.Write(" ");
                                Console.Write("\n" + fileInfo.LastWriteTime + "\n");
                            }
                            file_count++;
                        }
                        else folder_count++;
                        Console.Write(string.Format("Checked {0} file(s) in {1} folder(s). Found {2} PHP file(s) modified after {3}.", file_count, folder_count, found_count, date.ToShortDateString()));
                    }
                    Console.WriteLine("\nProcess completed.");
                }
            }
            else Console.WriteLine("Missing arguments.\nUSAGE: ftp_update_checker [USERNAME] [PASSWORD]\nHostname is set to " + host);
        }
    }
}
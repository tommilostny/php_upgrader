using System;
using WinSCP;

namespace FtpUpdateChecker
{
    /// <summary>  </summary>
    public class FtpChecker
    {
        /// <summary>  </summary>
        public DateTime FromDate { get; }

        /// <summary>  </summary>
        public string DisplayDate { get => $"{FromDate.ToShortDateString()}, {FromDate.ToShortTimeString()}"; }

        /// <summary>  </summary>
        public uint FileCount { get; private set; }

        /// <summary>  </summary>
        public uint FolderCount { get; private set; }

        /// <summary>  </summary>
        public uint FoundCount { get; private set; }

        /// <summary>  </summary>
        public uint PhpFilesCount { get; private set; }

        private ConsoleColor DefaultColor { get; } = Console.ForegroundColor;

        private SessionOptions SessionOptions { get; }

        /// <summary>  </summary>
        public FtpChecker(string username, string password, string hostname, DateTime fromDate)
        {
            SessionOptions = new SessionOptions
            {
                Protocol = Protocol.Ftp,
                HostName = hostname,
                UserName = username,
                Password = password,
                FtpSecure = FtpSecure.Explicit
            };
            FromDate = fromDate;
        }

        /// <summary>  </summary>
        /// <exception cref="InvalidOperationException" />
        public void Run(string path)
        {
            Console.WriteLine($"Connecting to {SessionOptions.UserName}@{SessionOptions.HostName} ...");
            using var session = new Session();

            try //Connect
            {
                session.Open(SessionOptions);
            }
            catch (SessionRemoteException exc)
            {
                throw new InvalidOperationException("Unable to open session with entered username and password.", exc);
            }

            Console.WriteLine($"Connection successful! Checking all files in {path} for updates after {DisplayDate}.\n");
            var enumerationOptions = EnumerationOptions.EnumerateDirectories | EnumerationOptions.AllDirectories;
            var fileInfos = session.EnumerateRemoteFiles(path, null, enumerationOptions);

            try //Enumerate files
            {
                foreach (var fileInfo in fileInfos)
                {
                    Console.Write("\r");

                    if (fileInfo.IsDirectory)
                    {
                        FolderCount++;
                        WriteStatus();
                        continue;
                    }
                    if (fileInfo.LastWriteTime >= FromDate)
                    {
                        PhpFilesCount += Convert.ToUInt32(fileInfo.FullName.Contains(".php"));
                        WriteFoundFile(fileInfo);
                    }
                    FileCount++;
                    WriteStatus();
                }
            }
            catch (SessionRemoteException exc)
            {
                throw new InvalidOperationException($"Entered path \"{path}\" doesn't exist on the server.", exc);
            }
        }

        /// <summary>  </summary>
        private void WriteStatus()
        {
            Console.Write($"Checked {FileCount} file(s) in {FolderCount} folder(s). " +
                $"Found {FoundCount} file(s) modified after {DisplayDate} ({PhpFilesCount} of them are PHP).");
        }

        /// <summary>  </summary>
        private void WriteFoundFile(RemoteFileInfo fileInfo)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{++FoundCount}. ");

            Console.ForegroundColor = DefaultColor;
            Console.Write(fileInfo.FullName);

            for (int i = fileInfo.FullName.Length; i < 110; i++)
                Console.Write(" ");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n\t{fileInfo.LastWriteTime}");
            Console.ForegroundColor = DefaultColor;
        }
    }
}

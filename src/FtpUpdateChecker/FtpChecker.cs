using System;
using WinSCP;

namespace FtpUpdateChecker
{
    /// <summary> Třída nad knihovnou WinSCP kontrolující soubory na FTP po určitém datu. </summary>
    public class FtpChecker : IDisposable
    {
        /// <summary> Datum, od kterého hlásit změnu. </summary>
        public DateTime FromDate { get; }

        /// <summary> Datum k zobrazení v textu. </summary>
        public string DisplayDate { get => $"{FromDate.ToShortDateString()}, {FromDate.ToShortTimeString()}"; }

        /// <summary> Celkový počet souborů. </summary>
        public uint FileCount { get; private set; }

        /// <summary> Celkový počet složek. </summary>
        public uint FolderCount { get; private set; }

        /// <summary> Počet souborů přidaných po datu <see cref="FromDate"/>. </summary>
        public uint FoundCount { get; private set; }

        /// <summary> Počet PHP souborů přidaných po datu <see cref="FromDate"/>. </summary>
        public uint PhpFoundCount { get; private set; }

        private ConsoleColor DefaultColor { get; } = Console.ForegroundColor;

        private SessionOptions SessionOptions { get; }

        private Session Session { get; } = new();

        /// <summary> Inicializace sezení spojení WinSCP, nastavení data. </summary>
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

        /// <summary> Spustit procházení všech souborů na FTP serveru v zadané cestě. </summary>
        public void Run(string path)
        {
            if (!Session.Opened)
            {
                Console.WriteLine($"Connecting to {SessionOptions.UserName}@{SessionOptions.HostName} ...");
                try //Connect
                {
                    Session.Open(SessionOptions);
                    Console.Write("Connection successful! ");
                }
                catch (SessionRemoteException)
                {
                    ConsoleOutput.WriteErrorMessage("Unable to open session with entered username and password.");
                    return;
                }
            }
            else Console.WriteLine();

            Console.WriteLine($"Checking all files in {path} for updates after {DisplayDate}.");
            var enumerationOptions = EnumerationOptions.EnumerateDirectories | EnumerationOptions.AllDirectories;
            var fileInfos = Session.EnumerateRemoteFiles(path, null, enumerationOptions);

            FileCount = FolderCount = PhpFoundCount = FoundCount = 0;
            WriteStatus();
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
                        FoundCount++;
                        PhpFoundCount += Convert.ToUInt32(fileInfo.FullName.EndsWith(".php"));
                        WriteFoundFile(fileInfo);
                    }
                    FileCount++;
                    WriteStatus();
                }
            }
            catch (SessionRemoteException)
            {
                ConsoleOutput.WriteErrorMessage($"Entered path \"{path}\" doesn't exist on the server.");
            }
            ConsoleOutput.WriteCompletedMessage();
        }

        private void WriteStatus()
        {
            Console.Write($"Checked {FileCount} file(s) in {FolderCount} folder(s). " +
                $"Found {FoundCount} file(s) modified after {DisplayDate} ({PhpFoundCount} of them are PHP).");
        }

        private void WriteFoundFile(RemoteFileInfo fileInfo)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{FoundCount}. ");

            Console.ForegroundColor = DefaultColor;
            Console.Write(fileInfo.FullName);

            for (int i = fileInfo.FullName.Length; i < 110; i++)
                Console.Write(" ");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n\t{fileInfo.LastWriteTime}");
            Console.ForegroundColor = DefaultColor;
        }

        /// <summary> Uzavřít spojení k FTP serveru. </summary>
        public void Dispose()
        {
            if (Session.Opened)
            {
                Session.Close();
            }
            GC.SuppressFinalize(this);
        }
    }
}

using System;
using System.IO;

namespace FtpUpdateChecker
{
    /// <summary> Třída obsahující informace o přihlašovacích údajích k FTP. </summary>
    public class FtpLoginParser
    {
        /// <summary> Uživatelské jméno. </summary>
        public string? Username { get; private set; }

        /// <summary> Heslo. </summary>
        public string? Password { get; private set; }

        /// <summary> Cesty v root na FTP serveru ke kontrole. </summary>
        public string[] Paths { get; private set; }

        /// <summary> Inicializace uživatelského jména a hesla. </summary>
        public FtpLoginParser(string username, string password)
        {
            Username = username;
            Password = password;
        }

        /// <summary> Zkusit načíst uživatelské jméno z názvu webu. </summary>
        /// <remarks> Použit výchozí parametr userName, pokud je webName prázdné/null. </remarks>
        public FtpLoginParser(string? webName, string password, string defaultUsername)
            : this(UsernameFromWebName(webName, defaultUsername), password)
        {
        }

        /// <summary> Načíst a zkontrolovat přihlašovací údaje dle parametru useLoginsFile. </summary>
        public bool LoadLoginInfo(bool useLoginsFile, string baseFolder)
        {
            if (useLoginsFile)
            {
                try
                {
                    Password = _LoadPasswordFromFile(out var paths);
                    Paths = paths;
                    return true;
                }
                catch (Exception exception)
                {
                    ConsoleOutput.WriteErrorMessage(exception.Message);
                    return false;
                }
            }
            else if (Username is null || Password is null)
            {
                ConsoleOutput.WriteErrorMessage("Missing arguments --username or --password argument.");
                return false;
            }
            return true;

            //heslo, pole cest oddělené čárkou (více webů na jednom ftp)
            string _LoadPasswordFromFile(out string[] paths)
            {
                if (string.IsNullOrWhiteSpace(Username))
                    throw new ArgumentNullException(nameof(Username), "Argument is required while in --use-logins-file mode.");

                using var sr = new StreamReader($"{baseFolder}ftp_logins.txt");

                while (!sr.EndOfStream)
                {
                    var login = sr.ReadLine().Split(" : ");

                    if (login[0].Trim() == Username)
                    {
                        paths = login[2].Split(',');
                        return login[1].Trim();
                    }
                }
                throw new InvalidOperationException($"Unable to load password from {baseFolder}ftp_logins.txt for user {Username}.");
            }
        }

        /// <param name="webName"> Jméno webu, ze kterého tvořit uživatelské jméno. </param>
        /// <param name="default"> Výchozí hodnota uživatelského jména, použita pokud je webName prázdné/null. </param>
        /// <returns> Uživatelské jméno s prefixem 'tom-' nebo výchozí hodnota. </returns>
        private static string UsernameFromWebName(string? webName, string @default) => webName switch
        {
            var w when string.IsNullOrWhiteSpace(w) => @default,
            { Length: >= 12 } => $"tom-{webName[0..12]}",
            _ => $"tom-{webName}"
        };
    }
}

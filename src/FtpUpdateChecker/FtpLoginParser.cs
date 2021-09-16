﻿using System;
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
        /// <remarks> Použit výchozí parametr userName místo webName, pokud není prázdné/null. </remarks>
        public FtpLoginParser(string? webName, string? password, string? overrideUsername, string baseFolder)
            : this(UsernameFromWebName(webName, overrideUsername), password)
        {
            LoadLoginInfo(baseFolder);
        }

        /// <summary> Načíst a zkontrolovat přihlašovací údaje dle parametru useLoginsFile. </summary>
        private void LoadLoginInfo(string baseFolder)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                throw new ArgumentNullException(nameof(Username), "Argument is required while in --use-logins-file mode.");
            }
            if (Password is null)
            {
                Password = _LoadPasswordFromFile(out var paths);
                Paths = paths;
            }

            //heslo, pole cest oddělené čárkou (více webů na jednom ftp)
            string _LoadPasswordFromFile(out string[] paths)
            {
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
        /// <param name="override"> Výchozí hodnota uživatelského jména, použita místo webName, pokud není prázdné/null. </param>
        /// <returns> Uživatelské jméno s prefixem 'tom-' nebo výchozí hodnota. </returns>
        private static string UsernameFromWebName(string? webName, string? @override) => @override switch
        {
            null => webName switch
            {
                var w when string.IsNullOrWhiteSpace(w) => throw new ArgumentNullException(nameof(webName)),
                { Length: >= 12 } => $"tom-{webName[..12]}",
                _ => $"tom-{webName}"
            },
            _ => @override
        };
    }
}

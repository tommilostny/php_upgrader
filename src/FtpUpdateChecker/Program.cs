using System.IO;

namespace FtpUpdateChecker
{
    class Program
    {
        const int DefaultYear = 2021;
        const int DefaultMonth = 7;
        const int DefaultDay = 8;

        /// <summary>
        /// Nástroj pro kontrolu nových souborů na FTP serveru po určitém datu.
        /// </summary>
        /// <param name="username">Uživatelské jméno na FTP. Není nutné zadávat, pokud je zadán argument --web-name (načte se dle názvu složky s prefixem 'tom-'). </param>
        /// <param name="password">Heslo pro přístup na FTP.</param>
        /// <param name="host">Url serveru.</param>
        /// <param name="path">Root složka skenovaného webu (musí být správně začínající lomítkem). Ignorován, pokud je zadán parametr --use-logins-file.</param>
        /// <param name="year">Soubory nad tímto rokem se zobrazí jako aktualizované.</param>
        /// <param name="month">Soubory nad tímto měsícem se zobrazí jako aktualizované.</param>
        /// <param name="day">Soubory nad tímto dnem se zobrazí jako aktualizované.</param>
        /// <param name="useLoginsFile">Použít heslo ze souboru ftp_logins.txt k zadanému uživatelskému jménu.</param>
        /// <param name="baseFolder">Kde je soubor ftp_logins.txt?</param>
        /// <param name="webName">Název složky v '{baseFolder}\weby'. Získá datum vytvoření, přepisuje argumenty --year, --month a --day.</param>
        static void Main(string? username = null, string? password = null, string host = "mcrai.vshosting.cz",
            string path = "/httpdocs", int year = DefaultYear, int month = DefaultMonth, int day = DefaultDay,
            bool useLoginsFile = false, string baseFolder = @"C:\McRAI\", string? webName = null)
        {
            var login = new FtpLoginParser(webName, password, username);
            if (!login.LoadLoginInfo(useLoginsFile, baseFolder))
                return;

            bool modifiedDate = year != DefaultYear || month != DefaultMonth || day != DefaultDay;
            var date = webName switch
            {
                not null and var w when !modifiedDate && Directory.Exists($@"{baseFolder}\weby\{w}")
                    => Directory.GetCreationTime($@"{baseFolder}\weby\{w}"),
                _ => new(year, month, day)
            };
            using var checker = new FtpChecker(login.Username, login.Password, host, date);

            if (!useLoginsFile)
            {
                checker.Run(path);
                return;
            }
            for (int i = 0; i < login.Paths?.Length; i++)
            {
                checker.Run(login.Paths[i]);
            }
        }
    }
}

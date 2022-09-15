﻿namespace FtpUpdateChecker;

class Program
{
    /// <summary>
    /// Nástroj pro kontrolu nových souborů na FTP serveru po určitém datu.
    /// </summary>
    /// <param name="username">Uživatelské jméno na FTP. Není nutné zadávat, pokud je zadán argument --web-name (načte se dle názvu složky s prefixem 'tom-'). </param>
    /// <param name="password">Heslo pro přístup na FTP. Není-li zadáno, načte se z ftp_logins.txt dle zadaného --username nebo načteného jména z --web-name. </param>
    /// <param name="host">Url serveru.</param>
    /// <param name="path">Root složka skenovaného webu. Není nutné zadávat, pokud je zadán argument --web-name (načte se dle uživatelského jména v ftp_logins.txz). </param>
    /// <param name="year">Soubory nad tímto rokem se zobrazí jako aktualizované.</param>
    /// <param name="month">Soubory nad tímto měsícem se zobrazí jako aktualizované.</param>
    /// <param name="day">Soubory nad tímto dnem se zobrazí jako aktualizované.</param>
    /// <param name="baseFolder">Kde je soubor ftp_logins.txt?</param>
    /// <param name="webName">Název složky v '{baseFolder}\weby'. Získá datum vytvoření, přepisuje argumenty --year, --month a --day.</param>
    static void Main(string? username = null, string? password = null, string host = McraiFtp.DefaultHostname1,
        string? path = null, int year = McraiFtp.DefaultYear, int month = McraiFtp.DefaultMonth, int day = McraiFtp.DefaultDay,
        string baseFolder = McraiFtp.DefaultBaseFolder, string? webName = null)
    {
        new McraiFtp(username, password, path, webName, baseFolder, host, day, month, year).Update();
    }
}

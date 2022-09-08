namespace FtpUpdateChecker;

/// <summary> Třída obsahující informace o přihlašovacích údajích k FTP. </summary>
internal sealed class LoginParser
{
    /// <summary> Uživatelské jméno. </summary>
    public string? Username { get; private set; }

    /// <summary> Heslo. </summary>
    public string? Password { get; private set; }

    /// <summary> Cesty v root na FTP serveru ke kontrole. </summary>
    public string Path { get; private set; }

    /// <summary> Inicializace uživatelského jména a hesla. </summary>
    public LoginParser(string username, string password)
    {
        Username = username;
        Password = password;
    }

    /// <summary> Zkusit načíst uživatelské jméno z názvu webu. </summary>
    /// <remarks> Použit výchozí parametr userName místo webName, pokud není prázdné/null. </remarks>
    public LoginParser(string? webName, string? password, string? overrideUsername, string baseFolder)
        : this(overrideUsername, password)
    {
        LoadCredentialsFromWebName(baseFolder, webName);
    }

    /// <summary> Načíst a zkontrolovat přihlašovací údaje dle parametru useLoginsFile. </summary>
    private void LoadCredentialsFromWebName(string baseFolder, string? webName)
    {
        if (webName is not null && Password is null && Username is null)
        {
            var loginsFilePath = System.IO.Path.Join(baseFolder, "ftp_logins.txt");
            using var sr = new StreamReader(loginsFilePath);

            while (!sr.EndOfStream)
            {
                var loginLine = sr.ReadLine();
                if (loginLine.StartsWith(webName))
                {
                    var login = loginLine.Split(" : ");
                    Username = login[1].Trim();
                    Password = login[2].Trim();
                    Path = login[3].Trim();
                    return;
                }
            }
            throw new InvalidOperationException($"Nelze načíst heslo ze souboru {loginsFilePath} pro web {webName}.");
        }
    }
}

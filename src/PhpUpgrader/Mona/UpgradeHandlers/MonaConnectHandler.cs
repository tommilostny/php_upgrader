namespace PhpUpgrader.Mona.UpgradeHandlers;

public class MonaConnectHandler : ConnectHandler
{
    private const string _hostnameVarPart = "$hostname_";
    private const string _databaseVarPart = "$database_";
    private const string _usernameVarPart = "$username_";
    private const string _passwordVarPart = "$password_";

    /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
    public override void UpgradeConnect(FileWrapper file, PhpUpgraderBase upgrader)
    {
        //konec, pokud aktuální soubor nepatří mezi validní connection soubory
        if (ConnectionPaths(upgrader.ConnectionFile).Any(cf => file.Path.EndsWith(cf, StringComparison.Ordinal)))
        {
            //načtení hlavičky connect souboru.
            LoadConnectHeader(file);

            //generování nových údajů k databázi, pokud jsou všechny zadány
            GenerateNewCredential(_hostnameVarPart, upgrader.Hostname, file);
            GenerateNewCredential(_databaseVarPart, upgrader.Database, file);
            GenerateNewCredential(_usernameVarPart, upgrader.Username, file);
            GenerateNewCredential(_passwordVarPart, upgrader.Password, file);

            //smazat zbytečné znaky
            file.Content.Replace("////", "//")
                        .Replace("\r\r", "\r");

            //na konec přidání obsahu předpřipraveného souboru
            file.Content.Append(File.ReadAllText(Path.Join(upgrader.BaseFolder, "important", "connection.txt")));
        }
    }

    private static IEnumerable<string> ConnectionPaths(string connectionFile)
    {
        yield return Path.Join("connect", connectionFile);
        yield return Path.Join("system", connectionFile);
        yield return Path.Join("Connections", connectionFile);
    }

    private static void LoadConnectHeader(FileWrapper file)
    {
        bool inComment, hostLoaded, dbnameLoaded, usernameLoaded, passwdLoaded;
        inComment = hostLoaded = dbnameLoaded = usernameLoaded = passwdLoaded = false;
        var lines = file.Content.Split();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.Contains("/*")) inComment = true;
            if (line.Contains("*/")) inComment = false;

            if (!inComment)
            {
                if (line.Contains(_hostnameVarPart) && !line.Contains($"//{_hostnameVarPart}"))
                {
                    hostLoaded = true;
                }
                else if (line.Contains(_databaseVarPart) && !line.Contains($"//{_databaseVarPart}"))
                {
                    dbnameLoaded = true;
                }
                else if (line.Contains(_usernameVarPart) && !line.Contains($"//{_usernameVarPart}"))
                {
                    usernameLoaded = true;
                }
                else if (line.Contains(_passwordVarPart) && !line.Contains($"//{_passwordVarPart}"))
                {
                    passwdLoaded = true;
                }
                if (hostLoaded && dbnameLoaded && usernameLoaded && passwdLoaded)
                {
                    lines.RemoveRange(i + 1, lines.Count - i - 1);
                    break;
                }
            }
        }
        lines.JoinInto(file.Content);
        if (file.Content[^1] != '\n')
        {
            file.Content.AppendLine();
        }
    }

    private static void GenerateNewCredential(string varPart, string? varValue, FileWrapper file)
    {
        if (!string.IsNullOrWhiteSpace(varValue))
        {
            var cred = $"{varPart}beta = '{varValue}';";
            if (!file.Content.Contains(cred))
            {
                file.Content.Replace($"\n{varPart}", $"\n//{varPart}");
                file.Content.AppendLine(cred);
            }
        }
    }
}

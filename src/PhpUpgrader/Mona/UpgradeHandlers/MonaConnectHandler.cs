namespace PhpUpgrader.Mona.UpgradeHandlers;

public partial class MonaConnectHandler : IConnectHandler
{
    private const string _hostnameVarPart = "$hostname_";
    private const string _databaseVarPart = "$database_";
    private const string _usernameVarPart = "$username_";
    private const string _passwordVarPart = "$password_";

    /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
    public virtual void UpgradeConnect(FileWrapper file, PhpUpgraderBase upgrader)
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
        yield return Path.Join("import", connectionFile);
    }

    private static void LoadConnectHeader(FileWrapper file)
    {
        bool inComment, hostLoaded, dbnameLoaded, usernameLoaded, passwdLoaded;
        inComment = hostLoaded = dbnameLoaded = usernameLoaded = passwdLoaded = false;
        var lines = file.Content.Split();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lookupStartPosition = 0;
            var commentStartPosition = line.IndexOf("*/");
            if (commentStartPosition != -1)
            {
                lookupStartPosition = commentStartPosition + 2;
                inComment = true;
            }
            var commentEndPosition = line.IndexOf("*/");
            if (commentEndPosition != -1)
            {
                lookupStartPosition = commentEndPosition + 2;
                inComment = false;
            }
            if (!inComment)
            {
                var lookupChars = new Lazy<string>(() => line.ToString()[lookupStartPosition..]);

                hostLoaded = hostLoaded || HostnameRegex().IsMatch(lookupChars.Value);
                dbnameLoaded = dbnameLoaded || DatabaseRegex().IsMatch(lookupChars.Value);
                usernameLoaded = usernameLoaded || UsernameRegex().IsMatch(lookupChars.Value);
                passwdLoaded = passwdLoaded || PasswordRegex().IsMatch(lookupChars.Value);

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

    [GeneratedRegex($@"(?<!\/\/)\{_hostnameVarPart}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex HostnameRegex();

    [GeneratedRegex($@"(?<!\/\/)\{_usernameVarPart}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex UsernameRegex();

    [GeneratedRegex($@"(?<!\/\/)\{_passwordVarPart}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex PasswordRegex();

    [GeneratedRegex($@"(?<!\/\/)\{_databaseVarPart}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex DatabaseRegex();
}

namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary> predelat soubor connect/connection.php >>> dle vzoru v adresari rs mona </summary>
    public virtual void UpgradeConnect(FileWrapper file)
    {
        const string hostnameVarPart = "$hostname_";
        const string databaseVarPart = "$database_";
        const string usernameVarPart = "$username_";
        const string passwordVarPart = "$password_";

        //konec, pokud aktuální soubor nepatří mezi validní connection soubory
        if (_ConnectionPaths().Any(cf => file.Path.EndsWith(cf)))
        {
            //načtení hlavičky connect souboru.
            _LoadConnectHeader();

            //generování nových údajů k databázi, pokud jsou všechny zadány
            _GenerateNewCredential(hostnameVarPart, Hostname);
            _GenerateNewCredential(databaseVarPart, Database);
            _GenerateNewCredential(usernameVarPart, Username);
            _GenerateNewCredential(passwordVarPart, Password);

            //smazat zbytečné znaky
            file.Content.Replace("////", "//");
            file.Content.Replace("\r\r", "\r");

            //na konec přidání obsahu předpřipraveného souboru
            file.Content.Append(File.ReadAllText(Path.Join(BaseFolder, "important", "connection.txt")));
        }

        IEnumerable<string> _ConnectionPaths()
        {
            yield return Path.Join("connect", ConnectionFile);
            yield return Path.Join("system", ConnectionFile);
            yield return Path.Join("Connections", ConnectionFile);
        }

        void _LoadConnectHeader()
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
                    if (line.Contains(hostnameVarPart) && !line.Contains($"//{hostnameVarPart}"))
                    {
                        hostLoaded = true;
                    }
                    else if (line.Contains(databaseVarPart) && !line.Contains($"//{databaseVarPart}"))
                    {
                        dbnameLoaded = true;
                    }
                    else if (line.Contains(usernameVarPart) && !line.Contains($"//{usernameVarPart}"))
                    {
                        usernameLoaded = true;
                    }
                    else if (line.Contains(passwordVarPart) && !line.Contains($"//{passwordVarPart}"))
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

        void _GenerateNewCredential(string varPart, string? varValue)
        {
            Lazy<string> cred = new(() => $"{varPart}beta = '{varValue}';");

            if (varValue is not null && !file.Content.Contains(cred.Value))
            {
                file.Content.Replace($"\n{varPart}", $"\n//{varPart}");
                file.Content.AppendLine(cred.Value);
            }
        }
    }
}

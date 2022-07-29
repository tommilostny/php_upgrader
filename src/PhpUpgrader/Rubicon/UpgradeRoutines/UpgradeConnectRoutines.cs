using PhpUpgrader.Mona.UpgradeRoutines;

namespace PhpUpgrader.Rubicon.UpgradeRoutines;

public static class UpgradeConnectRoutines
{
    /// <summary> Aktualizace souborů připojení systému Rubicon. </summary>
    public static FileWrapper UpgradeConnect_Rubicon(this FileWrapper file, RubiconUpgrader upgrader)
    {
        return file.UpgradeMonaLikeConnect(upgrader, "rubicon_import.php", "sportmall_import")
            .UpgradeMonaLikeConnect(upgrader, "hodnoceni.php", "hodnoceni_conn")
            .UpgradeSetup(upgrader)
            .UpgradeHostname(upgrader)
            .UpgradeOldDbConnect(upgrader);
    }

    /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php. </summary>
    public static FileWrapper UpgradeMonaLikeConnect(this FileWrapper file, RubiconUpgrader upgrader, string fileName, string varName)
    {
        if (!file.Path.EndsWith(Path.Join("Connections", fileName)))
        {
            return file;
        }
        //načíst původní dotazy z konce souboru.
        StringBuilder mysqlQueries = new();
        IEnumerable<Match> matches = Regex.Matches(file.Content.ToString(), @"mysql_query\("".+""\);", RegexOptions.Compiled);
        foreach (var match in matches)
        {
            var queryStartIndex = match.ValueSpan.IndexOf('"');
            mysqlQueries.AppendLine($"mysqli_query(${varName}, {match.ValueSpan[queryStartIndex..]}");
        }
        //aktualizovat stejně jako connect pro RS Mona, jen s proměnnou $sportmall_import.
        var backup = upgrader.ConnectionFile;
        upgrader.ConnectionFile = fileName;
        
        file.UpgradeConnect_Mona(upgrader);

        upgrader.ConnectionFile = backup;
        upgrader.RenameVar(file.Content, varName);

        //nakonec přidat aktualizované původní dotazy.
        file.Content.Replace($"mysqli_query(${varName}, \"SET CHARACTER SET utf8\");",
                             mysqlQueries.ToString());
        return file;
    }

    /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
    public static FileWrapper UpgradeSetup(this FileWrapper file, RubiconUpgrader upgrader)
    {
        if (!file.Path.EndsWith(Path.Join(upgrader.WebName, "setup.php")))
        {
            return file;
        }
        file.Content.Replace("$_SERVER[HTTP_HOST]", "$_SERVER['HTTP_HOST']");

        if (upgrader.Database is null || upgrader.Username is null || upgrader.Password is null
            || file.Content.Contains($"password = '{upgrader.Password}';"))
        {
            return file;
        }
        bool usernameLoaded = false, passwordLoaded = false, databaseLoaded = false;
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(_NewCredentialAndComment);

        var updated = Regex.Replace(content, @"\$setup_connect.*= ?"".*"";", evaluator, RegexOptions.Compiled);

        file.Content.Replace(content, updated);
        file.Content.Replace("////", "//");

        if (!usernameLoaded)
        {
            file.Warnings.Add("setup.php - nenačtené přihlašovací jméno.");
        }
        if (!passwordLoaded)
        {
            file.Warnings.Add("setup.php - nenačtené heslo.");
        }
        if (!databaseLoaded)
        {
            file.Warnings.Add("setup.php - nenačtený název databáze.");
        }
        file.Warnings.Add("setup.php - zkontrolovat připojení k databázi..");
        return file;

        string _NewCredentialAndComment(Match match)
        {
            if (content.AsSpan(0, match.Index).EndsWith("//"))
            {
                return match.Value;
            }
            var eqIndex = match.ValueSpan.IndexOf('=');
            var varName = match.ValueSpan[..eqIndex].Trim();
            var credential = varName switch
            {
                var vn when vn.EndsWith("username") && (usernameLoaded = true) => upgrader.Username,
                var vn when vn.EndsWith("password") && (passwordLoaded = true) => upgrader.Password,
                var vn when vn.EndsWith("db") && (databaseLoaded = true) => upgrader.Database,
                _ => null
            };
            return credential is null ? match.Value : $"//{match.Value}\n{varName} = '{credential}';";
        }
    }

    /// <summary> Hodnoty <b>$hostname_beta</b>, které nahradit <see cref="MonaUpgrader.Hostname"/>. </summary>
    private static readonly string[] _hostnamesToReplace =
    {
        "93.185.102.228", "mcrai.vshosting.cz", "217.16.184.116", "mcrai2.vshosting.cz", "localhost"
    };

    /// <summary> Aktualizace hostname z mcrai1 na server mcrai2. </summary>
    public static FileWrapper UpgradeHostname(this FileWrapper file, RubiconUpgrader upgrader)
    {
        var connBeta = file.Path.EndsWith(Path.Join("Connections", "beta.php"));
        foreach (var hn in _hostnamesToReplace)
        {
            if (upgrader.Hostname == hn)
            {
                continue;
            }
            if (connBeta && !file.Content.Contains($"//$hostname_beta = \"{hn}\";"))
            {
                file.Content.Replace($"$hostname_beta = \"{hn}\";",
                    $"//$hostname_beta = \"{hn}\";\n\t$hostname_beta = \"{upgrader.Hostname}\";");
            }
            if (!file.Content.Contains($"//$api = new RubiconAPI($_REQUEST['url'], '{hn}'"))
            {
                file.Content.Replace($"$api = new RubiconAPI($_REQUEST['url'], '{hn}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');",
                    $"//$api = new RubiconAPI($_REQUEST['url'], '{hn}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');\n\t$api = new RubiconAPI($_REQUEST['url'], '{upgrader.Hostname}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');");
            }
            file.UpgradeDatabaseConnectCall(hn, upgrader.Hostname);
        }
        return file;
    }

    /// <summary> Aktualizace Database::connect. </summary>
    public static FileWrapper UpgradeDatabaseConnectCall(this FileWrapper file, string oldHost, string newHost)
    {
        var lookingFor = $"Database::connect('{oldHost}'";
        var commented = $"//{lookingFor}";

        if (file.Content.Contains(lookingFor) && !file.Content.Contains(commented))
        {
            var content = file.Content.ToString();
            var evaluator = new MatchEvaluator(_DCMatchEvaluator);
            var updated = Regex.Replace(content, @$"( |\t)*Database::connect\('{oldHost}'.+\);", evaluator);

            file.Content.Replace(content, updated);
        }
        return file;

        string _DCMatchEvaluator(Match match)
        {
            var startIndex = match.ValueSpan.IndexOf('(') + oldHost.Length + 2;
            var afterOldHost = match.ValueSpan[startIndex..];

            var spaces = match.ValueSpan[..match.ValueSpan.IndexOf('D')];

            return $"{spaces}//{match.ValueSpan.TrimStart()}\n{spaces}Database::connect('{newHost}{afterOldHost}";
        }
    }

    /// <summary>
    /// Nalezeno v importy/_importy_old/DB_connect.php.
    /// (Raději také aktualizovat. Stejný soubor se někde může ještě používat.)
    /// </summary>
    public static FileWrapper UpgradeOldDbConnect(this FileWrapper file, RubiconUpgrader upgrader)
    {
        if (file.Path.EndsWith("DB_connect.php"))
        {
            file.Content.Replace("$DBLink = mysqli_connect ($host,$user,$pass) or mysql_errno() + mysqli_error($beta);",
                                 "$DBLink = mysqli_connect($host, $user, $pass);");
            file.Content.Replace("if (!mysql_select_db( $DBname, $DBLink ))",
                                 "mysqli_select_db($DBLink, $DBname);\nif (mysqli_connect_errno())");
            if (!file.Content.Contains("exit()"))
            {
                file.Content.Replace("echo \"ERROR\";", "echo \"ERROR\";\nexit();");
            }
            upgrader.RenameVar(file.Content, "DBLink");
        }
        return file;
    }
}

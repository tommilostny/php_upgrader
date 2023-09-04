using PhpUpgrader.Mona.UpgradeExtensions;
using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Rubicon.UpgradeHandlers;

public sealed partial class RubiconConnectHandler : MonaConnectHandler, IConnectHandler
{
    private static string? _setupPhp = null;
    private static string? _setup500Php = null;
    private static string? _rubiconConfCoreConfigurePhp = null;
    private static string? _rubiconApiPhp = null;
    private static string? _connectionsDir = null;
    private static string? _apiPlatbyDbConnectPhp = null;

    private static readonly string _monamyDir = $"{Path.DirectorySeparatorChar}monamy{Path.DirectorySeparatorChar}";
    private static readonly string _connectionPhp = Path.Join("connect", "connection.php");

    private static readonly string _betaPhp = Path.Join("Connections", "beta.php");
    private static readonly string _pListinaPhp = Path.Join("pdf", "p_listina.php");
    private static readonly string _pListinaUPhp = Path.Join("pdf", "p_listina_u.php");

    private static readonly string _coreModulePhp = Path.Join("core", "modules", "core", "module.php");
    private static readonly string _rssShopSportPhp = Path.Join("rss", "_off", "rss_shop-sport.php");

    /// <summary> Aktualizace souborů připojení systému Rubicon. </summary>
    public override void UpgradeConnect(FileWrapper file, PhpUpgraderBase upgrader)
    {
        _ = UpgradeMonaLikeConnect(file, upgrader);
        UpgradeSetup(file, upgrader as RubiconUpgrader);
        UpgradeHostname(file, upgrader);
        UpgradeOldDbConnect(file, upgrader);
        UpgradeRubiconModulesDB(file, upgrader);
        UpgradeDefines(file, upgrader);
        UpgradePgConnectWithStaticConnectionString(file, upgrader);
    }

    /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php. </summary>
    /// <returns> True, pokud se jedná o "connect soubor podobný jako v RS Mona" a bylo upraveno, jinak false. </returns>
    public bool UpgradeMonaLikeConnect(FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (file.Content.Contains("pg_connect"))
        {
            return true;
        }
        if (file.Path.Contains(_connectionsDir ??= $"{Path.DirectorySeparatorChar}Connections{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || (file.Path.Contains(_monamyDir, StringComparison.Ordinal)
                && file.Path.EndsWith(_connectionPhp, StringComparison.Ordinal)))
        {
            var content = file.Content.ToString();
            var varName = LoadConnectionVariableName(content);
            if (varName is null) //nemáme název proměnné? skončit.
            {
                return false;
            }
            //načíst původní dotazy z konce souboru.
            var mysqliQueries = LoadMysqlQueries(content, varName);

            //uložit si původní hodnoty parametrů
            var backup = (upgrader.ConnectionFile, upgrader.Database, upgrader.Username, upgrader.Password, upgrader.Hostname);
            upgrader.ConnectionFile = file.Path.Split(Path.DirectorySeparatorChar)[^1];

            upgrader.Database = upgrader.Username = upgrader.Password = null;
            if (file.Content.Contains("rdesign.cybersales.cz") || file.Content.Contains("mcrai2.vshosting.cz") || file.Content.Contains("217.16.184.116"))
            {
                upgrader.Hostname = null;
            }
            //aktualizovat stejně jako connect pro RS Mona.
            base.UpgradeConnect(file, upgrader);

            //načíst zálohu hodnot.
            (upgrader.ConnectionFile, upgrader.Database, upgrader.Username, upgrader.Password, upgrader.Hostname) = backup;
            (upgrader as MonaUpgrader)?.RenameVar(file.Content, varName);

            //nakonec přidat aktualizované původní dotazy.
            file.Content.Replace($"mysqli_query(${varName}, \"SET CHARACTER SET utf8\");", mysqliQueries);
            return true;
        }
        return false;
    }

    private static string? LoadConnectionVariableName(string content)
    {
        IEnumerable<Match> variables = ConnectVarRegex().Matches(content);
        string? varName = null;
        foreach (var match in variables)
        {
            var expectedVarName = match.Groups["beta"].Value;
            if (varName is null) //první průběh, načíst očekávaný název proměnné
            {
                varName = expectedVarName;
                continue;
            }
            //v dalších případech kontrolovat, zda se jedná o stejný název, jinak neupravovat.
            if (!string.Equals(expectedVarName, varName, StringComparison.Ordinal))
            {
                return null;
            }
        }
        return varName;
    }

    private static string LoadMysqlQueries(string content, string varName)
    {
        StringBuilder mysqliQueries = new();
        IEnumerable<Match> queries = MysqlQueryAnyStringRegex().Matches(content);
        foreach (var match in queries)
        {
            var queryStartIndex = match.ValueSpan.IndexOf('"');
            mysqliQueries.AppendLine(new MysqliQueryParamsFormat(), $"mysqli_query({varName}, {match.Value[queryStartIndex..]}");
        }
        return mysqliQueries.ToString();
    }

    private static void InitSetupVars(string path, string webName, out bool isRubiconCoreConfigure, out bool isRubiconApi)
    {
        _setupPhp ??= Path.Join(webName, "setup.php");
        _setup500Php ??= Path.Join(webName, "setup_500.php");
        _rubiconConfCoreConfigurePhp ??= Path.Join("conf", "core_configure.php");
        _rubiconApiPhp ??= Path.Join("classes", "RubiconAPI.class.php");
        _apiPlatbyDbConnectPhp ??= Path.Join("api_platby", "api_db_conn.php");

        isRubiconCoreConfigure = path.EndsWith(_rubiconConfCoreConfigurePhp, StringComparison.Ordinal)
            || path.EndsWith(_apiPlatbyDbConnectPhp, StringComparison.Ordinal);
        isRubiconApi = path.EndsWith(_rubiconApiPhp, StringComparison.OrdinalIgnoreCase);
    }

    private static void FillInDbCredentials(FileWrapper file, RubiconUpgrader upgrader, bool isRubiconCoreConfigure, bool isRubiconApi, bool containsDevDb)
    {
        var database = containsDevDb ? upgrader.DevDatabase : upgrader.Database;
        var username = containsDevDb ? upgrader.DevUsername : upgrader.Username;
        var password = containsDevDb ? upgrader.DevPassword : upgrader.Password;

        if ((upgrader.Hostname is "217.16.184.116" && !containsDevDb) || database is null || username is null || password is null
            || file.Content.Contains($"password = '{password}';")
            || file.Content.Contains($"password_beta = \"{password}\";")
            || file.Content.Contains($"password_sportmall_import = \"{password}\";"))
            return;

        bool usernameLoaded = false, passwordLoaded = false, databaseLoaded = false, hostnameLoaded = false;
        var content = file.Content.ToString();
        var updateHostname = isRubiconCoreConfigure || isRubiconApi || containsDevDb
            || HostNamesToReplace().Any(hn => content.Contains($"$hostname_beta = \"{hn}\";", StringComparison.Ordinal));

        file.Content.Replace(SetupConnectRegex().Replace(content, _NewCredentialAndComment)).Replace("////", "//");

        if (!usernameLoaded)
            file.Warnings.Add("setup - nenačtené přihlašovací jméno.");
        if (!passwordLoaded)
            file.Warnings.Add("setup - nenačtené heslo.");
        if (!databaseLoaded)
            file.Warnings.Add("setup - nenačtený název databáze.");
        if (updateHostname && !hostnameLoaded)
            file.Warnings.Add("setup - nenačtená adresa databázového serveru.");
        file.Warnings.Add("setup - zkontrolovat připojení k databázi.");

        string _NewCredentialAndComment(Match match)
        {
            if (content.AsSpan(0, match.Index).EndsWith("//", StringComparison.Ordinal))
            {
                return match.Value;
            }
            var eqIndex = match.ValueSpan.IndexOf('=');
            var varName = match.ValueSpan[..eqIndex].Trim();

            var credential = LoadCred(varName, "username", "$username_beta", "$username_sportmall_import", ref usernameLoaded, username)
                ?? LoadCred(varName, "password", "$password_beta", "$password_sportmall_import", ref passwordLoaded, password)
                ?? LoadCred(varName, "db", "$database_beta", "$database_sportmall_import", ref databaseLoaded, database)
                ?? LoadCred(varName, "database", string.Empty, string.Empty, ref databaseLoaded, database);

            if (updateHostname && credential is null)
            {
                var host = containsDevDb ? "127.0.0.1" : upgrader.Hostname;
                credential = LoadCred(varName, "hostname", "$hostname_beta", "$hostname_sportmall_import", ref hostnameLoaded, host);
            }
            var spaces = GetSpacesBeforeIndex(content, match.Index);
            return credential is null ? match.Value : $"//{match.Value}\n{spaces}{varName} = '{credential}';";
        }
    }
    
    private static string? LoadCred(ReadOnlySpan<char> varName, ReadOnlySpan<char> varEnd, ReadOnlySpan<char> betaName, ReadOnlySpan<char> sportMallName, ref bool loaded, string credValue)
        => !loaded
            && (varName.EndsWith(varEnd, StringComparison.Ordinal)
                || (betaName.Length > 0 && varName.StartsWith(betaName, StringComparison.Ordinal))
                || (sportMallName.Length > 0 && varName.StartsWith(sportMallName, StringComparison.Ordinal)))
            && (loaded = true)
                ? credValue
                : null;

    private static ReadOnlySpan<char> GetSpacesBeforeIndex(string content, int index)
    {
        var count = 0;
        for (var i = index - 1; i >= 0; i--)
        {
            if (content[i] is not ' ' and not '\t')
                break;
            count++;
        }
        return content.AsSpan(index - count, count);
    }

    /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
    public static void UpgradeSetup(FileWrapper file, RubiconUpgrader upgrader)
    {
        InitSetupVars(file.Path, upgrader.WebName, out var isRubiconCoreConfigure, out var isRubiconApi);
        var containsDevDb = file.Content.Contains("\"rubicon_6_dev_") || file.Content.Contains("'rubicon_6_dev_");

        if (!containsDevDb && !isRubiconCoreConfigure && !isRubiconApi
            && !file.Path.EndsWith(_setupPhp, StringComparison.Ordinal)
            && !file.Path.EndsWith(_setup500Php, StringComparison.Ordinal)
            && !file.Path.EndsWith("cat_url.php", StringComparison.Ordinal)
            && !file.Path.EndsWith("cat_fix.php", StringComparison.Ordinal)
            && !file.Path.EndsWith("a_store_pohoda.php", StringComparison.Ordinal)
            && !file.Path.EndsWith("b2b_db.php", StringComparison.Ordinal))
            return;

        FillInDbCredentials(file, upgrader, isRubiconCoreConfigure, isRubiconApi, containsDevDb);
        file.Content.Replace("$_SERVER[HTTP_HOST]", "$_SERVER['HTTP_HOST']");
    }

    /// <summary> Aktualizace hostname z mcrai1 na <see cref="PhpUpgraderBase.Hostname"/>. </summary>
    public static void UpgradeHostname(FileWrapper file, PhpUpgraderBase upgrader)
    {
        var connBeta = file.Path.EndsWith(_betaPhp, StringComparison.Ordinal)
                    || file.Path.EndsWith(_setupPhp, StringComparison.Ordinal);
        var moneyXmlInclude = file.Path.EndsWith("MONEY_XML_INCLUDE.php", StringComparison.Ordinal);
        var pListina = file.Path.EndsWith(_pListinaPhp, StringComparison.Ordinal)
                     || file.Path.EndsWith(_pListinaUPhp, StringComparison.Ordinal);

        foreach (var hn in HostNamesToReplace())
        {
            if (string.Equals(upgrader.Hostname, hn, StringComparison.Ordinal))
            {
                continue;
            }
            string? backup = (string.Equals(upgrader.Hostname, "217.16.184.116", StringComparison.Ordinal)
                    || string.Equals(upgrader.Hostname, "mcrai2.vshosting.cz", StringComparison.Ordinal))
                && (string.Equals(hn, "93.185.102.228", StringComparison.Ordinal)
                    || string.Equals(hn, "mcrai.vshosting.cz", StringComparison.Ordinal))
                ? upgrader.Hostname
                : null;
            if (backup is not null)
            {
                upgrader.Hostname = "mcrai-upgrade.vshosting.cz";
            }
            if (moneyXmlInclude && !file.Content.Contains($"//$conn = pg_connect(\"host = {hn}"))
            {
                file.Content.Replace($"$conn = pg_connect(\"host = {hn}",
                                     $"//$conn = pg_connect(\"host = {hn}\n$conn = pg_connect(\"host = {upgrader.Hostname}");
            }
            if ((connBeta || pListina) && !file.Content.Contains($"//$hostname_beta = \"{hn}\";"))
            {
                file.Content.Replace($"$hostname_beta = \"{hn}\";",
                                     $"//$hostname_beta = \"{hn}\";\n{(!pListina ? '\t' : null)}$hostname_beta = \"{upgrader.Hostname}\";");
            }
            if (!file.Content.Contains($"//$api = new RubiconAPI($_REQUEST['url'], '{hn}'"))
            {
                file.Content.Replace($"$api = new RubiconAPI($_REQUEST['url'], '{hn}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');",
                                     $"//$api = new RubiconAPI($_REQUEST['url'], '{hn}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');\n\t$api = new RubiconAPI($_REQUEST['url'], '{upgrader.Hostname}', $setup_connect_username, $setup_connect_password, $setup_connect_db, '5432');");
            }
            UpgradeDatabaseConnectCall(file, upgrader, hn, upgrader.Hostname);
            file.Content
                .Replace($"\"host\"      => \"{hn}\",", $"\"host\"      => \"{upgrader.Hostname}\",")
                .Replace($"$ftp_server\t= '{hn}';"    , $"$ftp_server\t= '{upgrader.Hostname}';");
            if (backup is not null)
            {
                upgrader.Hostname = backup;
            }
        }
    }

    /// <summary> Hodnoty <b>$hostname_beta</b>, které nahradit <see cref="PhpUpgraderBase.Hostname"/>. </summary>
    private static IEnumerable<string> HostNamesToReplace()
    {
        yield return "93.185.102.228";
        yield return "mcrai.vshosting.cz";
        yield return "217.16.184.116";
        yield return "mcrai2.vshosting.cz";
    }

    /// <summary> Aktualizace Database::connect. </summary>
    public static void UpgradeDatabaseConnectCall(FileWrapper file, PhpUpgraderBase upgrader, string oldHost, string newHost)
    {
        var lookingFor = $"Database::connect('{oldHost}'";
        var commented = $"//{lookingFor}";

        if (file.Content.Contains(lookingFor) && !file.Content.Contains(commented))
        {
            var content = file.Content.ToString();
            file.Content.Replace(
                Regex.Replace(content,
                              @$"( |\t)*Database::connect\('{oldHost}'.+\);",
                              _DCMatchEvaluator,
                              RegexOptions.None,
                              TimeSpan.FromSeconds(4)
                )
            );
        }

        string _DCMatchEvaluator(Match match)
        {
            var spaces = match.ValueSpan[..match.ValueSpan.IndexOf('D')];
            var sb = new StringBuilder($"{spaces}//{match.ValueSpan.TrimStart()}\n{spaces}Database::connect('{newHost}'");

            switch (upgrader)
            {
                case { Database: null } or { Username: null } or { Password: null }:
                    var startIndex = match.ValueSpan.IndexOf('(') + oldHost.Length + 3;
                    var afterOldHost = match.ValueSpan[startIndex..];
                    sb.Append(afterOldHost);
                    break;
                default:
                    sb.Append($", '{upgrader.Username}', '{upgrader.Password}', '{upgrader.Database}', '5432');");
                    break;
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Nalezeno v importy/_importy_old/DB_connect.php.
    /// (Raději také aktualizovat. Stejný soubor se někde může ještě používat.)
    /// </summary>
    public static void UpgradeOldDbConnect(FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (file.Path.EndsWith("DB_connect.php", StringComparison.Ordinal))
        {            
            file.Content.Replace("$DBLink = mysqli_connect ($host,$user,$pass) or mysql_errno() + mysqli_error($beta);",
                                 "$DBLink = mysqli_connect($host, $user, $pass);")
                        .Replace("$DBLink = mysql_connect ($host,$user,$pass) or mysql_errno() + mysql_error();",
                                 "$DBLink = mysqli_connect($host, $user, $pass);")
                        .Replace("if (!mysql_select_db( $DBname, $DBLink ))",
                                 "mysqli_select_db($DBLink, $DBname);\nif (mysqli_connect_errno())");
            if (!file.Content.Contains("exit()"))
            {
                file.Content.Replace("echo \"ERROR\";", "echo \"ERROR\";\nexit();");
            }
            (upgrader as MonaUpgrader).RenameVar(file.Content, "DBLink");
            return;
        }
        if (file.Path.EndsWith("helios_export.php", StringComparison.Ordinal))
        {
            file.Content.Replace("@$mybeta = mysql_pconnect($hostname_beta, $username_beta, $password_beta) or die (\"Nelze navázat spojení s databází.\");",
                                 "$mybeta = mysqli_connect($hostname_beta, $username_beta, $password_beta);")
                        .Replace("@mysql_Select_DB($database_beta) or die (\"Nenalezena databáze\");",
                                 "mysqli_select_db($mybeta, $database_beta);\n\nif(mysqli_connect_errno())\n  {\n    printf(\"Nelze navázat spojení s databází: %s\\n\", mysqli_connect_error());\n    exit();\n  }");
        }
    }
    
    public static void UpgradeRubiconModulesDB(FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (file.Path.EndsWith(_coreModulePhp, StringComparison.Ordinal))
        {
            file.Content.Replace("mysql_pconnect($rubicon_db->mysql_hostname, $rubicon_db->mysql_username, $rubicon_db->mysql_password)",
                                 "mysqli_connect($rubicon_db->mysql_hostname, $rubicon_db->mysql_username, $rubicon_db->mysql_password)")
                        .Replace("mysql_select_db($rubicon_db->mysql_database, $its_connect)",
                                 "mysqli_select_db($its_connect, $rubicon_db->mysql_database)");
            return;
        }
        if (file.Path.EndsWith(_rssShopSportPhp, StringComparison.Ordinal))
        {
            file.Content.Replace("@$db = mysql_pconnect($hostname_beta, $username_beta, $password_beta) or die (\"Nelze navázat spojení s databazí\");",
                                 "$beta = mysqli_connect($hostname_beta, $username_beta, $password_beta);")
                        .Replace("@mysql_Select_DB($database_beta) or die (\"Nenalezena databáze\");",
                                 "mysqli_select_db($beta, $database_beta);\r\n\r\nif(mysqli_connect_errno())\r\n  {\r\n    printf(\"Nelze navázat spojení s databází: %s\\n\", mysqli_connect_error());\r\n    exit();\r\n  }");
            (upgrader as MonaUpgrader).RenameVar(file.Content, newVarName: "beta", oldVarName: "db");
        }
    }

    /// <summary>
    /// // ===== Direct DB connection =====
    /// define('db_host', '********');
    /// define('db_user', '********');
    /// define('db_pass', '********');
    /// define('db_name', '********');
    /// </summary>
    private static void UpgradeDefines(FileWrapper file, PhpUpgraderBase upgrader)
    {
        file.Content.Replace(
            DefineDbVarRegex().Replace(
                file.Content.ToString(),
                _DefineEvaluator
            )
        );
        string _DefineEvaluator(Match match)
        {
            var varPart = match.Groups["var"].Value;
            var cred = varPart switch
            {
                "host" => upgrader.Hostname,
                "user" => upgrader.Username,
                "pass" => upgrader.Password,
                "name" => upgrader.Database,
                _ => null
            };
            return string.IsNullOrEmpty(cred) ? match.Value : $"define('db_{varPart}', '{cred}');";
        }
    }

    private static void UpgradePgConnectWithStaticConnectionString(FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (upgrader.Database is not null && upgrader.Username is not null && upgrader.Password is not null
            && file.Content.Contains("pg_connect") && !file.Content.Contains("port=$"))
        {
            file.Content.Replace(PgConnectRegex().Replace(
                file.Content.ToString(),
                m => $"pg_connect('host={upgrader.Hostname} port={m.Groups["port"]} dbname={upgrader.Database} user={upgrader.Username} password={upgrader.Password}'); //{m}"
            ));
        }
    }

    private class MysqliQueryParamsFormat : IFormatProvider, ICustomFormatter
    {
        private char[]? _startChars;

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => arg switch
        {
            null => null,
            string var and { Length: > 0 } => (_startChars ??= new[] { '$', '\"', '\'' }).Any(c => var[0] == c) ? var : '$' + var,
            var other => other.ToString()
        };

        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
    }

    [GeneratedRegex(@"(?<!\/(\/.*?|\*((.|\n)(?!\*\/))*?))\$(connect_|database|username|password|hostname|rubicon_db->pgsql_|setup_connect|\w+?_beta).*?=\s?[""'].*?[""'];", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex SetupConnectRegex();
    
    [GeneratedRegex(@"mysql_query\("".+""\);", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex MysqlQueryAnyStringRegex();
    
    [GeneratedRegex(@"\$(hostname|database|username|password)_(?<beta>\w+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex ConnectVarRegex();

    [GeneratedRegex(@"define\s?\(\s?(?<q>['""])db_(?<var>\w+?)\k<q>\s?,\s?\k<q>.*?\k<q>\s?\);", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex DefineDbVarRegex();

    [GeneratedRegex(@"pg_connect\s?\(\s?[""']host\s?=\s?\S+?\sport\s?=\s?(?<port>\d+?)\sdbname\s?=\s?\S+?\suser\s?=\s?\S+?\spassword\s?=\s?\S+?\s?[""']\s?\);", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex PgConnectRegex();
}

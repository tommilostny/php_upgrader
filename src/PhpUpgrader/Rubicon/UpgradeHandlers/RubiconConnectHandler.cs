using PhpUpgrader.Mona.UpgradeExtensions;
using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Rubicon.UpgradeHandlers;

public sealed partial class RubiconConnectHandler : MonaConnectHandler, IConnectHandler
{
    private static string? _setupPhp = null;
    private static string? _connectionsDir = null;

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
        if (!UpgradeMonaLikeConnect(file, upgrader))
        {
            UpgradeMysqlConnect(file);
        }
        UpgradeSetup(file, upgrader);
        UpgradeHostname(file, upgrader);
        UpgradeOldDbConnect(file, upgrader);
        UpgradeRubiconModulesDB(file, upgrader);
    }

    public static void UpgradeMysqlConnect(FileWrapper file)
    {
        //var content = file.Content.ToString();
        //var connectMatch = Regex.Match(content,
        //                               @"(?<beta>\$\w+?)\s*?=\s*?mysql(i_|_p)?connect\s*?\((?<host>.+?),(?<user>.+?),(?<pass>.+?)\)",
        //                               RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        //                               TimeSpan.FromSeconds(4));
        //if (connectMatch.Success)
        //{
        //    var dbSelectMatch = Regex.Match(content,
        //                                    @"mysql_select_db\s*?\((?<db>.+?),(?<beta>.+?)\)",
        //                                    RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        //                                    TimeSpan.FromSeconds(4));
        //    if (dbSelectMatch.Success)
        //    {
        //        var beta = connectMatch.Groups["beta"].Value;
        //        if (string.Equals(dbSelectMatch.Groups["beta"].Value.Trim(), beta, StringComparison.Ordinal))
        //        {
        //            var host = connectMatch.Groups["host"].Value.Trim();
        //            var user = connectMatch.Groups["user"].Value.Trim();
        //            var pass = connectMatch.Groups["pass"].Value.Trim();
        //            var db = dbSelectMatch.Groups["db"].Value.Trim();
        //
        //            file.Content.Replace(connectMatch.Value, $"{beta} = mysqli_connect({host}, {user}, {pass})")
        //                        .Replace(dbSelectMatch.Value, $"mysqli_select_db({beta})");
        //            
        //        }
        //    }
        //}
    }

    /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php. </summary>
    /// <returns> True, pokud se jedná o "connect soubor podobný jako v RS Mona" a bylo upraveno, jinak false. </returns>
    public bool UpgradeMonaLikeConnect(FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (file.Content.Contains("pg_connect"))
        {
            return true;
        }
        if (file.Path.Contains(_connectionsDir ??= Path.Join(upgrader.WebName, "Connections"), StringComparison.Ordinal)
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

            if (string.Equals(upgrader.Hostname, "localhost", StringComparison.Ordinal))
            {
                upgrader.Database = upgrader.Username = upgrader.Password = null;
            }
            if (file.Content.Contains("rdesign.cybersales.cz") || file.Content.Contains("mcrai2.vshosting.cz"))
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

    /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
    public static void UpgradeSetup(FileWrapper file, PhpUpgraderBase upgrader)
    {
        _setupPhp ??= Path.Join(upgrader.WebName, "setup.php");
        if (!file.Path.EndsWith(_setupPhp, StringComparison.Ordinal))
        {
            return;
        }
        file.Content.Replace("$_SERVER[HTTP_HOST]", "$_SERVER['HTTP_HOST']");

        if (upgrader.Database is null || upgrader.Username is null || upgrader.Password is null
            || file.Content.Contains($"password = '{upgrader.Password}';"))
        {
            return;
        }
        bool usernameLoaded = false, passwordLoaded = false, databaseLoaded = false;
        var content = file.Content.ToString();
        var evaluator = new MatchEvaluator(_NewCredentialAndComment);

        file.Content.Replace(SetupConnectRegex().Replace(content, evaluator))
                    .Replace("////", "//");

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
        file.Warnings.Add("setup.php - zkontrolovat připojení k databázi.");

        string _NewCredentialAndComment(Match match)
        {
            if (content.AsSpan(0, match.Index).EndsWith("//", StringComparison.Ordinal))
            {
                return match.Value;
            }
            var eqIndex = match.ValueSpan.IndexOf('=');
            var varName = match.ValueSpan[..eqIndex].Trim();

            var credential = _LoadCred(varName, "username", "$username_beta", ref usernameLoaded, upgrader.Username)
                ?? _LoadCred(varName, "password", "$password_beta", ref passwordLoaded, upgrader.Password)
                ?? _LoadCred(varName, "db", "$database_beta", ref databaseLoaded, upgrader.Database);

            return credential is null ? match.Value : $"//{match.Value}\n{varName} = '{credential}';";
        }

        static string? _LoadCred(ReadOnlySpan<char> varName, ReadOnlySpan<char> varEnd, ReadOnlySpan<char> betaName, ref bool loaded, string credValue)
            => !loaded && (varName.EndsWith(varEnd, StringComparison.Ordinal) || varName.Equals(betaName, StringComparison.Ordinal)) && (loaded = true)
                ? credValue
                : null;
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
        }
    }

    /// <summary> Hodnoty <b>$hostname_beta</b>, které nahradit <see cref="PhpUpgraderBase.Hostname"/>. </summary>
    private static IEnumerable<string> HostNamesToReplace()
    {
        yield return "93.185.102.228";
        yield return "mcrai.vshosting.cz";
        yield return "217.16.184.116";
        yield return "mcrai2.vshosting.cz";
        yield return "localhost";
    }

    /// <summary> Aktualizace Database::connect. </summary>
    public static void UpgradeDatabaseConnectCall(FileWrapper file, PhpUpgraderBase upgrader, string oldHost, string newHost)
    {
        var lookingFor = $"Database::connect('{oldHost}'";
        var commented = $"//{lookingFor}";

        if (file.Content.Contains(lookingFor) && !file.Content.Contains(commented))
        {
            var content = file.Content.ToString();
            var evaluator = new MatchEvaluator(_DCMatchEvaluator);
            file.Content.Replace(
                Regex.Replace(content,
                              @$"( |\t)*Database::connect\('{oldHost}'.+\);",
                              evaluator,
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

    [GeneratedRegex(@"\$(setup_connect|\w+?_beta).*?=\s?(""|').*?(""|');", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex SetupConnectRegex();
    
    [GeneratedRegex(@"mysql_query\("".+""\);", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex MysqlQueryAnyStringRegex();
    
    [GeneratedRegex(@"\$(hostname|database|username|password)_(?<beta>\w+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex ConnectVarRegex();
}

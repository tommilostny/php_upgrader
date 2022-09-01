﻿using PhpUpgrader.Mona.UpgradeExtensions;
using PhpUpgrader.Mona.UpgradeHandlers;

namespace PhpUpgrader.Rubicon.UpgradeHandlers;

public sealed class RubiconConnectHandler : MonaConnectHandler, IConnectHandler
{
    /// <summary> Aktualizace souborů připojení systému Rubicon. </summary>
    public override void UpgradeConnect(FileWrapper file, PhpUpgraderBase upgrader)
    {
        UpgradeMonaLikeConnect(file, upgrader, "rubicon_import.php", "sportmall_import");
        UpgradeMonaLikeConnect(file, upgrader, "hodnoceni.php", "hodnoceni_conn");
        UpgradeSetup(file, upgrader);
        UpgradeHostname(file, upgrader);
        UpgradeOldDbConnect(file, upgrader);
        UpgradeRubiconModulesDB(file);
    }

    /// <summary> Soubor /Connections/rubicon_import.php, podobný connect/connection.php. </summary>
    public void UpgradeMonaLikeConnect(FileWrapper file, PhpUpgraderBase upgrader, string fileName, string varName)
    {
        if (!file.Path.EndsWith(Path.Join("Connections", fileName), StringComparison.Ordinal))
        {
            return;
        }
        //načíst původní dotazy z konce souboru.
        StringBuilder mysqlQueries = new();
        IEnumerable<Match> matches = Regex.Matches(file.Content.ToString(), @"mysql_query\("".+""\);", RegexOptions.Compiled, TimeSpan.FromSeconds(4));
        foreach (var match in matches)
        {
            var queryStartIndex = match.ValueSpan.IndexOf('"');
            mysqlQueries.AppendLine(new MysqliQueryParamsFormat(), $"mysqli_query({varName}, {match.Value[queryStartIndex..]}");
        }
        //aktualizovat stejně jako connect pro RS Mona, jen s proměnnou $sportmall_import.
        var backup = upgrader.ConnectionFile;
        upgrader.ConnectionFile = fileName;

        base.UpgradeConnect(file, upgrader);

        upgrader.ConnectionFile = backup;
        (upgrader as MonaUpgrader).RenameVar(file.Content, varName);

        //nakonec přidat aktualizované původní dotazy.
        file.Content.Replace($"mysqli_query(${varName}, \"SET CHARACTER SET utf8\");",
                             mysqlQueries.ToString());
    }

    /// <summary> Aktualizace údajů k databázi v souboru setup.php. </summary>
    public static void UpgradeSetup(FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (!file.Path.EndsWith(Path.Join(upgrader.WebName, "setup.php"), StringComparison.Ordinal))
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

        var updated = Regex.Replace(content, @"\$setup_connect.*= ?"".*"";", evaluator, RegexOptions.Compiled, TimeSpan.FromSeconds(4));

        file.Content.Replace(content, updated)
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
        file.Warnings.Add("setup.php - zkontrolovat připojení k databázi..");

        string _NewCredentialAndComment(Match match)
        {
            if (content.AsSpan(0, match.Index).EndsWith("//", StringComparison.Ordinal))
            {
                return match.Value;
            }
            var eqIndex = match.ValueSpan.IndexOf('=');
            var varName = match.ValueSpan[..eqIndex].Trim();
            var credential = varName switch
            {
                var vn when vn.EndsWith("username", StringComparison.Ordinal) && (usernameLoaded = true) => upgrader.Username,
                var vn when vn.EndsWith("password", StringComparison.Ordinal) && (passwordLoaded = true) => upgrader.Password,
                var vn when vn.EndsWith("db", StringComparison.Ordinal) && (databaseLoaded = true) => upgrader.Database,
                _ => null
            };
            return credential is null ? match.Value : $"//{match.Value}\n{varName} = '{credential}';";
        }
    }

    /// <summary> Aktualizace hostname z mcrai1 na <see cref="PhpUpgraderBase.Hostname"/>. </summary>
    public static void UpgradeHostname(FileWrapper file, PhpUpgraderBase upgrader)
    {
        var connBeta = file.Path.EndsWith(Path.Join("Connections", "beta.php"), StringComparison.Ordinal);
        var moneyXmlInclude = file.Path.EndsWith("MONEY_XML_INCLUDE.php", StringComparison.Ordinal);
        var pListina = file.Path.EndsWith(Path.Join("pdf", "p_listina.php"), StringComparison.Ordinal)
                     || file.Path.EndsWith(Path.Join("pdf", "p_listina_u.php"), StringComparison.Ordinal);

        foreach (var hn in HostnamesToReplace())
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
            UpgradeDatabaseConnectCall(file, hn, upgrader.Hostname);
        }
    }

    /// <summary> Hodnoty <b>$hostname_beta</b>, které nahradit <see cref="PhpUpgraderBase.Hostname"/>. </summary>
    private static IEnumerable<string> HostnamesToReplace()
    {
        yield return "93.185.102.228";
        yield return "mcrai.vshosting.cz";
        yield return "217.16.184.116";
        yield return "mcrai2.vshosting.cz";
        yield return "localhost";
    }

    /// <summary> Aktualizace Database::connect. </summary>
    public static void UpgradeDatabaseConnectCall(FileWrapper file, string oldHost, string newHost)
    {
        var lookingFor = $"Database::connect('{oldHost}'";
        var commented = $"//{lookingFor}";

        if (file.Content.Contains(lookingFor) && !file.Content.Contains(commented))
        {
            var content = file.Content.ToString();
            var evaluator = new MatchEvaluator(_DCMatchEvaluator);
            var updated = Regex.Replace(content,
                                        @$"( |\t)*Database::connect\('{oldHost}'.+\);",
                                        evaluator,
                                        RegexOptions.None,
                                        TimeSpan.FromSeconds(4));

            file.Content.Replace(content, updated);
        }

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
    public static void UpgradeOldDbConnect(FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (file.Path.EndsWith("DB_connect.php", StringComparison.Ordinal))
        {
            file.Content.Replace("$DBLink = mysqli_connect ($host,$user,$pass) or mysql_errno() + mysqli_error($beta);",
                                 "$DBLink = mysqli_connect($host, $user, $pass);")
                        .Replace("if (!mysql_select_db( $DBname, $DBLink ))",
                                 "mysqli_select_db($DBLink, $DBname);\nif (mysqli_connect_errno())");
            if (!file.Content.Contains("exit()"))
            {
                file.Content.Replace("echo \"ERROR\";", "echo \"ERROR\";\nexit();");
            }
            (upgrader as MonaUpgrader).RenameVar(file.Content, "DBLink");
        }
    }

    public static void UpgradeRubiconModulesDB(FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("core", "modules", "core", "module.php"), StringComparison.Ordinal))
        {
            file.Content.Replace("mysql_pconnect($rubicon_db->mysql_hostname, $rubicon_db->mysql_username, $rubicon_db->mysql_password)",
                                 "mysqli_connect($rubicon_db->mysql_hostname, $rubicon_db->mysql_username, $rubicon_db->mysql_password)")
                        .Replace("mysql_select_db($rubicon_db->mysql_database, $its_connect)",
                                 "mysqli_select_db($its_connect, $rubicon_db->mysql_database)");
        }
    }

    private class MysqliQueryParamsFormat : IFormatProvider, ICustomFormatter
    {
        private char[]? _startChars;

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => arg switch
        {
            null => null,
            string var and { Length: > 0 } => (_startChars ??= new char[] { '$', '\"', '\'' }).Any(c => var[0] == c) ? var : '$' + var,
            var other => other.ToString()
        };

        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
    }
}

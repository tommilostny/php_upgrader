namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class HodnoceniConnect
{
    private static string? _connVar = null;
    private static string? _dbVar = null;

    private static readonly string[] _hodnoceniConnFiles = new[]
    {
        Path.Join("rss", "update_to_mysql.php"),
        Path.Join("rubicon", "modules", "category", "menu.php"),
    };

    private static readonly string[] _betaHodFiles = new[]
    {
        Path.Join("pdf", "p_listina.php"),
        Path.Join("pdf", "p_listina_u.php"),
        Path.Join("rss", "hodnoceni.php"),
    };

    private static readonly string[] _myBetaFiles = new[]
    {
        Path.Join("helios", "helios_export.php"),
    };

    /// <summary>
    /// Soubory pdf/p_listina.php, pdf/p_listina.php a rss/hodnoceni.php
    /// obsahují stejný kód využívající mysqli s proměnnou $beta_hod nebo $hodnoceni_conn.
    /// </summary>
    public static FileWrapper UpgradeHodnoceniDBCalls(this FileWrapper file)
    {
        if (AllUpgradableFiles().Any(f => file.Path.EndsWith(f, StringComparison.Ordinal)))
        {
            (_connVar, _dbVar) = GetConnectAndDbVariables(file);
            if (_connVar is not null && _dbVar is not null)
            {
                file.Content.Replace($"mysql_select_db(${_dbVar}, ${_connVar})", $"mysqli_select_db(${_connVar}, ${_dbVar})")
                            .Replace($"mysql_errno(${_connVar})", $"mysqli_errno(${_connVar})")
                            .Replace($"mysql_error(${_connVar})", $"mysqli_error(${_connVar})")
                            .Replace("mysqli_error($beta)", $"mysqli_error(${_connVar})")
                            .Replace(MysqliQueryBetaRegex().Replace(file.Content.ToString(), _mysqliQueryEvaluator));
            }
            _connVar = _dbVar = null;
        }
        return file;
    }

    private static readonly MatchEvaluator _mysqliQueryEvaluator = new(match =>
    {
        return new StringBuilder(match.Value)
            .Replace("mysqli_query($beta,", $"mysqli_query(${_connVar},")
            .Replace($", ${_connVar})", ")")
            .Replace($",${_connVar})", ")")
            .ToString();
    });

    private static (string?, string?) GetConnectAndDbVariables(FileWrapper file)
    {
        if (_hodnoceniConnFiles.Any(f => file.Path.EndsWith(f, StringComparison.Ordinal)))
        {
            return ("hodnoceni_conn", "database_hodnoceni_conn");
        }
        if (_betaHodFiles.Any(f => file.Path.EndsWith(f, StringComparison.Ordinal)))
        {
            return ("beta_hod", "dtb_hod");
        }
        if (_myBetaFiles.Any(f => file.Path.EndsWith(f, StringComparison.Ordinal)))
        {
            return ("mybeta", "database_beta");
        }
        return (null, null);
    }

    private static IEnumerable<string> AllUpgradableFiles()
    {
        foreach (var file in _hodnoceniConnFiles)
            yield return file;

        foreach (var file in _betaHodFiles)
            yield return file;

        foreach (var file in _myBetaFiles)
            yield return file;
    }

    [GeneratedRegex(@"mysqli_query\(\$beta,.+;", RegexOptions.None, matchTimeoutMilliseconds: 66666)]
    private static partial Regex MysqliQueryBetaRegex();
}

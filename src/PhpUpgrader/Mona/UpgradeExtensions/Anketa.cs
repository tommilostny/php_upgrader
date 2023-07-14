namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class Anketa
{
    /// <summary>
    /// upravit soubor anketa/anketa.php - r.3 (odmazat ../)
    ///     - include_once "../setup.php"; na include_once "setup.php";
    /// </summary>
    public static FileWrapper UpgradeAnketa(this FileWrapper file)
    {
        var s = Path.DirectorySeparatorChar;
        if (file.Path.Contains($"{s}anketa{s}", StringComparison.Ordinal))
        {
            var isAnketa = file.Path.EndsWith($"{s}anketa.php", StringComparison.Ordinal);
            if (isAnketa)
            {
                file.Content.Replace(@"include_once(""../setup.php"")", @"include_once(""setup.php"")");
            }
            var containsConnect = file.Content.Contains("mysqli_connect");
            if (containsConnect)
            {
                file.Content.Replace("mysqli_connect($server, $user, $pass) or die", "$link = mysqli_connect($server, $user, $pass);\n//mysqli_connect($server, $user, $pass) or die")
                    .Replace("mysql_select_db($db) or die", "mysqli_select_db($link, $db);\nif(mysqli_connect_errno())\n  {\n    printf(\"Nelze navázat spojení s databazí: %s\\n\", mysqli_connect_error());\n    exit();\n  }\n//mysql_select_db($db) or die")
                    .Replace("$beta", "$link");
            }
            if (isAnketa && containsConnect)
            {
                file.Content.Append("\n<?php mysqli_close($link); ?>");
            }
            if (file.Path.EndsWith($"{s}beta.php", StringComparison.Ordinal))
            {
                file.Content.Replace("$beta = mysql_pconnect($hostname_beta, $username_beta, $password_beta)", "$beta = mysqli_connect($hostname_beta, $username_beta, $password_beta);\n\nmysqli_select_db($beta, $database_beta);\n\nif(mysqli_connect_errno())\n  {\n    printf(\"Nelze navázat spojení s databazí: %s\\n\", mysqli_connect_error());\n    exit();\n  }\n//$beta = mysql_pconnect($hostname_beta, $username_beta, $password_beta)");
            }
        }
        if (file.Path.EndsWith($"{s}config.php", StringComparison.Ordinal) && file.Content.Contains("MySQL_Connect", StringComparison.Ordinal))
        {
            file.Content.Replace("MySQL_Connect($server, $user, $pass);", "global $link;\n$link = mysqli_connect($server, $user, $pass);")
                .Replace("MySQL_Select_DB($db);", "mysqli_select_db($link, $db);\nif(mysqli_connect_errno())\n  {\n    printf(\"Nelze navázat spojení s databazí: %s\\n\", mysqli_connect_error());\n    exit();\n  }")
                .Replace("$beta", "$link");
        }
        return file;
    }
}

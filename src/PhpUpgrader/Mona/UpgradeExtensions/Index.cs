namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class Index
{
    private static string? _rootIndexPhp = null;
    private static string? _templateIndexPhp = null;

    /// <summary> Přidá mysqli_close nebo pg_close na konec soubor index.php. </summary>
    public static FileWrapper UpgradeIndex(this FileWrapper file, MonaUpgrader upgrader)
    {
        if (IsRootIndexFile(file.Path, upgrader))
        {
            var closeArg = upgrader is RubiconUpgrader ? "pg_close" : "mysqli_close";
            if (!file.Content.Contains(closeArg))
            {
                file.Content.Append(HasClosingPhpTag(file) ? "\n<?php " : "\n")
                    .Append(new CloseFunctionFormat(), $"{closeArg}($beta); ?>");
            }
            if (file.Content.Contains("include \"smerovani.php\";")
                && !File.Exists(Path.Join(new FileInfo(file.Path).Directory.FullName, "smerovani.php")))
            {
                file.Content.Replace("include \"smerovani.php\";", "//include \"smerovani.php\";");
            }
        }
        return file;
    }

    private static bool IsRootIndexFile(string path, MonaUpgrader upgrader)
    {
        _rootIndexPhp ??= Path.Join(upgrader.WebName, "index.php");
        _templateIndexPhp ??= Path.Join("templates", upgrader.WebName, "index.php");

        return (path.EndsWith(_rootIndexPhp, StringComparison.Ordinal)
            && !path.EndsWith(_templateIndexPhp, StringComparison.Ordinal))
            || upgrader.OtherRootFolders?.Any(rf =>
            {
                return path.EndsWith(Path.Join(upgrader.WebName, rf, "index.php"), StringComparison.Ordinal)
                    || path.EndsWith(Path.Join(upgrader.WebName, rf, "index_new.php"), StringComparison.Ordinal);
            })
            == true;
    }

    private static bool HasClosingPhpTag(FileWrapper file)
    {
        for (var i = file.Content.Length - 2; i >= 0; i--)
        {
            if (file.Content[i] == '?'
                && file.Content[i + 1] == '>')
                return true;
            if (i < file.Content.Length - 5
                && file.Content[i] == '<'
                && file.Content[i + 1] == '?'
                && file.Content[i + 2] == 'p'
                && file.Content[i + 3] == 'h'
                && file.Content[i + 4] == 'p')
                return false;
        }
        return true;
    }

    private class CloseFunctionFormat : IFormatProvider, ICustomFormatter
    {
        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => arg switch
        {
            not null and string stringArg when stringArg.EndsWith("_close", StringComparison.OrdinalIgnoreCase) => stringArg,
            _ => null
        };

        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
    }
}

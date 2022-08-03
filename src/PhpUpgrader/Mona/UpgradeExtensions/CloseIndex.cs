namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class CloseIndex
{
    /// <summary> Přidá mysqli_close nebo pg_close na konec soubor index.php. </summary>
    public static FileWrapper UpgradeCloseIndex(this FileWrapper file, MonaUpgrader upgrader)
    {
        if (!IsRootIndexFile(file.Path, upgrader))
        {
            return file;
        }
        var closeArg = upgrader is RubiconUpgrader ? "pg_close" : "mysqli_close";

        if (!file.Content.Contains(closeArg))
        {
            file.Content.Append(new CloseFunctionFormat(), $"\n<?php {closeArg}($beta); ?>");
        }
        return file;
    }

    private static bool IsRootIndexFile(string path, MonaUpgrader upgrader)
    {
        const string indexFile = "index.php";
        return path.EndsWith(Path.Join(upgrader.WebName, indexFile), StringComparison.Ordinal)
            || upgrader.OtherRootFolders?.Any(rf => path.EndsWith(Path.Join(upgrader.WebName, rf, indexFile), StringComparison.Ordinal)) == true;
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

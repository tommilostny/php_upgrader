namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class CloseIndex
{
    /// <summary> Přidá mysqli_close nebo pg_close na konec soubor index.php. </summary>
    public static FileWrapper UpgradeCloseIndex(this FileWrapper file, MonaUpgrader upgrader)
    {
        if (IsRootIndexFile(file.Path, upgrader))
        {
            var closeArg = upgrader is RubiconUpgrader ? "pg_close" : "mysqli_close";
            if (!file.Content.Contains(closeArg))
            {
                file.Content.Append(new CloseFunctionFormat(), $"\n<?php {closeArg}($beta); ?>");
            }
        }
        return file;
    }

    private static bool IsRootIndexFile(string path, MonaUpgrader upgrader)
    {
        return path.EndsWith(Path.Join(upgrader.WebName, "index.php"), StringComparison.Ordinal)
            || upgrader.OtherRootFolders?.Any(rf =>
            {
                return path.EndsWith(Path.Join(upgrader.WebName, rf, "index.php"), StringComparison.Ordinal)
                    || path.EndsWith(Path.Join(upgrader.WebName, rf, "index_new.php"), StringComparison.Ordinal);
            })
            == true;
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

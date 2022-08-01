namespace PhpUpgrader;

internal static class StringBuilderExtensions
{
    /// <summary>
    /// Zjistí, zda instance <seealso cref="StringBuilder"/> obsahuje daný podřetězec.
    /// </summary>
    /// <param name="source">Kde se vyhledává?</param>
    /// <param name="value">Jaký podřetězec se vyhledává?</param>
    /// <param name="cmpType">Způsob porovnání řetězců (<seealso cref="StringComparison.Ordinal"/> je dobrý rychlý default).</param>
    /// <returns>Příznak existence podřetězce.</returns>
    internal static bool Contains(this StringBuilder source, ReadOnlySpan<char> value, StringComparison cmpType = StringComparison.Ordinal)
    {
        Span<char> window = stackalloc char[value.Length << 1];

        for (var i = 0; i < source.Length; i += value.Length)
        {
            Span<char> higherHalf = window[value.Length..];
            higherHalf.CopyTo(window);
            higherHalf.Clear();

            var maxCopyCount = source.Length - i;
            source.CopyTo(i, higherHalf, Math.Min(value.Length, maxCopyCount));

            if (((ReadOnlySpan<char>)window).Contains(value, cmpType))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Zjistí, zda instance <seealso cref="StringBuilder"/> obsahuje daný znak.
    /// </summary>
    /// <param name="source">Kde se vyhledává?</param>
    /// <param name="value">Jaký znak se vyhledává?</param>
    /// <returns>Příznak existence znaku.</returns>
    internal static bool Contains(this StringBuilder source, char value)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == value)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Rozdělí obsah daného <seealso cref="StringBuilder"/>u
    /// do seznamu <seealso cref="StringBuilder"/>ů dle daného oddělovače.
    /// </summary>
    /// <param name="source">Řetězec k rozdělení.</param>
    /// <param name="delimiter">Oddělovací znak v řezězci.</param>
    /// <returns>Rozdělený seznam nových instancí <seealso cref="StringBuilder"/>.</returns>
    internal static List<StringBuilder> Split(this StringBuilder source, char delimiter = '\n')
    {
        var current = new StringBuilder();
        var collection = new List<StringBuilder>();

        for (var i = 0; i < source.Length; i++)
        {
            var character = source[i];
            if (character == delimiter)
            {
                collection.Add(current);
                current = new();
                continue;
            }
            current.Append(character);
        }
        if (current.Length > 0)
        {
            collection.Add(current);
        }
        return collection;
    }

    /// <summary>
    /// Spojí řetězcovou reprezentaci prvků dané kolekce
    /// oddělenou daným znakem separátoru
    /// a nahradí tím obsah cílové instance <seealso cref="StringBuilder"/>.
    /// </summary>
    /// <param name="source">Kolekce prvků ke spojení do řetězce.</param>
    /// <param name="destination">Cílový StringBuilder, jehož obsah bude nahrazen.</param>
    /// <param name="separator">Oddělovač prvků kolekce.</param>
    internal static void JoinInto<T>(this IEnumerable<T> source, StringBuilder destination, char separator = '\n')
    {
        destination.Clear();
        destination.AppendJoin(separator, source);
    }

    /// <summary>
    /// Spojí řetězcovou reprezentaci prvků dané kolekce
    /// oddělenou daným znakem separátoru
    /// a nahradí tím obsah cílové instance <seealso cref="StringBuilder"/>.
    /// </summary>
    /// <param name="source">Kolekce prvků ke spojení do řetězce.</param>
    /// <param name="destination">Cílový StringBuilder, jehož obsah bude nahrazen.</param>
    /// <param name="separator">Oddělovač prvků kolekce.</param>
    internal static void JoinInto<T>(this IEnumerable<T> source, StringBuilder destination, string separator)
    {
        destination.Clear();
        destination.AppendJoin(separator, source);
    }

    /// <summary>
    /// Spočítá výskyty daného znaku v instanci <seealso cref="StringBuilder"/>.
    /// </summary>
    /// <param name="source">Řetězec, kde se počítá.</param>
    /// <param name="value">Hledaná hodnota.</param>
    /// <returns>Počet výskytů daného znaku.</returns>
    internal static int Count(this StringBuilder source, char value)
    {
        var count = 0;
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == value)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Nalezne index dané hodnoty v instanci <seealso cref="StringBuilder"/>.
    /// </summary>
    /// <param name="source">Řetězec, kde se hledá.</param>
    /// <param name="value">Hledaná hodnota.</param>
    /// <returns>Index začátku <paramref name="value"/> v <paramref name="source"/> nebo -1, pokud není nalezena.</returns>
    internal static int IndexOf(this StringBuilder source, ReadOnlySpan<char> value)
    {
        for (var (i, j) = (0, 0); i < source.Length; i++)
        {
            if (source[i] != value[j])
            {
                j = 0;
                continue;
            }
            if (++j == value.Length)
            {
                return i - j;
            }
        }
        return -1;
    }

    /// <summary>
    /// Nahradí <paramref name="oldValue"/> za <paramref name="newValue"/> v instanci <seealso cref="StringBuilder"/>.
    /// </summary>
    /// <param name="source">Řetězec, kde se nahrazuje.</param>
    /// <param name="oldValue">Hledaná hodnota k nahrazení.</param>
    /// <param name="newValue">Nová hodnota, kterou se nahrazuje.</param>
    /// <param name="startIndex">Od jakého indexu začít hledat <paramref name="oldValue"/>.</param>
    internal static StringBuilder Replace(this StringBuilder source, string oldValue, string? newValue, int startIndex)
    {
        return source.Replace(oldValue, newValue, startIndex, count: source.Length - startIndex);
    }
}

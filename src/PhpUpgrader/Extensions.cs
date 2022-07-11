namespace PhpUpgrader;

internal static class Extensions
{
    /// <summary>
    /// Spojí řetězcovou reprezentaci prvků dané kolekce
    /// oddělenou daným znakem separátoru
    /// a nahradí tím obsah cílové instance <seealso cref="StringBuilder"/>.
    /// </summary>
    /// <param name="collection">Kolekce prvků ke spojení do řetězce.</param>
    /// <param name="destination">Cílový StringBuilder, jehož obsah bude nahrazen.</param>
    /// <param name="separator">Oddělovač prvků kolekce.</param>
    internal static void JoinInto<T>(this List<T> collection, StringBuilder destination, char separator = '\n')
    {
        destination.Clear();
        destination.AppendJoin(separator, collection);
    }

    /// <summary>
    /// Spojí řetězcovou reprezentaci prvků dané kolekce
    /// oddělenou daným znakem separátoru
    /// a nahradí tím obsah cílové instance <seealso cref="StringBuilder"/>.
    /// </summary>
    /// <param name="collection">Kolekce prvků ke spojení do řetězce.</param>
    /// <param name="destination">Cílový StringBuilder, jehož obsah bude nahrazen.</param>
    /// <param name="separator">Oddělovač prvků kolekce.</param>
    internal static void JoinInto<T>(this List<T> collection, StringBuilder destination, string separator)
    {
        destination.Clear();
        destination.AppendJoin(separator, collection);
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

        for (int i = 0; i < source.Length; i++)
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
    /// Zjistí, zda instance <seealso cref="StringBuilder"/> obsahuje daný podřetězec.
    /// </summary>
    /// <param name="stringBuilder">Kde se vyhledává?</param>
    /// <param name="value">Jaký podřetězec se vyhledává?</param>
    /// <returns>Příznak existence podřetězce.</returns>
    internal static bool Contains(this StringBuilder stringBuilder, ReadOnlySpan<char> value)
    {
        Span<char> chunk = stackalloc char[value.Length << 1];

        for (int i = 0; i < stringBuilder.Length; i += value.Length)
        {
            Span<char> higher = chunk[value.Length..];
            higher.CopyTo(chunk);
            higher.Clear();

            var maxCopyCount = stringBuilder.Length - i;
            stringBuilder.CopyTo(i, higher, Math.Min(value.Length, maxCopyCount));

            if (chunk.Contains(value))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Zjistí existenci podřetězce v paměťovém rozsahu (<seealso cref="Span{T}"/>).
    /// </summary>
    /// <typeparam name="T">Typ prvku řetězce, který bude porovnáván (<seealso cref="EqualityComparer{T}"/>).</typeparam>
    /// <param name="span">Pole/rozsah, ve kterém se vyhledává.</param>
    /// <param name="value">Podřetězec k vyhledání.</param>
    /// <returns>Příznak existence podřetězce.</returns>
    private static bool Contains<T>(this Span<T> span, ReadOnlySpan<T> value)
    {
        for (int i = 0, j = 0; i < span.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(span[i], value[j]))
            {
                j = 0;
                continue;
            }
            if (++j == value.Length)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Zjistí, zda instance <seealso cref="StringBuilder"/> obsahuje daný znak.
    /// </summary>
    /// <param name="stringBuilder">Kde se vyhledává?</param>
    /// <param name="value">Jaký znak se vyhledává?</param>
    /// <returns>Příznak existence znaku.</returns>
    internal static bool Contains(this StringBuilder stringBuilder, char value)
    {
        for (int i = 0; i < stringBuilder.Length; i++)
        {
            if (stringBuilder[i] == value)
            {
                return true;
            }
        }
        return false;
    }
}

namespace Core.Reports;

public static class EnumerableExtensions
{
    // Додаткове завдання 2: топ-N за цілим ключем (за спаданням) з детермінованим другим ключем.
    // Повертає відкладений запит; матеріалізує той, хто викликає (ToList).
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source, Func<T, int> key, int n, Func<T, string>? tieBreaker = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);

        var ordered = source.OrderByDescending(key);
        var stable = tieBreaker is null
            ? ordered
            : ordered.ThenBy(tieBreaker, StringComparer.Ordinal);
        return stable.Take(Math.Max(n, 0));
    }
}

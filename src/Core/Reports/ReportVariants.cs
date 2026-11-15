using Core.Domain;

namespace Core.Reports;

// Додаткове завдання 4: варіанти одного звіту для порівняння часу у Stopwatch (див. Cli/Benchmark.cs).
// Звіт: сумарний залишок за одиницею серед позицій із залишком >= minQuantity.
public static class ReportVariants
{
    // A: один потоковий ланцюжок без проміжного ToList.
    public static IReadOnlyList<GroupSummary> Streaming(IEnumerable<Product> source, int minQuantity) =>
        source.Where(p => p.Quantity >= minQuantity)
              .GroupBy(p => p.Unit)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // B: ToList посеред ланцюжка — зайва копія 100 000 елементів у пам'яті.
    public static IReadOnlyList<GroupSummary> MidToList(IEnumerable<Product> source, int minQuantity)
    {
        var filtered = source.Where(p => p.Quantity >= minQuantity).ToList();
        return filtered.GroupBy(p => p.Unit)
                       .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
                       .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
                       .ToList();
    }

    // C: один відкладений запит перебирається тричі — кожне перебирання повторює Where.
    public static (int Count, int Total, int Max) UsedThrice(IEnumerable<Product> source, int minQuantity)
    {
        var query = source.Where(p => p.Quantity >= minQuantity);
        return (query.Count(), query.Sum(p => p.Quantity), query.Max(p => p.Quantity));
    }

    // D: той самий результат, але запит матеріалізовано один раз.
    public static (int Count, int Total, int Max) Materialized(IEnumerable<Product> source, int minQuantity)
    {
        var list = source.Where(p => p.Quantity >= minQuantity).ToList();
        return (list.Count, list.Sum(p => p.Quantity), list.Max(p => p.Quantity));
    }
}

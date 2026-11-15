using Core.Domain;

namespace Core.Reports;

// Усі запити — чисті функції над IEnumerable<Product>: без сховища, виводу і побічних ефектів.
// Кожен звіт повертає IReadOnlyList<DTO> (уже матеріалізований), а не відкладений запит.
public static class ReportService
{
    // 1) фільтр + сортування + проєкція. Where — перед OrderBy, щоб не сортувати зайве.
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(ToLine)
              .ToList();

    // 2) топ-N зі стабільним порядком: основний ключ — залишок, додатковий унікальний — SKU.
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, n, p => p.Sku)
              .Select(ToLine)
              .ToList();

    // 3) групування з підрахунком і сумою. Ключ — значення (рядок), а не посилання на об'єкт.
    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // 4) агрегати з обробкою порожньої послідовності (Average/Max на порожній кидають виняток).
    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0, 0)
            : (list.Count, list.Sum(p => p.Quantity), list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    // Додатково 2: TopBy вдруге — топ одиниць виміру за сумарним залишком.
    public static IReadOnlyList<GroupSummary> TopUnits(IEnumerable<Product> source, int n) =>
        ByUnit(source).TopBy(s => s.TotalQuantity, n, s => s.Key).ToList();

    // Додатково 3: той самий звіт, що й ByUnit, у синтаксисі запитів.
    public static IReadOnlyList<GroupSummary> ByUnitQuery(IEnumerable<Product> source) =>
        (from product in source
         group product by product.Unit into g
         orderby g.Sum(p => p.Quantity) descending, g.Key
         select new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
        .ToList();

    // Додатково 1: Join — лише товари, що мають рухи; одна пара = один рядок.
    public static IReadOnlyList<MovementLine> Movements(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        products.Join(movements,
                      product => product.Id,
                      movement => movement.ProductId,
                      (product, movement) => new MovementLine(product.Sku, movement.Delta))
                .OrderBy(m => m.Sku, StringComparer.Ordinal).ThenBy(m => m.Delta)
                .ToList();

    // Додатково 1: GroupJoin — усі товари, навіть без рухів (Moves = 0, NetDelta = 0).
    public static IReadOnlyList<MovementSummary> MovementSummaries(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        products.GroupJoin(movements,
                           product => product.Id,
                           movement => movement.ProductId,
                           (product, ms) => new MovementSummary(
                               product.Sku, product.Name, ms.Count(), ms.Sum(m => m.Delta)))
                .OrderBy(s => s.Sku, StringComparer.Ordinal)
                .ToList();

    private static StockLine ToLine(Product p) => new(p.Sku, p.Name, p.Quantity, p.Unit);
}

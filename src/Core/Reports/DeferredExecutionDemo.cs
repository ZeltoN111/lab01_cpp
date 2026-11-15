using Core.Domain;

namespace Core.Reports;

public sealed record DeferredDemoResult(
    int Before, int After, int SnapshotCount, int SnapshotCountAfterClear, int SourceCountAfterClear);

// Крок 3: демонстрація відкладеного виконання. Джерело — локальний список (service.All()
// щоразу повертає новий знімок, тому на ньому зміну було б не видно). Друку тут немає:
// Core повертає числа, Cli їх виводить.
public static class DeferredExecutionDemo
{
    public static DeferredDemoResult Run(IEnumerable<Product> items, int threshold = 10)
    {
        var source = items.ToList();
        var query = source.Where(p => p.Quantity < threshold);   // запит побудовано, не виконано

        int before = query.Count();                              // перше виконання
        source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
        int after = query.Count();                               // друге виконання бачить новий елемент

        var snapshot = query.ToList();                           // результат зафіксовано
        source.Clear();
        return new DeferredDemoResult(before, after, snapshot.Count, snapshot.Count, source.Count);
    }
}

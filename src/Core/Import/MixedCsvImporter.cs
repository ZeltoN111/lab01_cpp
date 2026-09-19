using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ProductRow p:
                    products.Add(p.Value);
                    break;
                case WarehouseRow w:
                    warehouses.Add(w.Value);
                    break;
                case FailedRow f:
                    errors.Add($"рядок {number}: {f.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, warehouses, errors);
    }

    private static RowOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", var id, var sku, var name, var unit, var qty]
                when int.TryParse(qty, out int q) && q >= 0
                => new ProductRow(new ProductDto(id, sku, name, unit, q)),

            ["W", var id, var name, var location]
                => new WarehouseRow(new WarehouseDto(id, name, location)),

            [var prefix, ..]
                => new FailedRow($"невідомий тип рядка '{prefix}' або неправильна кількість колонок"),

            _ => new FailedRow("порожній або нерозпізнаний рядок")
        };
    }

    private abstract record RowOutcome;
    private sealed record ProductRow(ProductDto Value) : RowOutcome;
    private sealed record WarehouseRow(WarehouseDto Value) : RowOutcome;
    private sealed record FailedRow(string Reason) : RowOutcome;
}
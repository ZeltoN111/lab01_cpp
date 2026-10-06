using Core.Domain;
using Core.Dto;
using Core.Import;

Console.WriteLine("=== Імпорт CSV → DTO → домен (зв'язок з тижнем 3) ===");

string csvPath = Path.Combine("data", "sample.csv");
ImportResult<ProductDto> parsed = ProductCsvImporter.Load(csvPath);
ImportResult<Product> domainResult = ProductDomainImporter.ToDomain(parsed);

Console.WriteLine($"Успішно створено сутностей: {domainResult.Items.Count}");
Console.WriteLine($"Відхилено (помилка парсингу або порушення інваріанту): {domainResult.Errors.Count}");
foreach (string e in domainResult.Errors)
    Console.WriteLine($"  ! {e}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 1: успіх ===");
Console.WriteLine();

Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
Console.WriteLine(product);

Console.WriteLine();

product.RegisterArrival(50);
Console.WriteLine(product);


Console.WriteLine();

product.Issue(30);
Console.WriteLine(product);

Console.WriteLine();


product.ChangeStatus(ProductStatus.Discontinued);
Console.WriteLine($"Статус товару {product.Sku}: {product.Status}");

Console.WriteLine();

Warehouse warehouse = Warehouse.Create("W-001", "Головний склад", "Львів, вул. Промислова 1", capacity: 200);
Console.WriteLine(warehouse);

Console.WriteLine();

Product sand = Product.Create("P-005", "SKU-005", "Пісок", "т", 60);
Product[] onWarehouse = [product];

WarehouseCapacityPolicy.EnsureCanAcceptArrival(warehouse, onWarehouse, incomingAmount: 50);
Console.WriteLine($"Прихід 50 од. на склад {warehouse.Id} (було {product.Quantity}, місткість {warehouse.Capacity}) — дозволено");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
Console.WriteLine();
TryDo("видача більша за залишок", () => product.Issue(1000));
Console.WriteLine();
TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
Console.WriteLine();
TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
Console.WriteLine();
TryDo("нульова видача", () => product.Issue(0));
Console.WriteLine();
TryDo("порожня назва складу", () => Warehouse.Create("W-002", " ", "Київ"));
Console.WriteLine();
TryDo("недопустимий перехід статусу (Active → Archived напряму)",
    () => Product.Create("P-004", "SKU-004", "Шпаклівка", "кг", 10).ChangeStatus(ProductStatus.Archived));
Console.WriteLine();
TryDo("повторний перехід у той самий стан (Discontinued → Discontinued)",
    () => product.ChangeStatus(ProductStatus.Discontinued));
Console.WriteLine();
TryDo("прихід, що перевищує місткість складу",
    () => WarehouseCapacityPolicy.EnsureCanAcceptArrival(warehouse, [product, sand], incomingAmount: 50));

Console.WriteLine();
Console.WriteLine($"Підсумковий залишок {product.Sku}: {product.Quantity} {product.Unit} (не змінився після відмов)");

return 0;

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}


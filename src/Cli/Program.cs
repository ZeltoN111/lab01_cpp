using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;

// Composition root: єдине місце, де створюються конкретні класи й встановлюються зв'язки.
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

bool useCache = args.Contains("--cache");
bool useFactory = args.Contains("--factory");

ICatalogStore store;
if (useFactory)
{
    store = StoreFactory.Create(args); // варіант B: вибір сховища винесено у фабрику
}
else
{
    store = useFile
        ? new FileCatalogStore(dataPath)
        : new InMemoryCatalogStore(SampleData.Products());
    if (useCache) store = new CachingCatalogStore(store); // декоратор над будь-яким сховищем
}
var service = new CatalogService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
if (useFile) Console.WriteLine($"Файл: {dataPath}");
Console.WriteLine();

// --- Сценарій 1: додати, змінити, знайти, показати список
Console.WriteLine($"Записів на старті: {service.All().Count}");

var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
Console.WriteLine($"Додано: {created}");

service.Receive(created.Id, 25);
Console.WriteLine($"Прихід +25 → {service.Find(created.Id)}");

service.Issue(created.Id, 10);
Console.WriteLine($"Видача −10 → {service.Find(created.Id)}");

Console.WriteLine();
Console.WriteLine($"Пошук за id={created.Id}: {service.Find(created.Id)?.Name ?? "не знайдено"}");
Console.WriteLine($"Пошук за id=nope: {service.Find("nope")?.Name ?? "не знайдено"}");

Console.WriteLine();
Console.WriteLine("Пошук з предикатом (залишок менше 100):");
foreach (var p in service.Search(p => p.Quantity < 100))
    Console.WriteLine($"  {p.Id,-9} {p.Name,-32} {p.Quantity,6} {p.Unit}");

Console.WriteLine();
Console.WriteLine("Список:");
foreach (var p in service.All())
    Console.WriteLine($"  {p.Id,-9} {p.Sku,-10} {p.Name,-32} {p.Quantity,6} {p.Unit}");

// --- Сценарій 2: відмови з людськими повідомленнями
Console.WriteLine();
Console.WriteLine("Сценарій відмови:");
TryDo("дубль id", () => store.Add(Core.Domain.Product.Create(created.Id, "SKU-102", "Дубль", "шт", 1)));
TryDo("прихід на неіснуючий id", () => service.Receive("no-such-id", 5));
TryDo("видача більша за залишок", () => service.Issue(created.Id, 1000));

return 0;

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: помилку НЕ викликано!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.Message}");
    }
}

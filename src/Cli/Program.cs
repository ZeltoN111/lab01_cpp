using Core;
using Core.Abstractions;
using Core.Events;
using Core.Import;
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

// Лаба 6: лог-файл формується через Path.Combine, каталог створюється кодом.
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

// --sinks: додаткове завдання 1 — ті самі сповіщення, але через INotificationSink у конструкторі
// (замість підписки на подію). Порівняння з подією — у README.
bool useSinks = args.Contains("--sinks");
var service = useSinks
    ? new CatalogService(store, [new ConsoleSink(), new FileSink(logPath)])
    : new CatalogService(store);

// --- Підписка (лаба 6): локальний метод, щоб потім можна було відписатися
void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
{
    var line = LogFormat.Line(e);
    Console.WriteLine(line);
    try
    {
        File.AppendAllText(logPath, line + Environment.NewLine);
    }
    catch (IOException ex) // виняток у підписнику не має ламати бізнес-операцію
    {
        Console.WriteLine($"  [не вдалося записати лог: {ex.Message}]");
    }
}

// Другий підписник — лямбда із замиканням над локальною змінною
int changes = 0;
if (!useSinks) service.Changed += OnCatalogChanged; // у режимі --sinks вивід робить sink
service.Changed += (_, _) => changes++;

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
Console.WriteLine($"Подій отримано: {changes}");

// --- Відписка: операція виконується, повідомлення немає (лічильник-лямбда ще підписана)
service.Changed -= OnCatalogChanged;
service.Receive(created.Id, 5); // тиха операція для консолі й файлу
Console.WriteLine($"Після відписки: {service.Find(created.Id)}");
Console.WriteLine($"Подій отримано: {changes}"); // +1 лише від лямбди-лічильника

Console.WriteLine();
Console.WriteLine($"Пошук за id={created.Id}: {service.Find(created.Id)?.Name ?? "не знайдено"}");
Console.WriteLine($"Пошук за id=nope: {service.Find("nope")?.Name ?? "не знайдено"}");

Console.WriteLine();
Console.WriteLine("FindAll(p => p.Quantity < 100) — Func замість події:");
foreach (var p in service.FindAll(p => p.Quantity < 100))
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
TryDo("прихід від'ємної кількості", () => service.Receive(created.Id, -5));
Console.WriteLine($"Подій отримано: {changes} (не змінилось: відмови подій не дали)");

// --- Видалення: ще один вид події
Console.WriteLine();
if (!useSinks) service.Changed += OnCatalogChanged; // підписуємось один раз, не в циклі
service.Remove(created.Id);
service.Changed -= OnCatalogChanged;
Console.WriteLine($"Remove неіснуючого id → {service.Remove("nope")} (події немає)");

// --- Додаткове завдання 2: Action<string> як звіт про пропущені рядки імпорту
Console.WriteLine();
Console.WriteLine("Імпорт data/sample.csv з Action<string> для пропущених рядків:");
string csvPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "sample.csv");
if (File.Exists(csvPath))
{
    var imported = ProductDomainImporter.ToDomain(
        ProductCsvImporter.Load(csvPath), skipped => Console.WriteLine($"  пропущено: {skipped}"));
    Console.WriteLine($"  імпортовано {imported.Items.Count}, пропущено {imported.Errors.Count}");
}
else Console.WriteLine("  файл не знайдено");

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

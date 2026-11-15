using Core;
using Core.Abstractions;
using Core.Events;
using Core.Import;
using Core.Reports;
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


var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);


bool useSinks = args.Contains("--sinks");
var service = useSinks
    ? new CatalogService(store, [new ConsoleSink(), new FileSink(logPath)])
    : new CatalogService(store);


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


int changes = 0;
if (!useSinks) service.Changed += OnCatalogChanged;
service.Changed += (_, _) => changes++;

// Лаба 7: підкоманда — перший аргумент без "--" (прапорці --file/--cache не є назвою звіту).
// Без підкоманди виконується звіт report-top; сценарій лаб 5-6 доступний як "scenario".
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

// Лаба 8: асинхронний імпорт постачань (top-level await) і генерація великого файлу для скасування.
if (command == "import")
{
    service.Changed -= OnCatalogChanged; // на сотнях тисяч записів рядок у консоль на кожну подію лише гальмує
    var result = await ImportCommand.RunAsync(service, args);
    Console.WriteLine($"  подій Added: {changes}");
    return result;
}
if (command == "gen-big") return await ImportCommand.GenerateBigAsync(args);

if (command != "scenario")
{
    // --empty: порожнє джерело, щоб показати, що звіти не падають.
    IReadOnlyList<Core.Domain.Product> items = args.Contains("--empty") ? [] : service.All();
    return RunReport(command, items);
}

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

static int RunReport(string command, IReadOnlyList<Core.Domain.Product> items)
{
    switch (command)
    {
        case "report-top":
            Print(ReportService.TopByQuantity(items, 5));
            break;
        case "report-low":
            Print(ReportService.LowStock(items, 10));
            break;
        case "report-groups":
            PrintGroups(ReportService.ByUnit(items));
            break;
        case "report-sum":
            var t = ReportService.Totals(items);
            Console.WriteLine($"  позицій {t.Items}, всього {t.Total}, середнє {t.Average:F2}, макс {t.Max}");
            break;
        case "report-top-units": // TopBy вдруге
            PrintGroups(ReportService.TopUnits(items, 3));
            break;
        case "report-groups-query": // той самий звіт у синтаксисі запитів
            PrintGroups(ReportService.ByUnitQuery(items));
            break;
        case "report-movements": // Join і GroupJoin
            Console.WriteLine("  Join (лише товари з рухами):");
            foreach (var m in ReportService.Movements(items, SampleData.Movements()))
                Console.WriteLine($"    {m.Sku,-10} {m.Delta,6:+0;-0}");
            Console.WriteLine("  GroupJoin (усі товари):");
            foreach (var s in ReportService.MovementSummaries(items, SampleData.Movements()))
                Console.WriteLine($"    {s.Sku,-10} {s.Name,-32} рухів: {s.Moves,2}  разом: {s.NetDelta,6:+0;-0;0}");
            break;
        case "report-deferred": // відкладене виконання
            var d = DeferredExecutionDemo.Run(items);
            Console.WriteLine($"  Дефіцитних до змін: {d.Before}");
            Console.WriteLine($"  Дефіцитних після змін: {d.After}");
            Console.WriteLine($"  У знімку лишилось: {d.SnapshotCountAfterClear} (джерело після Clear: {d.SourceCountAfterClear})");
            break;
        case "report-bench":
            Benchmark.Run();
            break;
        default:
            Console.WriteLine("report-top | report-low | report-groups | report-sum | report-top-units |");
            Console.WriteLine("report-groups-query | report-movements | report-deferred | report-bench | scenario");
            Console.WriteLine("import [шлях] [--stream] [--cancel-after=мс] [--timeout=мс] | gen-big [кількість]");
            break;
    }
    return 0;
}

static void Print(IReadOnlyList<StockLine> lines)
{
    foreach (var l in lines)
        Console.WriteLine($"  {l.Sku,-10} {l.Name,-32} {l.Quantity,6} {l.Unit}");
}

static void PrintGroups(IReadOnlyList<GroupSummary> groups)
{
    foreach (var g in groups)
        Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3}  сумарно: {g.TotalQuantity,6}");
}

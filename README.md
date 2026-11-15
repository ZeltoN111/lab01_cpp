 # CrossApp

 Наскрізний проєкт з крос-платформного програмування на .NET. Домен: **Склад** —
 облік залишків товарів по партіях.

 ## Структура solution

 ```text
 CrossApp/
   CrossApp.slnx
   README.md
   data/
     sample.csv / sample.json / sample_mixed.csv   (тестові дані для імпорту)
   src/
     Core/
       Core.csproj              (multi-targeting: net8.0;net10.0)
       EnvironmentInfo.cs        (інформація про середовище виконання)
       Dto/                      (record-и — формат файлу: ProductDto, WarehouseDto, ImportResult<T>)
       Import/                   (парсинг CSV/JSON, міст Import → Domain)
       Domain/                   (лаба 4: сутності з поведінкою — Product, Warehouse)
       Abstractions/             (НОВЕ, лаба 5: ICatalogStore — контракт сховища)
       Storage/                  (НОВЕ, лаба 5: InMemoryCatalogStore, FileCatalogStore, CachingCatalogStore)
       Services/                 (лаба 5: CatalogService; лаба 6: event Changed, FindAll)
       Events/                   (НОВЕ, лаба 6: CatalogChangeKind, CatalogChangedEventArgs)
       Reports/                  (НОВЕ, лаба 7: StockLine, GroupSummary, ReportService, TopBy, демо, варіанти)
       SampleData.cs             (лаба 5; лаба 7: розширено до 20 записів + Movements() + Generate())
     Cli/
       Cli.csproj                (ProjectReference на Core; multi-targeting)
       Program.cs                (composition root + сценарій)
       StoreFactory.cs           (лаба 5, додатково: вибір сховища у фабриці)
       Sinks.cs                  (НОВЕ, лаба 6, додатково: ConsoleSink, FileSink, LogFormat)
       Benchmark.cs              (НОВЕ, лаба 7, додатково: Stopwatch на 100 000 записів)
       ImportCommand.cs          (НОВЕ, лаба 8: import / gen-big, Ctrl+C, ConsoleProgress)
   tests/
     Core.Tests/                 (НОВЕ, лаба 8: xUnit, посилається лише на Core)
 ```

 `Core` — class library без точки входу. `Cli` — консольний застосунок, залежність
 лише в один бік: `Cli → Core`.

 ## Команди

 ```bash
 dotnet build
 dotnet run --project src/Cli -f net10.0
 ```

 Публікація (self-contained включає .NET runtime, framework-dependent — ні):

 ```bash
 dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true  -o publish/sc
 dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained false -o publish/fd
 ```

 | Режим | Розмір | Потрібен runtime |
 | --- | ---: | :---: |
 | self-contained | ~83 МБ | ні |
 | framework-dependent | ~0.2 МБ | так (.NET 10) |

 ## Попередні лабораторні (коротко)

 - **Лаба 1–2**: розділення `Core`/`Cli`, `ProjectReference`, multi-targeting
   (`net8.0;net10.0`), публікація self-contained/framework-dependent, `PublishSingleFile`/
   `PublishTrimmed`.
 - **Лаба 3**: record-и домену (`ProductDto`, `WarehouseDto`, `ImportResult<T>`) у
   `Core/Dto`; розбір `CSV`/`JSON` через `switch expression` і pattern matching у
   `Core/Import`. Додаткові завдання: JSON-імпортер, розпізнавання мішаних рядків
   товар/склад за префіксом, статистика імпорту з `CultureInfo.InvariantCulture`.
 - **Лаба 4**: доменна модель в `Core/Domain` — сутності `Product` і `Warehouse` з приватним
   станом і фабрикою `Create` (конструктор приватний), 12 інваріантів з типізованими
   винятками, `ToDto`/`FromDto` як міст до DTO тижня 3. Додатково: `ProductDomainImporter`
   (Import → Domain), `WarehouseCapacityPolicy` (інваріант на дві сутності), `ProductStatus` з
   дозволеними переходами через `switch`-вираз. Звіт — `Звіт_4_ФЕІ_36_Сухар_Роман.docx`.

 ## Лабораторна робота №5 — сервісний шар: інтерфейси, два сховища, ручний DI

 Домен **Склад**. Сервіс більше не залежить від способу зберігання даних.

 | Роль | Тип | Файл |
 | --- | --- | --- |
 | Контракт | `ICatalogStore` (`List`, `GetById`, `Add`, `Update`, `Remove`) | `Core/Abstractions/ICatalogStore.cs` |
 | Реалізація 1 | `InMemoryCatalogStore` — словник у пам'яті | `Core/Storage/InMemoryCatalogStore.cs` |
 | Реалізація 2 | `FileCatalogStore` — кеш + JSON-файл (на диск ідуть `ProductDto`) | `Core/Storage/FileCatalogStore.cs` |
 | Сервіс | `CatalogService` (`Add`, `Receive`, `Issue`, `All`, `Find`) | `Core/Services/CatalogService.cs` |
 | Composition root | `Program.cs` — єдине місце з `new InMemoryCatalogStore` / `new FileCatalogStore` | `Cli/Program.cs` |

 Залежності: `Cli → CatalogService → ICatalogStore → InMemoryCatalogStore | FileCatalogStore`.
 Залежність передається через конструктор вручну (`new` у `Program.cs`);
 `Microsoft.Extensions.DependencyInjection` не підключено.

 ### Запуск

 ```bash
 dotnet run --project src/Cli -f net10.0            # Сховище: InMemoryCatalogStore, дані зникають після виходу
 dotnet run --project src/Cli -f net10.0 -- --file  # Сховище: FileCatalogStore, дані накопичуються між запусками
 ```

 У файловому режимі JSON лежить у `data/catalog.json` поруч зі збіркою
 (`AppContext.BaseDirectory`, тобто `src/Cli/bin/Debug/net10.0/data/catalog.json`).
 Кирилиця в ньому записана як `\uXXXX` — штатне екранування `System.Text.Json`.
 Якщо змінити формат DTO, видаліть цей файл.

 Сценарій у `Program.cs`: додати товар, оформити прихід і видачу, знайти за id, показати
 список, потім три відмови (дубль id, прихід на неіснуючий id, видача більша за залишок) —
 перехоплюються в `try/catch` і друкуються зрозумілим текстом.

 Примітка: у файл потрапляють лише поля `ProductDto`, тому `Product.Status` після
 перезапуску завжди `Active`.

 ### Додаткові завдання

 1. **Декоратор `CachingCatalogStore`** (`Core/Storage`) — приймає інший `ICatalogStore` у
    конструкторі, реалізує той самий контракт і додає нову поведінку: `List()` та `GetById()`
    запам'ятовуються, а `Add`/`Update`/`Remove` скидають кеш (відсутні записи не кешуються).
    Обгортає будь-яку реалізацію; вмикається аргументом `--cache`.
 2. **Пошук з предикатом** — `CatalogService.Search(Func<Product, bool> predicate)`. Умову
    задає викликач (`service.Search(p => p.Quantity < 100)`), інтерфейс сховища не змінюється
    (сервіс фільтрує результат `List()`). Підготовка до тижнів 6–7.
 3. **Фабрика `StoreFactory.Create(args)`** (`Cli/StoreFactory.cs`) — усі `new` конкретних
    сховищ в одному місці; вмикається аргументом `--factory`.

 Порівняння варіантів composition root:

 | | Усе в `Program.cs` (за замовчуванням) | `StoreFactory` (`--factory`) |
 | --- | --- | --- |
 | Де `new` сховищ | тернарний вибір у `Program.cs` | `StoreFactory.Create` |
 | Читабельність | зв'язки видно в одному файлі | `Program.cs` коротший, але треба відкривати фабрику |
 | Нові сховища/прапорці | `Program.cs` росте | зміни локалізовані у фабриці |
 | Підходить для | двох-трьох реалізацій | кількох точок входу, що ділять вибір сховища |

 ```bash
 dotnet run --project src/Cli -f net10.0 -- --cache            # пам'ять + декоратор
 dotnet run --project src/Cli -f net10.0 -- --file --cache     # файл + декоратор
 dotnet run --project src/Cli -f net10.0 -- --factory --file   # вибір сховища через StoreFactory
 ```

 Коміт: `lab05`.

## Лабораторна робота №6 — доменні події, Action/Func, лямбди

`CatalogService` повідомляє про зміни подією `Changed`, а Core нічого не знає про консоль
і лог-файл (пошук `Console`/`logs` у `src/Core` порожній).

| Що | Де |
| --- | --- |
| `enum CatalogChangeKind { Added, Received, Issued, Removed }` | `Core/Events/CatalogChangeKind.cs` |
| `CatalogChangedEventArgs` (`Id`, `Kind`, `Delta`, `QuantityAfter`, `At`) | `Core/Events/CatalogChangedEventArgs.cs` |
| `event EventHandler<CatalogChangedEventArgs>? Changed` + `OnChanged` (`Changed?.Invoke`) | `Core/Services/CatalogService.cs` |
| `FindAll(Func<Product, bool>)` — значення у відповідь, тому делегат, а не подія | `CatalogService` |
| Підписники: локальний метод `OnCatalogChanged` (консоль + `logs/app.log`) і лямбда-лічильник `(_, _) => changes++` | `Cli/Program.cs` |

Подія піднімається лише після успішної зміни: якщо інваріант `Product` кинув виняток
(від'ємний прихід, видача більша за залишок) або запису немає, повідомлення не виникає.
`Remove` піднімає `Removed` лише коли запис справді видалено. Лог: `Path.Combine(AppContext.BaseDirectory, "logs", "app.log")`,
каталог створюється кодом (`src/Cli/bin/Debug/net10.0/logs/app.log`).

Сценарій: `Add` + `Receive` + `Issue` → «Подій отримано: 3» → `-= OnCatalogChanged` → `Receive`
(у консолі й файлі рядка немає, лічильник = 4) → три відмови без рядків подій → `Remove`.

```text
19:28:16 Added    8f8aa4b0  +50 → залишок 50
19:28:16 Received 8f8aa4b0  +25 → залишок 75
19:28:16 Issued   8f8aa4b0  -10 → залишок 65
```

### Додаткові завдання

1. **`INotificationSink`** (`Core/Abstractions`) з реалізаціями `ConsoleSink` і `FileSink` (`Cli/Sinks.cs`);
   сервіс отримує їх через конструктор: `new CatalogService(store, [new ConsoleSink(), new FileSink(logPath)])`.
   Запуск: `dotnet run --project src/Cli -f net10.0 -- --sinks`.
2. **`Action<string>`** — `ProductDomainImporter.ToDomain(parsed, onSkipped)` повідомляє про кожен
   пропущений рядок через колбек; у Cli це `skipped => Console.WriteLine(...)`.

| | Подія `Changed` | `INotificationSink` у конструкторі |
| --- | --- | --- |
| Підписка | довільна кількість, у будь-який момент | фіксується при створенні сервісу |
| Відписка | `-=` | немає (потрібен новий сервіс) |
| Сервіс знає | лише що «щось сталося» | інтерфейс одержувача |
| Підходить для | UI, тести, довільні клієнти (тижні 8, 10, 12) | постійні одержувачі, обов'язкові залежності |

## Лабораторна робота №7 — звіти через LINQ to Objects

Аналітика над колекцією `Product`: усі запити в `Core/Reports`, у `Cli` лише виклики й вивід
(у `Program.cs` немає жодного `Where`/`GroupBy`). Звіти — чисті функції над `IEnumerable<Product>`,
повертають `IReadOnlyList<DTO>` (не сутності). Лише LINQ to Objects, без PLINQ.

| Метод | Питання | Оператори |
| --- | --- | --- |
| `LowStock(source, n)` | Яких позицій бракує (< n)? | `Where` → `OrderBy`/`ThenBy` → `Select` → `ToList` |
| `TopByQuantity(source, n)` | Які n позицій мають найбільший залишок? | `TopBy` (= `OrderByDescending` + `ThenBy(Sku)` + `Take`) → `Select` |
| `ByUnit(source)` | Як залишки розподілені за одиницями? | `GroupBy` (+`OrdinalIgnoreCase`) → `Select` (`Count`, `Sum`) → `OrderByDescending` |
| `Totals(source)` | Скільки позицій, сума, середнє, максимум? | `Count`, `Sum`, `Average`, `Max`; порожнє джерело → нулі |

Підкоманди: `report-top` (за замовчуванням), `report-low`, `report-groups`, `report-sum`, а також
додаткові `report-top-units`, `report-groups-query`, `report-movements`, `report-deferred`, `report-bench`.
Сценарій лаб. 5–6 тепер запускається як `scenario`. Прапорець `--empty` підставляє порожнє джерело
(звіти не падають), `--file`/`--cache` не сприймаються як назва звіту.

```bash
dotnet run --project src/Cli -f net10.0 -- report-groups
dotnet run --project src/Cli -f net10.0 -- report-sum
dotnet run --project src/Cli -f net10.0 -- report-low --empty
```

Фрагмент `GroupBy` з проєкцією:

```csharp
source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)       // g.Key — значення одиниці
      .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
      .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
      .ToList();
```

Вивід на `SampleData` (20 записів, 7 одиниць):

```text
> report-top                         > report-low
  SKU-004  Цегла червона   5000 шт     SKU-017  Бетонозмішувач 160л   3 шт
  SKU-016  Арматура d12    2000 м      SKU-018  Мембрана покрівельна  7 м²
  SKU-010  Кабель ВВГнг    1500 м      SKU-013  Дошка обрізна         8 м³
  SKU-011  Труба ПП 25мм    800 м
  SKU-015  Плиточний клей   700 кг
> report-groups
  шт  позицій: 6  сумарно: 5737     м   позицій: 3  сумарно: 4300     кг  позицій: 3  сумарно: 1090
  л   позицій: 2  сумарно:  400     м²  позицій: 2  сумарно:  267     т   позицій: 3  сумарно:   90
  м³  позицій: 1  сумарно:    8
> report-sum
  позицій 20, всього 11892, середнє 594,60, макс 5000
```

### Відкладене виконання (крок 3)

`query = source.Where(p => p.Quantity < 10)` лише *описує* відбір — жодного елемента на цьому рядку
не перевіряється. Запит виконується щоразу, коли його перебирають: кожен виклик `Count()` проходить
`source` заново. Перший `Count()` дав **3** дефіцитні позиції, потім у `source` додали «Скобу»
(залишок 3), і другий `Count()` дав **4** — запит не зберігає результат, він бачить поточний стан
джерела. `ToList()` фіксує результат: після `source.Clear()` у знімку лишаються ті самі 4 елементи,
бо `snapshot.Count` — властивість готового списку, а не повторний прохід (`query.Count()` — метод).
Демо — `DeferredExecutionDemo` у Core, друкує `report-deferred`.

### Додаткові завдання

1. **Join / GroupJoin** — `ReportService.Movements` (`Join`: лише товари з рухами, запис про неіснуючий
   `P-999` відкидається) і `MovementSummaries` (`GroupJoin`: усі 20 товарів, у тих, що без рухів, `Moves = 0`).
   Результат — DTO `MovementLine`, `MovementSummary`. Команда `report-movements`.
2. **Власний `TopBy<T>(source, key, n, tieBreaker)`** (`Core/Reports/EnumerableExtensions.cs`) — використано
   у двох звітах: `TopByQuantity` і `TopUnits` (команда `report-top-units`).
3. **Синтаксис запитів** — `ByUnitQuery` (`from … group … into g … orderby … select`), команда
   `report-groups-query`, результат ідентичний `ByUnit`. Порівняння: для групування з агрегатами
   `from/group/into` читається майже як речення, а `into g` явно показує перехід від товарів до груп.
   Але `Where`/`Take`/`ThenBy` з компаратором у ньому незручні: у query syntax не можна передати
   `StringComparer` (тому «шт» і «ШТ» там лишилися б різними групами) і для `.ToList()` потрібні дужки.
   Тому в основному завданні — ланцюжок методів.
4. **Stopwatch на 100 000 записів** (`report-bench`, середнє з 20 прогонів після прогріву, Debug, net10.0, кілька запусків):

   | Варіант | Час, мс |
   | --- | ---: |
   | A: потоковий ланцюжок без `ToList` | 13–17 |
   | B: `ToList` посеред ланцюжка | 12–17 |
   | C: один запит перебрано тричі (`Count`, `Sum`, `Max`) | 7–13 |
   | D: той самий запит, `ToList` один раз | 5–8 |

   Висновок: проміжний `ToList` (A vs B) суттєвої різниці не дає — різниця в межах шуму, `GroupBy` усе одно
   буферизує дані, а зайва копія коштує лише пам'яті. Повторне перебирання (C vs D) помітно повільніше:
   `Where` виконується тричі замість одного разу.

Коміт: `lab07`.

## Лабораторна робота №8 — async/await, скасування, доменні винятки, unit-тести Core

| Що | Де |
| --- | --- |
| `DomainException` — порушення бізнес-правила («кількість не може бути 0», видача більша за залишок, недозволений перехід стану, немає запису) | `Core/DomainException.cs` |
| `StorageException` — технічна помилка файлу (зіпсований JSON), `JsonException` зберігається в `InnerException` | `Core/StorageException.cs` |
| `ICatalogImporter.LoadAsync(path, ct)` | `Core/Abstractions/ICatalogImporter.cs` |
| `JsonCatalogImporter` — `FileStream(useAsync: true)` + `JsonSerializer.DeserializeAsync`, `ThrowIfCancellationRequested` у циклі | `Core/Storage/JsonCatalogImporter.cs` |
| `CatalogService.ImportAsync(importer, path, ct)` — ідемпотентний, подія `Added` на кожен доданий запис | `Core/Services/CatalogService.cs` |
| `import` у Cli: top-level `await`, `Console.CancelKeyPress`, окремі `catch` | `Cli/ImportCommand.cs` |
| 19 тестів (`[Fact]`, `[Theory]`), сховище `InMemoryCatalogStore`, власний `TestData` | `tests/Core.Tests` |

Асинхронним став лише імпорт файлу (ввід-вивід). Сховище, звіти й події лишились синхронними:
вони працюють з пам'яттю, асинхронність там нічого не дала б.

### Тести

```bash
dotnet test
```

Очікувано: `Passed! - Failed: 0, Passed: 19, Skipped: 0`. Тести не читають `data/` з репозиторію:
три тести пишуть тимчасовий файл у `Path.GetTempPath()` і видаляють його у `finally`.

### Імпорт і скасування

```bash
dotnet run --project src/Cli -f net10.0 -- import                              # data/supplies.json → Імпортовано записів: 17
dotnet run --project src/Cli -f net10.0 -- import data/supplies_bad.json       # Дані відхилено: Запис 3: ...
dotnet run --project src/Cli -f net10.0 -- import data/supplies_broken.json    # Файл пошкоджено: ...
dotnet run --project src/Cli -f net10.0 -- gen-big 1000000                     # великий файл у bin/.../data/big.json
dotnet run --project src/Cli -f net10.0 -- import src/Cli/bin/Debug/net10.0/data/big.json --cancel-after=2000   # Імпорт скасовано.
```

Ctrl+C під час імпорту великого файлу теж дає «Імпорт скасовано.», а не стектрейс.
`supplies.json` містить 20 записів, 3 з яких (`P-001`, `P-004`, `P-010`) уже є в сховищі, тому додається 17.

### Додаткові завдання

1. **Потоковий імпорт** — `IStreamingCatalogImporter.StreamAsync` повертає `IAsyncEnumerable<Product>`
   (`DeserializeAsyncEnumerable`), параметр токена з `[EnumeratorCancellation]`; сервіс читає його через
   `await foreach` (`ImportStreamAsync`). Прапорець `--stream`.
2. **Зв'язані токени** — `ImportWithTimeoutAsync` об'єднує токен користувача і токен тайм-ауту через
   `CreateLinkedTokenSource`; якщо спрацював тайм-аут, кидає `TimeoutException`. Прапорець `--timeout=мс`,
   тести `ImportWithTimeout_*` показують, що тайм-аут і Ctrl+C розрізняються.
3. **`IProgress<int>`** — `StreamAsync` повідомляє відсоток прочитаного файлу, `ConsoleProgress` у Cli малює
   `прочитано: 42% [########            ]`.
4. **Покриття** — `dotnet test --collect:"XPlat Code Coverage"`: 32% рядків Core загалом; `JsonCatalogImporter`
   90%, `CatalogService` 74%. Не покриті код лаб 3–7 (CSV-імпортери, `FileCatalogStore`, демо й бенчмарк звітів),
   до яких ця лаба не торкалася.

Коміт: `lab08`.

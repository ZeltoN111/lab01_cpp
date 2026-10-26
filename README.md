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
       SampleData.cs             (НОВЕ, лаба 5: 16 демонстраційних записів)
     Cli/
       Cli.csproj                (ProjectReference на Core; multi-targeting)
       Program.cs                (composition root + сценарій)
       StoreFactory.cs           (лаба 5, додатково: вибір сховища у фабриці)
       Sinks.cs                  (НОВЕ, лаба 6, додатково: ConsoleSink, FileSink, LogFormat)
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

 # CrossApp

 Наскрізний проєкт з крос-платформного програмування на .NET.

 ## Предметна область

 **Склад**: облік залишків товарів по партіях.

 Основні сутності:

 - `Product` — товар;
 - `StockBatch` — партія товару;
 - `Warehouse` — склад;
 - `Movement` — переміщення.

 ## Середовище

 - .NET SDK `10.0.11`;
 - macOS `26.6.2`;
 - архітектура `arm64`.

 ## Структура solution

 ```text
 CrossApp/
   CrossApp.slnx
   README.md
   .gitignore
   data/
     sample.csv                  (10+ рядків, 3 навмисно пошкоджені)
     sample.json                 (той самий домен у форматі JSON)
     sample_mixed.csv             (рядки товарів і складів впереміш, з префіксом P;/W;)
   src/
     Core/
       Core.csproj              (multi-targeting: net8.0;net10.0)
       EnvironmentInfo.cs        (namespace Core)
       Dto/
         ProductDto.cs           (record, namespace Core.Dto)
         WarehouseDto.cs         (record, namespace Core.Dto)
         ImportResult.cs         (record ImportResult<T>)
         MixedImportResult.cs    (record для мішаного імпорту)
       Import/
         ProductCsvImporter.cs   (розбір CSV через switch expression)
         ProductJsonImporter.cs  (розбір JSON, System.Text.Json)
         MixedCsvImporter.cs     (рядки товарів/складів за префіксом)
     Cli/
       Cli.csproj                (ProjectReference на Core; multi-targeting: net10.0;net8.0)
       Program.cs
 ```

 `Core` — class library без точки входу: збирає інформацію про середовище і повертає її
 у вигляді запису (`record`), нічого не друкує. `Cli` — консольний застосунок з `Main`
 (top-level statements), який лише форматує та виводить те, що дав `Core`.
 Залежність напрямлена в один бік: `Cli` → `Core`. `Core` про `Cli` нічого не знає.

 ## Команди: додавання Core і посилання

 Виконано одноразово при створенні `Core`:

 ```bash
 dotnet new classlib -n Core -o src/Core -f net10.0
 dotnet sln add src/Core/Core.csproj
 dotnet add src/Cli/Cli.csproj reference src/Core/Core.csproj
 rm src/Core/Class1.cs
 ```

 Перевірка, що посилання з'явилося:

 ```bash
 cat src/Cli/Cli.csproj
 # має бути: <ProjectReference Include="..\Core\Core.csproj" />
 ```

 ## Build / Run

 ```bash
 dotnet build
 dotnet run --project src/Cli
 ```

 ## Публікація: self-contained vs framework-dependent

 Кожен варіант — в окрему папку `-o`, щоб не перезаписувати попередній результат:

 ```bash
 # self-contained
 dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true  -o publish/osx-arm64-sc
 dotnet publish src/Cli -c Release -f net10.0 -r win-x64    --self-contained true  -o publish/win-x64-sc

 # framework-dependent
 dotnet publish src/Cli -c Release -f net10.0 -r win-x64    --self-contained false -o publish/win-x64-fd
 ```

 > Прапорець `-f net10.0` обов'язковий, бо `Cli.csproj` тепер multi-target
 > (`net10.0;net8.0`) — без нього `dotnet publish` падає з помилкою
 > `NETSDK1129: ... must specify one of the following frameworks`.

 Запуск опублікованого застосунку напряму (не через `dotnet run`):

 ```bash
 cd publish/osx-arm64-sc
 chmod +x Cli
 ./Cli
 cd ../..
 ```

 Windows-публікація містить `Cli.exe` замість `Cli` і виконується лише на Windows —
 бінарник під один RID не запускається на іншій ОС (це не помилка, а очікувана поведінка).

 ### Порівняння режимів публікації

 | RID | Режим | Розмір публікації | Потрібен встановлений runtime |
 | --- | --- | ---: | :---: |
 | `osx-arm64` | self-contained | ~83 МБ | ні |
 | `win-x64` | self-contained | ~77 МБ | ні |
 | `win-x64` | framework-dependent | ~208 КБ | так (.NET 10 Runtime) |

 Self-contained публікація включає весь .NET runtime і всі системні збірки, тому важить
 десятки мегабайт незалежно від розміру власного коду. Framework-dependent публікація
 містить лише код застосунку (`Cli.dll`, `Core.dll` і конфіг) і покладається на те, що на
 цільовій машині вже встановлено відповідний .NET Runtime — тому важить кілобайти.

 ## Multi-targeting

 І `Core.csproj`, і `Cli.csproj` збираються одразу для двох TFM:

 ```xml
 <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
 ```

 Після `dotnet build` у `bin/Debug/` з'являються окремі підкаталоги `net8.0/` і
 `net10.0/`, кожен зі своєю збіркою. Умовна компіляція показана в
 `src/Core/EnvironmentInfo.cs`:

 ```csharp
 #if NET10_0_OR_GREATER
     private const string BuildNote = "збірка під net10.0";
 #else
     private const string BuildNote = "збірка під net8.0";
 #endif
 ```

 Різницю видно у виводі при запуску під різними TFM:

 ```bash
 dotnet run --project src/Cli -f net10.0
 dotnet run --project src/Cli -f net8.0
 ```

 net10.0:
 ```text
 ОС             : macOS 26.6.2
 Runtime        : .NET 10.0.11
 Каталог        : .../src/Cli/bin/Debug/net10.0/
 Збірка         : збірка під net10.0
 ```

 net8.0:
 ```text
 ОС             : Darwin 25.6.0 Darwin Kernel Version 25.6.0: ...; root:xnu-12377.161.14~5/RELEASE_ARM64_T8103
 Runtime        : .NET 8.0.31
 Каталог        : .../src/Cli/bin/Debug/net8.0/
 Збірка         : збірка під net8.0
 ```

 Різні `Runtime`, різний (детальніший) формат `OsDescription` під net8.0 і різний
 `BuildNote` — усе це резолвиться компілятором окремо для кожного TFM ще на етапі
 збірки, без жодного `if` під час виконання.

 ## Приклад локального запуску (`dotnet run`)

 ```text
 CrossApp – інформація про середовище
 ----------------------------------------------------
 ОС             : macOS 26.6.2
 Runtime        : .NET 10.0.11
 Архітектура    : Arm64
 RID (визначено): osx-arm64
 RID (від .NET) : osx-arm64
 Каталог        : /Users/roman/Desktop/labs/cpp/lab1/CrossApp/src/Cli/bin/Debug/net10.0/
 Збірка         : збірка під net10.0
 ```

 ## Приклад запуску з каталогу publish (`./Cli`, self-contained osx-arm64)

 ```text
 CrossApp – інформація про середовище
 ----------------------------------------------------
 ОС             : macOS 26.6.2
 Runtime        : .NET 10.0.11
 Архітектура    : Arm64
 RID (визначено): osx-arm64
 RID (від .NET) : osx-arm64
 Каталог        : /Users/roman/Desktop/labs/cpp/lab1/CrossApp/publish/osx-arm64-sc/
 Збірка         : збірка під net10.0
 ```

 Вивід ідентичний виводу `dotnet run` за змістом — відрізняється лише `Каталог`, бо
 застосунок запущено з іншого місця на диску.

 ## Додаткові завдання

 ### 1. Self-contained publish під два RID

 Публікація виконана під дві різні платформи (див. таблицю вище):

 ```bash
 dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true -o publish/osx-arm64-sc
 dotnet publish src/Cli -c Release -f net10.0 -r win-x64 --self-contained true -o publish/win-x64-sc
 ```

 Розмір включає весь .NET runtime, тому обидва каталоги значно більші за
 framework-dependent збірку (~208 КБ).

 ### 2. `PublishSingleFile=true`

 ```bash
 dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true \
   -p:PublishSingleFile=true -o publish/osx-arm64-singlefile
 ```

 | Варіант | Файлів у каталозі | Розмір | Запускається |
 | --- | ---: | ---: | :---: |
 | self-contained (звичайний) | 193 | 83 МБ | так |
 | self-contained + `PublishSingleFile` | 3 | 76 МБ | так |

 Кількість файлів впала з 193 до 3 (виконуваний `Cli`, `.pdb` і конфіг) — усі
 `System.*.dll` запаковані всередину одного бінарника. Розмір при цьому суттєво **не**
 зменшився (76 МБ проти 83 МБ) — увесь runtime і так входив до self-contained публікації,
 single file лише змінює спосіб пакування, а не прибирає код. Запускається так само, як
 і звичайний self-contained (`./Cli`), вивід ідентичний.

 ### 3. `PublishTrimmed=true`

 ```bash
 dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true \
   -p:PublishTrimmed=true -o publish/osx-arm64-trimmed
 ```

 | Варіант | Розмір | Попередження збірки |
 | --- | ---: | --- |
 | self-contained (без trim) | 83 МБ | — |
 | self-contained + `PublishTrimmed` | 20 МБ | 0 (жодних `warning IL2xxx`) |

 Розмір впав з 83 МБ до 20 МБ — trimmer прибрав усі невикористані частини .NET runtime
 (наприклад `System.Data`, `System.Net.Http` тощо, які код застосунку не викликає).
 Попереджень немає, тому що в `EnvironmentInfo`/`Program.cs` немає рефлексії — увесь
 виклик статичний (`RuntimeInformation.*`, звичайні поля record).

 ### 4. Multi-targeting з різним виводом на TFM

 Див. секцію [Multi-targeting](#multi-targeting) вище — `BuildNote` через `#if
 NET10_0_OR_GREATER`, показано реальний вивід під net10.0 і net8.0.

 ### 5. Запуск у Docker-контейнері

 Спочатку запустіть Docker Desktop. Потім виконайте команду з кореня проєкту:

 ```bash
 docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
   dotnet run --project src/Cli
 ```

 ## Лабораторна робота №3 — records, pattern matching, імпорт CSV/JSON

 Мета: описати перші дані домену сучасним C# (`record`, nullable reference types,
 pattern matching) і завантажити їх з файлу так, щоб пошкоджені рядки не переривали
 імпорт цілісних. Логіка розбору повністю в `Core` (`Core/Dto`, `Core/Import`), `Cli`
 лише викликає імпортер і форматує вивід.

 ### 1. Record-типи (`Core/Dto`)

 ```csharp
 namespace Core.Dto;

 public record ProductDto(
     string Id,
     string Sku,
     string Name,
     string Unit,
     int Quantity,
     string? Note = null);
 ```

 `Id`, `Sku`, `Name`, `Unit`, `Quantity` — без `?`: рядок, у якому будь-яке з цих полів
 відсутнє чи порожнє, вважається пошкодженим і взагалі не потрапляє в `Items` (це
 перевіряється ще на етапі `ParseLine`, до створення `ProductDto`). `Note` — єдине
 `string?` поле зі значенням за замовчуванням `null`, бо приміток у вхідних даних
 справді часто немає, і це нормальний, а не помилковий стан.

 ```csharp
 namespace Core.Dto;

 public record WarehouseDto(
     string Id,
     string Name,
     string Location);
 ```

 Другий тип, потрібний для додаткового завдання 2 (мішані рядки товар/склад). Усі поля
 обов'язкові — рядок про склад без адреси чи назви так само вважається пошкодженим.

 ```csharp
 namespace Core.Dto;

 public sealed record ImportResult<T>(
     IReadOnlyList<T> Items,
     IReadOnlyList<string> Errors);
 ```

 Узагальнений (generic) тип: один опис обслуговує і `ImportResult<ProductDto>`, і будь-
 який інший DTO в майбутньому. Поля оголошені як `IReadOnlyList<T>`, а не `List<T>`, щоб
 отримувач результату не міг дописати туди щось своє.

 **Чому тут `record`, а не `class`:** усі ці типи лише переносять уже готові дані одного
 рядка файлу — їх порівнюють за вмістом, не змінюють після створення і не мають власної
 поведінки чи життєвого циклу, а саме для цього й призначений `record`.

 ### 2. Розбір рядка: `switch expression` з патернами (`Core/Import/ProductCsvImporter.cs`)

 ```csharp
 private static ParseOutcome ParseLine(string line)
 {
     string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

     return parts switch
     {
         { Length: < 5 } => new ParseFailed($"очікую 5 колонок, отримав {parts.Length}"),
         [_, "", _, _, _] or [_, _, "", _, _]
             => new ParseFailed("SKU або назва порожні"),
         [_, _, _, _, var qty] when !int.TryParse(qty, out int q) || q < 0
             => new ParseFailed($"кількість '{qty}' не є невід'ємним числом"),
         [var id, var sku, var name, var unit, var qty]
             => new ParseOk(new ProductDto(id, sku, name, unit, int.Parse(qty))),
         _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
     };
 }
 ```

 Використані патерни (5 гілок, вимога методички — мінімум 3):

 | Патерн | Що перевіряє |
 | --- | --- |
 | `{ Length: < 5 }` | патерн властивості + реляційний (`<`) — замало колонок |
 | `[_, "", _, _, _] or [_, _, "", _, _]` | патерн списку + константний `""` + логічний `or` — порожній SKU або назва |
 | `[..., var qty] when !int.TryParse(...)` | патерн списку + охоронна умова `when` — нечислова/від'ємна кількість |
 | `[var id, var sku, var name, var unit, var qty]` | патерн списку рівно з 5 елементів з іменуванням |
 | `_` | гілка «усе інше» — забагато колонок |

 Результат розбору — власна ієрархія record-типів (`abstract record ParseOutcome` з
 `ParseOk`/`ParseFailed`), а не виняток: один битий рядок не перериває імпорт решти.

 ### 3. Формат файлу `data/sample.csv`

 - роздільник — `;` (крапка з комою, не конфліктує з комою в назвах товарів);
 - перший рядок — заголовок (`id;sku;name;unit;quantity`), розпізнається і пропускається
   окремою перевіркою в `Load`, файл без заголовка теж читається коректно;
 - кодування — UTF-8;
 - 13 рядків: 10 коректних і 3 навмисно пошкоджені (замало колонок, нечислова кількість,
   порожній SKU) — це тестові дані, а не недбалість.

 ### 4. Вивід консолі

 Коректний файл (`dotnet run --project src/Cli`, читає `data/sample.csv` за замовчуванням):

 ```text
 Завантажено записів: 10
   P-001  SKU-001    Цемент М400 25кг             120 шт
   P-002  SKU-002    Пісок будівельний             18 т
   P-003  SKU-003    Цегла червона               4200 шт
   P-004  SKU-004    Фарба водоемульсійна 10л      36 шт
   P-005  SKU-005    Шпаклівка фінішна            250 кг
 Пропущено рядків: 3
   ! рядок 12: очікую 5 колонок, отримав 4
   ! рядок 13: кількість 'багато' не є невід'ємним числом
   ! рядок 14: SKU або назва порожні
 Усього: 13, прийнято: 10, пропущено: 3, помилок: 23.1%
 ```

 Неіснуючий файл (`dotnet run --project src/Cli -- data/no-such-file.csv`):

 ```text
 Файл не знайдено: /Users/roman/Desktop/labs/cpp/lab1/CrossApp/data/no-such-file.csv
 ```

 (код виходу — `1`, без необробленого винятку).

 ### Додаткові завдання (лабораторна №3)

 #### 1. Другий імпортер — JSON

 `Core/Import/ProductJsonImporter.cs` читає той самий `ProductDto` з `data/sample.json`
 через `System.Text.Json`. `Cli` обирає імпортер за розширенням файлу окремим
 `switch expression`:

 ```csharp
 ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
 {
     ".csv" => ProductCsvImporter.Load(path),
     ".json" => ProductJsonImporter.Load(path),
     var ext => throw new NotSupportedException($"Непідтримуване розширення файлу: '{ext}'")
 };
 ```

 ```bash
 dotnet run --project src/Cli -f net10.0 -- data/sample.json
 ```
 ```text
 Завантажено записів: 3
   P-001  SKU-001    Цемент М400 25кг             120 шт
   P-002  SKU-002    Пісок будівельний             18 т
   P-003  SKU-003    Цегла червона               4200 шт
 Усього: 3, прийнято: 3, пропущено: 0, помилок: 0.0%
 ```

 На відміну від CSV, `System.Text.Json.Deserialize` не відновлюється посторічково: файл
 або валідний увесь, або ні (тоді `catch (JsonException)` повертає одну загальну
 помилку в `Errors`, без необробленого винятку).

 #### 2. Різнорідні рядки за префіксом типу

 `data/sample_mixed.csv` містить упереміш рядки товарів (`P;...`) і складів (`W;...`).
 `Core/Import/MixedCsvImporter.cs` розпізнає обидва в одному `switch` за константним
 патерном на першій позиції списку (`["P", ...]` / `["W", ...]`), результат —
 `MixedImportResult(Products, Warehouses, Errors)`. Викликається окремо, прапорцем
 `--mixed`, щоб не змішувати з основним сценарієм:

 ```bash
 dotnet run --project src/Cli -f net10.0 -- --mixed
 ```
 ```text
 Товарів: 2, складів: 2, помилок: 2
   ! рядок 5: невідомий тип рядка 'P' або неправильна кількість колонок
   ! рядок 6: невідомий тип рядка 'X' або неправильна кількість колонок
 ```

 #### 3. Статистика імпорту одним рядком

 ```csharp
 int total = result.Items.Count + result.Errors.Count;
 double errorRate = total == 0 ? 0 : (double)result.Errors.Count / total * 100;
 Console.WriteLine($"Усього: {total}, прийнято: {result.Items.Count}, " +
     $"пропущено: {result.Errors.Count}, " +
     $"помилок: {errorRate.ToString("F1", CultureInfo.InvariantCulture)}%");
 ```

 Форматування явно через `CultureInfo.InvariantCulture` — без цього `:F1` узяв би
 поточну культуру системи і на україномовній локалі вивів би `23,1%` з комою замість
 крапки, хоча вхідні дані парсяться з тим самим `InvariantCulture` (той самий принцип,
 що й для парсингу чисел з файлу, застосований до виводу).

 ### Самоперевірка

 - Records лежать у `Core/Dto`, а не в `Cli`.
 - У `Program.cs` немає жодного `Split` чи `int.Parse` — уся логіка розбору в `Core`.
 - Пошкоджений рядок дає повідомлення з номером рядка, а не падіння програми.
 - Файл із заголовком і без нього обробляються однаково коректно.
 - Числа парсяться (`int.TryParse`) і форматуються (`errorRate`) з `InvariantCulture`.
 - Запуск з неправильним шляхом дає зрозумілий текст і код виходу `1`, а не
   `NullReferenceException`/`FileNotFoundException` назовні.

 
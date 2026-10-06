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
       Domain/                   (НОВЕ, лаба 4: сутності з поведінкою — Product, Warehouse)
     Cli/
       Cli.csproj                (ProjectReference на Core; multi-targeting)
       Program.cs
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

 ## Лабораторна робота №4 — доменна модель: сутності, інваріанти, інкапсуляція

 У `Core/Domain` з'явились сутності з власною поведінкою (на відміну від DTO тижня 3,
 які лишаються лише форматом файлу): **`Product`** (товар із залишком, операції
 прихід/видача, статус) і **`Warehouse`** (склад із місткістю). Стан інкапсульований
 (приватне поле/`private set`), створення — лише через фабричний метод `Create`
 (конструктор приватний), `ToDto`/`FromDto` — симетричний міст до DTO тижня 3
 (`FromDto` завжди йде через `Create`, тобто проходить ті самі перевірки).

 ### Перелік інваріантів

 1. `Id` товару обов'язковий — `ArgumentException` (`Product.Create`)
 2. `Sku` не порожній — `ArgumentException` (`Product.Create`)
 3. Назва товару не порожня — `ArgumentException` (`Product.Create`)
 4. Одиниця виміру не порожня — `ArgumentException` (`Product.Create`)
 5. Початковий залишок не від'ємний — `ArgumentOutOfRangeException` (`Product.Create`)
 6. Кількість приходу > 0 — `ArgumentOutOfRangeException` (`Product.RegisterArrival`)
 7. Кількість видачі > 0 — `ArgumentOutOfRangeException` (`Product.Issue`)
 8. Видача не більша за поточний залишок — `InvalidOperationException` (`Product.Issue`)
 9. Перехід статусу лише за дозволеною схемою — `InvalidOperationException` (`Product.ChangeStatus`)
 10. `Id`/назва/розташування складу обов'язкові — `ArgumentException` (`Warehouse.Create`)
 11. Місткість складу > 0 — `ArgumentOutOfRangeException` (`Warehouse.Create`)
 12. Прихід не перевищує місткість складу, інваріант на дві сутності —
     `InvalidOperationException` (`WarehouseCapacityPolicy.EnsureCanAcceptArrival`)

 ### Додаткові завдання

 1. **Import → Domain** — `ProductDomainImporter.ToDomain` перетворює
    `ImportResult<ProductDto>` на `ImportResult<Product>`, об'єднуючи помилки парсингу
    й доменної валідації в одному результаті.
 2. **Інваріант на дві сутності** — `WarehouseCapacityPolicy` (не метод `Product`/
    `Warehouse`, бо жодна сутність одноосібно не володіє сумарним залишком складу —
    прообраз майбутнього `CatalogService`).
 3. **Явний стан** — `enum ProductStatus` + `Product.ChangeStatus`, допустимі переходи
    перевіряються `switch`-виразом з tuple-патерном.

 ### Запуск додаткових завдань

 Усі три завдання демонструються разом під час запуску CLI з кореня solution:

 ```bash
 dotnet run --project src/Cli -f net10.0
 ```

 Команда запускає `ProductDomainImporter.ToDomain` на `data/sample.csv`, сценарії
 переходів `ProductStatus` і перевірку `WarehouseCapacityPolicy`. Для перевірки
 сумісності з .NET 8 використайте:

 ```bash
 dotnet run --project src/Cli -f net8.0
 ```

 Детальний звіт з кодом, повним виводом консолі й поясненнями — `Звіт_4_ФЕІ_36_Сухар_Роман.docx`.

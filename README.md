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
   src/
     Core/
       Core.csproj              (multi-targeting: net8.0;net10.0)
       EnvironmentInfo.cs        (namespace Core)
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

 
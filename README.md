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

 ## Запуск

 З кореня проєкту виконайте:

 ```bash
 dotnet build
 dotnet run --project src/Cli
 ```

 ## Додаткові завдання

 ### 1. Self-contained publish під два RID

 Публікація виконана під дві різні платформи:

 ```bash
 dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true
 dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
 ```

 Порівняння розміру каталогів публікації:

 ```bash
 du -sh src/Cli/bin/Release/net10.0/osx-arm64/publish
 du -sh src/Cli/bin/Release/net10.0/linux-x64/publish
 ```

 | RID | Розмір публікації |
 | --- | ---: |
 | `osx-arm64` | ~83 MB |
 | `linux-x64` | ~79 MB |

 Розмір включає весь .NET runtime, тому обидва каталоги значно більші за framework-dependent збірку (~150 KB для звичайного `dotnet build`).

 ### 2. Прапорець `--json`

 Додано підтримку виводу системної інформації у форматі JSON через `System.Text.Json`.

 ```bash
 # Звичайний табличний вивід
 dotnet run --project src/Cli

 # Вивід у форматі JSON
 dotnet run --project src/Cli -- --json
 ```

 Приклад JSON-виводу:

 ```json
 {
   "OsDescription": "macOS 26.6.2",
   "OsVersion": "Unix 26.6.2",
   "ProcessArchitecture": "Arm64",
   "ClrVersion": "10.0.11",
   "Runtime": ".NET 10.0.11",
   "BaseDirectory": "/Users/roman/Desktop/labs/cpp/lab1/CrossApp/src/Cli/bin/Debug/net10.0/",
   "CurrentDirectory": "/Users/roman/Desktop/labs/cpp/lab1/CrossApp",
   "Domain": "Склад (товари, партії, залишки, переміщення)"
 }
 ```

 ### 3. Запуск у Docker-контейнері

 Спочатку запустіть Docker Desktop. Потім виконайте команду з кореня проєкту:

 ```bash
 docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
   dotnet run --project src/Cli
 ```

 ## Приклад локального запуску

 ```text
 CrossApp – практикум з крос-платформного програмування
 Студент: Сухар Роман, група ФеІ-36
 ----------------------------------------------------
 ОС (OSDescription) : macOS 26.6.2
 ОС (Environment) : Unix 26.6.2
 Архітектура процесу : Arm64
 Версія .NET (CLR) : 10.0.11
 Runtime : .NET 10.0.11
 Каталог застосунку : /Users/roman/Desktop/labs/cpp/lab1/CrossApp/src/Cli/bin/Debug/net10.0/
 Поточний каталог : /Users/roman/Desktop/labs/cpp/lab1/CrossApp
 ----------------------------------------------------
 Предметна область: Склад (товари, партії, залишки, переміщення)
 ```

 ## Приклад запуску в контейнері

 ```text
 CrossApp – практикум з крос-платформного програмування
 Студент: Сухар Роман, група ФеІ-36
 ----------------------------------------------------
 ОС (OSDescription) : Ubuntu 24.04.4 LTS
 ОС (Environment) : Unix 5.15.49.0
 Архітектура процесу : Arm64
 Версія .NET (CLR) : 10.0.11
 Runtime : .NET 10.0.11
 Каталог застосунку : /src/src/Cli/bin/Debug/net10.0/
 Поточний каталог : /src
 ----------------------------------------------------
 Предметна область: Склад (товари, партії, залишки, переміщення)
 ```

 ## Висновок

 Той самий код без жодних змін компілюється та виконується як на macOS (arm64), так і в Linux-контейнері (Ubuntu 24.04, arm64). Відрізняються лише значення `OSDescription` і шляхів `AppContext.BaseDirectory` та `Environment.CurrentDirectory`, а поведінка програми залишається однаковою.

 Це є практичним доказом крос-платформності .NET.
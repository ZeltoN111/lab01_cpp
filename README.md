# CrossApp
Наскрізний проєкт з крос-платформного програмування.
Предметна область: Склад. Сутності: Product (товар), StockBatch (партія), Warehouse (склад), Movement (переміщення). Призначення: облік залишків товарів по партіях.
## Запуск
dotnet build
dotnet run --project src/Cli
## Середовище
.NET SDK 10.0.11 , macOS 26.6.2 Arm64

## Додаткові завдання
1. Self-contained publish під 2 RID
Перевірка розмірів двох RID

du -sh src/Cli/bin/Release/net10.0/osx-arm64/publish 
du -sh src/Cli/bin/Release/net10.0/linux-x64/publish

В результаті отримуємо:

## Self-contained publish

| RID        | Розмір публікації |
|------------|--------------------|
| osx-arm64  | ~83 MB             |
| linux-x64  | ~79 MB             |

Розмір включає весь .NET runtime, тому обидва каталоги значно 
більші за framework-dependent збірку (~150 KB для звичайного build).
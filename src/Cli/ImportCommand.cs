using System.Diagnostics;
using System.Text.Json;
using Core;
using Core.Dto;
using Core.Services;
using Core.Storage;

// Лаба 8: підкоманди import і gen-big. Ланцюжок токена: Cli (cts) → ImportAsync → LoadAsync.
public static class ImportCommand
{
    public static async Task<int> RunAsync(CatalogService service, string[] args)
    {
        var path = args.Where(a => !a.StartsWith("--")).Skip(1).FirstOrDefault() ?? RepoData("supplies.json");

        using var cts = new CancellationTokenSource();
        // Обробник події — єдине допустиме місце для «void-лямбди»; e.Cancel = true не дає процесу завершитися.
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        if (TryGetMs(args, "--cancel-after=", out var cancelAfter)) cts.CancelAfter(cancelAfter);

        var importer = new JsonCatalogImporter();
        var before = service.All().Count;
        var sw = Stopwatch.StartNew();
        try
        {
            int added;
            if (args.Contains("--stream"))
                added = await service.ImportStreamAsync(importer, path, new ConsoleProgress(), cts.Token);
            else if (TryGetMs(args, "--timeout=", out var timeout))
                added = await service.ImportWithTimeoutAsync(importer, path, timeout, cts.Token);
            else
                added = await service.ImportAsync(importer, path, cts.Token);

            Console.WriteLine($"Імпортовано записів: {added}");
            Console.WriteLine($"  у сховищі: {before} → {service.All().Count}, час: {sw.ElapsedMilliseconds} мс");
            return 0;
        }
        // OperationCanceledException — до загальних обробників, інакше скасування виглядало б як збій.
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("Імпорт скасовано.");
            Console.WriteLine($"  у сховищі: {before} → {service.All().Count}, час: {sw.ElapsedMilliseconds} мс");
            return 2;
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"Імпорт скасовано за тайм-аутом: {ex.Message}");
            return 2;
        }
        catch (DomainException ex) { Console.WriteLine($"Дані відхилено: {ex.Message}"); return 1; }
        catch (StorageException ex) { Console.WriteLine($"Файл пошкоджено: {ex.Message}"); return 1; }
        catch (FileNotFoundException) { Console.WriteLine($"Файл не знайдено: {path}"); return 1; }
        catch (IOException ex) { Console.WriteLine($"Помилка читання файлу: {ex.Message}"); return 1; }
    }

    // Великий файл для демонстрації скасування; пишеться в bin/.../data, тобто не потрапляє в репозиторій.
    public static async Task<int> GenerateBigAsync(string[] args)
    {
        var count = int.TryParse(args.Where(a => !a.StartsWith("--")).Skip(1).FirstOrDefault(), out var n) ? n : 500_000;
        var path = Path.Combine(AppContext.BaseDirectory, "data", "big.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var dtos = Enumerable.Range(1, count)
            .Select(i => new ProductDto($"B-{i:D7}", $"SKU-B{i:D7}", $"Позиція постачання {i}", "шт", i % 1000 + 1));
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await JsonSerializer.SerializeAsync(stream, dtos);

        Console.WriteLine($"Згенеровано {count} записів: {path} ({new FileInfo(path).Length / 1024 / 1024} МБ)");
        return 0;
    }

    private static string RepoData(string name) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", name));

    private static bool TryGetMs(string[] args, string prefix, out TimeSpan value)
    {
        var raw = args.FirstOrDefault(a => a.StartsWith(prefix))?[prefix.Length..];
        value = int.TryParse(raw, out var ms) ? TimeSpan.FromMilliseconds(ms) : default;
        return raw is not null && ms > 0;
    }
}

// Додаткове завдання 3: підписник IProgress<int> малює відсотки в одному рядку.
// Власна реалізація, а не Progress<T>: у консолі той пересилає Report у пул потоків,
// і рядки могли б з'явитися не по порядку або вже після «Імпортовано записів».
public sealed class ConsoleProgress : IProgress<int>
{
    public void Report(int percent)
    {
        Console.Write($"\r  прочитано: {percent,3}% [{new string('#', percent / 5),-20}]");
        if (percent == 100) Console.WriteLine();
    }
}

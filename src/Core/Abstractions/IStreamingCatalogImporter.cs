using Core.Domain;

namespace Core.Abstractions;

// Додаткове завдання 1: потоковий імпорт — записи віддаються по одному, весь файл у пам'ять не вантажиться.
// Додаткове завдання 3: progress отримує відсоток прочитаного файлу (0–100).
public interface IStreamingCatalogImporter
{
    IAsyncEnumerable<Product> StreamAsync(string path, IProgress<int>? progress = null, CancellationToken ct = default);
}

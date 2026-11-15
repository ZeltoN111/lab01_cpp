using Core.Domain;

namespace Core.Abstractions;

// Асинхронний контракт імпорту: читання файлу — операція введення-виведення, тому Task.
public interface ICatalogImporter
{
    Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default);
}

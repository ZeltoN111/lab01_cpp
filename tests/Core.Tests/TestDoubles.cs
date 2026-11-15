using Core.Abstractions;
using Core.Domain;

namespace Core.Tests;

// Імпортер у пам'яті: сервіс отримує його так само, як JsonCatalogImporter, але без диска.
internal sealed class FakeImporter(IReadOnlyList<Product> items) : ICatalogImporter
{
    public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default) =>
        Task.FromResult(items);
}

// «Повільне джерело»: ніколи не завершується саме, лише через скасування токена.
internal sealed class HangingImporter : ICatalogImporter
{
    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        await Task.Delay(Timeout.Infinite, ct);
        return [];
    }
}

// Синхронний збирач відсотків: Progress<T> викликав би колбек у пулі потоків уже після Assert.
internal sealed class ListProgress : IProgress<int>
{
    public List<int> Values { get; } = [];
    public void Report(int value) => Values.Add(value);
}

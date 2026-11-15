using System.Text.Json;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    [Fact] // 5) скасування: токен скасовано ще до виклику
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact] // 6) тимчасовий файл із некоректним записом → DomainException з номером запису
    public async Task LoadAsync_FileWithInvalidRecord_ThrowsDomainException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, """
                [ { "Id": "P-1", "Sku": "SKU-1", "Name": "Цемент", "Unit": "шт", "Quantity": 5 },
                  { "Id": "P-2", "Sku": "",      "Name": "Пісок",  "Unit": "т",  "Quantity": 1 } ]
                """);

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => new JsonCatalogImporter().LoadAsync(path));

            Assert.StartsWith("Запис 2:", ex.Message);
            Assert.IsType<ArgumentException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // зіпсований JSON — технічна помилка, а не доменна; JsonException зберігається всередині
    public async Task LoadAsync_MalformedJson_ThrowsStorageException_WithInnerJsonException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, """[ { "Id": "P-1", "Quantity": п'ять } ]""");

            var ex = await Assert.ThrowsAsync<StorageException>(
                () => new JsonCatalogImporter().LoadAsync(path));

            Assert.IsAssignableFrom<JsonException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // ідемпотентність і подія на кожен доданий запис; імпортер і сховище — у пам'яті
    public async Task ImportAsync_SkipsExistingIds_AndRaisesEventPerAddedItem()
    {
        var store = new InMemoryCatalogStore([Product.Create("P-1", "SKU-1", "Цемент", "шт", 5)]);
        var service = new CatalogService(store);
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var importer = new FakeImporter(
        [
            Product.Create("P-1", "SKU-1", "Цемент", "шт", 5),
            Product.Create("P-2", "SKU-2", "Пісок", "т", 3),
            Product.Create("P-3", "SKU-3", "Щебінь", "т", 8),
        ]);

        var first = await service.ImportAsync(importer, "fake");
        var second = await service.ImportAsync(importer, "fake");

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.Equal(3, store.List().Count);
        Assert.Equal(new[] { "P-2", "P-3" }, events.Select(e => e.Id));
    }

    [Fact] // додаткове 2: зв'язані токени — спрацював саме тайм-аут
    public async Task ImportWithTimeout_WhenTimeoutFires_ThrowsTimeoutException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        var ex = await Assert.ThrowsAsync<TimeoutException>(
            () => service.ImportWithTimeoutAsync(new HangingImporter(), "slow", TimeSpan.FromMilliseconds(50)));

        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact] // додаткове 2: скасував користувач — це скасування, а не тайм-аут
    public async Task ImportWithTimeout_WhenUserCancels_ThrowsOperationCanceled()
    {
        using var user = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportWithTimeoutAsync(new HangingImporter(), "slow", TimeSpan.FromSeconds(30), user.Token));
    }

    [Fact] // додаткове 1: [EnumeratorCancellation] — токен з WithCancellation доходить до StreamAsync
    public async Task StreamAsync_TokenFromWithCancellation_CancelsEnumeration()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var stream = new JsonCatalogImporter().StreamAsync("any.json").WithCancellation(cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in stream) { }
        });
    }

    [Fact] // додаткові 1 і 3: потоковий імпорт додає всі записи, прогрес зростає і закінчується на 100
    public async Task ImportStreamAsync_AddsAllRecords_AndReportsProgressTo100()
    {
        var path = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}.json");
        try
        {
            var dtos = Enumerable.Range(1, 3000).Select(i => new { Id = $"S-{i}", Sku = $"SKU-{i}", Name = "Позиція", Unit = "шт", Quantity = i });
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(dtos));
            var service = new CatalogService(new InMemoryCatalogStore());
            var progress = new ListProgress();

            var added = await service.ImportStreamAsync(new JsonCatalogImporter(), path, progress);

            Assert.Equal(3000, added);
            Assert.Equal(100, progress.Values[^1]);
            Assert.Equal(progress.Values.Order(), progress.Values);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

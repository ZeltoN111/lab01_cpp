using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Бізнес-операції складу. Сервіс знає лише про ICatalogStore:
// ані про файл, ані про словник, ані про вивід, ані про лог.
// Про зміни він повідомляє подією Changed (і, за потреби, через INotificationSink).
public sealed class CatalogService(ICatalogStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _sinks = sinks?.ToArray() ?? [];

    // event, а не public-поле делегата: ззовні доступні лише += і -=, викликати може тільки сервіс.
    public event EventHandler<CatalogChangedEventArgs>? Changed;

    // Єдине місце підняття події; викликається лише ПІСЛЯ успішної зміни стану.
    private void OnChanged(Product p, CatalogChangeKind kind, int delta, int? quantityAfter = null)
    {
        var args = new CatalogChangedEventArgs(p.Id, kind, delta, quantityAfter ?? p.Quantity);
        Changed?.Invoke(this, args);
        foreach (var sink in _sinks) sink.Notify(args);
    }

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product); // інваріанти вже перевірив Product.Create
        OnChanged(product, CatalogChangeKind.Added, quantity); // після зміни, не до
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");
        product.RegisterArrival(quantity); // якщо кине виняток — події не буде
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public bool Remove(string id)
    {
        var product = _store.GetById(id);
        if (product is null || !_store.Remove(id)) return false; // нічого не видалено — події немає
        OnChanged(product, CatalogChangeKind.Removed, -product.Quantity, quantityAfter: 0);
        return true;
    }

    // Лаба 8: імпортер приходить параметром (ручний DI тижня 5), подія — для кожного доданого запису (тиждень 6).
    public async Task<int> ImportAsync(ICatalogImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (TryAddImported(item)) added++;
        }
        return added;
    }

    // Додаткове завдання 1: потоковий імпорт — запис додається одразу після прочитання.
    public async Task<int> ImportStreamAsync(
        IStreamingCatalogImporter importer, string path, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        var added = 0;
        await foreach (var item in importer.StreamAsync(path, progress, ct))
        {
            if (TryAddImported(item)) added++;
        }
        return added;
    }

    // Додаткове завдання 2: зв'язаний токен «користувач АБО тайм-аут». Якщо спрацював саме тайм-аут,
    // кидаємо TimeoutException, щоб викликач відрізнив його від Ctrl+C.
    public async Task<int> ImportWithTimeoutAsync(
        ICatalogImporter importer, string path, TimeSpan timeout, CancellationToken userToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutCts.Token);
        try
        {
            return await ImportAsync(importer, path, linked.Token);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !userToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Імпорт не завершився за {timeout.TotalMilliseconds:0} мс.", ex);
        }
    }

    // Ідемпотентність: повторний імпорт того самого файлу нічого не дублює.
    private bool TryAddImported(Product item)
    {
        if (_store.GetById(item.Id) is not null) return false;
        _store.Add(item);
        OnChanged(item, CatalogChangeKind.Added, item.Quantity);
        return true;
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Пошук за довільною умовою: критерій задає той, хто викликає.
    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Збережено з лаби 5 для сумісності.
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate) => FindAll(predicate);
}

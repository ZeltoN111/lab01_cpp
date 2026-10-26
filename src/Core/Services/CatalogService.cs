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
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        product.RegisterArrival(quantity); // якщо кине виняток — події не буде
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
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

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Пошук за довільною умовою: критерій задає той, хто викликає.
    // Func (а не подія), бо потрібне значення у відповідь.
    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Збережено з лаби 5 для сумісності.
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate) => FindAll(predicate);
}

using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Декоратор: той самий контракт ICatalogStore, нова поведінка — результати читання
// запам'ятовуються, а будь-яка зміна скидає кеш. Обгортає будь-яку реалізацію.
public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Product> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Product>? _list;

    public IReadOnlyList<Product> List() => _list ??= _inner.List();

    public Product? GetById(string id)
    {
        if (_byId.TryGetValue(id, out var cached)) return cached;
        var found = _inner.GetById(id);
        if (found is not null) _byId[found.Id] = found; // відсутні записи не кешуємо
        return found;
    }

    public void Add(Product item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) Invalidate();
        return removed;
    }

    private void Invalidate()
    {
        _list = null;
        _byId.Clear();
    }
}

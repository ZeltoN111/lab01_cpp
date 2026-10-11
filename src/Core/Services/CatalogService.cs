using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

// Бізнес-операції складу. Сервіс знає лише про ICatalogStore:
// ані про файл, ані про словник, ані про консоль.
public sealed class CatalogService(ICatalogStore store)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product); // інваріанти вже перевірив Product.Create
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        product.RegisterArrival(quantity); // метод сутності з тижня 4
        _store.Update(product);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        product.Issue(quantity);
        _store.Update(product);
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Пошук за довільною умовою: предикат передає викликач, сховище про нього не знає.
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
}

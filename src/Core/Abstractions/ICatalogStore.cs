using Core.Domain;

namespace Core.Abstractions;

// Контракт сховища: лише ті операції, які справді потрібні сервісу.
// Product? — запису з таким id може не бути.
// IReadOnlyList — зовнішній код не може змінити сховище в обхід Add/Update/Remove.
public interface ICatalogStore
{
    IReadOnlyList<Product> List();
    Product? GetById(string id);
    void Add(Product item);
    void Update(Product item);
    bool Remove(string id);
}

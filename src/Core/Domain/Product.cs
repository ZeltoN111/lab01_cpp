using Core.Dto;

namespace Core.Domain;

public sealed class Product
{
    private int _quantity;

    public string Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Unit { get; }
    public string? Note { get; }
    public int Quantity => _quantity;
    public ProductStatus Status { get; private set; } = ProductStatus.Active;

    private Product(string id, string sku, string name, string unit, int quantity, string? note)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Unit = unit;
        Note = note;
        _quantity = quantity;
    }

    // Єдиний спосіб створити товар: усі перевірки тут, перед створенням об'єкта.
    public static Product Create(string id, string sku, string name, string unit, int quantity, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця виміру не може бути порожньою", nameof(unit));
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                "Початковий залишок не може бути від'ємним");

        return new Product(id.Trim(), sku.Trim().ToUpperInvariant(), name.Trim(), unit.Trim(), quantity, note);
    }

    public void RegisterArrival(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість приходу має бути більшою за нуль");

        _quantity += amount;
    }

    public void Issue(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість видачі має бути більшою за нуль");
        if (amount > _quantity)
            throw new InvalidOperationException(
                $"Не можна видати {amount}: залишок {Sku} = {_quantity}");

        _quantity -= amount;
    }

    // Допустимі переходи стану: Active ⇄ Discontinued → Archived (термінальний, без повернення).
    public void ChangeStatus(ProductStatus newStatus)
    {
        bool allowed = (Status, newStatus) switch
        {
            (ProductStatus.Active, ProductStatus.Discontinued) => true,
            (ProductStatus.Discontinued, ProductStatus.Active) => true,
            (ProductStatus.Discontinued, ProductStatus.Archived) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Товар {Sku}: перехід зі стану {Status} у {newStatus} неможливий");

        Status = newStatus;
    }

    // Мапінг у формат тижня 3 і назад — знадобиться сховищу тижня 5.
    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Quantity, Note);

    public static Product FromDto(ProductDto dto) =>
        Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Quantity, dto.Note);

    public override string ToString() => $"{Id} [{Sku}] {Name} — {Quantity} {Unit}";
}

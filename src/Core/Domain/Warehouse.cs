using Core.Dto;

namespace Core.Domain;

public sealed class Warehouse
{
    public string Id { get; }
    public string Name { get; }
    public string Location { get; }
    public int Capacity { get; }

    private Warehouse(string id, string name, string location, int capacity)
    {
        Id = id;
        Name = name;
        Location = location;
        Capacity = capacity;
    }

    public static Warehouse Create(string id, string name, string location, int capacity = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Розташування складу не може бути порожнім", nameof(location));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                "Місткість складу має бути більшою за нуль");

        return new Warehouse(id.Trim(), name.Trim(), location.Trim(), capacity);
    }

    public WarehouseDto ToDto() => new(Id, Name, Location);

    public static Warehouse FromDto(WarehouseDto dto) => Create(dto.Id, dto.Name, dto.Location);

    public override string ToString() => $"{Id} {Name} ({Location})";
}

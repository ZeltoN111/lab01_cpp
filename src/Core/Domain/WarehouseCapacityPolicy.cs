namespace Core.Domain;

// Інваріант, що охоплює ДВІ сутності: прихід товару не повинен перевищити
// місткість складу (Warehouse.Capacity), з огляду на сумарний залишок УСІХ
// товарів, уже розміщених на цьому складі.
//
// Навмисно НЕ метод Product і НЕ метод Warehouse:
// - Product не повинен знати про існування конкретного Warehouse чи інших Product —
//   інакше виникає залежність між сутностями, яких проєктно не повинно бути напряму.
// - Warehouse не зберігає список товарів (це колекція, якою керує сховище/сервіс
//   тижня 5), тому "сумарний залишок по складу" — дані, якими жодна окрема
//   сутність одноосібно не володіє.
// Тому перевірка живе окремо, як майбутній прообраз CatalogService: приймає обидві
// сутності ззовні та агреговане значення (яке на 5-му тижні порахує сховище).
public static class WarehouseCapacityPolicy
{
    public static void EnsureCanAcceptArrival(Warehouse warehouse, int currentTotalQuantity, int incomingAmount)
    {
        if (incomingAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(incomingAmount), incomingAmount,
                "Кількість приходу має бути більшою за нуль");

        int projected = currentTotalQuantity + incomingAmount;
        if (projected > warehouse.Capacity)
            throw new InvalidOperationException(
                $"Склад {warehouse.Id} не вмістить прихід: місткість {warehouse.Capacity}, " +
                $"поточний залишок {currentTotalQuantity}, після приходу було б {projected}");
    }
}

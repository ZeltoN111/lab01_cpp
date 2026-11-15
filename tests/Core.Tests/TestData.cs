using Core.Domain;

namespace Core.Tests;

// Маленький фіксований набір для тестів — не SampleData, щоб зміни демо-даних не ламали тести.
// SKU-MID і SKU-MIX мають однаковий залишок: так перевіряється детермінований другий ключ топ-N.
internal static class TestData
{
    public static IReadOnlyList<Product> Items() =>
    [
        Product.Create("T-1", "SKU-LOW", "Скоба", "шт", 3),
        Product.Create("T-2", "SKU-MIX", "Дюбель", "шт", 100),
        Product.Create("T-3", "SKU-BIG", "Цегла", "шт", 500),
        Product.Create("T-4", "SKU-LOW2", "Брус", "м³", 5),
        Product.Create("T-5", "SKU-MID", "Цвях", "кг", 100),
    ];
}

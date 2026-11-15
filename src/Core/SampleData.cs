using Core.Domain;
using Core.Reports;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Product> Products() =>
    [
        Product.Create("P-001", "SKU-001", "Цемент М400 25кг", "шт", 120),
        Product.Create("P-002", "SKU-002", "Пісок будівельний", "т", 40),
        Product.Create("P-003", "SKU-003", "Щебінь фракція 5-20", "т", 35),
        Product.Create("P-004", "SKU-004", "Цегла червона", "шт", 5000),
        Product.Create("P-005", "SKU-005", "Шпаклівка фінішна", "кг", 300),
        Product.Create("P-006", "SKU-006", "Гіпсокартон 12.5мм", "шт", 150),
        Product.Create("P-007", "SKU-007", "Профіль CD 60", "шт", 400),
        Product.Create("P-008", "SKU-008", "Ґрунтовка глибокого проникнення", "л", 220),
        Product.Create("P-009", "SKU-009", "Фарба біла водоемульсійна", "л", 180),
        Product.Create("P-010", "SKU-010", "Кабель ВВГнг 3x2.5", "м", 1500),
        Product.Create("P-011", "SKU-011", "Труба ПП 25мм", "м", 800),
        Product.Create("P-012", "SKU-012", "Утеплювач мінвата 100мм", "м²", 260),
        Product.Create("P-013", "SKU-013", "Дошка обрізна 50x150", "м³", 8),
        Product.Create("P-014", "SKU-014", "Саморізи 3.5x35", "кг", 90),
        Product.Create("P-015", "SKU-015", "Плиточний клей", "кг", 700),
        Product.Create("P-016", "SKU-016", "Арматура d12", "м", 2000),
        Product.Create("P-017", "SKU-017", "Бетонозмішувач 160л", "шт", 3),
        Product.Create("P-018", "SKU-018", "Мембрана покрівельна", "м²", 7),
        Product.Create("P-019", "SKU-019", "Клей-піна монтажна", "шт", 64),
        Product.Create("P-020", "SKU-020", "Вапно гашене", "т", 15),
    ];

    // Журнал переміщень для Join/GroupJoin. P-005 і P-020 рухів не мають.
    public static IReadOnlyList<StockMovement> Movements() =>
    [
        new("P-001", +50), new("P-001", -20), new("P-002", -5),
        new("P-004", +1000), new("P-004", -300), new("P-004", -150),
        new("P-007", -40), new("P-013", -4), new("P-013", +2),
        new("P-017", -2), new("P-016", +500), new("P-999", +1), // P-999 — товару немає: Join його відкине
    ];

    // Детермінована генерація для вимірювання часу (лаба 7, додатково 4).
    public static IReadOnlyList<Product> Generate(int count, int seed = 42)
    {
        string[] units = ["шт", "т", "кг", "л", "м", "м²", "м³"];
        var rnd = new Random(seed);
        var list = new List<Product>(count);
        for (int i = 1; i <= count; i++)
            list.Add(Product.Create($"G-{i:D6}", $"GEN-{i:D6}", $"Товар {i}", units[rnd.Next(units.Length)], rnd.Next(0, 5000)));
        return list;
    }
}

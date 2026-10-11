using Core.Domain;

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
        Product.Create("P-013", "SKU-013", "Дошка обрізна 50x150", "м³", 12),
        Product.Create("P-014", "SKU-014", "Саморізи 3.5x35", "кг", 90),
        Product.Create("P-015", "SKU-015", "Плиточний клей", "кг", 700),
        Product.Create("P-016", "SKU-016", "Арматура d12", "м", 2000),
    ];
}

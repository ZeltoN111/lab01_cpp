using Core.Dto;
using Core.Domain;

namespace Core.Import;

// Міст між форматом тижня 3 (ImportResult<ProductDto>) і доменом тижня 4:
// та сама ідея "дані + помилки", але тепер помилкою вважається і порушення
// доменного інваріанту, а не лише зіпсований рядок файлу.
public static class ProductDomainImporter
{
    public static ImportResult<Product> ToDomain(
        ImportResult<ProductDto> parsed, Action<string>? onSkipped = null)
    {
        var items = new List<Product>();
        var errors = new List<string>(parsed.Errors); // помилки парсингу переносяться як є
        foreach (var e in parsed.Errors) onSkipped?.Invoke(e); // звіт про пропущені рядки без залежності від способу виводу

        foreach (ProductDto dto in parsed.Items)
        {
            try
            {
                items.Add(Product.FromDto(dto));
            }
            catch (Exception ex)
            {
                string reason = $"{dto.Sku} (id={dto.Id}): {ex.Message}";
                errors.Add(reason);
                onSkipped?.Invoke(reason);
            }
        }

        return new ImportResult<Product>(items, errors);
    }
}

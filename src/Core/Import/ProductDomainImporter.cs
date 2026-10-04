using Core.Dto;
using Core.Domain;

namespace Core.Import;

// Міст між форматом тижня 3 (ImportResult<ProductDto>) і доменом тижня 4:
// та сама ідея "дані + помилки", але тепер помилкою вважається і порушення
// доменного інваріанту, а не лише зіпсований рядок файлу.
public static class ProductDomainImporter
{
    public static ImportResult<Product> ToDomain(ImportResult<ProductDto> parsed)
    {
        var items = new List<Product>();
        var errors = new List<string>(parsed.Errors); // помилки парсингу переносяться як є

        foreach (ProductDto dto in parsed.Items)
        {
            try
            {
                items.Add(Product.FromDto(dto));
            }
            catch (Exception ex)
            {
                errors.Add($"{dto.Sku} (id={dto.Id}): {ex.Message}");
            }
        }

        return new ImportResult<Product>(items, errors);
    }
}

using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
            return new ImportResult<ProductDto>(items, []);
        }
        catch (JsonException ex)
        {
            return new ImportResult<ProductDto>([], [$"не вдалося розібрати JSON: {ex.Message}"]);
        }
    }
}
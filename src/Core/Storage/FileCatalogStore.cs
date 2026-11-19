using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Той самий контракт, інша механіка: кеш у пам'яті, дозавантаження при першому
// зверненні, запис на диск після кожної зміни. На диск ідуть DTO, а не сутності.
// Лаба 9: шлях приходить з конфігурації, JSON у camelCase з читабельною кирилицею, запис атомарний.
public sealed class FileCatalogStore(string path) : ICatalogStore // підписка на контракт
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // порожня примітка не пишеться як "note": null
        // Кирилиця пишеться як є, а не Ц. Файл не вбудовується в HTML, тому це безпечно.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    public string FilePath => _path;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (!File.Exists(_path)) { _loaded = true; return; }

        List<ProductDto> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<ProductDto>>(File.ReadAllText(_path), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл даних {Path.GetFileName(_path)} пошкоджено: {ex.Message}", ex);
        }

        _cache.Clear();
        for (var i = 0; i < dtos.Count; i++)
        {
            try
            {
                var product = Product.FromDto(dtos[i]); // повторно перевіряє інваріанти
                _cache[product.Id] = product;
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Файл даних, запис {i + 1}: {ex.Message}", ex);
            }
        }
        _loaded = true;
    }

    // Атомарний запис: спершу повний файл .tmp, потім заміна одним File.Move.
    // Якщо процес упаде посеред запису, старий файл лишиться цілим.
    private void Flush()
    {
        var dtos = _cache.Values.Select(product => product.ToDto()).ToList();
        var tmp = _path + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));
        File.Move(tmp, _path, overwrite: true);
    }

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }

    // Експорт усієї колекції одним записом. Не частина ICatalogStore: контракт сховища не змінюється.
    public int ReplaceAll(IEnumerable<Product> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _cache.Clear();
        foreach (var item in items) _cache[item.Id] = item;
        _loaded = true;
        Flush();
        return _cache.Count;
    }
}

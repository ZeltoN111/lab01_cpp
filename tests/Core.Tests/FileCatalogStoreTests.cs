using Core.Domain;
using Core.Storage;

namespace Core.Tests;

// Сховище працює саме з диском, тому тут тимчасовий каталог, який видаляється у finally.
public class FileCatalogStoreTests
{
    [Fact]
    public void ReplaceAll_WritesCamelCaseJson_WithReadableCyrillic_AndNoTmpFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "items.json");
        try
        {
            new FileCatalogStore(path).ReplaceAll([Product.Create("P-1", "SKU-1", "Цемент", "мішок", 40)]);

            var json = File.ReadAllText(path);
            Assert.Contains("\"sku\": \"SKU-1\"", json);
            Assert.Contains("Цемент", json);       // не Ц...
            Assert.DoesNotContain("note", json);   // null не пишеться
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact] // імпорт: новий екземпляр сховища читає те, що записав попередній
    public void SavedCollection_IsLoadedBack_ByNewInstance()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "items.json");
        try
        {
            new FileCatalogStore(path).Add(Product.Create("P-1", "SKU-1", "Пісок", "т", 3));

            var loaded = new FileCatalogStore(path).GetById("P-1");

            Assert.NotNull(loaded);
            Assert.Equal("Пісок", loaded.Name);
            Assert.Equal(3, loaded.Quantity);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact] // зіпсований файл — зрозумілий StorageException з JsonException усередині
    public void CorruptedFile_ThrowsStorageException()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "items.json");
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, """[ { "id": "P-1", "quantity": сорок } ]""");

            var ex = Assert.Throws<StorageException>(() => new FileCatalogStore(path).List());

            Assert.IsAssignableFrom<System.Text.Json.JsonException>(ex.InnerException);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

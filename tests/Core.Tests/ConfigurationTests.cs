using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Core.Tests;

public class ConfigurationTests
{
    [Fact] // пізніше джерело перекриває раніше: так змінна середовища перекриває appsettings.json
    public void LaterSource_OverridesEarlier_WhenBindingAppOptions()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataPath"] = "data",
                ["Storage:FileName"] = "items.json",
                ["Logging:MinimumLevel"] = "Information",
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataPath"] = "override",
                ["Logging:MinimumLevel"] = "Debug",
            })
            .Build();

        var options = config.Get<AppOptions>()!;

        Assert.Equal("override", options.Storage.DataPath);
        Assert.Equal("items.json", options.Storage.FileName);
        Assert.Equal(LogLevel.Debug, options.Logging.MinimumLevel);
    }

    [Fact] // ключа немає в жодному джерелі — лишається значення за замовчуванням з класу
    public void MissingKeys_KeepDefaults()
    {
        var options = new ConfigurationBuilder().Build().Get<AppOptions>() ?? new AppOptions();

        Assert.Equal("data", options.Storage.DataPath);
        Assert.Equal("шт", options.Warehouse.DefaultUnit);
    }

    [Fact]
    public void ResolveFullPath_RelativePath_IsUnderBaseDir_AndDirectoryIsCreated()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        try
        {
            var full = new StorageOptions { DataPath = "data", FileName = "items.json" }.ResolveFullPath(baseDir);

            Assert.Equal(Path.Combine(baseDir, "data", "items.json"), full);
            Assert.True(Directory.Exists(Path.Combine(baseDir, "data")));
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveFullPath_AbsolutePath_IgnoresBaseDir()
    {
        var absolute = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        try
        {
            var full = new StorageOptions { DataPath = absolute }.ResolveFullPath("ignored-base");

            Assert.Equal(Path.Combine(absolute, "items.json"), full);
        }
        finally
        {
            Directory.Delete(absolute, recursive: true);
        }
    }
}

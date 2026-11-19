namespace Core.Configuration;

// Налаштування застосунку: Cli заповнює їх з appsettings.json, змінних середовища тощо.
// Core не знає, звідки прийшли значення, — це звичайні класи з властивостями.
public sealed class AppOptions
{
    public string EnvironmentName { get; init; } = "Development";
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public WarehouseOptions Warehouse { get; init; } = new();
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "items.json";

    // Відносно чого рахувати DataPath: каталог застосунку чи системна папка даних користувача.
    public StorageRoot Root { get; init; } = StorageRoot.AppBase;

    // Абсолютний DataPath береться як є, відносний — від baseDir; каталог створюється, якщо його немає.
    public string ResolveFullPath(string baseDir)
    {
        var root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }

    // Базовий каталог для відносного DataPath. AppBase — поруч зі збіркою (не залежить від cwd),
    // LocalAppData — системний каталог даних користувача (профіль Production).
    public string ResolveBaseDirectory(string appBaseDir) => Root switch
    {
        StorageRoot.LocalAppData => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossApp"),
        _ => appBaseDir
    };
}

public enum StorageRoot { AppBase, LocalAppData }

public sealed class LoggingOptions
{
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
}

public enum LogLevel { Debug, Information, Warning, Error }

// Варіант «Склад»: одиниця виміру, якщо її не вказали під час додавання товару.
public sealed class WarehouseOptions
{
    public string DefaultUnit { get; init; } = "шт";
}

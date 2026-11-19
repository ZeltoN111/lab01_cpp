using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

// Лаба 9: збирання конфігурації. Кожне наступне джерело перекриває попереднє:
// appsettings.json → appsettings.{Environment}.json → user-secrets → змінні CROSSAPP_ → аргументи.
public sealed record AppConfig(AppOptions Options, StorageOptions Storage, string DataFile, string? ApiKey)
{
    public static AppConfig Load(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // не поточний каталог: запуск з будь-якого cwd дає той самий файл
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .AddCommandLine(args)
            .Build();

        var options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває розділ Storage.
        var legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        var storage = new StorageOptions
        {
            DataPath = string.IsNullOrWhiteSpace(legacy) ? options.Storage.DataPath : legacy,
            FileName = options.Storage.FileName,
            Root = options.Storage.Root
        };

        var dataFile = storage.ResolveFullPath(storage.ResolveBaseDirectory(AppContext.BaseDirectory));

        // Secret читається як звичайний ключ; у AppOptions не потрапляє, щоб його випадково не надрукувати.
        return new AppConfig(options, storage, dataFile, config["Integration:ApiKey"]);
    }

    public string ApiKeyStatus => ApiKey is null ? "ApiKey: не заданий" : $"ApiKey: присутній, довжина {ApiKey.Length}";
}

// Мінімальний лог із рівнем з конфігурації: Development друкує Debug, Production — лише Warning і вище.
public sealed class ConsoleLog(LogLevel minimum)
{
    public void Debug(string message) => Write(LogLevel.Debug, message);
    public void Info(string message) => Write(LogLevel.Information, message);
    public void Warn(string message) => Write(LogLevel.Warning, message);

    private void Write(LogLevel level, string message)
    {
        if (level < minimum) return;
        Console.WriteLine(level == LogLevel.Debug ? $"[debug] {message}" : message);
    }
}

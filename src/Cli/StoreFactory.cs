using Core;
using Core.Abstractions;
using Core.Storage;

// Варіант composition root «через фабрику»: усі new конкретних сховищ — тут,
// Program.cs лише викликає Create(args, dataFile). Шлях до файлу приходить з конфігурації (лаба 9).
public static class StoreFactory
{
    public static ICatalogStore Create(string[] args, string dataFile)
    {
        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(dataFile)
            : new InMemoryCatalogStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}

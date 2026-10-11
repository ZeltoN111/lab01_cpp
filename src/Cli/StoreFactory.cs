using Core;
using Core.Abstractions;
using Core.Storage;

// Варіант composition root «через фабрику»: усі new конкретних сховищ — тут,
// Program.cs лише викликає Create(args). Порівняйте з тернарним вибором у Program.cs.
public static class StoreFactory
{
    public static ICatalogStore Create(string[] args)
    {
        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(Path.Combine(AppContext.BaseDirectory, "data", "catalog.json"))
            : new InMemoryCatalogStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}

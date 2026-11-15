using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Імпорт постачань із JSON: справжній асинхронний ввід-вивід (FileStream з useAsync: true), без Task.Run.
public sealed class JsonCatalogImporter : ICatalogImporter, IStreamingCatalogImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); // скасований токен — скасування, а не «файл не знайдено»

        await using var stream = OpenRead(path);
        List<ProductDto> dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(stream, Options, ct)
                ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
        }
        catch (JsonException ex) // технічна помилка формату, не бізнес-правило
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} не є коректним JSON: {ex.Message}", ex);
        }

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToProduct(dtos[i], i + 1));
        }
        return result;
    }

    // [EnumeratorCancellation]: токен з WithCancellation(...) потрапляє в цей параметр.
    public async IAsyncEnumerable<Product> StreamAsync(
        string path, IProgress<int>? progress = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = OpenRead(path);
        var records = JsonSerializer.DeserializeAsyncEnumerable<ProductDto>(stream, Options, ct);
        await using var e = records.GetAsyncEnumerator(ct);

        var number = 0;
        var lastPercent = -1;
        while (true)
        {
            ProductDto? dto;
            try
            {
                if (!await e.MoveNextAsync()) break;
                dto = e.Current;
            }
            catch (JsonException ex) // yield у try/catch заборонений, тому catch лише навколо читання
            {
                throw new StorageException($"Файл {Path.GetFileName(path)} не є коректним JSON: {ex.Message}", ex);
            }

            ct.ThrowIfCancellationRequested();
            number++;
            if (dto is null) throw new DomainException($"Запис {number}: порожній запис.");

            var percent = stream.Length == 0 ? 100 : (int)(stream.Position * 100 / stream.Length);
            if (percent != lastPercent) progress?.Report(lastPercent = percent);

            yield return ToProduct(dto, number);
        }
        if (lastPercent != 100) progress?.Report(100);
    }

    private static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

    // Інваріанти Product кидають ArgumentException; для імпорту це погані дані — DomainException з номером запису.
    private static Product ToProduct(ProductDto dto, int number)
    {
        try { return Product.FromDto(dto); }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }
}

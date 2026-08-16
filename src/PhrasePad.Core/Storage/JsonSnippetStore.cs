using System.Text.Json;
using PhrasePad.Core.Models;

namespace PhrasePad.Core.Storage;

public sealed class JsonSnippetStore : ISnippetStore
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _baseDirectory;
    private readonly JsonSerializerOptions _serializerOptions;

    public JsonSnippetStore(
        string? baseDirectory = null,
        string fileName = "snippets.json",
        JsonSerializerOptions? serializerOptions = null)
    {
        _baseDirectory = baseDirectory ?? AppDataPaths.ResolvePhrasePadDirectory();
        _serializerOptions = serializerOptions ?? DefaultSerializerOptions;

        Directory.CreateDirectory(_baseDirectory);
        StoragePath = Path.Combine(_baseDirectory, fileName);
    }

    public string StoragePath { get; }

    public async Task<SnippetLibrary> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(StoragePath))
        {
            return new SnippetLibrary();
        }

        await using var stream = File.OpenRead(StoragePath);
        var library = await JsonSerializer.DeserializeAsync<SnippetLibrary>(
            stream,
            _serializerOptions,
            cancellationToken);

        return library ?? new SnippetLibrary();
    }

    public async Task SaveAsync(SnippetLibrary library, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(library);

        Directory.CreateDirectory(_baseDirectory);

        await using var stream = File.Create(StoragePath);
        await JsonSerializer.SerializeAsync(stream, library, _serializerOptions, cancellationToken);
    }
}

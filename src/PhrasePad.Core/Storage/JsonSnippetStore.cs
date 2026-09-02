using System.Security.Cryptography;
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
    private byte[]? _lastLoadedHash;
    private bool _hasLoaded;

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
        await using var lease = await AcquireLockAsync(cancellationToken);
        if (!File.Exists(StoragePath))
        {
            _lastLoadedHash = null;
            _hasLoaded = true;
            return new SnippetLibrary();
        }

        var bytes = await File.ReadAllBytesAsync(StoragePath, cancellationToken);
        var library = ValidateLibrary(
            JsonSerializer.Deserialize<SnippetLibrary>(bytes, _serializerOptions));
        _lastLoadedHash = SHA256.HashData(bytes);
        _hasLoaded = true;
        return library;
    }

    public async Task SaveAsync(SnippetLibrary library, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(library);
        ValidateLibrary(library);

        await using var lease = await AcquireLockAsync(cancellationToken);
        if (_hasLoaded)
        {
            var currentHash = await GetCurrentHashAsync(cancellationToken);
            if (!HashesMatch(_lastLoadedHash, currentHash))
            {
                throw new SnippetStoreConcurrencyException(StoragePath);
            }
        }

        var temporaryPath = Path.Combine(
            _baseDirectory,
            $".{Path.GetFileName(StoragePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, library, _serializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            var savedHash = SHA256.HashData(await File.ReadAllBytesAsync(temporaryPath, cancellationToken));
            ReplaceStorageFile(temporaryPath);
            _lastLoadedHash = savedHash;
            _hasLoaded = true;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void ReplaceStorageFile(string temporaryPath)
    {
        var destinationExists = File.Exists(StoragePath);
        if (!OperatingSystem.IsWindows())
        {
            var mode = destinationExists
                ? File.GetUnixFileMode(StoragePath)
                : UnixFileMode.UserRead | UnixFileMode.UserWrite;
            File.SetUnixFileMode(temporaryPath, mode);
        }

        if (OperatingSystem.IsWindows() && destinationExists)
        {
            File.Replace(temporaryPath, StoragePath, destinationBackupFileName: null);
            return;
        }

        File.Move(temporaryPath, StoragePath, overwrite: true);
    }

    private static SnippetLibrary ValidateLibrary(SnippetLibrary? library)
    {
        if (library?.Groups is null || library.Snippets is null)
        {
            throw new JsonException("The snippet library must contain non-null groups and snippets arrays.");
        }

        if (library.Groups.Any(group => group is null || group.Name is null))
        {
            throw new JsonException("The snippet library contains an invalid group.");
        }

        if (library.Snippets.Any(snippet =>
                snippet is null || snippet.Trigger is null || snippet.Expansion is null))
        {
            throw new JsonException("The snippet library contains an invalid snippet.");
        }

        return library;
    }

    private async Task<FileStream> AcquireLockAsync(CancellationToken cancellationToken)
    {
        var lockPath = $"{StoragePath}.lock";
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.Asynchronous);
            }
            catch (IOException) when (attempt < 200)
            {
                await Task.Delay(25, cancellationToken);
            }
        }
    }

    private async Task<byte[]?> GetCurrentHashAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(StoragePath))
        {
            return null;
        }

        return SHA256.HashData(await File.ReadAllBytesAsync(StoragePath, cancellationToken));
    }

    private static bool HashesMatch(byte[]? left, byte[]? right) =>
        left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);
}

public sealed class SnippetStoreConcurrencyException(string storagePath)
    : IOException($"The snippet library changed after it was loaded: {storagePath}");

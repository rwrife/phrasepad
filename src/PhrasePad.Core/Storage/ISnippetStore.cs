using PhrasePad.Core.Models;

namespace PhrasePad.Core.Storage;

public interface ISnippetStore
{
    string StoragePath { get; }

    Task<SnippetLibrary> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SnippetLibrary library, CancellationToken cancellationToken = default);
}

using PhrasePad.Core.Models;

namespace PhrasePad.Core.Expansion;

public interface IExpansionEngine
{
    ValueTask<ExpansionResult> ExpandAsync(
        Snippet snippet,
        string typedTrigger,
        CancellationToken cancellationToken = default);

    ValueTask<ExpansionResult> ExpandAsync(
        Snippet snippet,
        string typedTrigger,
        IReadOnlyDictionary<string, string> placeholderValues,
        CancellationToken cancellationToken = default);
}

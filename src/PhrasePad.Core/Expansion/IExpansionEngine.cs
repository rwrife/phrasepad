using PhrasePad.Core.Models;

namespace PhrasePad.Core.Expansion;

public interface IExpansionEngine
{
    ValueTask<ExpansionResult> ExpandAsync(
        Snippet snippet,
        string typedTrigger,
        CancellationToken cancellationToken = default);
}

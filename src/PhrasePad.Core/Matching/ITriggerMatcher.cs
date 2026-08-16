using PhrasePad.Core.Models;

namespace PhrasePad.Core.Matching;

public interface ITriggerMatcher
{
    string CurrentBuffer { get; }

    TriggerMatch? ProcessKeystroke(char input, IReadOnlyCollection<Snippet> snippets);

    TriggerMatch? TryMatchExplicit(string candidate, IReadOnlyCollection<Snippet> snippets);

    void Reset();
}

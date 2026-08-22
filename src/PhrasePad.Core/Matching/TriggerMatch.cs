using PhrasePad.Core.Models;

namespace PhrasePad.Core.Matching;

public sealed record TriggerMatch(
    Snippet Snippet,
    int TriggerStartIndex,
    int CharactersToErase,
    TriggerMode Mode)
{
    public string Trigger => Snippet.Trigger;
}

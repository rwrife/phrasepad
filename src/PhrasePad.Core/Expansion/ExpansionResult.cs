namespace PhrasePad.Core.Expansion;

public sealed record ExpansionResult(
    string FinalText,
    int CaretOffset,
    int BackspaceCount);

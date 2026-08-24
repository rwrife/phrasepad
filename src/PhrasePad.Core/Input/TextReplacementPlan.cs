namespace PhrasePad.Core.Input;

public sealed record TextReplacementPlan(
    string Text,
    int BackspaceCount,
    int CaretLeftCount,
    TextReplacementStrategy Strategy);

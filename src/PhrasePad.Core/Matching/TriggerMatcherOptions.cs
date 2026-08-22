namespace PhrasePad.Core.Matching;

public sealed class TriggerMatcherOptions
{
    private int _maxBufferLength = 256;

    public TriggerMode Mode { get; init; } = TriggerMode.PrefixCharacter;

    public char PrefixCharacter { get; init; } = ';';

    public bool CaseSensitive { get; init; }

    public bool RequireWordBoundary { get; init; } = true;

    public bool ResetOnNonMatchingSeparator { get; init; } = true;

    public string Separators { get; init; } = " \t\r\n.,!?()[]{}<>\"'";

    public int MaxBufferLength
    {
        get => _maxBufferLength;
        init => _maxBufferLength = value < 1 ? 1 : value;
    }
}

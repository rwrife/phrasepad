using System.Globalization;
using PhrasePad.Core.Expansion;

namespace PhrasePad.Core.Input;

public sealed class TextReplacementPlanner
{
    private readonly int _pasteThreshold;

    public TextReplacementPlanner(int pasteThreshold = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pasteThreshold);
        _pasteThreshold = pasteThreshold;
    }

    public TextReplacementPlan Plan(ExpansionResult expansion)
    {
        ArgumentNullException.ThrowIfNull(expansion);

        var strategy = expansion.FinalText.Length >= _pasteThreshold
            || expansion.FinalText.Contains('\n')
            || expansion.FinalText.Contains('\r')
            ? TextReplacementStrategy.ClipboardPaste
            : TextReplacementStrategy.UnicodeKeystrokes;

        var caretSuffix = expansion.FinalText[expansion.CaretOffset..];
        var caretLeftCount = StringInfo.ParseCombiningCharacters(caretSuffix).Length;

        return new TextReplacementPlan(
            expansion.FinalText,
            expansion.BackspaceCount,
            caretLeftCount,
            strategy);
    }
}

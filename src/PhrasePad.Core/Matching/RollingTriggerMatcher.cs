using System.Text;
using PhrasePad.Core.Models;

namespace PhrasePad.Core.Matching;

public sealed class RollingTriggerMatcher : ITriggerMatcher
{
    private readonly TriggerMatcherOptions _options;
    private readonly StringBuilder _buffer;
    private readonly StringComparison _comparison;

    public RollingTriggerMatcher(TriggerMatcherOptions? options = null)
    {
        _options = options ?? new TriggerMatcherOptions();
        _buffer = new StringBuilder(_options.MaxBufferLength);
        _comparison = _options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }

    public string CurrentBuffer => _buffer.ToString();

    public TriggerMatch? ProcessKeystroke(char input, IReadOnlyCollection<Snippet> snippets)
    {
        ArgumentNullException.ThrowIfNull(snippets);

        _buffer.Append(input);
        TrimBuffer();

        TriggerMatch? match = _options.Mode switch
        {
            TriggerMode.PrefixCharacter => IsSeparator(input)
                ? FindBestSuffixMatch(
                    _buffer.ToString(0, _buffer.Length - 1),
                    snippets,
                    TriggerMode.PrefixCharacter,
                    deferPrefixAmbiguity: false)
                : FindBestSuffixMatch(
                    _buffer.ToString(),
                    snippets,
                    TriggerMode.PrefixCharacter,
                    deferPrefixAmbiguity: true),
            TriggerMode.WhitespaceTerminated => char.IsWhiteSpace(input)
                ? FindBestSuffixMatch(
                    _buffer.ToString(0, _buffer.Length - 1),
                    snippets,
                    TriggerMode.WhitespaceTerminated,
                    deferPrefixAmbiguity: false)
                : null,
            TriggerMode.Explicit => null,
            _ => null
        };

        if (match is not null)
        {
            Reset();
            return match;
        }

        if (_options.ResetOnNonMatchingSeparator && IsSeparator(input))
        {
            Reset();
        }

        return null;
    }

    public TriggerMatch? TryMatchExplicit(string candidate, IReadOnlyCollection<Snippet> snippets)
    {
        ArgumentNullException.ThrowIfNull(snippets);

        if (_options.Mode != TriggerMode.Explicit || string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        return FindBestSuffixMatch(candidate, snippets, TriggerMode.Explicit, deferPrefixAmbiguity: false);
    }

    public void Reset() => _buffer.Clear();

    private TriggerMatch? FindBestSuffixMatch(
        string text,
        IReadOnlyCollection<Snippet> snippets,
        TriggerMode mode,
        bool deferPrefixAmbiguity)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        Snippet? best = null;
        var bestStart = -1;
        var bestLength = -1;

        foreach (var snippet in snippets)
        {
            if (!snippet.Enabled || string.IsNullOrWhiteSpace(snippet.Trigger))
            {
                continue;
            }

            if (mode == TriggerMode.PrefixCharacter && snippet.Trigger[0] != _options.PrefixCharacter)
            {
                continue;
            }

            if (!text.EndsWith(snippet.Trigger, _comparison))
            {
                continue;
            }

            var triggerStart = text.Length - snippet.Trigger.Length;

            if (_options.RequireWordBoundary && !HasWordBoundary(text, snippet.Trigger, triggerStart))
            {
                continue;
            }

            if (deferPrefixAmbiguity && mode == TriggerMode.PrefixCharacter && HasLongerPrefixCandidate(snippet, snippets))
            {
                continue;
            }

            if (snippet.Trigger.Length <= bestLength)
            {
                continue;
            }

            best = snippet;
            bestStart = triggerStart;
            bestLength = snippet.Trigger.Length;
        }

        return best is null
            ? null
            : new TriggerMatch(best, bestStart, bestLength, mode);
    }

    private bool HasLongerPrefixCandidate(Snippet snippet, IReadOnlyCollection<Snippet> snippets)
    {
        foreach (var candidate in snippets)
        {
            if (!candidate.Enabled || string.IsNullOrWhiteSpace(candidate.Trigger))
            {
                continue;
            }

            if (candidate.Trigger.Length <= snippet.Trigger.Length)
            {
                continue;
            }

            if (candidate.Trigger[0] != _options.PrefixCharacter)
            {
                continue;
            }

            if (candidate.Trigger.StartsWith(snippet.Trigger, _comparison))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasWordBoundary(string text, string trigger, int triggerStart)
    {
        if (triggerStart <= 0)
        {
            return true;
        }

        if (trigger.Length == 0 || !IsWordChar(trigger[0]))
        {
            return true;
        }

        return !IsWordChar(text[triggerStart - 1]);
    }

    private bool IsSeparator(char input)
    {
        if (char.IsWhiteSpace(input))
        {
            return true;
        }

        return _options.Separators.IndexOf(input) >= 0;
    }

    private void TrimBuffer()
    {
        if (_buffer.Length <= _options.MaxBufferLength)
        {
            return;
        }

        _buffer.Remove(0, _buffer.Length - _options.MaxBufferLength);
    }

    private static bool IsWordChar(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}

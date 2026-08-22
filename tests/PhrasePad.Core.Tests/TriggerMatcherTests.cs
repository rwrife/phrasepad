using PhrasePad.Core.Matching;
using PhrasePad.Core.Models;
using Xunit;

namespace PhrasePad.Core.Tests;

public sealed class TriggerMatcherTests
{
    [Fact]
    public void PrefixMode_ReturnsLongestEnabledMatch()
    {
        var snippets = new[]
        {
            new Snippet { Trigger = ";a", Expansion = "A" },
            new Snippet { Trigger = ";addr", Expansion = "Address" },
            new Snippet { Trigger = ";address", Expansion = "Disabled", Enabled = false }
        };

        var matcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.PrefixCharacter,
            PrefixCharacter = ';'
        });

        var match = Feed(matcher, "hello ;addr", snippets);

        Assert.NotNull(match);
        Assert.Equal(";addr", match!.Snippet.Trigger);
        Assert.Equal(TriggerMode.PrefixCharacter, match.Mode);
    }

    [Fact]
    public void DisabledSnippets_AreIgnored()
    {
        var snippets = new[]
        {
            new Snippet { Trigger = ";off", Expansion = "Nope", Enabled = false }
        };

        var matcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.PrefixCharacter,
            PrefixCharacter = ';'
        });

        var match = Feed(matcher, "test ;off", snippets);
        Assert.Null(match);
    }

    [Fact]
    public void Buffer_RemainsBounded()
    {
        var matcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.Explicit,
            MaxBufferLength = 5,
            ResetOnNonMatchingSeparator = false
        });

        Feed(matcher, "abcdefghij", Array.Empty<Snippet>());

        Assert.Equal(5, matcher.CurrentBuffer.Length);
        Assert.Equal("fghij", matcher.CurrentBuffer);
    }

    [Fact]
    public void NonMatchingSeparator_ResetsBuffer()
    {
        var matcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.PrefixCharacter,
            PrefixCharacter = ';',
            ResetOnNonMatchingSeparator = true
        });

        Feed(matcher, "abc.", Array.Empty<Snippet>());

        Assert.Equal(string.Empty, matcher.CurrentBuffer);
    }

    [Fact]
    public void WhitespaceMode_RespectsWordBoundary()
    {
        var snippets = new[]
        {
            new Snippet { Trigger = "sig", Expansion = "Signature" }
        };

        var matcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.WhitespaceTerminated,
            RequireWordBoundary = true
        });

        var noBoundaryMatch = Feed(matcher, "assign ", snippets);
        Assert.Null(noBoundaryMatch);

        var boundaryMatch = Feed(matcher, " sig ", snippets);
        Assert.NotNull(boundaryMatch);
        Assert.Equal("sig", boundaryMatch!.Snippet.Trigger);
    }

    [Fact]
    public void CaseSensitivity_IsConfigurable()
    {
        var snippets = new[]
        {
            new Snippet { Trigger = "Sig", Expansion = "Signature" }
        };

        var insensitiveMatcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.WhitespaceTerminated,
            CaseSensitive = false,
            RequireWordBoundary = true
        });

        var sensitiveMatcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.WhitespaceTerminated,
            CaseSensitive = true,
            RequireWordBoundary = true
        });

        var insensitiveMatch = Feed(insensitiveMatcher, " sig ", snippets);
        var sensitiveMatch = Feed(sensitiveMatcher, " sig ", snippets);

        Assert.NotNull(insensitiveMatch);
        Assert.Null(sensitiveMatch);
    }

    [Fact]
    public void ExplicitMode_MatchesViaExplicitCall()
    {
        var snippets = new[]
        {
            new Snippet { Trigger = ";addr", Expansion = "123 Main" }
        };

        var matcher = new RollingTriggerMatcher(new TriggerMatcherOptions
        {
            Mode = TriggerMode.Explicit,
            PrefixCharacter = ';'
        });

        var passiveMatch = Feed(matcher, ";addr", snippets);
        var explicitMatch = matcher.TryMatchExplicit(";addr", snippets);

        Assert.Null(passiveMatch);
        Assert.NotNull(explicitMatch);
        Assert.Equal(";addr", explicitMatch!.Snippet.Trigger);
    }

    private static TriggerMatch? Feed(
        ITriggerMatcher matcher,
        string input,
        IReadOnlyCollection<Snippet> snippets)
    {
        TriggerMatch? latestMatch = null;

        foreach (var ch in input)
        {
            var current = matcher.ProcessKeystroke(ch, snippets);
            if (current is not null)
            {
                latestMatch = current;
            }
        }

        return latestMatch;
    }
}

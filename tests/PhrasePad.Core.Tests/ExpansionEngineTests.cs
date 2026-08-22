using PhrasePad.Core.Expansion;
using PhrasePad.Core.Models;
using Xunit;

namespace PhrasePad.Core.Tests;

public sealed class ExpansionEngineTests
{
    [Fact]
    public async Task ExpandAsync_ReturnsPlainTextAndTypedTriggerLength()
    {
        var engine = new ExpansionEngine(Array.Empty<ITokenResolver>());
        var snippet = new Snippet { Trigger = ";sig", Expansion = "Kind regards" };

        var result = await engine.ExpandAsync(snippet, ";sig");

        Assert.Equal("Kind regards", result.FinalText);
        Assert.Equal(12, result.CaretOffset);
        Assert.Equal(4, result.BackspaceCount);
    }

    [Fact]
    public async Task ExpandAsync_UsesMatchingTokenResolver()
    {
        var engine = new ExpansionEngine(new[]
        {
            new StubTokenResolver("name", "PhrasePad")
        });
        var snippet = new Snippet { Trigger = ";app", Expansion = "Try {name}!" };

        var result = await engine.ExpandAsync(snippet, ";app");

        Assert.Equal("Try PhrasePad!", result.FinalText);
    }

    [Fact]
    public async Task ExpandAsync_RemovesCursorTokenAndReturnsItsPosition()
    {
        var engine = new ExpansionEngine(Array.Empty<ITokenResolver>());
        var snippet = new Snippet { Trigger = ";hello", Expansion = "Hello {cursor}!" };

        var result = await engine.ExpandAsync(snippet, ";hello");

        Assert.Equal("Hello !", result.FinalText);
        Assert.Equal(6, result.CaretOffset);
    }

    [Fact]
    public async Task ExpandAsync_RendersEscapedBracesLiterally()
    {
        var engine = new ExpansionEngine(new[]
        {
            new StubTokenResolver("name", "resolved")
        });
        var snippet = new Snippet
        {
            Trigger = ";literal",
            Expansion = "{{name}} costs {{5}}"
        };

        var result = await engine.ExpandAsync(snippet, ";literal");

        Assert.Equal("{name} costs {5}", result.FinalText);
    }

    [Fact]
    public async Task ExpandAsync_FormatsDateAndTimeTokens()
    {
        var now = new DateTimeOffset(2026, 8, 22, 14, 5, 9, TimeSpan.Zero);
        var engine = new ExpansionEngine(new ITokenResolver[]
        {
            new DateTimeTokenResolver(new FixedTimeProvider(now))
        });
        var snippet = new Snippet
        {
            Trigger = ";now",
            Expansion = "{date:yyyy-MM-dd} {time:HH:mm:ss}"
        };

        var result = await engine.ExpandAsync(snippet, ";now");

        Assert.Equal("2026-08-22 14:05:09", result.FinalText);
    }

    [Fact]
    public async Task ExpandAsync_UsesDefaultDateAndTimeFormats()
    {
        var now = new DateTimeOffset(2026, 8, 22, 14, 5, 9, TimeSpan.Zero);
        var engine = new ExpansionEngine(new ITokenResolver[]
        {
            new DateTimeTokenResolver(
                new FixedTimeProvider(now),
                System.Globalization.CultureInfo.InvariantCulture)
        });
        var snippet = new Snippet { Trigger = ";now", Expansion = "{date} {time}" };

        var result = await engine.ExpandAsync(snippet, ";now");

        Assert.Equal("08/22/2026 14:05", result.FinalText);
    }

    [Fact]
    public async Task ExpandAsync_ReadsClipboardThroughAbstraction()
    {
        var engine = new ExpansionEngine(new ITokenResolver[]
        {
            new ClipboardTokenResolver(new FakeClipboardReader("copied text"))
        });
        var snippet = new Snippet
        {
            Trigger = ";clip",
            Expansion = "Clipboard: {clipboard}"
        };

        var result = await engine.ExpandAsync(snippet, ";clip");

        Assert.Equal("Clipboard: copied text", result.FinalText);
    }

    [Fact]
    public async Task ExpandAsync_ThrowsWhenCancellationIsRequestedForPlainText()
    {
        var engine = new ExpansionEngine(Array.Empty<ITokenResolver>());
        var snippet = new Snippet { Trigger = ";plain", Expansion = "plain text" };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await engine.ExpandAsync(snippet, ";plain", cancellation.Token));
    }

    private sealed class FakeClipboardReader(string text) : IClipboardReader
    {
        public ValueTask<string> ReadTextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(text);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class StubTokenResolver(string supportedToken, string value) : ITokenResolver
    {
        public bool CanResolve(string token) => token == supportedToken;

        public ValueTask<string> ResolveAsync(
            string token,
            CancellationToken cancellationToken = default) => ValueTask.FromResult(value);
    }
}

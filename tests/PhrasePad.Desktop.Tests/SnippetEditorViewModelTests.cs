using PhrasePad.Core.Expansion;
using PhrasePad.Desktop.ViewModels;
using Xunit;

namespace PhrasePad.Desktop.Tests;

public sealed class SnippetEditorViewModelTests
{
    [Fact]
    public async Task EditingExpansion_UpdatesPreviewAndCaretMarker()
    {
        var engine = new ExpansionEngine(Array.Empty<ITokenResolver>());
        var subject = new SnippetEditorViewModel(engine);

        subject.Trigger = ";hello";
        subject.Expansion = "Hello {cursor}!";
        await subject.PreviewUpdateTask;

        Assert.Equal("Hello !", subject.PreviewText);
        Assert.Equal(6, subject.PreviewCaretOffset);
        Assert.Equal("Hello │!", subject.PreviewWithCaret);
    }

    [Fact]
    public void NewEditor_IsEnabledByDefault()
    {
        var engine = new ExpansionEngine(Array.Empty<ITokenResolver>());
        var subject = new SnippetEditorViewModel(engine);

        Assert.True(subject.IsEnabled);

        subject.IsEnabled = false;

        Assert.False(subject.IsEnabled);
    }

    [Fact]
    public async Task LatestEdit_WinsWhenAnOlderPreviewFinishesLast()
    {
        var engine = new ControlledExpansionEngine();
        var subject = new SnippetEditorViewModel(engine);

        subject.Expansion = "first";
        var firstUpdate = subject.PreviewUpdateTask;
        subject.Expansion = "second";
        var secondUpdate = subject.PreviewUpdateTask;

        engine.Complete("second", "second result");
        await secondUpdate;
        engine.Complete("first", "stale result");
        await firstUpdate;

        Assert.Equal("second result", subject.PreviewText);
    }

    private sealed class ControlledExpansionEngine : IExpansionEngine
    {
        private readonly Dictionary<string, TaskCompletionSource<ExpansionResult>> _updates = [];

        public ValueTask<ExpansionResult> ExpandAsync(
            PhrasePad.Core.Models.Snippet snippet,
            string typedTrigger,
            CancellationToken cancellationToken = default)
        {
            if (snippet.Expansion.Length == 0)
            {
                return ValueTask.FromResult(new ExpansionResult(string.Empty, 0, typedTrigger.Length));
            }

            var completion = new TaskCompletionSource<ExpansionResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _updates.Add(snippet.Expansion, completion);
            return new ValueTask<ExpansionResult>(completion.Task);
        }

        public ValueTask<ExpansionResult> ExpandAsync(
            PhrasePad.Core.Models.Snippet snippet,
            string typedTrigger,
            IReadOnlyDictionary<string, string> placeholderValues,
            CancellationToken cancellationToken = default) =>
            ExpandAsync(snippet, typedTrigger, cancellationToken);

        public void Complete(string expansion, string result)
        {
            _updates[expansion].SetResult(new ExpansionResult(result, result.Length, 0));
        }
    }
}

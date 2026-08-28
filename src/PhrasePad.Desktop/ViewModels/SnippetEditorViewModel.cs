using System;
using System.Threading;
using System.Threading.Tasks;
using PhrasePad.Core.Expansion;
using PhrasePad.Core.Models;

namespace PhrasePad.Desktop.ViewModels;

public sealed class SnippetEditorViewModel : ViewModelBase
{
    private readonly IExpansionEngine _expansionEngine;
    private int _previewRevision;
    private string _trigger = string.Empty;
    private string _expansion = string.Empty;
    private bool _isEnabled = true;
    private string _previewText = string.Empty;
    private int _previewCaretOffset;
    private string _previewWithCaret = "│";

    public SnippetEditorViewModel(IExpansionEngine expansionEngine)
    {
        _expansionEngine = expansionEngine ?? throw new ArgumentNullException(nameof(expansionEngine));
        SchedulePreview();
    }

    public string Trigger
    {
        get => _trigger;
        set
        {
            if (SetProperty(ref _trigger, value ?? string.Empty))
            {
                SchedulePreview();
            }
        }
    }

    public string Expansion
    {
        get => _expansion;
        set
        {
            if (SetProperty(ref _expansion, value ?? string.Empty))
            {
                SchedulePreview();
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string PreviewText
    {
        get => _previewText;
        private set => SetProperty(ref _previewText, value);
    }

    public int PreviewCaretOffset
    {
        get => _previewCaretOffset;
        private set => SetProperty(ref _previewCaretOffset, value);
    }

    public string PreviewWithCaret
    {
        get => _previewWithCaret;
        private set => SetProperty(ref _previewWithCaret, value);
    }

    public Task PreviewUpdateTask { get; private set; } = Task.CompletedTask;

    private void SchedulePreview()
    {
        var revision = Interlocked.Increment(ref _previewRevision);
        PreviewUpdateTask = UpdatePreviewAsync(revision);
    }

    private async Task UpdatePreviewAsync(int revision)
    {
        var snippet = new Snippet
        {
            Trigger = Trigger,
            Expansion = Expansion
        };

        var result = await _expansionEngine.ExpandAsync(snippet, Trigger);
        if (revision != Volatile.Read(ref _previewRevision))
        {
            return;
        }

        PreviewText = result.FinalText;
        PreviewCaretOffset = result.CaretOffset;
        PreviewWithCaret = result.FinalText.Insert(result.CaretOffset, "│");
    }
}

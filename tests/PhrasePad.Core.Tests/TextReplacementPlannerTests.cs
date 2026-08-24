using PhrasePad.Core.Expansion;
using PhrasePad.Core.Input;
using Xunit;

namespace PhrasePad.Core.Tests;

public sealed class TextReplacementPlannerTests
{
    [Fact]
    public void Plan_PreservesExactBackspaceCountAndComputesCaretMovement()
    {
        var planner = new TextReplacementPlanner();
        var expansion = new ExpansionResult("Hello !", CaretOffset: 6, BackspaceCount: 4);

        var plan = planner.Plan(expansion);

        Assert.Equal("Hello !", plan.Text);
        Assert.Equal(4, plan.BackspaceCount);
        Assert.Equal(1, plan.CaretLeftCount);
        Assert.Equal(TextReplacementStrategy.UnicodeKeystrokes, plan.Strategy);
    }

    [Fact]
    public void Plan_UsesClipboardPasteForMultilineText()
    {
        var planner = new TextReplacementPlanner();
        var expansion = new ExpansionResult("First line\nSecond line", CaretOffset: 22, BackspaceCount: 5);

        var plan = planner.Plan(expansion);

        Assert.Equal(TextReplacementStrategy.ClipboardPaste, plan.Strategy);
    }

    [Fact]
    public void Plan_UsesClipboardPasteAtConfiguredLengthThreshold()
    {
        var planner = new TextReplacementPlanner(pasteThreshold: 10);
        var expansion = new ExpansionResult("1234567890", CaretOffset: 10, BackspaceCount: 3);

        var plan = planner.Plan(expansion);

        Assert.Equal(TextReplacementStrategy.ClipboardPaste, plan.Strategy);
    }

    [Fact]
    public void Plan_CountsUnicodeTextElementsWhenMovingCaretLeft()
    {
        var planner = new TextReplacementPlanner();
        var expansion = new ExpansionResult("Hi 🙂", CaretOffset: 3, BackspaceCount: 4);

        var plan = planner.Plan(expansion);

        Assert.Equal(1, plan.CaretLeftCount);
    }
}

namespace PhrasePad.Core.Models;

public sealed class SnippetLibrary
{
    public int Version { get; init; } = 1;

    public List<SnippetGroup> Groups { get; init; } = [];

    public List<Snippet> Snippets { get; init; } = [];
}

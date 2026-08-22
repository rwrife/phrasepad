namespace PhrasePad.Core.Models;

public sealed class SnippetGroup
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = string.Empty;

    public bool Enabled { get; init; } = true;

    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

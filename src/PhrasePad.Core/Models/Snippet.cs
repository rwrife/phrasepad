namespace PhrasePad.Core.Models;

public sealed class Snippet
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Trigger { get; init; } = string.Empty;

    public string Expansion { get; init; } = string.Empty;

    public Guid? GroupId { get; init; }

    public bool Enabled { get; init; } = true;

    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

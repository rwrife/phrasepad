namespace PhrasePad.Core.Expansion;

public interface IClipboardReader
{
    ValueTask<string> ReadTextAsync(CancellationToken cancellationToken = default);
}

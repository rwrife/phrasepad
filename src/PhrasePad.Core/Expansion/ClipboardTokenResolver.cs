namespace PhrasePad.Core.Expansion;

public sealed class ClipboardTokenResolver(IClipboardReader clipboardReader) : ITokenResolver
{
    public bool CanResolve(string token) => token == "clipboard";

    public ValueTask<string> ResolveAsync(
        string token,
        CancellationToken cancellationToken = default) =>
        clipboardReader.ReadTextAsync(cancellationToken);
}

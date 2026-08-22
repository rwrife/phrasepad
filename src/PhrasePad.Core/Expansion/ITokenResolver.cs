namespace PhrasePad.Core.Expansion;

public interface ITokenResolver
{
    bool CanResolve(string token);

    ValueTask<string> ResolveAsync(
        string token,
        CancellationToken cancellationToken = default);
}

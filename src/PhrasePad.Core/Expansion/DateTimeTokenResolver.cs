using System.Globalization;

namespace PhrasePad.Core.Expansion;

public sealed class DateTimeTokenResolver : ITokenResolver
{
    private readonly TimeProvider _timeProvider;
    private readonly IFormatProvider _formatProvider;

    public DateTimeTokenResolver(
        TimeProvider? timeProvider = null,
        IFormatProvider? formatProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
    }

    public bool CanResolve(string token) =>
        token == "date" || token.StartsWith("date:", StringComparison.Ordinal) ||
        token == "time" || token.StartsWith("time:", StringComparison.Ordinal);

    public ValueTask<string> ResolveAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var separator = token.IndexOf(':');
        var name = separator < 0 ? token : token[..separator];
        var format = separator < 0
            ? name == "date" ? "d" : "t"
            : token[(separator + 1)..];
        var value = _timeProvider.GetLocalNow().ToString(format, _formatProvider);

        return ValueTask.FromResult(value);
    }
}

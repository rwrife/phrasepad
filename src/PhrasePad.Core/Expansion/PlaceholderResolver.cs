using System.Text;

namespace PhrasePad.Core.Expansion;

public sealed class PlaceholderResolver : IPlaceholderResolver
{
    private readonly IReadOnlyList<ITokenResolver> _tokenResolvers;

    public PlaceholderResolver(IEnumerable<ITokenResolver> tokenResolvers)
    {
        _tokenResolvers = tokenResolvers.ToArray();
    }

    public IReadOnlyList<PlaceholderField> GetFields(string expansion)
    {
        var fields = new List<PlaceholderField>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < expansion.Length; index++)
        {
            if (expansion[index] != '{')
            {
                continue;
            }

            if (index + 1 < expansion.Length && expansion[index + 1] == '{')
            {
                index++;
                continue;
            }

            var closingBrace = expansion.IndexOf('}', index + 1);
            if (closingBrace < 0)
            {
                break;
            }

            var expression = expansion[(index + 1)..closingBrace];
            index = closingBrace;

            if (expression.Length == 0 || expression == "cursor" ||
                _tokenResolvers.Any(resolver => resolver.CanResolve(expression)))
            {
                continue;
            }

            var separator = expression.IndexOf(':');
            var name = separator < 0 ? expression : expression[..separator];
            if (name.Length == 0 || !names.Add(name))
            {
                continue;
            }

            var defaultValue = separator < 0 ? null : expression[(separator + 1)..];
            fields.Add(new PlaceholderField(name, defaultValue));
        }

        return fields;
    }

    public string Fill(string expansion, IReadOnlyDictionary<string, string> values)
    {
        var output = new StringBuilder(expansion.Length);

        for (var index = 0; index < expansion.Length; index++)
        {
            if (expansion[index] != '{' ||
                index + 1 < expansion.Length && expansion[index + 1] == '{')
            {
                output.Append(expansion[index]);
                if (expansion[index] == '{')
                {
                    output.Append(expansion[++index]);
                }

                continue;
            }

            var closingBrace = expansion.IndexOf('}', index + 1);
            if (closingBrace < 0)
            {
                output.Append(expansion[index]);
                continue;
            }

            var expression = expansion[(index + 1)..closingBrace];
            var separator = expression.IndexOf(':');
            var name = separator < 0 ? expression : expression[..separator];
            var hasValue = values.TryGetValue(name, out var value);
            if (!hasValue && separator >= 0)
            {
                value = expression[(separator + 1)..];
                hasValue = true;
            }

            if (name.Length > 0 && expression != "cursor" &&
                !_tokenResolvers.Any(resolver => resolver.CanResolve(expression)) &&
                hasValue)
            {
                output.Append(value);
            }
            else
            {
                output.Append(expansion, index, closingBrace - index + 1);
            }

            index = closingBrace;
        }

        return output.ToString();
    }
}

using System.Text;
using PhrasePad.Core.Models;

namespace PhrasePad.Core.Expansion;

public sealed class ExpansionEngine : IExpansionEngine
{
    private readonly IReadOnlyList<ITokenResolver> _tokenResolvers;

    public ExpansionEngine(IEnumerable<ITokenResolver> tokenResolvers)
    {
        _tokenResolvers = tokenResolvers.ToArray();
    }

    public async ValueTask<ExpansionResult> ExpandAsync(
        Snippet snippet,
        string typedTrigger,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var output = new StringBuilder();
        int? caretOffset = null;
        var template = snippet.Expansion;

        for (var index = 0; index < template.Length; index++)
        {
            if (template[index] == '{' && index + 1 < template.Length && template[index + 1] == '{')
            {
                output.Append('{');
                index++;
                continue;
            }

            if (template[index] == '}' && index + 1 < template.Length && template[index + 1] == '}')
            {
                output.Append('}');
                index++;
                continue;
            }

            if (template[index] != '{')
            {
                output.Append(template[index]);
                continue;
            }

            var closingBrace = template.IndexOf('}', index + 1);
            if (closingBrace < 0)
            {
                output.Append(template[index]);
                continue;
            }

            var token = template[(index + 1)..closingBrace];
            if (token == "cursor")
            {
                caretOffset ??= output.Length;
                index = closingBrace;
                continue;
            }

            var resolver = _tokenResolvers.FirstOrDefault(candidate => candidate.CanResolve(token));
            if (resolver is null)
            {
                output.Append(template, index, closingBrace - index + 1);
            }
            else
            {
                output.Append(await resolver.ResolveAsync(token, cancellationToken));
            }

            index = closingBrace;
        }

        var finalText = output.ToString();
        return new ExpansionResult(finalText, caretOffset ?? finalText.Length, typedTrigger.Length);
    }
}

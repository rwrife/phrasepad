using PhrasePad.Core.Expansion;
using Xunit;

namespace PhrasePad.Core.Tests;

public sealed class PlaceholderResolverTests
{
    [Fact]
    public void GetFields_ReturnsOrderedDeduplicatedFieldsWithDefaults()
    {
        IPlaceholderResolver resolver = new PlaceholderResolver(Array.Empty<ITokenResolver>());

        var fields = resolver.GetFields("Hello {name}; amount {amount:0.00}; again {name}");

        Assert.Collection(
            fields,
            field =>
            {
                Assert.Equal("name", field.Name);
                Assert.Null(field.DefaultValue);
            },
            field =>
            {
                Assert.Equal("amount", field.Name);
                Assert.Equal("0.00", field.DefaultValue);
            });
    }

    [Fact]
    public void GetFields_ExcludesDynamicTokensAndCursorMarker()
    {
        IPlaceholderResolver resolver = new PlaceholderResolver(new[]
        {
            new StubTokenResolver("date:yyyy-MM-dd")
        });

        var fields = resolver.GetFields("{recipient} on {date:yyyy-MM-dd}{cursor}");

        var field = Assert.Single(fields);
        Assert.Equal("recipient", field.Name);
    }

    [Fact]
    public void Fill_ReplacesEveryOccurrenceByName()
    {
        IPlaceholderResolver resolver = new PlaceholderResolver(Array.Empty<ITokenResolver>());
        var values = new Dictionary<string, string> { ["name"] = "Ada" };

        var result = resolver.Fill("Hello {name}; goodbye {name:friend}", values);

        Assert.Equal("Hello Ada; goodbye Ada", result);
    }

    [Fact]
    public void Fill_UsesDefaultWhenValueIsMissing()
    {
        IPlaceholderResolver resolver = new PlaceholderResolver(Array.Empty<ITokenResolver>());

        var result = resolver.Fill("Amount: {amount:0.00}", new Dictionary<string, string>());

        Assert.Equal("Amount: 0.00", result);
    }

    private sealed class StubTokenResolver(string supportedToken) : ITokenResolver
    {
        public bool CanResolve(string token) => token == supportedToken;

        public ValueTask<string> ResolveAsync(
            string token,
            CancellationToken cancellationToken = default) => ValueTask.FromResult(string.Empty);
    }
}

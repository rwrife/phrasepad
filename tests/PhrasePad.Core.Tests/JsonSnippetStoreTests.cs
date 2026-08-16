using PhrasePad.Core.Models;
using PhrasePad.Core.Storage;
using Xunit;

namespace PhrasePad.Core.Tests;

public sealed class JsonSnippetStoreTests
{
    [Fact]
    public async Task RoundTrip_PreservesAllData()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phrasepad-tests-{Guid.NewGuid():N}");

        try
        {
            var groupId = Guid.NewGuid();
            var created = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            var updated = created.AddMinutes(5);

            var library = new SnippetLibrary
            {
                Version = 1,
                Groups =
                [
                    new SnippetGroup
                    {
                        Id = groupId,
                        Name = "General",
                        Enabled = true,
                        CreatedUtc = created,
                        UpdatedUtc = updated
                    }
                ],
                Snippets =
                [
                    new Snippet
                    {
                        Id = Guid.NewGuid(),
                        Trigger = ";sig",
                        Expansion = "Best regards",
                        GroupId = groupId,
                        Enabled = true,
                        CreatedUtc = created,
                        UpdatedUtc = updated
                    },
                    new Snippet
                    {
                        Id = Guid.NewGuid(),
                        Trigger = ";addr",
                        Expansion = "123 Main St",
                        GroupId = groupId,
                        Enabled = false,
                        CreatedUtc = created,
                        UpdatedUtc = updated
                    }
                ]
            };

            var store = new JsonSnippetStore(root);
            await store.SaveAsync(library);
            var loaded = await store.LoadAsync();

            Assert.True(File.Exists(store.StoragePath));

            Assert.Equal(library.Version, loaded.Version);
            Assert.Equal(library.Groups.Count, loaded.Groups.Count);
            Assert.Equal(library.Snippets.Count, loaded.Snippets.Count);

            var expectedGroup = library.Groups.Single();
            var actualGroup = loaded.Groups.Single();
            Assert.Equal(expectedGroup.Id, actualGroup.Id);
            Assert.Equal(expectedGroup.Name, actualGroup.Name);
            Assert.Equal(expectedGroup.Enabled, actualGroup.Enabled);
            Assert.Equal(expectedGroup.CreatedUtc, actualGroup.CreatedUtc);
            Assert.Equal(expectedGroup.UpdatedUtc, actualGroup.UpdatedUtc);

            foreach (var expectedSnippet in library.Snippets.OrderBy(s => s.Trigger))
            {
                var actualSnippet = loaded.Snippets.Single(s => s.Id == expectedSnippet.Id);
                Assert.Equal(expectedSnippet.Trigger, actualSnippet.Trigger);
                Assert.Equal(expectedSnippet.Expansion, actualSnippet.Expansion);
                Assert.Equal(expectedSnippet.GroupId, actualSnippet.GroupId);
                Assert.Equal(expectedSnippet.Enabled, actualSnippet.Enabled);
                Assert.Equal(expectedSnippet.CreatedUtc, actualSnippet.CreatedUtc);
                Assert.Equal(expectedSnippet.UpdatedUtc, actualSnippet.UpdatedUtc);
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Load_WhenFileDoesNotExist_ReturnsEmptyLibrary()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phrasepad-tests-{Guid.NewGuid():N}");

        try
        {
            var store = new JsonSnippetStore(root);
            var loaded = await store.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Empty(loaded.Groups);
            Assert.Empty(loaded.Snippets);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

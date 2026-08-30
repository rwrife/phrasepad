using PhrasePad.Core.Models;
using PhrasePad.Core.Storage;
using Xunit;

namespace PhrasePad.Core.Tests;

public sealed class SnippetLibraryMergerTests
{
    [Fact]
    public void Merge_AddsNewSnippetsWithoutReplacingExistingSnippets()
    {
        var existing = new Snippet { Trigger = ";existing", Expansion = "Keep me" };
        var imported = new Snippet { Trigger = ";new", Expansion = "Add me" };
        var current = new SnippetLibrary { Snippets = [existing] };
        var incoming = new SnippetLibrary { Snippets = [imported] };

        var result = SnippetLibraryMerger.Merge(current, incoming);

        Assert.Empty(result.Conflicts);
        Assert.Equal(2, result.Library.Snippets.Count);
        Assert.Contains(result.Library.Snippets, snippet => snippet.Id == existing.Id);
        Assert.Contains(result.Library.Snippets, snippet => snippet.Id == imported.Id);
    }

    [Fact]
    public void Merge_ReportsTriggerConflictsAndKeepsTheCurrentSnippet()
    {
        var existing = new Snippet { Trigger = ";sig", Expansion = "Current" };
        var incoming = new Snippet { Trigger = ";sig", Expansion = "Imported" };

        var result = SnippetLibraryMerger.Merge(
            new SnippetLibrary { Snippets = [existing] },
            new SnippetLibrary { Snippets = [incoming] });

        var snippet = Assert.Single(result.Library.Snippets);
        Assert.Equal(existing.Id, snippet.Id);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("snippet", conflict.Kind);
        Assert.Equal(";sig", conflict.Key);
    }

    [Fact]
    public void Merge_ReportsGroupConflictsAndKeepsTheCurrentGroup()
    {
        var id = Guid.NewGuid();
        var existing = new SnippetGroup { Id = id, Name = "Current" };
        var incoming = new SnippetGroup { Id = id, Name = "Imported" };

        var result = SnippetLibraryMerger.Merge(
            new SnippetLibrary { Groups = [existing] },
            new SnippetLibrary { Groups = [incoming] });

        var group = Assert.Single(result.Library.Groups);
        Assert.Equal("Current", group.Name);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("group", conflict.Kind);
        Assert.Equal(id.ToString(), conflict.Key);
    }

    [Fact]
    public void Merge_DoesNotAttachImportedSnippetsToAConflictingGroup()
    {
        var groupId = Guid.NewGuid();
        var currentGroup = new SnippetGroup { Id = groupId, Name = "Current" };
        var importedGroup = new SnippetGroup { Id = groupId, Name = "Imported" };
        var importedSnippet = new Snippet
        {
            Trigger = ";imported",
            Expansion = "Must not be re-parented",
            GroupId = groupId
        };

        var result = SnippetLibraryMerger.Merge(
            new SnippetLibrary { Groups = [currentGroup] },
            new SnippetLibrary { Groups = [importedGroup], Snippets = [importedSnippet] });

        Assert.Empty(result.Library.Snippets);
        Assert.Contains(result.Conflicts, conflict =>
            conflict.Kind == "snippet" && conflict.Key == importedSnippet.Trigger);
    }

    [Fact]
    public void Merge_DoesNotAttachAnOrphanedImportedSnippetToAnExistingGroup()
    {
        var group = new SnippetGroup { Name = "Existing" };
        var importedSnippet = new Snippet
        {
            Trigger = ";orphan",
            Expansion = "No imported group definition",
            GroupId = group.Id
        };

        var result = SnippetLibraryMerger.Merge(
            new SnippetLibrary { Groups = [group] },
            new SnippetLibrary { Snippets = [importedSnippet] });

        Assert.Empty(result.Library.Snippets);
        Assert.Contains(result.Conflicts, conflict =>
            conflict.Kind == "snippet" && conflict.Key == importedSnippet.Trigger);
    }
}

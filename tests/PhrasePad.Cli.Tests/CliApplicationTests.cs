using System.Text.Json;
using PhrasePad.Cli;
using PhrasePad.Core.Models;
using PhrasePad.Core.Storage;
using Xunit;

namespace PhrasePad.Cli.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task List_WithJson_WritesMachineReadableLibrary()
    {
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary
        {
            Snippets = [new Snippet { Trigger = ";sig", Expansion = "Regards" }]
        });

        var exitCode = await fixture.RunAsync("list", "--json");

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(fixture.Output.ToString());
        var snippet = Assert.Single(document.RootElement.GetProperty("snippets").EnumerateArray());
        Assert.Equal(";sig", snippet.GetProperty("trigger").GetString());
        Assert.Empty(fixture.Error.ToString());
    }

    [Fact]
    public async Task List_WritesGroupsAndSnippetsAsText()
    {
        var group = new SnippetGroup { Name = "General" };
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary
        {
            Groups = [group],
            Snippets = [new Snippet { Trigger = ";sig", Expansion = "Regards", GroupId = group.Id }]
        });

        var exitCode = await fixture.RunAsync("list");

        Assert.Equal(0, exitCode);
        Assert.Contains("General", fixture.Output.ToString());
        Assert.Contains(";sig", fixture.Output.ToString());
    }

    [Fact]
    public async Task Expand_FillsPlaceholdersAndMarksTheCaretWithoutSendingKeystrokes()
    {
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary
        {
            Snippets =
            [
                new Snippet
                {
                    Trigger = ";hello",
                    Expansion = "Hello {name}{cursor}!"
                }
            ]
        });

        var exitCode = await fixture.RunAsync("expand", ";hello", "--field", "name=Ada");

        Assert.Equal(0, exitCode);
        Assert.Equal($"Hello Ada⟦cursor⟧!{Environment.NewLine}", fixture.Output.ToString());
        Assert.Empty(fixture.Error.ToString());
    }

    [Fact]
    public async Task Expand_WithJson_WritesTextAndCaretOffset()
    {
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary
        {
            Snippets = [new Snippet { Trigger = ";edit", Expansion = "before{cursor}after" }]
        });

        var exitCode = await fixture.RunAsync("expand", ";edit", "--json");

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(fixture.Output.ToString());
        Assert.Equal("beforeafter", document.RootElement.GetProperty("text").GetString());
        Assert.Equal(6, document.RootElement.GetProperty("caretOffset").GetInt32());
        Assert.Equal("before⟦cursor⟧after", document.RootElement.GetProperty("preview").GetString());
    }

    [Fact]
    public async Task Export_WritesTheCurrentLibraryToTheRequestedFile()
    {
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary
        {
            Snippets = [new Snippet { Trigger = ";backup", Expansion = "Saved" }]
        });
        var exportPath = Path.Combine(fixture.Root, "backup.json");

        var exitCode = await fixture.RunAsync("export", exportPath, "--json");

        Assert.Equal(0, exitCode);
        var exported = await new JsonSnippetStore(fixture.Root, "backup.json").LoadAsync();
        Assert.Equal(";backup", Assert.Single(exported.Snippets).Trigger);
        using var document = JsonDocument.Parse(fixture.Output.ToString());
        Assert.Equal(exportPath, document.RootElement.GetProperty("path").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("snippetCount").GetInt32());
    }

    [Fact]
    public async Task Import_MergesNewSnippetsAndReportsConflictsWithoutClobbering()
    {
        var currentSnippet = new Snippet { Trigger = ";sig", Expansion = "Current" };
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary
        {
            Snippets = [currentSnippet]
        });
        var importPath = Path.Combine(fixture.Root, "import.json");
        await new JsonSnippetStore(fixture.Root, "import.json").SaveAsync(new SnippetLibrary
        {
            Snippets =
            [
                new Snippet { Trigger = ";sig", Expansion = "Imported conflict" },
                new Snippet { Trigger = ";new", Expansion = "New value" }
            ]
        });

        var exitCode = await fixture.RunAsync("import", importPath, "--json");

        Assert.Equal(4, exitCode);
        var merged = await new JsonSnippetStore(fixture.Root).LoadAsync();
        Assert.Equal(2, merged.Snippets.Count);
        Assert.Equal("Current", merged.Snippets.Single(snippet => snippet.Trigger == ";sig").Expansion);
        Assert.Equal("New value", merged.Snippets.Single(snippet => snippet.Trigger == ";new").Expansion);
        using var document = JsonDocument.Parse(fixture.Output.ToString());
        Assert.Equal(1, document.RootElement.GetProperty("addedSnippets").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("conflictCount").GetInt32());
    }

    [Fact]
    public async Task Import_WithInvalidJson_ReturnsAnErrorExitCode()
    {
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary());
        var importPath = Path.Combine(fixture.Root, "invalid.json");
        await File.WriteAllTextAsync(importPath, "not json");

        var exitCode = await fixture.RunAsync("import", importPath);

        Assert.Equal(1, exitCode);
        Assert.Contains("invalid.json", fixture.Error.ToString());
    }

    [Theory]
    [InlineData("list", "--library")]
    [InlineData("list", "--unknown")]
    [InlineData("expand", ";sig", "--field")]
    public async Task MalformedOptions_ReturnAUsageError(params string[] arguments)
    {
        await using var fixture = await CliFixture.CreateAsync(new SnippetLibrary
        {
            Snippets = [new Snippet { Trigger = ";sig", Expansion = "Hello {name}" }]
        });

        var exitCode = await fixture.RunAsync(arguments);

        Assert.Equal(2, exitCode);
        Assert.NotEmpty(fixture.Error.ToString());
    }

    private sealed class CliFixture : IAsyncDisposable
    {
        private CliFixture(string root)
        {
            Root = root;
            LibraryPath = Path.Combine(root, "snippets.json");
        }

        public string Root { get; }
        public string LibraryPath { get; }
        public StringWriter Output { get; } = new();
        public StringWriter Error { get; } = new();

        public static async Task<CliFixture> CreateAsync(SnippetLibrary library)
        {
            var fixture = new CliFixture(Path.Combine(
                Path.GetTempPath(),
                $"phrasepad-cli-tests-{Guid.NewGuid():N}"));
            await new JsonSnippetStore(fixture.Root).SaveAsync(library);
            return fixture;
        }

        public Task<int> RunAsync(params string[] arguments)
        {
            var app = new CliApplication(Output, Error);
            return app.RunAsync([.. arguments, "--library", LibraryPath]);
        }

        public ValueTask DisposeAsync()
        {
            Output.Dispose();
            Error.Dispose();
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}

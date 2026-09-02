using System.Text.Json;
using PhrasePad.Core.Expansion;
using PhrasePad.Core.Storage;

namespace PhrasePad.Cli;

public sealed class CliApplication(TextWriter output, TextWriter error)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        try
        {
            return await RunCoreAsync(args, cancellationToken);
        }
        catch (CliUsageException exception)
        {
            await error.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            var subject = args.Length > 1 ? $" for '{args[1]}'" : string.Empty;
            await error.WriteLineAsync($"Command failed{subject}: {exception.Message}");
            return 1;
        }
    }

    private async Task<int> RunCoreAsync(string[] args, CancellationToken cancellationToken)
    {
        var parsed = ParseArguments(args);
        var store = CreateStore(parsed.LibraryPath);

        if (parsed.Command == "list")
        {
            var library = await store.LoadAsync(cancellationToken);
            if (parsed.Json)
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(library, JsonOptions));
                return 0;
            }

            foreach (var group in library.Groups.OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase))
            {
                await output.WriteLineAsync($"[{group.Name}] {(group.Enabled ? string.Empty : "(disabled)")}".TrimEnd());
            }

            foreach (var snippet in library.Snippets.OrderBy(snippet => snippet.Trigger, StringComparer.Ordinal))
            {
                var groupName = library.Groups.FirstOrDefault(group => group.Id == snippet.GroupId)?.Name ?? "Ungrouped";
                await output.WriteLineAsync($"{snippet.Trigger}\t{groupName}\t{(snippet.Enabled ? "enabled" : "disabled")}");
            }

            return 0;
        }

        if (parsed.Command == "expand")
        {
            var trigger = parsed.Operand!;
            var library = await store.LoadAsync(cancellationToken);
            var snippet = library.Snippets.FirstOrDefault(candidate =>
                candidate.Enabled &&
                string.Equals(candidate.Trigger, trigger, StringComparison.Ordinal) &&
                (candidate.GroupId is null || library.Groups.Any(group => group.Id == candidate.GroupId && group.Enabled)));
            if (snippet is null)
            {
                await error.WriteLineAsync($"No enabled snippet found for trigger '{trigger}'.");
                return 3;
            }

            var engine = new ExpansionEngine([new DateTimeTokenResolver()]);
            var result = await engine.ExpandAsync(snippet, trigger, parsed.Fields, cancellationToken);
            var preview = result.FinalText.Insert(result.CaretOffset, "⟦cursor⟧");
            if (parsed.Json)
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(new
                {
                    trigger = snippet.Trigger,
                    text = result.FinalText,
                    result.CaretOffset,
                    preview
                }, JsonOptions));
            }
            else
            {
                await output.WriteLineAsync(preview);
            }

            return 0;
        }

        if (parsed.Command == "export")
        {
            var library = await store.LoadAsync(cancellationToken);
            var exportPath = Path.GetFullPath(parsed.Operand!);
            await CreateStore(exportPath).SaveAsync(library, cancellationToken);
            if (parsed.Json)
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(new
                {
                    path = exportPath,
                    groupCount = library.Groups.Count,
                    snippetCount = library.Snippets.Count
                }, JsonOptions));
            }
            else
            {
                await output.WriteLineAsync($"Exported {library.Snippets.Count} snippet(s) to {exportPath}.");
            }

            return 0;
        }

        if (parsed.Command == "import")
        {
            var importPath = Path.GetFullPath(parsed.Operand!);
            if (!File.Exists(importPath))
            {
                await error.WriteLineAsync($"Import file not found: {importPath}");
                return 1;
            }

            var current = await store.LoadAsync(cancellationToken);
            var incoming = await CreateStore(importPath).LoadAsync(cancellationToken);
            var merged = SnippetLibraryMerger.Merge(current, incoming);
            await store.SaveAsync(merged.Library, cancellationToken);
            var addedGroups = merged.Library.Groups.Count - current.Groups.Count;
            var addedSnippets = merged.Library.Snippets.Count - current.Snippets.Count;

            if (parsed.Json)
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(new
                {
                    path = importPath,
                    addedGroups,
                    addedSnippets,
                    conflictCount = merged.Conflicts.Count,
                    conflicts = merged.Conflicts
                }, JsonOptions));
            }
            else
            {
                await output.WriteLineAsync($"Imported {addedSnippets} snippet(s) and {addedGroups} group(s).");
                foreach (var conflict in merged.Conflicts)
                {
                    await error.WriteLineAsync($"Conflict ({conflict.Kind} {conflict.Key}): {conflict.Reason}");
                }
            }

            return merged.Conflicts.Count == 0 ? 0 : 4;
        }

        throw new CliUsageException($"Unknown command: {parsed.Command}");
    }

    private static ParsedArguments ParseArguments(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            throw new CliUsageException("A command is required. Use list, expand, import, or export.");
        }

        var command = args[0];
        var requiresOperand = command is "expand" or "import" or "export";
        if (command is not ("list" or "expand" or "import" or "export"))
        {
            throw new CliUsageException($"Unknown command: {command}");
        }

        string? operand = null;
        var index = 1;
        if (requiresOperand)
        {
            if (index >= args.Count || args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new CliUsageException($"Usage: phrasepad {command} <{(command == "expand" ? "trigger" : "file")}> [options]");
            }

            operand = args[index++];
        }

        var json = false;
        var hasLibrary = false;
        string? libraryPath = null;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);

        while (index < args.Count)
        {
            var option = args[index++];
            if (option == "--json")
            {
                if (json)
                {
                    throw new CliUsageException("Option --json may only be specified once.");
                }

                json = true;
                continue;
            }

            if (option == "--library")
            {
                if (hasLibrary)
                {
                    throw new CliUsageException("Option --library may only be specified once.");
                }

                if (index >= args.Count || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new CliUsageException("Option --library requires a file path.");
                }

                hasLibrary = true;
                libraryPath = args[index++];
                continue;
            }

            if (option == "--field" && command == "expand")
            {
                if (index >= args.Count || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new CliUsageException("Option --field requires name=value.");
                }

                var assignment = args[index++];
                var separator = assignment.IndexOf('=');
                if (separator <= 0)
                {
                    throw new CliUsageException("Option --field requires name=value.");
                }

                fields[assignment[..separator]] = assignment[(separator + 1)..];
                continue;
            }

            throw new CliUsageException($"Unknown option or extra argument: {option}");
        }

        return new ParsedArguments(command, operand, json, libraryPath, fields);
    }

    private static JsonSnippetStore CreateStore(string? libraryPath)
    {
        if (string.IsNullOrWhiteSpace(libraryPath))
        {
            return new JsonSnippetStore();
        }

        var fullPath = Path.GetFullPath(libraryPath);
        return new JsonSnippetStore(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
    }

    private sealed record ParsedArguments(
        string Command,
        string? Operand,
        bool Json,
        string? LibraryPath,
        IReadOnlyDictionary<string, string> Fields);

    private sealed class CliUsageException(string message) : Exception(message);
}

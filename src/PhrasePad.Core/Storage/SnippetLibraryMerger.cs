using PhrasePad.Core.Models;

namespace PhrasePad.Core.Storage;

public static class SnippetLibraryMerger
{
    public static SnippetLibraryMergeResult Merge(SnippetLibrary current, SnippetLibrary incoming)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(incoming);

        var groups = current.Groups.ToList();
        var snippets = current.Snippets.ToList();
        var conflicts = new List<SnippetLibraryConflict>();
        var conflictingGroupIds = new HashSet<Guid>();
        var incomingGroupIds = incoming.Groups.Select(group => group.Id).ToHashSet();

        foreach (var candidate in incoming.Groups)
        {
            var existing = groups.FirstOrDefault(group => group.Id == candidate.Id);
            if (existing is null)
            {
                groups.Add(candidate);
                continue;
            }

            if (!GroupEquals(existing, candidate))
            {
                conflictingGroupIds.Add(candidate.Id);
                conflicts.Add(new SnippetLibraryConflict(
                    "group",
                    candidate.Id.ToString(),
                    "A group with the same id already exists."));
            }
        }

        foreach (var candidate in incoming.Snippets)
        {
            if (candidate.GroupId is Guid referencedGroupId && !incomingGroupIds.Contains(referencedGroupId))
            {
                conflicts.Add(new SnippetLibraryConflict(
                    "snippet",
                    candidate.Trigger,
                    "The snippet references a group that is missing from the imported library."));
                continue;
            }

            if (candidate.GroupId is Guid groupId && conflictingGroupIds.Contains(groupId))
            {
                conflicts.Add(new SnippetLibraryConflict(
                    "snippet",
                    candidate.Trigger,
                    "The snippet references a conflicting group and was not imported."));
                continue;
            }

            var existing = snippets.FirstOrDefault(snippet =>
                snippet.Id == candidate.Id ||
                string.Equals(snippet.Trigger, candidate.Trigger, StringComparison.Ordinal));
            if (existing is null)
            {
                snippets.Add(candidate);
                continue;
            }

            if (!SnippetEquals(existing, candidate))
            {
                conflicts.Add(new SnippetLibraryConflict(
                    "snippet",
                    candidate.Trigger,
                    "A snippet with the same id or trigger already exists."));
            }
        }

        return new SnippetLibraryMergeResult(
            new SnippetLibrary
            {
                Version = Math.Max(current.Version, incoming.Version),
                Groups = groups,
                Snippets = snippets
            },
            conflicts);
    }

    private static bool GroupEquals(SnippetGroup left, SnippetGroup right) =>
        left.Id == right.Id &&
        left.Name == right.Name &&
        left.Enabled == right.Enabled &&
        left.CreatedUtc == right.CreatedUtc &&
        left.UpdatedUtc == right.UpdatedUtc;

    private static bool SnippetEquals(Snippet left, Snippet right) =>
        left.Id == right.Id &&
        left.Trigger == right.Trigger &&
        left.Expansion == right.Expansion &&
        left.GroupId == right.GroupId &&
        left.Enabled == right.Enabled &&
        left.CreatedUtc == right.CreatedUtc &&
        left.UpdatedUtc == right.UpdatedUtc;
}

public sealed record SnippetLibraryMergeResult(
    SnippetLibrary Library,
    IReadOnlyList<SnippetLibraryConflict> Conflicts);

public sealed record SnippetLibraryConflict(string Kind, string Key, string Reason);

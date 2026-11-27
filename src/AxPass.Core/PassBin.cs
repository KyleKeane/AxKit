namespace AxPass.Core;

/// <summary>What a delete does to an entry or a group, as far as axPass knows before it runs the tool.</summary>
public enum DeleteFate
{
    /// <summary>It moves to the Recycle Bin.</summary>
    Recycled,

    /// <summary>It is in the Recycle Bin, and is deleted for good.</summary>
    ForGood,

    /// <summary>The tool's output does not say which: the delete asks first, in those words.</summary>
    NotKnown,
}

/// <summary>
/// What became of one entry or group the tool removed, read from the database's listing before and after: the group
/// it lies in now (<paramref name="To"/>; null when it is in none, deleted for good), and whether it is the only
/// one of its name there (<paramref name="Sole"/>), which is what lets the tool move it back by that name.
/// </summary>
public sealed record Removed(PassNode Node, string? To, bool Sole);

/// <summary>
/// What axPass knows of a database's Recycle Bin (docs/axPass/SPEC.md PW-6.4; the review of 2026-11-27), all of it
/// from KeePassXC's tool's own output, and the words that say no more than is known. Pure, tested.
///
/// The database marks its Recycle Bin by the group's own identity, and the tool prints that nowhere: <c>db-info</c>
/// says whether the database has one, <c>ls</c> lists groups by name, and <c>rm</c> moves an entry to the bin or,
/// with the bin off or for an entry in it, deletes it for good without a word (with <c>-q</c>). Until this review
/// axPass took the top group named "Recycle Bin" for the bin: a database with the bin off, or with its bin under
/// another name (made by a KeePassXC in another language), had entries deleted for good under "Moved to the
/// Recycle Bin; Ctrl+Z brings it back". Now:
/// <list type="bullet">
/// <item>the bin is known (<see cref="Known"/>) in one way: axPass has seen a deleted entry arrive in a group
/// since the database was unlocked. Until the review's second pass the top group of the tool's own name for it
/// was taken too, when the tool said the database has a bin; with the real bin under another name beside a
/// group of that name, an entry in the real bin was then deleted for good without the question;</item>
/// <item>a delete whose end is not known asks first, saying so (<see cref="Fate"/>, <see cref="Question"/>);</item>
/// <item>after every delete the listing is read again and what is said is what it shows (<see cref="MovedTo"/>,
/// <see cref="Said"/>): moved, and to which group, or deleted for good;</item>
/// <item>Ctrl+Z moves an entry back only while it is the one entry of its name in the bin (<see cref="Removed.Sole"/>,
/// asked again of a fresh listing before the move): the tool reaches an entry by its path alone and takes the first
/// of two, which is the older one.</item>
/// </list>
/// </summary>
public static class PassBin
{
    /// <summary>Whether the tool's <c>db-info</c> says the database has a Recycle Bin: its line for that, whole (<see cref="KeePassCliTexts.RecycleBinOn"/>).</summary>
    public static bool IsOn(string info) =>
        info.ReplaceLineEndings("\n").Split('\n').Any(line => line.Trim() == KeePassCliTexts.RecycleBinOn);

    /// <summary>
    /// The group axPass takes for the Recycle Bin: the group it saw a deleted entry arrive in (<paramref name="seen"/>),
    /// while that group is there. Null: not known, which it is from each unlock until the first delete was seen.
    /// A group is never the bin by its name, also not the tool's own name for it in a database the tool says has
    /// one: the bin may be another group beside it.
    /// </summary>
    public static string? Known(PassTree tree, string? seen) =>
        seen is { Length: > 0 } && tree.Nodes.Any(node => node.IsGroup && node.Path == seen) ? seen : null;

    /// <summary>Whether a path is the bin or in it; never, while the bin is not known.</summary>
    public static bool Holds(string? bin, string path) =>
        bin is not null && (path == bin || path.StartsWith(bin + KeePassCliTexts.PathSeparator, StringComparison.Ordinal));

    /// <summary>
    /// What a delete would do. With the bin off, or not said to be on: not known, whatever was seen before (M7:
    /// a bin seen once counted after KeePassXC's own window had turned it off, and the entry went for good under
    /// "Moved to the Recycle Bin"). With the bin known: for good inside it, to the bin outside it. With the bin
    /// not known: an entry at the top goes to it (the top is never inside the bin); anything else is not known,
    /// since it may lie in the bin already or, for a top group, may be the bin itself.
    /// </summary>
    public static DeleteFate Fate(bool on, string? bin, PassNode node) =>
        !on ? DeleteFate.NotKnown
        : bin is not null ? (Holds(bin, node.Path) ? DeleteFate.ForGood : DeleteFate.Recycled)
        : !node.IsGroup && node.Group.Length == 0 ? DeleteFate.Recycled
        : DeleteFate.NotKnown;

    /// <summary>
    /// The group a removal shows to be the Recycle Bin: the one group that gained what was removed, and only when
    /// exactly one did (the review's third pass, M7: until then the first group that gained a name was taken, and
    /// a group of the same name gaining an entry elsewhere at the same time, KeePassXC's own window or a sync, made
    /// the wrong group the bin). Null when none gained (a delete for good) or more than one did.
    /// </summary>
    public static string? SeenBin(PassTree before, PassTree after, IEnumerable<PassNode> removed)
    {
        var gained = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in removed)
        {
            foreach (var group in after.Nodes.Where(one => one.IsGroup && one.Path != node.Group))
            {
                var path = PassTree.Join(group.Path, node.Name);
                if (after.Count(path, node.IsGroup) > before.Count(path, node.IsGroup))
                {
                    gained.Add(group.Path);
                }
            }
        }

        return gained.Count == 1 ? gained.First() : null;
    }

    /// <summary>
    /// Where a removed entry or group lies now: the group that has one more of its name than before the removal
    /// (the nearest to the top, for a group moved with a group of its own name inside); null when no group gained
    /// one, which is a delete for good.
    /// </summary>
    public static string? MovedTo(PassTree before, PassTree after, PassNode node)
    {
        foreach (var group in after.Nodes.Where(one => one.IsGroup && one.Path != node.Group))
        {
            var path = PassTree.Join(group.Path, node.Name);
            if (after.Count(path, node.IsGroup) > before.Count(path, node.IsGroup))
            {
                return group.Path;
            }
        }

        return null;
    }

    /// <summary>What became of each node the tool removed, from the listing before and the one after.</summary>
    public static IReadOnlyList<Removed> Outcome(PassTree before, PassTree after, IEnumerable<PassNode> removed) =>
        [.. removed.Select(node => MovedTo(before, after, node) is { } to
            ? new Removed(node, to, after.Count(PassTree.Join(to, node.Name), node.IsGroup) == 1)
            : new Removed(node, null, false))];

    /// <summary>
    /// The question before a delete whose end is not known (<see cref="DeleteFate.NotKnown"/>): its title and its
    /// text, for one entry, one group with what it holds (<paramref name="inside"/>), or several entries, each named.
    /// </summary>
    public static (string Title, string Text) Question(bool on, IReadOnlyList<PassNode> nodes, string inside = "")
    {
        var one = nodes.Count == 1;
        var what = !one ? $"{nodes.Count} entries"
            : nodes[0].IsGroup ? $"the group {nodes[0].Name}"
            : nodes[0].Name;
        var it = !one ? "one of them"
            : nodes[0].IsGroup ? "the group"
            : nodes[0].Name;
        var text = on
            ? $"This database has a Recycle Bin, but KeePassXC's tool does not say which group it is. If {it} is in it already, it is deleted for good, and nothing brings it back."
            : $"KeePassXC's tool does not say that this database has a Recycle Bin. {(one ? "It" : "They")} may be deleted for good, and nothing brings {(one ? "it" : "them")} back then.";
        if (one && nodes[0].IsGroup && inside.Length > 0)
        {
            text += $" The group holds {inside}, which go with it.";
        }

        return ($"Delete {what}?", one ? text : text + "\n" + string.Join("\n", nodes.Select(node => node.Name)));
    }

    /// <summary>"the Recycle Bin", and the group's name with it when it is not the tool's own name for it.</summary>
    private static string Place(string to) =>
        to == KeePassCliTexts.RecycleBin ? "the Recycle Bin" : $"the Recycle Bin, the group {to}";

    /// <summary>
    /// The one sentence after a delete: what the listing shows became of what was removed. <paramref name="asked"/>
    /// is how many were to be removed (more than <paramref name="done"/> when the tool refused one, which stops the
    /// rest); <paramref name="inBin"/> says the delete was of something in the Recycle Bin, so that for good is
    /// what was asked for; <paramref name="undo"/> is the undo key's name.
    /// </summary>
    public static string Said(IReadOnlyList<Removed> done, int asked, bool inBin, string undo)
    {
        if (asked == 1 && done.Count == 1)
        {
            var (node, to, sole) = done[0];
            return to is null ? (inBin ? $"Deleted {node.Name}" : $"Deleted {node.Name} for good: it went to no Recycle Bin")
                : node.IsGroup ? $"Moved {node.Name} to {Place(to)}"
                : sole ? $"Moved {node.Name} to {Place(to)}; {undo} brings it back"
                : $"Moved {node.Name} to {Place(to)}. {undo} cannot bring it back: another entry there is called {node.Name}, and only KeePassXC's own window tells them apart";
        }

        var moved = done.Where(one => one.To is not null).ToList();
        var gone = done.Count - moved.Count;
        var back = moved.Count(one => one.Sole);
        var parts = new List<string>();
        if (moved.Count > 0)
        {
            var count = done.Count == asked ? (moved.Count == 1 ? "1 entry" : $"{moved.Count} entries") : $"{moved.Count} of {asked} entries";
            parts.Add($"Moved {count} to {Place(moved[0].To!)}");
        }

        if (gone > 0)
        {
            parts.Add((parts.Count == 0 ? "Deleted " : "deleted ") + (moved.Count == 0 && done.Count < asked ? $"{gone} of {asked} entries" : gone == 1 ? "1 entry" : $"{gone} entries") + " for good");
        }

        var text = string.Join(", ", parts);
        return back == 0 ? (moved.Count == 0 ? text : $"{text}. {undo} cannot bring them back: each shares its name with another entry there")
            : back == moved.Count ? (gone == 0 ? $"{text}; {undo} brings them back" : $"{text}; {undo} brings the moved ones back")
            : $"{text}; {undo} brings {back} of the moved ones back, the others share a name with another entry there";
    }

    /// <summary>What Ctrl+Z says it takes back, and "Undone:" names: the entries moved to the bin that can come back.</summary>
    public static string StepSaid(IReadOnlyList<Removed> back) =>
        back.Count == 1 ? $"Moved {back[0].Node.Name} to the Recycle Bin" : $"Moved {back.Count} entries to the Recycle Bin";

    /// <summary>Why Ctrl+Z does not move an entry back from the bin: it is not the one entry of its name there.</summary>
    public static string NotSole(string name) =>
        $"The Recycle Bin holds more than one entry called {name}, or none, and KeePassXC's tool reaches an entry by its name alone: KeePassXC's own window moves it back";
}

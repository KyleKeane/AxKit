namespace AxPass.Core;

/// <summary>A group or an entry of the database: its full path ("Email/Gmail"), the group it is in ("Email"; empty at the top), its name.</summary>
public sealed record PassNode(string Path, string Group, string Name, bool IsGroup);

/// <summary>
/// The database's groups and entries as <c>ls -R -f</c> lists them (docs/axPass/kdbx-notes.md section 8): one line per
/// group and entry with its full path, a group ending in a slash, an empty group's line <c>group/[empty]</c>. The
/// levels of the navigator come from it (docs/axPass/SPEC.md section 1).
/// </summary>
public sealed class PassTree
{
    public const string RecycleBin = KeePassCliTexts.RecycleBin;

    private const string EmptyMark = KeePassCliTexts.EmptyMark;

    private readonly List<PassNode> _nodes;

    /// <summary>How many lines the listing had for each group and entry: two entries of one name in one group are one node and two lines.</summary>
    private readonly Dictionary<string, int> _lines;

    private PassTree(List<PassNode> nodes, Dictionary<string, int> lines)
    {
        _nodes = nodes;
        _lines = lines;
    }

    public IReadOnlyList<PassNode> Nodes => _nodes;

    public static PassTree Parse(string listing)
    {
        var nodes = new List<PassNode>();
        var lines = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var raw in listing.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0)
            {
                continue;
            }

            var isGroup = line.EndsWith('/');
            var path = isGroup ? line[..^1] : line;
            if (!isGroup && (path == EmptyMark || path.EndsWith("/" + EmptyMark, StringComparison.Ordinal)))
            {
                continue;
            }

            var key = Key(path, isGroup);
            lines[key] = lines.GetValueOrDefault(key) + 1;
            if (lines[key] > 1)
            {
                continue;
            }

            nodes.Add(NodeOf(path, isGroup));
        }

        return new PassTree(nodes, lines);
    }

    /// <summary>The group or entry a full path names: "Email/Gmail" is Gmail in the group Email.</summary>
    public static PassNode NodeOf(string path, bool isGroup)
    {
        var slash = path.LastIndexOf('/');
        return new PassNode(path, slash < 0 ? string.Empty : path[..slash], slash < 0 ? path : path[(slash + 1)..], isGroup);
    }

    private static string Key(string path, bool isGroup) => (isGroup ? "g:" : "e:") + path;

    /// <summary>
    /// A group's level: its groups, then its entries, each in the database's order; at the top the group
    /// <paramref name="bin"/>, the Recycle Bin when axPass knows it (<see cref="PassBin"/>), comes last of the groups.
    /// </summary>
    public IReadOnlyList<PassNode> Level(string group, string? bin = null)
    {
        var inside = _nodes.Where(node => node.Group == group).ToList();
        var groups = inside.Where(node => node.IsGroup).OrderBy(node => node.Path == bin ? 1 : 0).ToList();
        return [.. groups, .. inside.Where(node => !node.IsGroup)];
    }

    /// <summary>The groups and entries directly in a group.</summary>
    public int CountIn(string group) => _nodes.Count(node => node.Group == group);

    public bool HasGroup(string group) => group.Length == 0 || _nodes.Any(node => node.IsGroup && node.Path == group);

    public bool HasEntry(string path) => _nodes.Any(node => !node.IsGroup && node.Path == path);

    /// <summary>
    /// How many entries, or groups, the database has under one path: more than one where two of one name lie in one
    /// group, which the tool lists as two lines of the same path and reaches only one of by that path.
    /// </summary>
    public int Count(string path, bool isGroup) => _lines.GetValueOrDefault(Key(path, isGroup));

    /// <summary>"Email/Gmail" from a group and a name; the name alone at the top.</summary>
    public static string Join(string group, string name) => group.Length == 0 ? name : group + "/" + name;

    /// <summary>Why a title or a group's name is refused (the tool's paths have no escape for a slash, kdbx-notes 8); null when it is fine.</summary>
    public static string? RefusedName(string name) =>
        name.Trim().Length == 0 ? "Type a name"
        : name.Contains('/') ? "A name cannot hold a slash: KeePassXC's tool reads it as a group"
        : null;

    /// <summary>"mine": the database's name for the window, its file name without .kdbx.</summary>
    public static string DatabaseName(string path) => System.IO.Path.GetFileNameWithoutExtension(path);
}

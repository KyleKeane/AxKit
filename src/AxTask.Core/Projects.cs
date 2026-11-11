using AxKit.Core;

namespace AxTask.Core;

/// <summary>A folder of lists found under a project folder (SPEC.md TK-7b.2): where it is, the name it is shown by, and how many lists it holds.</summary>
public sealed record TaskProject(string Path, string Name, int Lists);

/// <summary>An item a search found (TK-7b.4): its file, the folder of lists it is in (the top folder or a project), and the words of its row.</summary>
public sealed record SearchHit(string Path, string Top, string Place, string List, string Title)
{
    /// <summary>"Fix the tap, in alpha / Home".</summary>
    public string Label => $"{Title}, in {Place} / {List}";
}

/// <summary>
/// The user's other folders of lists, gathered (SPEC.md TK-7b, asked 2026-11-10): the folders of lists found under the
/// project folders the user names, and a search for words across them and the top folder. Plain reads of the disk,
/// called off the UI thread by the window; nothing here writes.
/// </summary>
public static class Projects
{
    /// <summary>How deep under a project folder a folder of lists is looked for (TK-D6): the project folder itself is depth 0.</summary>
    public const int Depth = 4;

    /// <summary>Folders never looked into (TK-D6): build output and package folders hold no lists and many files.</summary>
    private static readonly string[] Skipped = [".git", "node_modules", "bin", "obj"];

    /// <summary>
    /// Every folder of lists under the project folders (TK-7b.2, TK-D6): a folder that holds a list, a folder with a
    /// <c>list.json</c> at its top; looked for to <see cref="Depth"/> folders down, hidden folders and
    /// <see cref="Skipped"/> left out, never inside a folder of lists already found; the top folder
    /// <paramref name="top"/> and anything inside it left out, since it is shown as the top (TK-7b.5). By name, a name two
    /// share told apart by the folder above ("tasks in alpha").
    /// </summary>
    public static IReadOnlyList<TaskProject> Find(IEnumerable<string> projectFolders, string top)
    {
        var found = new List<string>();
        foreach (var root in projectFolders)
        {
            Look(root, 0, top, found);
        }

        var distinct = found.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var names = distinct.GroupBy(PathName.Of, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var projects = distinct.Select(path => new TaskProject(path, NameOf(path, names[PathName.Of(path)] > 1), ItemFolder.ReadTop(path).Lists.Count)).ToList();
        projects.Sort((a, b) => NaturalOrder.Instance.Compare(a.Name, b.Name));
        return projects;
    }

    private static string NameOf(string path, bool shared) =>
        shared && Path.GetDirectoryName(path) is { } parent ? $"{PathName.Of(path)} in {PathName.Of(parent)}" : PathName.Of(path);

    private static void Look(string folder, int depth, string top, List<string> found)
    {
        if (Inside(folder, top))
        {
            return;
        }

        IReadOnlyList<DirectoryInfo> children;
        try
        {
            children = new DirectoryInfo(folder).EnumerateDirectories().Where(child => !Passed(child)).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return;
        }

        if (children.Any(child => File.Exists(Path.Combine(child.FullName, ItemDocument.ListFile))))
        {
            found.Add(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)));
            return;
        }

        if (depth >= Depth)
        {
            return;
        }

        foreach (var child in children)
        {
            Look(child.FullName, depth + 1, top, found);
        }
    }

    private static bool Passed(DirectoryInfo folder) =>
        folder.Name.StartsWith('.') || (folder.Attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) != 0
        || Skipped.Contains(folder.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether a folder is <paramref name="top"/> or inside it.</summary>
    public static bool Inside(string folder, string top)
    {
        var a = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        var b = Path.TrimEndingDirectorySeparator(Path.GetFullPath(top));
        return a.Equals(b, StringComparison.OrdinalIgnoreCase) || a.StartsWith(b + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The words of a search, split at spaces; none for blank text.</summary>
    public static IReadOnlyList<string> Words(string typed) => typed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Whether every word is in the item's Title or one of its text fields (text and long text), case not minded
    /// (TK-7b.4); false for no words.
    /// </summary>
    public static bool Matches(ItemDocument item, IReadOnlyList<string> words)
    {
        if (words.Count == 0)
        {
            return false;
        }

        var text = string.Join("\n", item.Fields.Where(field => field.Type is FieldTypes.Text or FieldTypes.LongText).Select(field => FieldTypes.TextOf(field.Value)).Prepend(item.Title));
        return words.All(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The items of every list of each folder of lists (the Archive left out, as All tasks leaves it, TK-5.1) that hold
    /// the words, each with its place's name and its list's (TK-7b.4); by place in the order given, then by list and title.
    /// </summary>
    public static IReadOnlyList<SearchHit> Search(IEnumerable<(string Top, string Name)> places, string typed)
    {
        var words = Words(typed);
        var hits = new List<SearchHit>();
        foreach (var (top, name) in places)
        {
            foreach (var item in ItemFolder.AllItems(top))
            {
                if (Matches(item.Document, words))
                {
                    hits.Add(new SearchHit(item.Path, top, name, ItemFolder.ListName(top, Path.GetDirectoryName(item.Path)!), ItemFolder.NameOf(item)));
                }
            }
        }

        return hits;
    }
}

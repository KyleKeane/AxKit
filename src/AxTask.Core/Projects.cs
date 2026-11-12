using AxKit.Core;

namespace AxTask.Core;

/// <summary>A folder of lists found under a project folder (SPEC.md TK-7b.2): where it is, the name it is shown by, and how many lists it holds.</summary>
public sealed record TaskProject(string Path, string Name, int Lists)
{
    /// <summary>The project folder it was found under, as the settings hold it (TK-7b.3: the section it is shown in); empty when not known.</summary>
    public string Root { get; init; } = string.Empty;
}

/// <summary>
/// An item read from a folder of lists (TK-5.6): the item, the folder of lists it lives in (the top folder or a
/// project's), and that place's name for its row, empty for the folder shown as the top.
/// </summary>
public sealed record GatheredItem(ItemFile Item, string Top, string Place)
{
    /// <summary>"Home" for the folder shown as the top, "alpha / Home" for a project's (TK-5.6), as the search says it.</summary>
    public string Where => Place.Length == 0 ? ItemFolder.ListName(Top, System.IO.Path.GetDirectoryName(Item.Path)!) : $"{Place} / {ItemFolder.ListName(Top, System.IO.Path.GetDirectoryName(Item.Path)!)}";
}

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
    /// <summary>How deep under a project folder a folder of lists is looked for (TK-7b.2, TK-D7): the project folder itself is depth 0.</summary>
    public const int Depth = 4;

    /// <summary>Folders never looked into (TK-7b.2, TK-D7): build output and package folders hold no lists and many files.</summary>
    private static readonly string[] Skipped = [".git", "node_modules", "bin", "obj"];

    /// <summary>
    /// Every folder of lists under the project folders (TK-7b.2, TK-D7): a folder that holds a list, a folder with a
    /// <c>list.json</c> at its top, or an Archive that holds one when every list was archived; looked for to
    /// <see cref="Depth"/> folders down, hidden folders and <see cref="Skipped"/> left out, never inside a folder of lists
    /// already found; the top folder <paramref name="top"/> and anything inside it left out, since it is shown as the top
    /// (TK-7b.5). By name, a name two share told apart by the folder above ("tasks in alpha"), and by that folder's path
    /// when two still share it ("tasks in alpha (D:\Work\alpha)").
    /// </summary>
    public static IReadOnlyList<TaskProject> Find(IEnumerable<string> projectFolders, string top)
    {
        var found = new List<string>();
        var rootOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in projectFolders)
        {
            var before = found.Count;
            Look(root, 0, top, found);
            foreach (var path in found.Skip(before))
            {
                // A folder under two project folders belongs to the first that found it.
                rootOf.TryAdd(path, root);
            }
        }

        var distinct = found.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var first = distinct.ToDictionary(path => path, path => NameOf(path, Shared(distinct, PathName.Of, path)), StringComparer.OrdinalIgnoreCase);
        var projects = distinct.Select(path => new TaskProject(path, Shared(distinct, other => first[other], path) && Path.GetDirectoryName(path) is { } parent ? $"{first[path]} ({parent})" : first[path], ItemFolder.ReadTop(path).Lists.Count) { Root = rootOf[path] }).ToList();
        projects.Sort((a, b) => NaturalOrder.Instance.Compare(a.Name, b.Name));
        return projects;
    }

    /// <summary>Whether another path of <paramref name="paths"/> has the same name as <paramref name="path"/>.</summary>
    private static bool Shared(List<string> paths, Func<string, string> name, string path) =>
        paths.Count(other => name(other).Equals(name(path), StringComparison.OrdinalIgnoreCase)) > 1;

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

        // A folder whose lists were all archived holds lists in its Archive alone: it is the folder of lists, never the Archive.
        if (children.Any(child => HoldsList(child.FullName))
            || children.Any(child => child.Name.Equals(ItemFolder.ArchiveName, StringComparison.OrdinalIgnoreCase) && HoldsAList(child)))
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

    private static bool HoldsList(string folder) => File.Exists(Path.Combine(folder, ItemDocument.ListFile));

    private static bool HoldsAList(DirectoryInfo archive)
    {
        try
        {
            return archive.EnumerateDirectories().Any(child => !Passed(child) && HoldsList(child.FullName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
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
    public static IReadOnlyList<SearchHit> Search(IEnumerable<(string Top, string Name)> places, string typed) =>
        Search(Gather(places), typed);

    /// <summary>The same search over items already read (TK-7b.4, TK-5.6), each place named by its <see cref="GatheredItem.Place"/>.</summary>
    public static IReadOnlyList<SearchHit> Search(IEnumerable<GatheredItem> items, string typed)
    {
        var words = Words(typed);
        return items.Where(gathered => Matches(gathered.Item.Document, words))
            .Select(gathered => new SearchHit(gathered.Item.Path, gathered.Top, gathered.Place, ItemFolder.ListName(gathered.Top, Path.GetDirectoryName(gathered.Item.Path)!), ItemFolder.NameOf(gathered.Item)))
            .ToList();
    }

    /// <summary>
    /// Every item of every list of each folder of lists, the Archive left out (TK-5.6, TK-7b.4), with its place's name;
    /// by place in the order given. A plain read of the disk, called off the UI thread for the projects.
    /// </summary>
    public static IReadOnlyList<GatheredItem> Gather(IEnumerable<(string Top, string Name)> places) =>
        places.SelectMany(place => ItemFolder.AllItems(place.Top).Select(item => new GatheredItem(item, place.Top, place.Name))).ToList();

    /// <summary>The items of every project found (TK-5.6), each named by its project.</summary>
    public static IReadOnlyList<GatheredItem> Gather(IEnumerable<TaskProject> projects) =>
        Gather(projects.Select(project => (project.Path, project.Name)));

    /// <summary>
    /// The item at <paramref name="path"/> as one of a project's (TK-5.6): read again when it is an item of a list of one
    /// of the projects, the Archive left out; null otherwise or when it is gone.
    /// </summary>
    public static GatheredItem? GatherOne(string path, IEnumerable<TaskProject> projects)
    {
        var list = Path.GetDirectoryName(path);
        var top = list is null ? null : Path.GetDirectoryName(list);
        if (list is null || top is null || !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(path).Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(list).Equals(ItemFolder.ArchiveName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var project = projects.FirstOrDefault(project => Path.TrimEndingDirectorySeparator(project.Path).Equals(Path.TrimEndingDirectorySeparator(top), StringComparison.OrdinalIgnoreCase));
        return project is not null && ItemFolder.ReadItem(path) is { } item ? new GatheredItem(item, project.Path, project.Name) : null;
    }
}

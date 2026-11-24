using AxKit.Core;
using AxKit.Core.Conversion;

namespace AxTask.Core;

/// <summary>
/// An item read from its file (docs/axTask/forms-framework.md section 13): where it is, its values as the file holds
/// them (<see cref="Flat"/>), the configuration it is read through, and the text it was read from (for the check
/// against the file before a change, IA-5.5). <see cref="Document"/> is the item as the window shows it: its Title,
/// then the configuration's fields with the item's values (hide, keep, restore, AF-11.3); <see cref="Kept"/> are the
/// values the configuration does not name. An item whose configuration is not there or does not read has none
/// (<see cref="ConfigProblem"/> says why): every value is shown as plain text and nothing of it is changed (AF-13.4).
/// </summary>
public sealed class ItemFile
{
    private readonly ShownItem _shown;

    public ItemFile(string path, FlatItem flat, ListConfig? config, string text, string? configProblem = null)
    {
        Path = path;
        Flat = flat;
        Config = config;
        Text = text;
        ConfigProblem = config is null ? configProblem ?? "Its configuration is not there" : null;
        _shown = config is null ? ItemView.Plain(flat) : ItemView.Of(flat, config);
        Document = ItemView.Document(_shown);
    }

    public string Path { get; }

    public FlatItem Flat { get; }

    public ListConfig? Config { get; }

    public string Text { get; }

    /// <summary>Why the item is shown as plain text and cannot be changed, "Its configuration, list.json, is not there"; null for an item read through its configuration.</summary>
    public string? ConfigProblem { get; }

    public ItemDocument Document { get; }

    public IReadOnlyList<KeptValue> Kept => _shown.Kept;

    /// <summary>"2 more fields kept: Sprint, Old priority" (AF-11.4); null when the item keeps none.</summary>
    public string? KeptLine => _shown.KeptLine;

    /// <summary>
    /// The file's text after a change made to <see cref="Document"/>: only what changed is written, so a value the
    /// configuration does not name stays as it is, and a field shown empty stays out of the file until it is given a
    /// value (<see cref="ItemView.Changed"/>).
    /// </summary>
    public string TextWith(ItemDocument changed) => ItemView.Changed(Flat, Document, changed, Config).ToText();
}

/// <summary>
/// A list's configuration as read from its <c>list.json</c>: the configuration, or none with the reason
/// (<see cref="Problem"/>: "is not there", "does not read: it is not JSON at line 12"), and the file's text for the
/// check before a change (null when there is no file).
/// </summary>
public sealed record ListRead(ListConfig? Config, string? Text, string? Problem);

/// <summary>
/// A list's items (IA-2.2), how many files and folders in it are not shown (IA-4.6; a folder inside a list, TK-D4), and
/// the JSON files in it that do not read as items (TK-4.6; repairs.md R-66), each of which the window shows as a row.
/// <see cref="Earlier"/>: the list is written in axTask's earlier format and is not read until it is converted
/// (AF-13.6); it has no items then.
/// </summary>
public sealed record FolderLevel(IReadOnlyList<ItemFile> Items, int NotShown, IReadOnlyList<string> Unreadable)
{
    public bool Earlier { get; init; }

    /// <summary>The list's own configuration as it was read with the items; null for a folder that is not there.</summary>
    public ListRead? List { get; init; }

    /// <summary>"The list's configuration does not read: it is not JSON at line 12" (AF-11.7); null for a list whose configuration reads, or that has none.</summary>
    public string? ConfigProblem => List is { Config: null, Text: not null, Problem: { } problem } ? "The list's configuration " + problem : null;
}

/// <summary>The top folder (TK-3a): its lists by name, the Archive's lists, how many items the Archive holds of its own, and what is not shown (items kept in the top folder itself, files that are not filters).</summary>
public sealed record TopLevel(IReadOnlyList<string> Lists, IReadOnlyList<string> ArchivedLists, int ArchivedItems, int NotShown);
/// <summary>
/// The folder of lists on disk (docs/axKit/item-apps.md IA-4): a list is a folder, its details <c>list.json</c> in it,
/// an item a JSON file named after its Title. The archive is the top folder's <c>Archive</c> list (IA-D4), and the
/// smart lists are the top folder's <c>.filter.json</c> files (TK-5.2).
/// </summary>
public static class ItemFolder
{
    public const string ArchiveName = "Archive";
    public const string FilterExtension = ".filter.json";

    /// <summary>
    /// The top folder of lists when the user set none: <c>Tasks</c> in axKit's folder in Documents (bundle AX-5.11,
    /// the maintainer's decision of 2026-11-23), or the <c>Documents\Tasks</c> of before for someone whose lists
    /// are there (<see cref="AppPaths.TasksFolder"/>).
    /// </summary>
    public static string DefaultFolder => AppPaths.TasksFolder;

    /// <summary>A list's items by title (TK-D4: a folder inside it is not shown, and counted), hidden and dot entries left out.</summary>
    public static FolderLevel Read(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return new FolderLevel([], 0, []);
        }

        var notShown = new DirectoryInfo(folder).EnumerateDirectories().Count(directory => !Hidden(directory));
        // AF-13.6: a list of the earlier format is not read; the window offers its conversion. Its own file says so,
        // or one of its items does (below): the files are read once either way.
        var own = ReadConfig(folder);
        if (own.Text is not null && EarlierFormat.IsEarlierList(own.Text))
        {
            return new FolderLevel([], notShown, []) { Earlier = true };
        }

        var items = new List<ItemFile>();
        var unreadable = new List<string>();
        var configs = new Dictionary<string, ListRead>(StringComparer.OrdinalIgnoreCase) { [Path.Combine(folder, ItemDocument.ListFile)] = own };
        foreach (var file in new DirectoryInfo(folder).EnumerateFiles())
        {
            if (Hidden(file) || file.Name.Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ReadItem(file.FullName, configs) is { } item)
            {
                items.Add(item);
            }
            else
            {
                // A JSON file that does not read as an item is a damaged item, shown as a row; any other file is counted.
                if (file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsEarlierItem(file.FullName))
                    {
                        return new FolderLevel([], notShown, []) { Earlier = true };
                    }

                    unreadable.Add(file.FullName);
                }
                else
                {
                    notShown++;
                }
            }
        }

        items.Sort((a, b) => NaturalOrder.Instance.Compare(NameOf(a), NameOf(b)));
        unreadable.Sort(NaturalOrder.Instance.Compare);
        // TK-4.9: the list's own order, when its configuration has one; the items it does not name stay by name, after
        // it, or before it when new items go to the top (TK-4.10).
        items = ListOrder.Arrange(items, item => Path.GetFileNameWithoutExtension(item.Path), ListOrder.Of(own.Config), ListOrder.NewAtTop(own.Config));
        return new FolderLevel(items, notShown, unreadable) { List = own };
    }

    /// <summary>Whether a list is written in axTask's earlier format (AF-13.6): its row says so, and opening it offers the conversion.</summary>
    public static bool IsEarlier(string folder) => ListConversion.IsEarlier(folder);

    private static bool IsEarlierItem(string path)
    {
        try
        {
            return EarlierFormat.IsEarlierItem(TextDocument.Read(path).Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>The top folder's lists and the Archive's (TK-3a.1), each by name.</summary>
    public static TopLevel ReadTop(string top)
    {
        if (!Directory.Exists(top))
        {
            return new TopLevel([], [], 0, 0);
        }

        var lists = new List<string>();
        foreach (var directory in new DirectoryInfo(top).EnumerateDirectories())
        {
            if (!Hidden(directory) && !directory.Name.Equals(ArchiveName, StringComparison.OrdinalIgnoreCase))
            {
                lists.Add(directory.FullName);
            }
        }

        var notShown = new DirectoryInfo(top).EnumerateFiles()
            .Count(file => !Hidden(file) && !file.Name.EndsWith(FilterExtension, StringComparison.OrdinalIgnoreCase) && !file.Name.Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase));
        var archive = ArchiveFolder(top);
        var archived = new List<string>();
        var archivedItems = 0;
        if (Directory.Exists(archive))
        {
            archived.AddRange(new DirectoryInfo(archive).EnumerateDirectories().Where(directory => !Hidden(directory)).Select(directory => directory.FullName));
            archivedItems = Read(archive).Items.Count;
        }

        lists.Sort((a, b) => NaturalOrder.Instance.Compare(Path.GetFileName(a), Path.GetFileName(b)));
        archived.Sort((a, b) => NaturalOrder.Instance.Compare(Path.GetFileName(a), Path.GetFileName(b)));
        return new TopLevel(lists, archived, archivedItems, notShown);
    }

    /// <summary>A list's section (TK-3a.3): its details' Section field, trimmed; empty for User lists.</summary>
    public static string SectionOf(string list) => ReadList(list)?.Detail(SectionField).Trim() ?? string.Empty;

    /// <summary>The detail of a list that names its section (TK-3a.3).</summary>
    public const string SectionField = "Section";

    /// <summary>The detail of a list that holds its notes, a text of several lines.</summary>
    public const string NotesField = "Notes";

    /// <summary>
    /// The item in a file, read through the configuration its <c>"schema"</c> names (AF-13.2); null when the file is
    /// not a JSON item or cannot be read (IA-4.6). <paramref name="configs"/> keeps the configurations read, so that
    /// a list's file is read once for all its items.
    /// </summary>
    public static ItemFile? ReadItem(string path, Dictionary<string, ListRead>? configs = null)
    {
        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var text = TextDocument.Read(path).Text;
            var flat = FlatItem.Parse(text);
            // The link is a path from the item's own folder, and never leads off this computer (S-5's rule).
            if (Resolve(path, flat.Schema.Trim()) is not { } configPath)
            {
                return new ItemFile(path, flat, null, text, $"Its configuration, {flat.Schema.Trim()}, is not a file beside it");
            }

            ListRead? read = null;
            if (configs is null || !configs.TryGetValue(configPath, out read))
            {
                read = ReadConfigFile(configPath);
                configs?.TryAdd(configPath, read);
            }

            return new ItemFile(path, flat, read.Config, text, $"Its configuration, {flat.Schema.Trim()}, {read.Problem}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>A list's configuration from its <c>list.json</c>; null when there is none or it does not read (AF-11.7).</summary>
    public static ListConfig? ReadList(string folder) => ReadConfig(folder).Config;

    /// <summary>A list's configuration as read, with the file's text and, when there is none to read through, why.</summary>
    public static ListRead ReadConfig(string folder) => ReadConfigFile(Path.Combine(folder, ItemDocument.ListFile));

    private static ListRead ReadConfigFile(string path)
    {
        string text;
        try
        {
            if (!File.Exists(path))
            {
                return new ListRead(null, null, "is not there");
            }

            text = TextDocument.Read(path).Text;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new ListRead(null, null, "could not be read: " + ex.Message);
        }

        try
        {
            return new ListRead(ListConfig.Parse(text), text, null);
        }
        catch (InvalidDataException ex)
        {
            return new ListRead(null, text, "does not read: " + ex.Message);
        }
    }

    /// <summary>
    /// A new item of a list as its file (AF-13.1): the link to the list's configuration, the Title, the configuration's
    /// fields with the document's values in the configuration's order, then any value of the document the
    /// configuration does not name.
    /// </summary>
    public static FlatItem NewItemFile(ItemDocument item, ListConfig config)
    {
        var flat = FlatItem.New(item.Title, config);
        foreach (var field in item.Fields.Where(field => !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase)))
        {
            flat = flat.With(field.Title, field.Value, config);
        }

        return flat;
    }

    /// <summary>
    /// The configuration a list without one gets (AF-11.1): the fields its items hold, each once, in the order they
    /// are met, typed by JSON's own kind of the first value; the fields of a new list when it has no items yet.
    /// </summary>
    public static ListConfig StarterConfig(string folder)
    {
        var fields = new List<ItemField>();
        if (Directory.Exists(folder))
        {
            foreach (var file in new DirectoryInfo(folder).EnumerateFiles("*.json").Where(file => !Hidden(file)).OrderBy(file => file.Name, NaturalOrder.Instance))
            {
                try
                {
                    foreach (var (name, value) in FlatItem.Parse(TextDocument.Read(file.FullName).Text).Values)
                    {
                        if (!name.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase) && !fields.Any(field => field.Title.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            var type = FieldTypes.OfKind(value);
                            fields.Add(new ItemField(name, type, FieldTypes.Empty(type), []));
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
                {
                    // Not an item: nothing of it is a field.
                }
            }
        }

        return ListConfig.New(fields.Count == 0 ? TaskRules.DefaultTemplate : fields);
    }

    /// <summary>The item's name: its Title, or its file name when the Title is empty.</summary>
    public static string NameOf(ItemFile item) =>
        item.Document.Title.Trim() is { Length: > 0 } title ? title : Path.GetFileNameWithoutExtension(item.Path);

    // FileStem and FreePath (IA-4.5) are AxKit.Core's ItemFiles since their second use, the reports list (2026-11-07).

    /// <summary>Why a new list's name is refused; null when it is fine.</summary>
    public static string? RefusedListName(string folder, string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "Type the list's name";
        }

        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || trimmed.EndsWith('.') || trimmed.StartsWith('.'))
        {
            return "A list's name cannot hold \\ / : * ? \" < > | or start or end in a full stop";
        }

        return Directory.Exists(Path.Combine(folder, trimmed)) || File.Exists(Path.Combine(folder, trimmed))
            ? $"There is a list called {trimmed} here already"
            : null;
    }

    /// <summary>What a list says of a JSON file in it that does not read as an item (TK-4.6): the row's words.</summary>
    public static string UnreadableRow(string path) => $"{Path.GetFileName(path)}, not readable as an item, Enter opens it in axJSON";

    /// <summary>
    /// What a list that gathers items (All tasks, a date list, a tag, a filter) says of the files it could not read
    /// (TK-4.6): "2 items not readable, in Home, Work", the lists by name in their order; empty when there are none.
    /// </summary>
    public static string UnreadableSummary(string top, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return string.Empty;
        }

        var lists = paths.Select(path => ListName(top, Path.GetDirectoryName(path)!)).Distinct(StringComparer.OrdinalIgnoreCase);
        return $"{(paths.Count == 1 ? "1 item" : paths.Count + " items")} not readable, in {string.Join(", ", lists)}";
    }

    /// <summary>Every item in a list of the top folder and every JSON file there that does not read as one, the Archive left out, in one reading (TK-5.1, TK-4.6).</summary>
    public static (IReadOnlyList<ItemFile> Items, IReadOnlyList<string> Unreadable) ReadAll(string top)
    {
        var levels = ReadTop(top).Lists.Select(Read).ToList();
        return (levels.SelectMany(level => level.Items).ToList(), levels.SelectMany(level => level.Unreadable).ToList());
    }

    /// <summary>The lists of the top folder that are written in axTask's earlier format (AF-13.6), by path: not gathered until they are converted.</summary>
    public static IReadOnlyList<string> EarlierLists(string top) => ReadTop(top).Lists.Where(IsEarlier).ToList();

    /// <summary>What a gathering list says of the lists it did not read: "2 lists in the earlier format are not read: Home, Work. Open one to convert it"; empty for none.</summary>
    public static string EarlierSummary(IReadOnlyList<string> lists) => lists.Count == 0
        ? string.Empty
        : $"{(lists.Count == 1 ? "1 list in the earlier format is" : lists.Count + " lists in the earlier format are")} not read: {string.Join(", ", lists.Select(Path.GetFileName))}. Open one to convert it";

    /// <summary>Every item in a list of the top folder, the Archive left out (TK-5.1).</summary>
    public static IReadOnlyList<ItemFile> AllItems(string top) =>
        ReadAll(top).Items;

    public static string ArchiveFolder(string top) => Path.Combine(top, ArchiveName);

    /// <summary>Whether a path is the Archive or inside it.</summary>
    public static bool IsArchive(string top, string path)
    {
        var archive = ArchiveFolder(top);
        return path.Equals(archive, StringComparison.OrdinalIgnoreCase) || path.StartsWith(archive + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>"Home", "Work, Course plan": the list a path is in, from the top folder; "the top" for the top itself.</summary>
    public static string ListName(string top, string folder)
    {
        var relative = Path.GetRelativePath(top, folder);
        return relative == "." ? "the top" : relative.Replace(Path.DirectorySeparatorChar.ToString(), ", ");
    }

    /// <summary>A link's path (IA-D3): from the item's own folder to the other item, with forward slashes.</summary>
    public static string LinkTo(string fromItem, string toItem) =>
        Path.GetRelativePath(Path.GetDirectoryName(fromItem)!, toItem).Replace('\\', '/');

    /// <summary>
    /// The file a link names, from the item that holds it; null for a link that is not relative (<c>//server/x.json</c>,
    /// <c>C:\x.json</c>) or that leads off this computer, which is never opened: a link in a shared folder would
    /// otherwise make Windows authenticate to a server of someone else's choosing as the item is merely shown
    /// (security audit S-5).
    /// </summary>
    public static string? Resolve(string fromItem, string link)
    {
        var relative = link.Replace('/', Path.DirectorySeparatorChar);
        if (relative.Length == 0 || Path.IsPathRooted(relative) || relative.Contains(':'))
        {
            return null;
        }

        var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fromItem)!, relative));
        return LocalPaths.IsLocal(target) ? target : null;
    }

    private static bool Hidden(FileSystemInfo entry) =>
        entry.Name.StartsWith('.') || (entry.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
}

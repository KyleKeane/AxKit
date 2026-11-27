using System.Text.Json;
using System.Text.RegularExpressions;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The places of the windows (docs/axKit/workflow-graph.md WG-1.1), beside <see cref="KeyTests"/> as the places tables
/// are beside the key tables: every kind of window of docs/axKit/components.json names its places in its table, and
/// gives them in its class; every app has the places of its kind, and a table of its own exactly where it shows a
/// control of its own that takes the focus (read from the sources); and no control on view that takes the focus is
/// without a place, as the crawl of the windows found them (the graph file's <c>unplaced</c>, written by the inventory
/// run from the windows themselves).
/// </summary>
public sealed class PlacesTests
{
    /// <summary>Each kind of window's table, by the kind's id in docs/axKit/components.json, and the class file that gives its places.</summary>
    private static readonly IReadOnlyDictionary<string, (IReadOnlyList<Place> Places, string File)> Types =
        new Dictionary<string, (IReadOnlyList<Place>, string)>
        {
            ["conversation"] = (ConversationPlaces.All, "src/AxKit.Forms/ConversationForm.cs"),
            ["editor"] = (EditorPlaces.All, "src/AxKit.Forms/TextEditorForm.cs"),
            ["navigator"] = (ItemPlaces.All, "src/AxKit.Forms/ItemForm.cs"),
            ["hotkey"] = (HotkeyPlaces.All, "src/AxKit.Forms/HotkeyForm.cs"),
        };

    /// <summary>The apps' own tables, for a place their kind does not have (WG-1.1); held to the files named Ax&lt;Name&gt;Places.cs below.</summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<Place>> AppTables =
        new Dictionary<string, IReadOnlyList<Place>>
        {
            ["axHelp"] = AxHelp.AxHelpPlaces.All,
        };

    /// <summary>The override every window that gives places has, in these words.</summary>
    private const string GivesPlaces = "override IReadOnlyList<(Place Place, Control Control)> Places";

    private static JsonDocument Components() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "components.json")));

    [Fact]
    public void Every_kind_of_window_names_its_places_and_gives_them()
    {
        using var document = Components();
        var kinds = document.RootElement.GetProperty("window types").EnumerateArray().Select(type => type.GetProperty("id").GetString()!).Order(StringComparer.Ordinal);
        Assert.Equal(kinds, Types.Keys.Order(StringComparer.Ordinal));
        var wrong = new List<string>();
        foreach (var (kind, (places, file)) in Types)
        {
            wrong.AddRange(Problems(kind, places));
            if (!File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), file)).Contains(GivesPlaces, StringComparison.Ordinal))
            {
                wrong.Add($"{kind}: {file} does not give its places ({GivesPlaces})");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    /// <summary>Every app of the bundle is on a kind with places, and an app's own table neither repeats nor lacks what it must.</summary>
    [Fact]
    public void Every_app_has_its_places()
    {
        using var document = Components();
        var types = document.RootElement.GetProperty("apps").EnumerateArray().ToDictionary(app => app.GetProperty("app").GetString()!, app => app.GetProperty("type").GetString()!);
        var wrong = new List<string>();
        foreach (var app in AxKit.Core.Dispatch.Apps)
        {
            if (!types.TryGetValue(app.Name, out var type) || !Types.TryGetValue(type, out var kind))
            {
                wrong.Add($"{app.Name}: on no kind of window with places");
                continue;
            }

            if (AppTables.TryGetValue(app.Name, out var own))
            {
                wrong.AddRange(Problems(app.Name, own));
                wrong.AddRange(own.Where(place => kind.Places.Any(typePlace => typePlace.Id == place.Id)).Select(place => $"{app.Name}: its own place {place.Id} is a place of its kind already"));
            }
        }

        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    /// <summary>
    /// From the sources: an app's window that holds a control of its own that takes the focus (a field or a property
    /// made in its folder, of one of the kinds of control below; a name given to its kind's control, <c>_list =&gt;
    /// Navigator</c>, is none) has a places table, Ax&lt;Name&gt;Places.cs, and gives it;
    /// and every such table is one this test knows. A control a notice or a form holds is made where it is shown, in
    /// a method, and is never a place.
    /// </summary>
    [Fact]
    public void A_window_with_a_control_of_its_own_has_a_places_table()
    {
        var root = SourceTexts.RepositoryRoot();
        var control = new Regex(@"^\s*(?:private|protected|public|internal)[^=;(]*\b(?:AxKitTextBox|AxKitList|TranscriptView|TextBox|RichTextBox|ListBox|CheckedListBox|ComboBox|TreeView|ListView|TrackBar)\??\s+_?\w+\s*(?:=(?!>)|\{)", RegexOptions.Multiline);
        var wrong = new List<string>();
        var tables = new List<string>();
        foreach (var app in AxKit.Core.Dispatch.Apps)
        {
            var folder = Path.Combine(root, "src", "Ax" + app.Name[2..]);
            if (!Directory.Exists(folder))
            {
                // axGIT's folder is AxGit: the folder of the app's project, whatever the case of its name.
                folder = Directory.GetDirectories(Path.Combine(root, "src")).FirstOrDefault(path => string.Equals(Path.GetFileName(path), "Ax" + app.Name[2..], StringComparison.OrdinalIgnoreCase)) ?? folder;
            }

            if (!Directory.Exists(folder))
            {
                continue;
            }

            var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories).Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)).ToList();
            var table = files.FirstOrDefault(file => Path.GetFileName(file).EndsWith("Places.cs", StringComparison.Ordinal));
            if (table is not null)
            {
                tables.Add(app.Name);
            }

            var own = files.Where(file => control.IsMatch(File.ReadAllText(file))).Select(file => Path.GetRelativePath(root, file).Replace('\\', '/')).ToList();
            if (own.Count > 0 && table is null)
            {
                wrong.Add($"{app.Name}: {string.Join(", ", own)} holds a control of its own that takes the focus, and the app has no places table");
            }

            if (table is not null && !files.Any(file => File.ReadAllText(file).Contains(GivesPlaces, StringComparison.Ordinal)))
            {
                wrong.Add($"{app.Name}: its places table is given by no window ({GivesPlaces})");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
        Assert.Equal(AppTables.Keys.Order(StringComparer.Ordinal), tables.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// From the windows themselves: every control on view that takes the focus has a place. The inventory run's crawl
    /// asks each window it crawls (the channel's <c>places</c>) and writes what has none into the graph file; a control
    /// added without its place is named there at the next write, and fails here (WG-1.1).
    /// </summary>
    [Fact]
    public void No_control_the_crawl_found_is_without_a_place()
    {
        var path = Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "workflow-graph.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var unplaced = document.RootElement.GetProperty("unplaced").EnumerateArray().Select(entry => entry.GetString()).ToList();
        Assert.True(unplaced.Count == 0, "Controls that take the focus and are no place (give each its place in its window's places table): " + string.Join("; ", unplaced));
    }

    /// <summary>A table's faults: no place, an id twice or not in plain lower-case letters, no words, no waypoint.</summary>
    private static IEnumerable<string> Problems(string table, IReadOnlyList<Place> places)
    {
        if (places.Count == 0)
        {
            yield return $"{table}: no places";
        }

        foreach (var place in places)
        {
            if (!Regex.IsMatch(place.Id, "^[a-z]+$"))
            {
                yield return $"{table}: the place id {place.Id} is not plain lower-case letters";
            }

            if (place.Words.Trim().Length == 0)
            {
                yield return $"{table}: {place.Id} has no words";
            }
        }

        foreach (var twice in places.GroupBy(place => place.Id).Where(group => group.Count() > 1))
        {
            yield return $"{table}: {twice.Key} twice";
        }

        if (!places.Any(place => place.Waypoint))
        {
            yield return $"{table}: no waypoint";
        }
    }
}

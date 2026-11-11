using AxKit.Core;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The user's other folders of lists, gathered (SPEC.md TK-7b, 2026-11-11): the project folders in Options, the Projects
/// row of the top level and its level, a project entered as a top folder of its own, and the search across every
/// list (Ctrl+Shift+F). Finding the projects and searching read the disk off the UI thread (<see cref="Projects"/>).
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>The Projects row's key and its Tag.</summary>
    private const string ProjectsKey = "projects:";

    private static readonly object ProjectsTag = new();

    /// <summary>A project's row in the Projects level.</summary>
    private sealed record ProjectTag(TaskProject Project);

    /// <summary>A row of the search's level: what was found, and the item as read for this fill.</summary>
    private sealed record FoundItem(SearchHit Hit, ItemFile Item);

    /// <summary>Where a search was started from, to go back to with Left or Escape.</summary>
    private sealed record SearchReturn(string? Project, string? ProjectName, string Folder, SmartList? Smart, bool AtProjects, string? Select);

    /// <summary>The search's level: the words, what they found, and where it was started from.</summary>
    private sealed record SearchLevel(string Words, IReadOnlyList<SearchHit> Hits, SearchReturn Back);

    /// <summary>The project shown as the top (TK-7b.3); null for the user's own top folder.</summary>
    private string? _project;

    private string? _projectName;

    /// <summary>Whether the level is the Projects level (TK-7b.3).</summary>
    private bool _atProjects;

    private SearchLevel? _search;

    /// <summary>The projects found; null until the first look has ended.</summary>
    private IReadOnlyList<TaskProject>? _projects;

    /// <summary>The number of the latest look for projects; an older one's result is dropped.</summary>
    private int _findRun;

    private string _lastWords = string.Empty;

    private readonly List<ToolStripItem> _projectFolderItems = [];

    /// <summary>Whether the level is the search's or the Projects level, where nothing is made or moved.</summary>
    private bool Gathered => _search is not null || _atProjects;

    private bool InSearch() => _search is not null && InNavigator();

    /// <summary>Options → Project folders (TK-7b.1): Add a project folder…, and Remove a project folder ▸ made again as it opens.</summary>
    private ToolStripMenuItem CreateProjectFoldersMenu()
    {
        var menu = new ToolStripMenuItem("&Project folders");
        menu.DropDownItems.Add(new ToolStripMenuItem("&Add a project folder...", null, (_, _) => AddProjectFolder()));
        var remove = new ToolStripMenuItem("&Remove a project folder");
        menu.DropDownItems.Add(remove);
        CommandPalette.RefreshedOnOpening(menu, () => FillProjectFolderItems(remove));
        return menu;
    }

    /// <summary>"1. Projects (D:\Projects)" for each folder, or one item that says there are none (AX-9.5).</summary>
    private void FillProjectFolderItems(ToolStripMenuItem remove)
    {
        foreach (var old in _projectFolderItems)
        {
            old.Owner?.Items.Remove(old);
            old.Dispose();
        }

        _projectFolderItems.Clear();
        var folders = _settings.ProjectFolders;
        if (folders.Count == 0)
        {
            _projectFolderItems.Add(new ToolStripMenuItem("&No project folders yet"));
        }

        for (var i = 0; i < folders.Count; i++)
        {
            var folder = folders[i];
            var label = (i < 9 ? $"&{i + 1}. " : $"{i + 1}. ") + $"{PathName.Of(folder)} ({folder})".Replace("&", "&&", StringComparison.Ordinal);
            _projectFolderItems.Add(new ToolStripMenuItem(label, null, (_, _) => RemoveProjectFolder(folder)));
        }

        remove.DropDownItems.AddRange([.. _projectFolderItems]);
    }

    /// <summary>
    /// Options → Project folders → Add a project folder… (TK-7b.1): a typed path, or, with the field left empty, the
    /// Windows folder picker; "Added Projects, looking for lists".
    /// </summary>
    private void AddProjectFolder()
    {
        AskText("Add a project folder",
            "A folder axTask looks in for your other folders of lists, four folders down. They show under Projects, the first line of the top level. Leave the field empty and press Enter to choose the folder with the Windows folder picker.",
            "Type the folder's path, or leave it empty for the folder picker:", "Type the folder's path, or leave it empty for the folder picker", string.Empty, "&Add",
            typed =>
            {
                var path = typed.Trim();
                if (path.Length == 0)
                {
                    if (ChooseFolder("Choose a folder with your projects") is not { } chosen)
                    {
                        return;
                    }

                    if (_settings.RefusedProjectFolder(chosen) is { } why)
                    {
                        Announce(why, true);
                        return;
                    }

                    path = chosen;
                }

                var added = _settings.AddProjectFolder(path);
                SaveSettings();
                _projects = null;
                FindProjects();
                RefillGathered();
                Announce($"Added {PathName.Of(added)}, looking for lists", true);
            },
            typed => typed.Trim().Length == 0 ? null : _settings.RefusedProjectFolder(typed), required: false);
    }

    /// <summary>Options → Project folders → Remove a project folder ▸ one folder (TK-7b.1): "Removed Projects"; its projects go from the Projects level.</summary>
    private void RemoveProjectFolder(string folder)
    {
        _settings.ProjectFolders.RemoveAll(path => path.Equals(folder, StringComparison.OrdinalIgnoreCase));
        SaveSettings();
        if (_settings.ProjectFolders.Count == 0 && _atProjects)
        {
            _atProjects = false;
        }

        FindProjects();
        RefillGathered();
        Announce("Removed " + PathName.Of(folder), true);
    }

    /// <summary>The level filled again when it shows the Projects row or level; nothing said.</summary>
    private void RefillGathered()
    {
        if (_search is null && _project is null && (AtTop || _atProjects))
        {
            Fill(SelectedKey());
        }
    }

    /// <summary>The projects looked for again, off the UI thread (TK-7b.2); the Projects row and level filled when they come.</summary>
    private void FindProjects()
    {
        var run = ++_findRun;
        if (_settings.ProjectFolders.Count == 0)
        {
            _projects = [];
            return;
        }

        var roots = _settings.ProjectFolders.ToList();
        var home = _settings.TopFolder;
        Do(async () =>
        {
            var found = await Task.Run(() => Projects.Find(roots, home));
            if (run != _findRun || IsDisposed)
            {
                return;
            }

            var waiting = _projects is null && _atProjects;
            _projects = found;
            RefillGathered();
            if (waiting && !NoticeOpen)
            {
                AnnounceLevel();
            }
        });
    }

    /// <summary>"Projects, 5 lists": the first row of the top level while project folders are set (TK-7b.3).</summary>
    private ItemRow ProjectsRow() =>
        new(true, ProjectsKey, "Projects", _projects is null ? "Projects, looking for lists" : $"Projects, {Count(_projects.Sum(project => project.Lists), "list")}", ProjectsTag);

    /// <summary>The Projects level: each project by name with its count of lists.</summary>
    private string ReadProjects(List<ItemRow> rows)
    {
        foreach (var project in _projects ?? [])
        {
            rows.Add(new ItemRow(true, project.Path, project.Name, $"{project.Name}, {Count(project.Lists, "list")}", new ProjectTag(project)));
        }

        return _projects is null ? "Projects, looking for lists"
            : _projects.Count == 0 ? "Projects, none found: Options, Project folders"
            : $"Projects, {Count(_projects.Count, "project")}";
    }

    /// <summary>The search's level (TK-7b.4): each item found, read again, "Fix the tap, in alpha / Home".</summary>
    private string ReadSearch(SearchLevel search, List<ItemRow> rows)
    {
        foreach (var hit in search.Hits)
        {
            if (ItemFolder.ReadItem(hit.Path) is { } item)
            {
                var name = ItemFolder.NameOf(item);
                rows.Add(new ItemRow(false, hit.Path, name, (hit with { Title = name }).Label, new FoundItem(hit, item)));
            }
        }

        return $"Found {search.Words}, {Count(rows.Count, "item")}";
    }

    /// <summary>Ctrl+Shift+F (TK-7b.4): the words asked for, then every list searched, the user's own and every project's.</summary>
    private void FindEverywhere()
    {
        AskText("Find in every list",
            "Words to find in the titles and text fields of the items of every list: your own lists and every project's. An item holding every word is found.",
            "Type the words to find in every list:", "Type the words to find in every list", _lastWords, "&Find",
            typed => RunSearch(typed.Trim()),
            typed => Projects.Words(typed).Count == 0 ? "Type a word to find" : null, emptyMessage: "Type a word to find");
    }

    private void RunSearch(string words)
    {
        _lastWords = words;
        var roots = _settings.ProjectFolders.ToList();
        var home = _settings.TopFolder;
        Do(async () =>
        {
            var (found, hits) = await Task.Run(() =>
            {
                var projects = Projects.Find(roots, home);
                return (projects, Projects.Search([(home, PathName.Of(home)), .. projects.Select(project => (project.Path, project.Name))], words));
            });
            if (IsDisposed)
            {
                return;
            }

            _projects = found;
            if (hits.Count == 0)
            {
                Sounds.Edge();
                Announce("Nothing found for " + words, true);
                return;
            }

            var back = _search?.Back ?? new SearchReturn(_project, _projectName, _folder, _smart, _atProjects, SelectedKey());
            _search = new SearchLevel(words, hits, back);
            _project = null;
            _projectName = null;
            _smart = null;
            _atProjects = false;
            _folder = TopFolder;
            Fill(select: null);
            Navigator.Select();
            AnnounceLevel();
        });
    }

    /// <summary>Right or Enter on what the search found (TK-7b.4): its list opened where it lives, the item selected.</summary>
    private void OpenFound()
    {
        if (SelectedRow() is not { Tag: FoundItem found })
        {
            Sounds.Edge();
            return;
        }

        _search = null;
        _atProjects = false;
        _smart = null;
        var home = _settings.TopFolder;
        var inHome = found.Hit.Top.Equals(home, StringComparison.OrdinalIgnoreCase);
        _project = inHome ? null : found.Hit.Top;
        _projectName = inHome ? null : found.Hit.Place;
        _folder = Path.GetDirectoryName(found.Item.Path)!;
        RememberLevel();
        Fill(found.Item.Path);
        AnnounceLevel();
    }

    /// <summary>Left or Escape out of the search: back where it was started, the row selected then selected again.</summary>
    private string LeaveSearch(SearchLevel search)
    {
        var back = search.Back;
        _search = null;
        _project = back.Project;
        _projectName = back.ProjectName;
        _folder = back.Folder;
        _smart = back.Smart;
        _atProjects = back.AtProjects;
        return back.Select ?? string.Empty;
    }

    /// <summary>The detail pane of the Projects row and of a project.</summary>
    private ItemDetail ProjectDetail(ItemRow row) => row.Tag is ProjectTag { Project: var project }
        ? new ItemDetail("Details of " + project.Name, null, null,
        [
            new ItemDetailLine(row.Label),
            new ItemDetailLine("Its folder: " + project.Path),
            new ItemDetailLine("Right opens it as a top folder of its own: its lists, smart lists and Archive, changed in place"),
        ])
        : new ItemDetail("Details of Projects", null, null,
        [
            new ItemDetailLine(row.Label),
            new ItemDetailLine(_settings.ProjectFolders.Count == 1 ? "Found under 1 project folder: " + _settings.ProjectFolders[0] : $"Found under {_settings.ProjectFolders.Count} project folders: {string.Join(", ", _settings.ProjectFolders)}"),
            new ItemDetailLine("Every folder holding a list, four folders down; Options, Project folders adds and removes them. Ctrl+Shift+F finds words in every list"),
        ]);
}

using AxKit.Core;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The user's other folders of lists, gathered (SPEC.md TK-7b, 2026-11-11): the project folders in Options, a section
/// of the top level for each with the folders of lists found under it, a project entered as a top folder of its own,
/// and the search across every list (Ctrl+Shift+F). Finding the projects and searching read the disk off the UI thread
/// (<see cref="Projects"/>).
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>A project's row, in the section of the project folder it was found under.</summary>
    private sealed record ProjectTag(TaskProject Project);

    /// <summary>The header row of a project folder's section (TK-7b.3): the folder as the settings hold it.</summary>
    private sealed record ProjectSectionTag(string Root);

    /// <summary>A row of the search's level: what was found, and the item as read for this fill.</summary>
    private sealed record FoundItem(SearchHit Hit, ItemFile Item);

    /// <summary>Where a search was started from, to go back to with Left or Escape.</summary>
    private sealed record SearchReturn(string? Project, string? ProjectName, string Folder, SmartList? Smart, string? Select);

    /// <summary>The search's level: the words, what they found, and where it was started from.</summary>
    private sealed record SearchLevel(string Words, IReadOnlyList<SearchHit> Hits, SearchReturn Back);

    /// <summary>The project shown as the top (TK-7b.3); null for the user's own top folder.</summary>
    private string? _project;

    private string? _projectName;

    private SearchLevel? _search;

    /// <summary>The projects found; null until the first look has ended.</summary>
    private IReadOnlyList<TaskProject>? _projects;

    /// <summary>
    /// Every item of every project found, read by the same look off the UI thread (TK-5.6), for the smart lists and the
    /// date lists; kept up to date for a file the window changes itself (<see cref="PatchProjectItems"/>).
    /// </summary>
    private IReadOnlyList<GatheredItem>? _projectItems;

    /// <summary>The number of the latest look for projects; an older one's result is dropped.</summary>
    private int _findRun;

    /// <summary>The number of the latest search; an older one's result is dropped.</summary>
    private int _searchRun;

    /// <summary>Whether the projects found changed while a notice was open, so that the level is read again as it closes.</summary>
    private bool _refillHeld;

    private string _lastWords = string.Empty;

    private readonly List<ToolStripItem> _projectFolderItems = [];

    /// <summary>Whether the level is the search's, where nothing is made or moved.</summary>
    private bool Gathered => _search is not null;

    private bool InSearch() => _search is not null && InNavigator();

    /// <summary>
    /// Options' project folder items (TK-7b.1): Add a project folder…, and Remove a project folder ▸ made again as it
    /// opens; Options' own items since 2026-11-11, a Project folders submenu before, so that no menu is three deep (AX-9.1).
    /// </summary>
    private ToolStripItem[] CreateProjectFolderItems()
    {
        var remove = new ToolStripMenuItem("&Remove a project folder");
        CommandPalette.RefreshedOnOpening(remove, () => FillProjectFolderItems(remove));
        // Filled once now too, so that the palette, which reads a menu with items, finds it a menu (AX-10.2).
        FillProjectFolderItems(remove);
        return [new ToolStripMenuItem("&Add a project folder...", null, (_, _) => AddProjectFolder()), remove];
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
    /// Windows folder picker; then the user's own top level on the folder's new section, "Added Projects in Work,
    /// looking for lists".
    /// </summary>
    private void AddProjectFolder()
    {
        AskText("Add a project folder",
            "A folder axTask looks in for your other folders of lists, four folders down. Each project folder is a section of the top level, after your own lists, named Projects in and the folder's name. Leave the field empty and press Enter to choose the folder with the Windows folder picker: there, Enter opens a folder, and the Select Folder button chooses the one you are in.",
            "Type the folder's path, or leave it empty for the folder picker:", "Type the folder's path, or leave it empty for the folder picker", string.Empty, "&Add",
            typed =>
            {
                // Once the notice has closed: the picker remembers the control in focus to give it back, which must be
                // the navigator, never the notice's field (heard 2026-11-11: the empty field read again after the picker).
                var path = typed.Trim();
                BeginInvoke(() =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    if (path.Length > 0)
                    {
                        AddChosenProjectFolder(path, afterDialog: false);
                        return;
                    }

                    if (ChooseFolder("Choose a folder with your projects") is not { } chosen)
                    {
                        return;
                    }

                    if (_settings.RefusedProjectFolder(chosen) is { } why)
                    {
                        SayAfterDialog(why);
                        return;
                    }

                    AddChosenProjectFolder(chosen, afterDialog: true);
                });
            },
            typed => typed.Trim().Length == 0 ? null : _settings.RefusedProjectFolder(typed), required: false);
    }

    /// <summary>The folder kept, looked in, and its section selected on the user's own top level (TK-7b.1).</summary>
    private void AddChosenProjectFolder(string path, bool afterDialog)
    {
        var added = _settings.AddProjectFolder(path);
        SaveSettings();
        _projects = null;
        _search = null;
        _project = null;
        _projectName = null;
        _smart = null;
        _folder = TopFolder;
        RememberLevel();
        Fill(ProjectSectionKey(added));
        Navigator.Select();
        FindProjects();
        var said = $"Added {ProjectSectionName(added)}, looking for lists";
        if (afterDialog)
        {
            SayAfterDialog(said);
        }
        else
        {
            Announce(said, true);
        }
    }

    /// <summary>
    /// Said once the window is in front again after a Windows dialog: an announcement raised as the dialog closes is lost
    /// under NVDA's reading of the window coming back (heard 2026-11-11).
    /// </summary>
    private void SayAfterDialog(string text)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 500 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            if (!IsDisposed)
            {
                Announce(text, true);
            }
        };
        timer.Start();
    }

    /// <summary>Options → Project folders → Remove a project folder ▸ one folder (TK-7b.1): "Removed Projects"; its section goes from the top level.</summary>
    private void RemoveProjectFolder(string folder)
    {
        _settings.ProjectFolders.RemoveAll(path => path.Equals(folder, StringComparison.OrdinalIgnoreCase));
        SaveSettings();
        if (_project is not null && _search is null && (_projects ?? []).Any(project => project.Path.Equals(_project, StringComparison.OrdinalIgnoreCase) && project.Root.Equals(folder, StringComparison.OrdinalIgnoreCase)))
        {
            // TK-7b.1: a project shown from the folder removed gives way to the user's own top level.
            _project = null;
            _projectName = null;
            _smart = null;
            _folder = TopFolder;
            FindProjects();
            Fill(select: null);
            Announce("Removed " + PathName.Of(folder) + ". " + Navigator.AccessibleName, true);
            return;
        }

        FindProjects();
        RefillGathered();
        Announce("Removed " + PathName.Of(folder), true);
    }

    /// <summary>
    /// The user's own top level read again, which holds the projects' sections, the selection kept and a changed row
    /// rewritten in place (<see cref="ItemForm.Refill"/>); nothing said.
    /// </summary>
    private void RefillGathered()
    {
        if (_search is null && _project is null && AtTop)
        {
            Refill();
        }
    }

    /// <summary>
    /// The level by its state, not by the status bar's place, which a look ending during an AI tool's question can
    /// rewrite (MC-7.3): the project, the folder, the smart list and the search's words.
    /// </summary>
    protected override string LevelIdentity() =>
        $"{_project}\n{_folder}\n{_smart?.Key}\n{_search?.Words}";

    /// <summary>Whether a look or a search that ends now must leave the panes alone: a notice is open, or a rename in place.</summary>
    private bool Busy => NoticeOpen || Navigator.Renaming || DetailPane.Renaming;

    /// <summary>A refill held back while a notice was open (TK-7b.2), done as the last notice closes.</summary>
    protected override void OnNoticeClosed()
    {
        base.OnNoticeClosed();
        if (_refillHeld && !Navigator.Renaming && !DetailPane.Renaming)
        {
            _refillHeld = false;
            RefillGathered();
        }
    }

    /// <summary>The projects looked for again, off the UI thread (TK-7b.2); the projects' sections filled when they come.</summary>
    private void FindProjects()
    {
        var run = ++_findRun;
        if (_settings.ProjectFolders.Count == 0)
        {
            var had = _projectItems is { Count: > 0 };
            _projects = [];
            _projectItems = [];
            if (had)
            {
                RefillGathered();
            }

            return;
        }

        var roots = _settings.ProjectFolders.ToList();
        var home = _settings.TopFolder;
        Do(async () =>
        {
            // One walk for the projects and their items (TK-5.6), off the UI thread.
            var (found, items) = await Task.Run(() =>
            {
                var projects = Projects.Find(roots, home);
                return (projects, Projects.Gather(projects));
            });
            if (run != _findRun || IsDisposed)
            {
                return;
            }

            // TK-7b.2: the panes change only when what was found did, and never under a notice or a rename in place.
            var changed = TakeLook(found, items);
            if (!changed)
            {
                return;
            }

            if (Busy)
            {
                _refillHeld = true;
                return;
            }

            RefillGathered();
        });
    }

    /// <summary>What a look found kept; true when the projects or any of their items changed since the last.</summary>
    private bool TakeLook(IReadOnlyList<TaskProject> found, IReadOnlyList<GatheredItem> items)
    {
        var changed = _projects is null || !_projects.SequenceEqual(found) || _projectItems is null
            || !_projectItems.Select(gathered => (gathered.Item.Path, gathered.Item.Text)).SequenceEqual(items.Select(gathered => (gathered.Item.Path, gathered.Item.Text)));
        _projects = found;
        _projectItems = items;
        return changed;
    }

    /// <summary>
    /// The projects' items the window's own change touched, read again on the UI thread, one file each (TK-5.6): an item
    /// marked done from a smart list leaves it at once, before the next look.
    /// </summary>
    private void PatchProjectItems(IEnumerable<FileChange> parts)
    {
        if (_projects is not { Count: > 0 } projects || _projectItems is null)
        {
            return;
        }

        var paths = parts.SelectMany(part => part.To is { } to ? new[] { part.Path, to } : [part.Path]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (!paths.Any(path => projects.Any(project => Projects.Inside(path, project.Path))))
        {
            return;
        }

        var kept = _projectItems.Where(gathered => !paths.Contains(gathered.Item.Path, StringComparer.OrdinalIgnoreCase)).ToList();
        kept.AddRange(paths.Select(path => Projects.GatherOne(path, projects)).OfType<GatheredItem>());
        _projectItems = kept;
    }

    /// <summary>"Projects in Work": a project folder's section, named after the folder (TK-7b.3).</summary>
    private static string ProjectSectionName(string root) => "Projects in " + PathName.Of(root);

    private static string ProjectSectionKey(string root) => "projects:" + root;

    /// <summary>
    /// A heading of level 2 for each project folder, in the order they were added (TK-7b.3): "Projects in Work, heading
    /// level 2, 2 projects", "looking for lists" until the first look ends, "none found" when it found nothing; under it
    /// each project found there, "alpha, project, 3 lists".
    /// </summary>
    private void AddProjectSections(List<ItemRow> rows)
    {
        foreach (var root in _settings.ProjectFolders)
        {
            var name = ProjectSectionName(root);
            var found = (_projects ?? []).Where(project => project.Root.Equals(root, StringComparison.OrdinalIgnoreCase)).ToList();
            var state = _projects is null ? "looking for lists" : found.Count == 0 ? "none found" : Count(found.Count, "project");
            rows.Add(Heading(ProjectSectionKey(root), name, 2, state, new ProjectSectionTag(root)));
            foreach (var project in found)
            {
                rows.Add(new ItemRow(true, project.Path, project.Name, $"{project.Name}, project, {Count(project.Lists, "list")}", new ProjectTag(project)));
            }
        }
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

    /// <summary>
    /// The search run off the UI thread, "Searching every list" said at once; what it finds is dropped when another
    /// search was started meanwhile, or when a notice or a rename in place is open as it ends, so that the focus stays
    /// where the user took it (TK-7b.4).
    /// </summary>
    private void RunSearch(string words)
    {
        _lastWords = words;
        var run = ++_searchRun;
        var roots = _settings.ProjectFolders.ToList();
        var home = _settings.TopFolder;
        Announce("Searching every list", true);
        Do(async () =>
        {
            // The look for projects and their items done once, and the search run over what it read (TK-5.6).
            var (found, items, hits) = await Task.Run(() =>
            {
                var projects = Projects.Find(roots, home);
                var projectItems = Projects.Gather(projects);
                return (projects, projectItems, Projects.Search([.. Projects.Gather([(home, PathName.Of(home))]), .. projectItems], words));
            });
            if (run != _searchRun || IsDisposed || Busy)
            {
                return;
            }

            if (TakeLook(found, items))
            {
                _refillHeld = false;
                RefillGathered();
            }

            if (hits.Count == 0)
            {
                Sounds.Edge();
                Announce("Nothing found for " + words, true);
                return;
            }

            var back = _search?.Back ?? new SearchReturn(_project, _projectName, _folder, _smart, SelectedKey());
            _search = new SearchLevel(words, hits, back);
            _project = null;
            _projectName = null;
            _smart = null;
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
        _smart = null;
        var home = _settings.TopFolder;
        var inHome = found.Hit.Top.Equals(home, StringComparison.OrdinalIgnoreCase);
        _project = inHome ? null : found.Hit.Top;
        _projectName = inHome ? null : found.Hit.Place;
        _folder = Path.GetDirectoryName(found.Item.Path)!;
        // TK-7b.3: a found item opened is never remembered as the list open last; the user's own place is kept.
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
        return back.Select ?? string.Empty;
    }

    /// <summary>The detail pane of a project folder's section and of a project.</summary>
    private ItemDetail ProjectDetail(ItemRow row) => row.Tag is ProjectTag { Project: var project }
        ? new ItemDetail("Details of " + project.Name, null, null,
        [
            new ItemDetailLine(row.Label),
            new ItemDetailLine("Its folder: " + project.Path),
            new ItemDetailLine("Right opens it as a top folder of its own: its lists, smart lists and Archive, changed in place"),
        ])
        : new ItemDetail("Details of " + row.Name, null, null,
        [
            new ItemDetailLine(row.Label),
            new ItemDetailLine("The project folder: " + ((ProjectSectionTag)row.Tag!).Root),
            new ItemDetailLine("Every folder holding a list, four folders down; Options, Add a project folder adds and removes project folders. Ctrl+Shift+F finds words in every list"),
        ]);
}

using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// axTask's commands (IA-3, IA-5, TK-4, TK-5) and the one way a file changes (<see cref="Commit"/>): every change is a
/// <see cref="Step"/> of writes and moves, checked against the file as it was shown (IA-5.5), applied, kept for Ctrl+Z,
/// the panes filled again, its words said once (IA-5.4). The field commands are the shared window's; they reach a file
/// through <see cref="SaveFields"/>.
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>The steps Ctrl+Z takes back, twenty at most.</summary>
    private readonly StepHistory _undo = new(20);

    // ---- the navigator's commands ----

    // ---- the selection (bundle AX-8.16; SPEC.md TK-4.7, 2026-11-22) ----

    /// <summary>The navigator has a selection: Space selects, and the commands below that can act on several items act on the selected ones.</summary>
    protected override bool HasSelection => true;

    /// <summary>Items are selected, in a list, a smart list and the search alike; a list, a project and a file that does not read as an item are not.</summary>
    protected override bool CanSelectRow(ItemRow row) => ItemOf(row) is not null;

    /// <summary>Space on what cannot be selected: why, in a few words.</summary>
    protected override void NothingToSelect(ItemRow? row)
    {
        EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
        Announce(row switch
        {
            null => Rows.Count == 0 ? "Nothing here to select" : "No items here to select: go into a list, Right",
            { IsSection: true } => "A heading: nothing to select",
            { Tag: UnreadableTag } => NotAnItem,
            _ => "Items are selected, not lists: Right goes into this one",
        }, true);
    }

    /// <summary>The item of a row: a list's, or the one the search found, which is changed where it lives (TK-7b.4); null on anything else.</summary>
    private static ItemFile? ItemOf(ItemRow? row) => row switch
    {
        { Tag: ItemFile file } => file,
        { Tag: FoundItem found } => found.Item,
        _ => null,
    };

    /// <summary>
    /// What a command that can act on several items acts on (AX-8.16, TK-4.7): the selected items, in the list's
    /// order, or the current item alone when none is selected; empty when there is neither.
    /// </summary>
    private List<ItemFile> Targets() => [.. RowsInHand().Select(ItemOf).OfType<ItemFile>()];

    // ---- the navigator's commands ----

    /// <summary>
    /// Ctrl+Enter (TK-4.4; Space until 2026-11-22, which selects now): a task done or not done; the edge sound on a
    /// list or a plain item. With items selected (TK-4.7): all of their tasks done while any is not, not done when
    /// every one is, one step for Ctrl+Z, and what was passed over counted in the words.
    /// </summary>
    private void ToggleDone()
    {
        if (Selection.Count > 0)
        {
            DoneForSelected();
            return;
        }

        // A task the search found is marked where it lives, its file written (TK-7b.4).
        if (ItemOf(SelectedRow()) is not { } item)
        {
            EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
            return;
        }

        var done = TaskRules.DoneIndex(item.Document);
        if (done < 0)
        {
            Refuse("Not a task");
            return;
        }

        var now = !TaskRules.IsDone(item.Document);
        var select = LeavesWhenDone(now) ? NeighbourKey() : item.Path;
        if (Changeable(item) && CheckFresh(item.Path, item.Text))
        {
            Commit(new Step([FileChange.Write(item.Path, item.Text, item.TextWith(item.Document.WithValue(done, JsonValue.Create(now))))], (now ? "Done: " : "Not done: ") + ItemFolder.NameOf(item)), select);
        }
    }

    /// <summary>Whether a task marked done leaves the level shown: a smart list shows the tasks not done, and a list with Hide done tasks on does.</summary>
    private bool LeavesWhenDone(bool now) => now && (_smart is not null || (_settings.HideDone && _search is null));

    private void DoneForSelected()
    {
        var items = Targets();
        var tasks = items.Where(item => TaskRules.IsTask(item.Document)).ToList();
        if (tasks.Count == 0)
        {
            Refuse(items.Count == 1 ? "The selected item is not a task" : "None of the selected items is a task");
            return;
        }

        var now = ManyItems.DoneNow(tasks.Select(task => task.Document));
        var change = tasks.Where(task => TaskRules.IsDone(task.Document) != now).ToList();
        if (!change.All(Changeable) || !change.All(task => CheckFresh(task.Path, task.Text)))
        {
            return;
        }

        var parts = change.Select(task => FileChange.Write(task.Path, task.Text, task.TextWith(task.Document.WithValue(TaskRules.DoneIndex(task.Document), JsonValue.Create(now))))).ToList();
        var select = LeavesWhenDone(now) ? KeyAfter([.. change.Select(task => task.Path)]) : SelectedKey();
        Commit(new Step(parts, ManyItems.DoneSaid(now, change.Count, tasks.Count - change.Count, items.Count - tasks.Count)), select);
    }

    /// <summary>
    /// Whether an item can be changed: not one whose configuration is not there or does not read, which is shown as
    /// plain text (AF-13.4); the reason is said then.
    /// </summary>
    private bool Changeable(ItemFile item)
    {
        if (item.ConfigProblem is not { } problem)
        {
            return true;
        }

        Refuse(problem + $": nothing of the item is changed until it is there and reads. {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on the list opens it");
        return false;
    }

    /// <summary>
    /// The configuration a change in a list goes by, with its file as it is on disk: the list's own, or, for a list
    /// that has no file yet, the one it gets (AF-11.1), which the caller writes in the same step. Null, with the
    /// reason said, for a configuration that does not read: axTask writes nothing into that file and adds nothing
    /// beside it until it reads again (AF-11.7).
    /// </summary>
    private (ListConfig Config, string? Text)? ConfigurationForChange(string folder)
    {
        var read = ItemFolder.ReadConfig(folder);
        if (read.Config is { } config)
        {
            return (config, read.Text);
        }

        if (read.Text is null)
        {
            return (ItemFolder.StarterConfig(folder), null);
        }

        Refuse(ConfigurationProblem(folder) ?? "The list's configuration does not read");
        return null;
    }

    /// <summary>
    /// Ctrl+N in the navigator: a new item in the list shown (IA-4.3), its fields from the list's template; at the top
    /// level, in the list selected (TK-4.3a), the focus staying on that list.
    /// </summary>
    protected override void NewItem()
    {
        if (AtTop && SelectedRow() is { IsList: true, Tag: null } selected && !ItemFolder.IsArchive(TopFolder, selected.Key))
        {
            if (ItemFolder.IsEarlier(selected.Key))
            {
                // A list of the earlier format takes no item until it is converted: the question Right asks.
                AskConversion(selected);
                return;
            }

            AskNewItem(selected.Key, selected.Name, inList: false);
            return;
        }

        if (_smart is not null)
        {
            Announce("A smart list gathers items from the lists: open a list to add one", true);
            return;
        }

        if (Gathered)
        {
            // TK-7b.4: items are made in a list where it lives.
            Announce("Items are made in a list: open the item where it lives first, Enter", true);
            return;
        }

        if (AtTop || IsArchiveFolder(_folder))
        {
            // TK-D4: items live in lists; the Archive keeps what was moved there. At the top, a heading, a smart list,
            // a project or the Archive is selected: a list there takes the item (above).
            Announce(AtTop ? $"Items go in a list: Right on a list, then {BundleKeys.Text(ItemKeys.New)}" :"The Archive keeps what was archived: open a list to add an item", true);
            return;
        }

        // TK-4.3: in the list shown, the title is typed in place, as F2 renames; from the top level the notice asks.
        if (ConfigurationForChange(_folder) is not { } shown)
        {
            return;
        }

        var folder = _folder;
        NewItemInPlace(ListOrder.NewAtTop(shown.Config), title => AddItem(folder, Path.GetFileName(folder), title, inList: true));
    }

    /// <summary>
    /// The question for a new item's title and the item made in <paramref name="folder"/>: in the list shown
    /// (<paramref name="inList"/>), the new item selected and "Added &lt;title&gt;" said; from the top level, the list
    /// stays selected and "Added &lt;title&gt; to &lt;list&gt;" is said (TK-4.3a).
    /// </summary>
    private void AskNewItem(string folder, string listName, bool inList)
    {
        if (ConfigurationForChange(folder) is null)
        {
            return;
        }

        AskText("New item", $"In {listName}.", "Title:", "Title", string.Empty, "&Add", title => AddItem(folder, listName, title, inList), typed => typed.Trim().Length == 0 ? "Type the item's title" : null, byUser: true);
    }

    /// <summary>The new item written in <paramref name="folder"/>, from the notice or from the title typed in place.</summary>
    private void AddItem(string folder, string listName, string title, bool inList)
    {
        if (ConfigurationForChange(folder) is not { } list)
        {
            return;
        }

        // TK-4.3c: a date and a !priority at the end of the title fill Due and Priority and leave the title.
        var words = TitleWords.Read(title, list.Config.ItemFields, DateOnly.FromDateTime(DateTime.Now));
        title = words.Title;
        var item = TaskRules.WithCreated(FlatItem.New(title, list.Config), list.Config, DateTime.Now);
        foreach (var (field, value) in words.Values)
        {
            item = item.With(field, value, list.Config);
        }

        var path = ItemFiles.FreePath(folder, ItemFiles.FileStem(title), ".json");
        var said = (inList ? "Added " + title : $"Added {title} to {listName}") + words.Said;
        Commit(new Step(NewItemParts(folder, list, path, item), said), inList ? path : folder);
    }

    /// <summary>
    /// The writes of a new item: its file, and before it the list's configuration when the list has none on disk yet
    /// (AF-11.1), so that the item is read through a file that is there, or when the list's order takes the new item at
    /// its top or its bottom (TK-4.10, <see cref="ListOrder.Added"/>).
    /// </summary>
    private static List<FileChange> NewItemParts(string folder, (ListConfig Config, string? Text) list, string path, FlatItem item)
    {
        var parts = new List<FileChange>();
        var config = list.Config;
        var shown = ItemFolder.Read(folder).Items.Select(file => Path.GetFileNameWithoutExtension(file.Path)).ToList();
        if (ListOrder.Added(ListOrder.Of(config), shown, Path.GetFileNameWithoutExtension(path), ListOrder.NewAtTop(config)) is { } order)
        {
            config = ListOrder.WithOrder(config, order);
        }

        if (list.Text is null || !ReferenceEquals(config, list.Config))
        {
            parts.Add(FileChange.Write(Path.Combine(folder, ItemDocument.ListFile), list.Text, config.ToText()));
        }

        parts.Add(FileChange.Write(path, null, item.ToText()));
        return parts;
    }

    /// <summary>
    /// Ctrl+Shift+N: a new list in the top folder (TK-D4), in the section of the row selected (TK-3a.3) and with the
    /// template of the list selected or shown; the sections shown after, with the new list selected.
    /// </summary>
    protected override void NewList()
    {
        if (RefusedNewList() is { } why)
        {
            Announce(why, true);
            return;
        }

        var folder = TopFolder;
        var section = SelectedRow() switch
        {
            { Tag: SectionTag { Custom: true } header } => header.Name,
            { IsList: true, Tag: null } list when !ItemFolder.IsArchive(TopFolder, list.Key) => ItemFolder.SectionOf(list.Key),
            _ => AtTop || _smart is not null || IsArchiveFolder(_folder) ? string.Empty : ItemFolder.SectionOf(_folder),
        };
        var templateFrom = SelectedRow() is { IsList: true, Tag: null } selected ? selected.Key : AtTop || _smart is not null ? null : _folder;
        AskText("New list", section.Length == 0 ? "In Your lists." : $"In the section {section}.", "Name:", "Name", string.Empty, "&Make", name =>
        {
            var trimmed = name.Trim();
            var listFile = Path.Combine(folder, trimmed, ItemDocument.ListFile);
            // The fields of the list selected or shown, for its items; the section first among its details (TK-3a.3).
            var template = TaskRules.TemplateOf(templateFrom is null ? null : ItemFolder.ReadList(templateFrom));
            var details = ListConfig.New(template).WithDetail(ItemFolder.SectionField, JsonValue.Create(section));
            _smart = null;
            _folder = TopFolder;
            Commit(new Step([FileChange.WriteInNewFolder(listFile, details.ToText())], "Made " + trimmed), Path.Combine(folder, trimmed));
        }, typed => ItemFolder.RefusedListName(folder, typed) ?? (typed.Trim().Equals(ItemFolder.ArchiveName, StringComparison.OrdinalIgnoreCase) ? "Archive is the archive's own name" : null), byUser: true);
    }

    /// <summary>Why no list is made here: in the search's level (TK-7b.4), or on a project's row or section (TK-7b.3); null where lists are made.</summary>
    private string? RefusedNewList() => Gathered ? "Lists are made among the sections: Left first"
        : SelectedRow() is { Tag: ProjectTag or ProjectSectionTag } ? "A project's lists are made inside it: Right on a project first"
        : null;

    /// <summary>
    /// List, Move to section (Ctrl+M, TK-3a.3): the list's Section field set from a list of the sections there are,
    /// User lists, and a new one.
    /// </summary>
    private void MoveToSection()
    {
        if (SelectedRow() is not { IsList: true, Tag: null } row || ItemFolder.IsArchive(TopFolder, row.Key) || !AtTop)
        {
            Refuse("Select a list among the sections first");
            return;
        }

        var current = ItemFolder.SectionOf(row.Key);
        var sections = ItemFolder.ReadTop(TopFolder).Lists.Select(ItemFolder.SectionOf).Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, NaturalOrder.Instance).ToList();
        List<string> choices = [YourLists, .. sections, "A new section"];
        var start = current.Length == 0 ? 0 : Math.Max(0, choices.FindIndex(choice => choice.Equals(current, StringComparison.OrdinalIgnoreCase)));
        // One form (bundle AX-8.10): the sections as a list and, for a new one, its name; a list and then a second
        // notice for the name until 2026-11-14.
        var form = new FormControls();
        var section = form.AddList("Choose the section", choices, start);
        var name = form.AddText("Type the new section's name, when you chose A new section");
        bool IsNew() => section.SelectedIndex == choices.Count - 1;
        ShowForm("Move " + row.Name,
            $"Now in {(current.Length == 0 ? YourLists : current)}. Up and Down choose the section; Enter or {BundleKeys.Text(BundleKeys.MainAction)} moves the list there. For a section that does not exist yet, choose A new section, the last one, then Tab and type its name.",
            form, "&Move",
            () => SetSection(row, IsNew() ? name.Text.Trim() : section.SelectedIndex <= 0 ? string.Empty : choices[section.SelectedIndex]),
            () => !IsNew() ? null
                : name.Text.Trim().Length == 0 ? (name, "Type the section's name")
                : choices.Contains(name.Text.Trim(), StringComparer.OrdinalIgnoreCase) ? (name, $"There is a section called {name.Text.Trim()} already")
                : null, byUser: true);
    }

    /// <summary>The list's Section field written (TK-3a.3).</summary>
    private void SetSection(ItemRow row, string section)
    {
        var listFile = Path.Combine(row.Key, ItemDocument.ListFile);
        if (ConfigurationForChange(row.Key) is not { } list)
        {
            return;
        }

        Commit(new Step([FileChange.Write(listFile, list.Text, list.Config.WithDetail(ItemFolder.SectionField, JsonValue.Create(section)).ToText())], $"Moved {row.Name} to {(section.Length == 0 ? YourLists : section)}"), row.Key);
    }

    /// <summary>List, New smart list (TK-5.3): a filter file with All tasks' conditions, opened in axJSON.</summary>
    private void NewSmartList()
    {
        if (Gathered)
        {
            Announce("Smart lists are made among the sections: Left first", true);
            return;
        }

        AskText("New smart list", "Its conditions start as All tasks', and open in axJSON to change.", "Name:", "Name", string.Empty, "&Make", name =>
        {
            var path = Path.Combine(TopFolder, name.Trim() + ItemFolder.FilterExtension);
            _smart = null;
            _folder = TopFolder;
            if (Commit(new Step([FileChange.Write(path, null, SmartList.NewFileText())], "Made the smart list " + name.Trim()), path))
            {
                WindowMenu.Start(Dispatch.AxJSON, path);
            }
        }, typed =>
        {
            var trimmed = typed.Trim();
            if (ItemFolder.RefusedListName(TopFolder, trimmed) is { } why)
            {
                return why;
            }

            return File.Exists(Path.Combine(TopFolder, trimmed + ItemFolder.FilterExtension)) ? $"There is a smart list called {trimmed} already" : null;
        }, byUser: true);
    }

    /// <summary>F2 in the navigator (AX-8.7): an item's Title (its file renamed with it, IA-4.5) or a list's name, in place; the links to them kept (IA-D3).</summary>
    protected override string? RenameRefused(ItemRow row) => row switch
    {
        { Tag: SectionTag { Custom: true } } => $"A section is renamed by moving its lists: {BundleKeys.Text(AxTaskKeys.MoveToSection)} on each",
        { Tag: SectionTag } => "This section keeps its name",
        { Tag: SmartList { BuiltIn: true } smart } => smart.Name + " is built in and keeps its name",
        { Tag: SmartList } => $"Rename a smart list's file in axFile: {BundleKeys.Text(AxTaskKeys.ShowInAxFile)}",
        { Tag: ItemFile } => null,
        { Tag: FoundItem } => "Open it where it lives first: Enter",
        { Tag: ProjectTag } => $"A project's folder is renamed in axFile: {BundleKeys.Text(AxTaskKeys.ShowInAxFile)}",
        { Tag: ProjectSectionTag } => "A project folder's section keeps its folder's name",
        { Tag: UnreadableTag } => NotAnItem,
        _ when ReferenceEquals(row.Tag, ArchivedItemsTag) || IsArchiveFolder(row.Key) => "The Archive keeps its name",
        _ => null,
    };

    protected override string? RefusedName(ItemRow row, string name)
    {
        if (row.Tag is ItemFile)
        {
            return name.Length == 0 ? "Type the item's title" : null;
        }

        if (name.Equals(row.Name, StringComparison.OrdinalIgnoreCase))
        {
            // A change of case only: the same folder.
            return null;
        }

        return ItemFolder.RefusedListName(Path.GetDirectoryName(row.Key)!, name)
            ?? (name.Equals(ItemFolder.ArchiveName, StringComparison.OrdinalIgnoreCase) ? "Archive is the archive's own name" : null);
    }

    protected override void RenameTo(ItemRow row, string name)
    {
        if (row.Tag is ItemFile)
        {
            RenameItem(row, name);
            return;
        }

        var to = Path.Combine(Path.GetDirectoryName(row.Key)!, name);
        var parts = new List<FileChange> { FileChange.Move(row.Key, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), row.Key, to));
        Commit(new Step(parts, $"Renamed {row.Name} to {name}"), to);
    }
    protected override void RenameItem(ItemRow row, string title)
    {
        if (row.Tag is FoundItem)
        {
            // The links to it are kept within its own folder of lists, which the search's level is not (TK-7b.4).
            Refuse("Open it where it lives first: Enter in the navigator");
            return;
        }

        if (row.Tag is not ItemFile item)
        {
            return;
        }

        if (!Changeable(item))
        {
            return;
        }

        var (parts, to) = ItemChangeParts(item, item.Document.WithTitle(title));
        if (CheckFresh(item.Path, item.Text))
        {
            Commit(new Step(parts, "Renamed " + title), to);
        }
    }

    /// <summary>Delete in the navigator (IA-D4): the item or list into the Archive list, the links to it kept; Ctrl+Z brings it back.</summary>
    protected override void DeleteRow(ItemRow row)
    {
        if (Selection.Count > 0)
        {
            // TK-4.7: with items selected, Delete archives them, whatever row is current.
            ArchiveSelected();
            return;
        }

        if (row.Tag is UnreadableTag)
        {
            Announce(NotAnItem, true);
            return;
        }

        if (row.Tag is SmartList)
        {
            Announce($"A smart list's file is deleted in axFile: {BundleKeys.Text(AxTaskKeys.ShowInAxFile)}", true);
            return;
        }

        if (row.Tag is SectionTag)
        {
            Announce($"A section goes when its lists leave it: {BundleKeys.Text(AxTaskKeys.MoveToSection)} on each", true);
            return;
        }

        if (row.Tag is FoundItem)
        {
            Refuse("Open it where it lives first: Enter");
            return;
        }

        if (row.Tag is ProjectTag or ProjectSectionTag)
        {
            Announce("A project stays: Options, Remove a project folder removes the folder it was found in", true);
            return;
        }

        // A project's item in a smart list goes to its own folder of lists' Archive (TK-5.6).
        var top = TopOf(row.Key);
        if (IsArchiveFolder(row.Key) || ItemFolder.IsArchive(top, Path.GetDirectoryName(row.Key)!))
        {
            Announce(IsArchiveFolder(row.Key) ? "The Archive stays" : $"Already in the Archive: {BundleKeys.Text(AxTaskKeys.ShowInAxFile)} shows it in axFile, which deletes it for good", true);
            return;
        }

        var archive = ItemFolder.ArchiveFolder(top);
        var to = row.IsList
            ? ItemFiles.FreePath(archive, Path.GetFileName(row.Key), string.Empty)
            : ItemFiles.FreePath(archive, Path.GetFileNameWithoutExtension(row.Key), ".json");
        var parts = new List<FileChange> { FileChange.Move(row.Key, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(top), row.Key, to));
        if (row.Tag is not ItemFile item || CheckFresh(item.Path, item.Text))
        {
            Commit(new Step(parts, $"Archived {row.Name}"), NeighbourKey());
        }
    }

    /// <summary>
    /// Delete with items selected (TK-4.7): every one of them into its own folder of lists' Archive, the links to
    /// them kept, one step for Ctrl+Z, "Archived 3 items". Not from the search, where an item is opened where it
    /// lives first, and not for items that are in the Archive already.
    /// </summary>
    private void ArchiveSelected()
    {
        var items = Targets();
        if (RowsInHand().Any(row => row.Tag is FoundItem))
        {
            Refuse("Open an item where it lives first: Enter. The search archives nothing");
            return;
        }

        if (items.Any(item => ItemFolder.IsArchive(TopOf(item.Path), Path.GetDirectoryName(item.Path)!)))
        {
            Announce($"Already in the Archive: {BundleKeys.Text(AxTaskKeys.ShowInAxFile)} shows an item in axFile, which deletes it for good", true);
            return;
        }

        if (!items.All(item => CheckFresh(item.Path, item.Text)))
        {
            return;
        }

        var select = KeyAfter([.. items.Select(item => item.Path)]);
        CommitMoves([.. items.Select(item => (Func<List<FileChange>>)(() =>
        {
            var top = TopOf(item.Path);
            var to = ItemFiles.FreePath(ItemFolder.ArchiveFolder(top), Path.GetFileNameWithoutExtension(item.Path), ".json");
            var parts = new List<FileChange> { FileChange.Move(item.Path, to) };
            parts.AddRange(Links.AfterMove(ItemFolder.AllItems(top), item.Path, to));
            return parts;
        }))], $"Archived {ManyItems.Items(items.Count)}", select);
    }

    /// <summary>Where Ctrl+M moves an item, or the selected items, to a list (TK-4.7); on a list with nothing selected it is Move to section.</summary>
    private bool OnItemsToMove() => InNavigator() && (Selection.Count > 0 || ItemOf(SelectedRow()) is not null);

    /// <summary>Where Ctrl+M moves a list to a section (TK-3a.3).</summary>
    private bool OnListToMove() => InNavigator() && !OnItemsToMove();

    /// <summary>
    /// Ctrl+M on an item, Context, Move to a list (TK-4.7): the selected items, or the item you are on, into a list
    /// of the folder chosen in one form, or into a new list named there, which gets the fields and the section of the
    /// list the first item is in. One step for Ctrl+Z, the links following: "3 items moved to Later". Not from what
    /// a search found, and not for a project's item gathered into the user's smart lists, which is moved in its
    /// project.
    /// </summary>
    private void MoveToList()
    {
        if (Gathered)
        {
            Refuse("Open an item where it lives first: Enter. The search moves nothing");
            return;
        }

        var items = Targets();
        if (items.Count == 0)
        {
            Refuse("Move to an item first");
            return;
        }

        var top = TopFolder;
        if (items.Any(item => !TopOf(item.Path).Equals(top, StringComparison.OrdinalIgnoreCase)))
        {
            Announce("An item of a project is moved inside its project: Right on the project first", true);
            return;
        }

        if (!items.All(Changeable))
        {
            return;
        }

        var from = items.Select(item => Path.GetDirectoryName(item.Path)!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var lists = ItemFolder.ReadTop(top).Lists
            .Where(list => !ItemFolder.IsEarlier(list) && !(from.Count == 1 && list.Equals(from[0], StringComparison.OrdinalIgnoreCase))).ToList();
        var what = items.Count == 1 ? ItemFolder.NameOf(items[0]) : ManyItems.Items(items.Count);
        List<string> choices = [.. lists.Select(list => ItemFolder.ListName(top, list)), "A new list"];
        // One form (bundle AX-8.10), as Move to section: the lists as a list and, for a new one, its name.
        var form = new FormControls();
        var chosen = form.AddList("Choose the list", choices, 0);
        var name = form.AddText("Type the new list's name, when you chose A new list");
        bool IsNew() => chosen.SelectedIndex == choices.Count - 1;
        string? NameRefused() => ItemFolder.RefusedListName(top, name.Text)
            ?? (name.Text.Trim().Equals(ItemFolder.ArchiveName, StringComparison.OrdinalIgnoreCase) ? "Archive is the archive's own name" : null);
        ShowForm("Move " + what,
            $"Now in {string.Join(", ", from.Select(folder => ItemFolder.ListName(top, folder)))}. Up and Down choose the list; Enter or {BundleKeys.Text(BundleKeys.MainAction)} moves {(items.Count == 1 ? "the item" : "the " + what)} there, as one step that {BundleKeys.Text(ItemKeys.Undo)} takes back. For a list that does not exist yet, choose A new list, the last one, then Tab and type its name: it gets the same fields. Escape leaves, and nothing is moved.",
            form, "&Move",
            () =>
            {
                if (!IsNew())
                {
                    MoveItemsTo(items, lists[chosen.SelectedIndex], string.Empty, made: null);
                    return;
                }

                // A new list beside the first item's own: its fields for the items, its section among the details (TK-3a.3).
                var trimmed = name.Text.Trim();
                var template = TaskRules.TemplateOf(ItemFolder.ReadList(from[0]));
                var section = ItemFolder.IsArchive(top, from[0]) ? string.Empty : ItemFolder.SectionOf(from[0]);
                var details = ListConfig.New(template).WithDetail(ItemFolder.SectionField, JsonValue.Create(section));
                MoveItemsTo(items, Path.Combine(top, trimmed), string.Empty, FileChange.WriteInNewFolder(Path.Combine(top, trimmed, ItemDocument.ListFile), details.ToText()));
            },
            () => IsNew() && NameRefused() is { } refused ? (name, refused) : null, byUser: true);
    }

    /// <summary>
    /// TK-4.2: the list in hand made a checklist or a plain list. The change is the configuration's alone (AF-13.3):
    /// a Done field first among the fields of its items, or none. No item's file is touched: as a checklist every
    /// item shows Done, not done until it is checked; as a plain list a Done an item holds is kept in its file, and
    /// shows again when the list is a checklist again.
    /// </summary>
    private void SwitchList(bool done)
    {
        var folder = SelectedRow() is { IsList: true, Tag: null } row && !DetailPane.Focused ? row.Key : _smart is null && !AtTop ? _folder : null;
        if (folder is null)
        {
            Refuse("Open a list first");
            return;
        }

        if (ItemFolder.IsEarlier(folder))
        {
            Announce("This list is written in axTask's earlier format: Enter on it converts it first", true);
            return;
        }

        if (ConfigurationForChange(folder) is not { } list)
        {
            return;
        }

        var name = folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase) ? "The top folder" : Path.GetFileName(folder);
        var has = list.Config.ItemFields.Any(field => field.Type == FieldTypes.Check && field.Title.Equals(TaskRules.DoneField, StringComparison.OrdinalIgnoreCase));
        if (has == done && list.Text is not null)
        {
            Announce(done ? $"{name} is a checklist already" : $"{name} is a plain list already", true);
            return;
        }

        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        var changed = list.Config.WithItemFields(TaskRules.TemplateWithDone(list.Config.ItemFields, done));
        Commit(new Step([FileChange.Write(listFile, list.Text, changed.ToText())],
            done ? $"{name} is a checklist: every item has Done" : $"{name} is a plain list: Done is hidden, and kept in the items that have it"), SelectedKey());
    }

    // ---- the detail pane's link (IA-D3) ----

    /// <summary>A link: the other item's title typed, found among every item under the top folder.</summary>
    protected override bool ChangeSpecial(ItemRow row, int index, ItemField field)
    {
        // An item whose configuration is not there or does not read is not changed (AF-13.4): said before any
        // notice asks for a value that could not be kept.
        if (ShownDetail.Source is FileSource { Item: { } shown } && !Changeable(shown))
        {
            return true;
        }

        if (field.Type != FieldTypes.Link || ShownDetail.Source is not FileSource source)
        {
            return false;
        }

        if (row.Tag is FoundItem)
        {
            // A link is looked for among the items of its own folder of lists (TK-7b.4).
            Refuse("Open it where it lives first: Shift+Tab, then Enter");
            return true;
        }

        var from = source.Path;
        var current = FieldTypes.TextOf(field.Value) is { Length: > 0 } link && ItemFolder.Resolve(from, link) is { } linkedPath && ItemFolder.ReadItem(linkedPath) is { } linked ? ItemFolder.NameOf(linked) : string.Empty;
        ItemFile? target = null;
        AskText("Change " + field.Title, "The title of the item to link to, in any list. Empty for none.", "Title:", "Title", current, "&Link",
            _ => SetValue(index, field, JsonValue.Create(target is null ? string.Empty : ItemFolder.LinkTo(from, target.Path))),
            typed =>
            {
                var title = typed.Trim();
                if (title.Length == 0)
                {
                    target = null;
                    return null;
                }

                var matches = ItemFolder.AllItems(TopFolder).Where(item => !item.Path.Equals(from, StringComparison.OrdinalIgnoreCase)
                    && ItemFolder.NameOf(item).Equals(title, StringComparison.OrdinalIgnoreCase)).ToList();
                target = matches.FirstOrDefault();
                return matches.Count switch
                {
                    0 => $"No item is called {title}",
                    1 => null,
                    _ => $"{matches.Count} items are called {title}: rename one first",
                };
            }, required: false, byUser: true);
        return true;
    }

    // ---- everywhere ----

    /// <summary>Ctrl+Z (IA-5.4): the last step taken back, when nothing changed what it left since.</summary>
    protected override void Undo()
    {
        if (_undo.TakeLast() is not { } step)
        {
            Refuse("Nothing to undo");
            return;
        }

        try
        {
            if (Changes.CheckUndo(step) is { } why)
            {
                Refuse($"Cannot undo {step.Said}: {why}");
                return;
            }

            Changes.Undo(step);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{step.Said} could not be undone: {ex.Message}");
            Refresh(announce: false);
            return;
        }

        Log.Info("Undone a step");
        PatchProjectItems(step.Parts);
        RecordHistory(ItemHistory.Undone(step.Parts));
        // The selection on what the step touched first, where it is again.
        var first = step.Parts[0];
        Fill(first.IsMove ? first.Path : SelectedKey() ?? first.Path);
        Announce("Undone: " + step.Said, true);
    }

    private void ToggleHideDone()
    {
        _settings.HideDone = !_settings.HideDone;
        _hideDoneItem.Checked = _settings.HideDone;
        SaveSettings();
        Fill(SelectedKey());
        Announce(MenuText.CheckSaid("Hide done tasks", _settings.HideDone), true);
    }

    /// <summary>What F2 and Delete say on a file that does not read as an item (TK-4.6): nothing of axTask's changes it.</summary>
    private static string NotAnItem => $"Not readable as an item: Enter opens it in axJSON, {BundleKeys.Text(AxTaskKeys.ShowInAxFile)} shows it in axFile";

    /// <summary>Ctrl+J: the item, the list's details or the smart list in axJSON (G3).</summary>
    private void OpenInAxJson()
    {
        switch (SelectedRow())
        {
            case { Tag: ItemFile item }:
                WindowMenu.Start(Dispatch.AxJSON, item.Path);
                return;
            case { Tag: FoundItem found }:
                WindowMenu.Start(Dispatch.AxJSON, found.Item.Path);
                return;
            case { Tag: UnreadableTag }:
                OpenUnreadable();
                return;
            case { Tag: ProjectTag }:
                Announce($"A project is a folder: {BundleKeys.Text(AxTaskKeys.ShowInAxFile)} shows it in axFile", true);
                return;
            case { Tag: ProjectSectionTag }:
                Announce("A project folder gathers folders: Options, Add a project folder adds one", true);
                return;
            case { Tag: SectionTag }:
                Announce($"A section is its lists' Section field: {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on a list", true);
                return;
            case { Tag: SmartList { Path: { } filter } }:
                WindowMenu.Start(Dispatch.AxJSON, filter);
                return;
            case { Tag: SmartList smart }:
                Announce(smart.Name + " is built in: List, New smart list makes one to change", true);
                return;
            case { } row:
                EditConfiguration(row.Key, row.Name);
                return;
            default:
                Refuse("Nothing selected");
                return;
        }
    }

    /// <summary>
    /// Edit the list's configuration (AF-11.1): its <c>list.json</c> in axJSON. A list that has no file yet gets one
    /// written first, with the fields its items hold and an empty list of actions, so that there is something to
    /// edit and its members are there to be filled in. A configuration that does not read is opened as it is, to be
    /// mended; a list of the earlier format is converted first.
    /// </summary>
    private void EditConfiguration(string folder, string name)
    {
        if (ItemFolder.IsEarlier(folder))
        {
            Announce($"{name} is written in axTask's earlier format: Enter on it converts it first", true);
            return;
        }

        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        if (!File.Exists(listFile)
            && !Commit(new Step([FileChange.Write(listFile, null, ItemFolder.StarterConfig(folder).ToText())], $"Written the configuration of {name}"), SelectedKey()))
        {
            return;
        }

        try
        {
            _configEdited = (listFile, Changes.ReadOrNull(listFile));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _configEdited = null;
        }

        WindowMenu.Start(Dispatch.AxJSON, listFile);
    }

    /// <summary>The configuration that Edit the list's configuration opened last, with its text then (AF-11.1): looked at again when this window comes back to the front.</summary>
    private (string Path, string? Text)? _configEdited;

    /// <summary>
    /// Back from axJSON (AF-11.1): when the configuration opened from here has changed, the panes are read again and
    /// one line says what it holds now, or that it does not read and where ("The list's configuration changed: 5
    /// fields, 2 actions"). Said after the window's own words on coming to the front; left for the next time while
    /// a notice or a rename is open.
    /// </summary>
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (_configEdited is not { } edited || Busy)
        {
            return;
        }

        string? now;
        try
        {
            now = Changes.ReadOrNull(edited.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return;
        }

        if (now == edited.Text)
        {
            return;
        }

        _configEdited = (edited.Path, now);
        var folder = Path.GetDirectoryName(edited.Path)!;
        After(500, () =>
        {
            if (Busy)
            {
                return;
            }

            Fill(SelectedKey());
            Announce(ConfigurationChanged(folder), true);
        });
    }

    /// <summary>"The list's configuration changed: 5 fields, 2 actions", with what its reader passed over; or that it does not read, and where it is mended.</summary>
    private static string ConfigurationChanged(string folder)
    {
        var read = ItemFolder.ReadConfig(folder);
        if (read.Config is not { } config)
        {
            return read.Text is null ? "The list's configuration is gone" : ConfigurationProblem(folder) ?? "The list's configuration does not read";
        }

        var said = $"The list's configuration changed: {Count(config.ItemFields.Count, "field")}, {Count(config.Actions?.Count ?? 0, "action")}";
        return config.Problems.Count == 0 ? said : $"{said}. {string.Join(". ", config.Problems.Select(problem => char.ToUpperInvariant(problem[0]) + problem[1..]))}";
    }

    /// <summary>The Field menu's Edit the list's configuration: of the list whose details, or whose item, the detail pane shows.</summary>
    private void EditConfigurationOfShown()
    {
        if (ShownDetail.Source is not FileSource source || Path.GetDirectoryName(source.Path) is not { } folder)
        {
            Refuse("Select a list or an item first");
            return;
        }

        EditConfiguration(folder, Path.GetFileName(folder));
    }

    /// <summary>Ctrl+E: the selection's file or folder in axFile; the folder shown when nothing is selected.</summary>
    private void ShowInAxFile() =>
        WindowMenu.Start(Dispatch.AxFile, SelectedRow() switch
        {
            { Tag: ProjectSectionTag header } => header.Root,
            { Tag: SmartList { BuiltIn: true } or SectionTag or UnreadableTag { Path: null } } => TopFolder,
            { } row => row.Key,
            null => _smart is null ? _folder : TopFolder,
        });

    private void ChooseTopFolder()
    {
        if (ChooseFolder("Choose the top folder of lists", Directory.Exists(TopFolder) ? TopFolder : null) is not { } folder)
        {
            return;
        }

        OpenTopFolder(folder);
    }

    /// <summary>Session, Recent top folders (TK-4.5a): the folders used lately but the one in use, as "1. Tasks (path)".</summary>
    private void FillRecentFolders() =>
        RecentMenu.FillFolders(_recentFoldersItem,
            _settings.RecentFolders.Where(folder => !folder.Equals(Path.TrimEndingDirectorySeparator(_settings.TopFolder), StringComparison.OrdinalIgnoreCase)),
            "&No recent top folders", CommandMark.Act, OpenTopFolder);

    /// <summary>
    /// Another top folder, chosen in the picker or among the recent ones (TK-4.5a): the one left and the one opened go to
    /// the front of the recent list, then the top level of the new one is shown with its undo emptied.
    /// </summary>
    private void OpenTopFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Announce("No folder is there any more: " + folder, true);
            return;
        }

        _settings.RememberFolder(_settings.TopFolder);
        _settings.RememberFolder(folder);
        _settings.Folder = folder;
        _settings.LastList = null;
        SaveSettings();
        _undo.Clear();
        _smart = null;
        _search = null;
        _project = null;
        _projectName = null;
        _folder = TopFolder;
        Fill(select: null);
        Announce("Folder " + PathName.Of(folder) + ". " + Navigator.AccessibleName, true);
    }

    // ---- the change itself ----

    /// <summary>A change to the detail pane's document (an item or a list's details), written to its file.</summary>
    protected override void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField)
    {
        if (detail.Source is not FileSource source || detail.Document is not { } shown)
        {
            return;
        }

        if (source.Item is { } item && !Changeable(item))
        {
            return;
        }

        if (!CheckFresh(source.Path, source.Text))
        {
            return;
        }

        string text;
        if (source.Item is { } changedItem)
        {
            // An item: only the values that changed are written into its file (AF-11.3).
            text = changedItem.TextWith(changed);
        }
        else if (source.List is { } list)
        {
            // A list's details: each one that changed, into its configuration; the rest of the file as it was.
            foreach (var field in changed.Fields)
            {
                var before = shown.IndexOf(field.Title);
                if (before < 0 || !JsonNode.DeepEquals(shown.Fields[before].Value, field.Value))
                {
                    list = list.WithDetail(field.Title, field.Value);
                }
            }

            text = list.ToText();
        }
        else
        {
            return;
        }

        if (Commit(new Step([FileChange.Write(source.Path, source.Text, text)], said), SelectedKey()))
        {
            SelectField(selectField);
        }
    }

    /// <summary>IA-5.5: the file still as it was shown; otherwise the panes read again, that said, and false.</summary>
    private bool CheckFresh(string path, string? shownText)
    {
        string? now;
        try
        {
            now = Changes.ReadOrNull(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{Path.GetFileNameWithoutExtension(path)} could not be read: {ex.Message}");
            return false;
        }

        if (now == shownText)
        {
            return true;
        }

        // A project's item shown in a smart list is read again too, not only the look's copy (TK-5.6).
        PatchProjectItems([FileChange.Write(path, shownText, now ?? string.Empty)]);
        Fill(SelectedKey());
        Announce("The item changed on disk; it has been read again", true);
        return false;
    }

    /// <summary>The one way the files change: the step applied, kept for Ctrl+Z, the panes filled on <paramref name="select"/>, its words said. False after the error notice.</summary>
    private bool Commit(Step step, string? select)
    {
        try
        {
            Changes.Apply(step.Parts);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{step.Said} could not be done: {ex.Message}");
            Refresh(announce: false);
            return false;
        }

        return Committed(step, select);
    }

    /// <summary>A step that is on disk: kept for Ctrl+Z, the panes filled on <paramref name="select"/>, its words said.</summary>
    private bool Committed(Step step, string? select)
    {
        Log.Info("A change written");
        PatchProjectItems(step.Parts);
        RecordHistory(step.Parts);
        _undo.Add(step);

        Fill(select);
        Announce(step.Said, true);
        return true;
    }

    /// <summary>
    /// Several moves as one step (TK-4.7): each move's parts are made from the disk as the moves before it left it
    /// and applied at once, so that an item that links to two of the moved items is written right; all the parts
    /// are one step for Ctrl+Z. A failure takes back what was applied, then says so. False after the error notice.
    /// </summary>
    private bool CommitMoves(IReadOnlyList<Func<List<FileChange>>> moves, string said, string? select)
    {
        var applied = new List<FileChange>();
        try
        {
            foreach (var move in moves)
            {
                foreach (var part in move())
                {
                    Changes.Apply([part]);
                    applied.Add(part);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            var back = string.Empty;
            try
            {
                Changes.Undo(new Step(applied, said));
            }
            catch (Exception undo) when (undo is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                back = $" What was done before it could not all be taken back: {undo.Message}";
            }

            PatchProjectItems(applied);
            Fail($"{said} could not be done: {ex.Message}{back}");
            Refresh(announce: false);
            return false;
        }

        return Committed(new Step(applied, said), select);
    }

    private bool IsArchiveFolder(string path) => path.Equals(ItemFolder.ArchiveFolder(TopFolder), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The folder of lists a file lives in: a project's, for one of its items gathered into the user's smart lists
    /// (TK-5.6), so that its links and its Archive are its own; the folder shown as the top otherwise.
    /// </summary>
    private string TopOf(string path) =>
        _project is null && _projects?.FirstOrDefault(project => Projects.Inside(path, project.Path)) is { } owner ? owner.Path : TopFolder;
}

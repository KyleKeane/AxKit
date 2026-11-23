using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Forms;
using AxTemplate.Core;

namespace AxTemplate;

/// <summary>
/// axTemplate's browser (SPEC.md TP-8): the templates as one level of the bundle's item window
/// (<see cref="ItemForm"/>), each row a template's name, and the pane of its fields beside it: Title, Text and the
/// checks of the places it also shows in. Everything of the navigator (the keys, the selection, filter and sort,
/// the Context menu, the form of a field) is the item window's; this class reads the folder
/// (<see cref="TemplateStore"/>) and writes the changes, each one a <see cref="Step"/> that Ctrl+Z takes back.
/// Making, renaming, changing and deleting a template are never run for an AI tool (TP-D7): their menu items are
/// withheld from the AI tools, and a key is never theirs.
/// </summary>
public sealed class BrowserForm : ItemForm
{
    /// <summary>How many changes Ctrl+Z can take back, the oldest dropped.</summary>
    private const int MaxUndo = 50;

    private readonly string _folder;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _listMenu = new("&List");
    private readonly ToolStripMenuItem _templateMenu = new("&Template");
    private readonly ToolStripMenuItem _fieldMenu = new("Fiel&d");
    private readonly List<Step> _undo = [];

    private ListConfig _config = TemplateStore.Configuration(null);
    private IReadOnlyList<TemplateFile> _files = [];

    /// <summary>Whether the folder has been read once: until then the empty list says that it is being read.</summary>
    private bool _read;

    public BrowserForm(TemplateSettings settings, string? settingsError)
        : base(AxTemplateApp.Name, settings, TemplateSettings.DefaultPath, settingsError)
    {
        _folder = AppPaths.TemplatesRoot;

        // ---- menus and keys: one command list (docs/axKit/app-kit.md AK-2), each key from the tables (TP-7.1) ----
        // The shared keys (ItemKeys) are ItemForm's commands, registered after these by PlaceControls; the items here
        // for them only show them. F5 is this window's own: the folder is read again off the UI thread (TP-8.5).
        var session = new ToolStripMenuItem("&Session");
        Add(session, new Command("&Refresh", ItemKeys.Refresh, () => _ = ReadFolderAsync(SelectedKey(), announce: true)) { When = NotRenaming, Mark = CommandMark.Read });
        session.DropDownItems.Add(CreateShowInMenu("Show the templates' folder &in",
            Item("File &Explorer", () => OpenInExplorer(_folder), Keys.None, CommandMark.Open), () => _folder, FolderMissing));
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(CreateExitItem());

        var actions = new ToolStripMenuItem("");
        actions.DropDownItems.Add(CommandPalette.NeverForAiTools(Item("&New template...", NewItem, ItemKeys.New, CommandMark.Change)));
        actions.DropDownItems.Add(CommandPalette.NeverForAiTools(Item("&Undo", Undo, ItemKeys.Undo, CommandMark.Change)));
        actions.DropDownItems.Add(new ToolStripSeparator());
        actions.DropDownItems.Add(Item("Select &all", SelectAllRows, SelectionKeys.SelectAll, CommandMark.Change));
        actions.DropDownItems.Add(Item("&Deselect all", SelectNoRows, SelectionKeys.SelectNone, CommandMark.Change));


        _templateMenu.DropDownItems.Add(Item("&Open its form", OpenFormOfSelected, ItemKeys.Open, CommandMark.Open));
        Add(_templateMenu, new Command("&Copy its text", BundleKeys.MainAction, CopySelected) { When = NotRenaming, Mark = CommandMark.Read });
        Add(_templateMenu, new Command("Open its file in ax&JSON", AxTemplateKeys.OpenInJson, OpenInJson) { When = NotRenaming, Mark = CommandMark.Open });
        _templateMenu.DropDownItems.Add(Item("Read in &full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));
        _templateMenu.DropDownItems.Add(new ToolStripSeparator());
        _templateMenu.DropDownItems.Add(CommandPalette.NeverForAiTools(Item("&Rename", RenameSelected, ItemKeys.Rename, CommandMark.Change)));
        _templateMenu.DropDownItems.Add(CommandPalette.NeverForAiTools(Item("&Delete", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Act)));
        _templateMenu.DropDownItems.Add(new ToolStripSeparator());
        _templateMenu.DropDownItems.Add(Item("Se&lect or deselect", SelectRow, SelectionKeys.Select, CommandMark.Change));
        _templateMenu.DropDownItems.Add(Item("Select to &here", SelectToHere, SelectionKeys.SelectTo, CommandMark.Change));

        _fieldMenu.DropDownItems.Add(CommandPalette.NeverForAiTools(Item("&Change...", OpenFormAtField, ItemKeys.Open, CommandMark.Change)));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "axTemplate &resources",
            [("The &marks of a template", "https://github.com/KyleKeane/AxKit/blob/main/docs/axTemplate/user-guide.md")]);

        PlaceMenuBar(_menu, session, actions, new ToolStripMenuItem(), CreateContextMenu(_listMenu, _templateMenu, _fieldMenu), options, help);
        PlaceControls(_menu);
        Fill(select: null);
        _ = ReadFolderAsync(select: null, announce: false);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override IEnumerable<string> DiagnosticsLines => [$"Templates: {_files.Count(file => file.Item is not null)}, files that are none: {_files.Count(file => file.Item is null)}"];

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxTemplate];

    /// <summary>TP-8.5: back in this window, the folder is read again, and shown again only when it changed (an edit in axJSON, the launcher's nothing).</summary>
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (_read && Working is null && !NoticeOpen && NotRenaming())
        {
            _ = ReadFolderAsync(SelectedKey(), announce: false, onlyWhenChanged: true);
        }
    }

    // ---- the folder (TP-2.5, TP-8.5) ----

    /// <summary>
    /// The folder made ready and read off the UI thread, then shown with the selection on <paramref name="select"/>.
    /// The navigator's read (<see cref="ItemForm.BeginRead"/>): a later read wins over one still under way.
    /// </summary>
    private async Task ReadFolderAsync(string? select, bool announce, bool onlyWhenChanged = false)
    {
        var read = BeginRead();
        TemplateList list;
        try
        {
            var folder = _folder;
            list = await Task.Run(() => TemplateStore.Open(folder));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (EndRead(read))
            {
                _read = true;
                Fill(select);
                Failed("The folder could not be read", ex, "The templates' folder could not be read: " + ex.Message);
            }

            return;
        }

        if (!EndRead(read))
        {
            return;
        }

        var same = _read && list.Config.ToText() == _config.ToText()
            && list.Files.Select(file => (file.Path, file.Text)).SequenceEqual(_files.Select(file => (file.Path, file.Text)));
        (_config, _files, _read) = (list.Config, list.Files, true);
        if (onlyWhenChanged && same)
        {
            // Nothing changed: the list is left as it is, so that nothing is read out again on coming back.
            return;
        }

        Fill(select);
        if (announce)
        {
            Announce("Refreshed: " + Navigator.AccessibleName, true);
        }
    }

    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel()
    {
        var rows = _files
            .Select(file => new ItemRow(false, file.Path, file.Title, file.Item is null ? file.Title + ", not a template" : file.Title, file))
            .ToList();
        var count = _files.Count(file => file.Item is not null);
        return (rows, $"Templates, {count} {(count == 1 ? "template" : "templates")}", "Templates", _folder);
    }

    protected override string? EmptyLine => _read ? $"No templates yet: {BundleKeys.Text(ItemKeys.New)} makes one" : "Reading the templates";

    /// <summary>What a file that is no template is said to be, its key from the table.</summary>
    private static string NotATemplate => $"Not a template: {BundleKeys.Text(AxTemplateKeys.OpenInJson)} opens the file in axJSON to mend";

    /// <summary>One level without sections: Ctrl+Shift+N, Ctrl+Up and Ctrl+Down have nothing to do here, and are no commands of this window.</summary>
    protected override IReadOnlyCollection<Keys> KeysWithoutUse => AxTemplateKeys.WithoutUse;

    protected override void NewList()
    {
    }

    protected override bool HasSelection => true;

    protected override string RowNoun => "template";

    // ---- the fields (TP-8.3) ----

    protected override ItemDetail DetailOf(ItemRow? row)
    {
        switch (row)
        {
            case { Tag: TemplateFile { Item: { } item } file }:
                var shown = ItemView.Of(item, _config);
                return new ItemDetail("Fields of " + row.Name, file.Path, ItemView.Document(shown),
                    shown.KeptLine is { } kept ? [new ItemDetailLine(kept)] : [], file);
            case { Tag: TemplateFile file }:
                return new ItemDetail("Details of " + row.Name, file.Path, null,
                [
                    new ItemDetailLine(row.Label),
                    new ItemDetailLine(file.Text is null
                        ? "The file could not be read: F5 reads the folder again"
                        : $"The file is JSON that is not an item, or not JSON: {BundleKeys.Text(AxTemplateKeys.OpenInJson)} in the navigator opens it in axJSON to mend"),
                ]);
            default:
                return new ItemDetail("Details, nothing selected", null, null,
                    [new ItemDetailLine(_read ? (_files.Count == 0 ? $"No templates yet: {BundleKeys.Text(ItemKeys.New)} in the navigator makes one" :"Nothing selected") : "Reading the templates")]);
        }
    }

    /// <summary>A field of the pane changed (not the Title, which is a rename): only what changed is written into the item's file.</summary>
    protected override void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField)
    {
        if (detail.Source is not TemplateFile { Item: { } item, Text: { } text } file || detail.Document is not { } shown || !Fresh(file))
        {
            return;
        }

        var after = ItemView.Changed(item, shown, changed, _config).ToText();
        if (Commit(new Step([FileChange.Write(file.Path, text, after)], said), file.Path))
        {
            SelectField(selectField);
        }
    }

    // ---- new, rename, delete, undo (TP-8.4) ----

    /// <summary>
    /// Ctrl+N (TP-8.4; the maintainer, 2026-11-23: "adding a new template should open the full form rather than the
    /// smaller naming dialog"): the template's form, empty, the item window's own; Add makes the template with
    /// everything typed in it, a name another template has refused in the form.
    /// </summary>
    protected override void NewItem()
    {
        var fresh = FlatItem.New(string.Empty, _config);
        var shown = ItemView.Document(ItemView.Of(fresh, _config));
        OpenItemForm(new ItemFormSource("the new template", shown, (changed, _) => AddTemplate(fresh, shown, changed))
        {
            Title = "New template",
            Button = "&Add",
            Refuse = changed => TemplateStore.NameRefused(changed.Title, Names()) is { } why ? (ItemDocument.TitleField, why) : null,
            Note = "The template shows in the launcher once it is added. The marks {caret}, {selection} and {clipboard} in the Text work as the guide says.",
        }, null);
    }

    private void AddTemplate(FlatItem fresh, ItemDocument shown, ItemDocument changed)
    {
        var title = changed.Title.Trim();
        var path = ItemFiles.FreePath(_folder, ItemFiles.FileStem(title), ".json");
        Commit(new Step([FileChange.Write(path, null, ItemView.Changed(fresh, shown, changed, _config).ToText())], "Added " + title), path);
    }

    protected override string? RenameRefused(ItemRow row) =>
        row.Tag is TemplateFile { Item: not null } ? null : NotATemplate;

    protected override string? RefusedName(ItemRow row, string name) => TemplateStore.NameRefused(name, Names(), own: row.Name);

    /// <summary>F2, and the Title field of the pane: the name written into the item, and the file renamed with it.</summary>
    protected override void RenameItem(ItemRow row, string title)
    {
        if (row.Tag is not TemplateFile { Item: { } item, Text: { } text } file)
        {
            return;
        }

        var name = title.Trim();
        if (TemplateStore.NameRefused(name, Names(), own: row.Name) is { } why)
        {
            Announce(why, true);
            return;
        }

        if (!Fresh(file))
        {
            return;
        }

        var parts = new List<FileChange> { FileChange.Write(file.Path, text, item.With(ItemDocument.TitleField, JsonValue.Create(name), _config).ToText()) };
        var to = ItemFiles.FreePath(_folder, ItemFiles.FileStem(name), ".json", own: file.Path);
        // A change of capitals alone keeps the file: Windows holds both names for the same one.
        if (!string.Equals(to, file.Path, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(FileChange.Move(file.Path, to));
        }
        else
        {
            to = file.Path;
        }

        Commit(new Step(parts, "Renamed " + name), to);
    }

    /// <summary>Delete: the selected templates, or the one the navigator is on, after a question; Ctrl+Z brings them back.</summary>
    protected override void DeleteRow(ItemRow row)
    {
        var rows = SelectedRows() is { Count: > 0 } selected ? selected : [row];
        var files = rows.Select(one => one.Tag).OfType<TemplateFile>().ToList();
        if (files.FirstOrDefault(file => file.Text is null) is { } unread)
        {
            Announce($"{unread.Title} could not be read, so {BundleKeys.Text(ItemKeys.Undo)} could not bring it back: delete its file in axFile", true);
            return;
        }

        var what = files.Count == 1 ? files[0].Title : $"{files.Count} templates";
        ShowNotice(Notice.Ask($"Delete {what}?", $"{BundleKeys.Text(ItemKeys.Undo)} brings back what was deleted, for as long as this window is open.", "&Delete", () =>
        {
            if (!files.All(Fresh))
            {
                return;
            }

            // The selection lands on the row after the last one deleted, or the one before the first.
            var gone = rows.Select(one => one.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var last = Rows.ToList().FindLastIndex(one => gone.Contains(one.Key));
            var next = Rows.Skip(last + 1).FirstOrDefault() ?? Rows.LastOrDefault(one => !gone.Contains(one.Key));
            Commit(new Step([.. files.Select(file => new FileChange(file.Path, file.Text, null))], "Deleted " + what), next?.Key);
        }));
    }

    protected override void Undo()
    {
        if (_undo.Count == 0)
        {
            Announce("Nothing to undo", true);
            return;
        }

        var step = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        try
        {
            if (Changes.CheckUndo(step) is { } why)
            {
                Announce($"Cannot undo {step.Said}: {why}", true);
                return;
            }

            Changes.Undo(step);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Failed("A change could not be undone", ex, $"{step.Said} could not be undone: {ex.Message}");
            _ = ReadFolderAsync(SelectedKey(), announce: false);
            return;
        }

        Log.Info("Undone a step");
        _files = TemplateStore.After(_files, step.Parts, undone: true);
        // The selection on what the step touched first, where it is again.
        Fill(step.Parts[0].Path);
        Announce("Undone: " + step.Said, true);
    }

    // ---- the template's own commands ----

    /// <summary>Ctrl+Enter (TP-8.4): the template's text on the clipboard with every mark filled with nothing, for a look at what it makes.</summary>
    private void CopySelected() => WithTemplate(file =>
    {
        var made = TextTemplate.Expand(file.Item!.Text(TemplateStore.TextField), string.Empty, string.Empty).Text;
        if (made.Length == 0)
        {
            // The clipboard takes no empty text; a new template has none yet.
            Announce(TemplateStore.NoTextYet(file.Title) + ": Enter opens its form", true);
            return;
        }

        CopyText(made, "Copied");
    });

    /// <summary>Ctrl+J: the template's file in axJSON, where a field is added or a broken file mended.</summary>
    private void OpenInJson() => WithRow(row =>
    {
        if (row.Tag is TemplateFile file)
        {
            WindowMenu.Start(Dispatch.AxJSON, file.Path);
        }
    });

    /// <summary>A command on the template the navigator is on; a file that is none says so.</summary>
    private void WithTemplate(Action<TemplateFile> command) => WithRow(row =>
    {
        if (row.Tag is TemplateFile { Item: not null } file)
        {
            command(file);
        }
        else
        {
            Announce(NotATemplate, true);
        }
    });

    // ---- the template's form (TP-8.3a): the item window's, as axTask's ----

    /// <summary>
    /// A template's form (the maintainer, 2026-11-23: "enter on any template should open a form with all fields"):
    /// the item window's own (ItemForm.Form.cs), the same as axTask's, over the document the pane shows. A new name
    /// that another template has is refused in the form, and a save writes every change as one step, the file renamed
    /// with a new Title, as F2 does.
    /// </summary>
    protected override ItemFormSource? FormOf(ItemRow row)
    {
        if (row.Tag is not TemplateFile { Item: { } item, Text: not null } file)
        {
            return null;
        }

        var shown = ItemView.Document(ItemView.Of(item, _config));
        return new ItemFormSource(file.Title, shown, (changed, said) => SaveTemplateForm(file, shown, changed, said))
        {
            Refuse = changed => TemplateStore.NameRefused(changed.Title.Trim(), Names(), own: file.Title) is { } why ? (ItemDocument.TitleField, why) : null,
            Note = "The marks {caret}, {selection} and {clipboard} in the Text work as the guide says.",
        };
    }

    private void SaveTemplateForm(TemplateFile file, ItemDocument shown, ItemDocument changed, string said)
    {
        if (!Fresh(file))
        {
            return;
        }

        var name = changed.Title.Trim().Length > 0 ? changed.Title.Trim() : file.Title;
        var parts = new List<FileChange> { FileChange.Write(file.Path, file.Text, ItemView.Changed(file.Item!, shown, changed, _config).ToText()) };
        var to = file.Path;
        if (name != file.Title)
        {
            to = ItemFiles.FreePath(_folder, ItemFiles.FileStem(name), ".json", own: file.Path);
            // A change of capitals alone keeps the file: Windows holds both names for the same one.
            if (!string.Equals(to, file.Path, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(FileChange.Move(file.Path, to));
            }
            else
            {
                to = file.Path;
            }
        }

        Commit(new Step(parts, said), to);
    }

    // ---- the changes (IA-5.4, IA-5.5) ----

    private IEnumerable<string> Names() => _files.Where(file => file.Item is not null).Select(file => file.Title);

    /// <summary>
    /// Whether the file is still as it was read (IA-5.5): a change is made only then. When it is not (an edit in
    /// axJSON since), the folder is read again and that is said, and nothing is changed.
    /// </summary>
    private bool Fresh(TemplateFile file)
    {
        string? now;
        try
        {
            now = Changes.ReadOrNull(file.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Failed("A file could not be read", ex, $"{file.Title} could not be read: {ex.Message}");
            return false;
        }

        if (now == file.Text)
        {
            return true;
        }

        Announce("The template changed on disk; it is read again", true);
        _ = ReadFolderAsync(SelectedKey(), announce: false);
        return false;
    }

    /// <summary>
    /// An error the user must see, shown as a notice in full. The log gets <paramref name="logged"/> and the kind of
    /// the failure alone (TP-7.3): the window base's <c>Fail</c> would write the shown words, which name a template
    /// and its file, into the log.
    /// </summary>
    private void Failed(string logged, Exception ex, string shown)
    {
        Log.Error($"{logged}: {ex.GetType().Name}");
        ShowError("Error", shown);
    }

    /// <summary>A step applied, kept for Ctrl+Z, the panes filled on <paramref name="select"/>, its words said. The files are small and on this computer: written here, as axTask's.</summary>
    private bool Commit(Step step, string? select)
    {
        try
        {
            Changes.Apply(step.Parts);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Failed("A change could not be written", ex, $"{step.Said} could not be done: {ex.Message}");
            _ = ReadFolderAsync(SelectedKey(), announce: false);
            return false;
        }

        // TP-7.3: that a template changed, never its name or its text.
        Log.Info("A change written");
        _files = TemplateStore.After(_files, step.Parts);
        _undo.Add(step);
        if (_undo.Count > MaxUndo)
        {
            _undo.RemoveAt(0);
        }

        Fill(select);
        Announce(step.Said, true);
        return true;
    }
}

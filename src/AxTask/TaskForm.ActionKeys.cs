using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The action keys (docs/axKit/reports.md RP-8; SPEC.md TK-7a): Ctrl+1 to Ctrl+9 run the actions a list's
/// <c>list.json</c> assigns (set fields, move, open), and Actions, Action keys sets them. axTask is local only
/// (2026-11-14): nothing here makes a network request; an open of a field's web address hands it to the browser.
/// </summary>
public sealed partial class TaskForm
{
    private readonly ToolStripMenuItem _actionKeysMenu = new("Action &keys");
    private readonly ToolStripMenuItem[] _actionKeyItems = new ToolStripMenuItem[ActionKeys.Last + 1];

    // ---- the list in hand ----

    private ItemDocument? CurrentListDetails => _smart is null && !AtTop && Directory.Exists(_folder) ? ItemFolder.ReadList(_folder) : null;

    /// <summary>The menu of this file, placed by the constructor: the action keys in Context on an item.</summary>
    private void PlaceActionKeyMenu()
    {
        for (var digit = ActionKeys.First; digit <= ActionKeys.Last; digit++)
        {
            var d = digit;
            var item = CommandPalette.Marked(new ToolStripMenuItem("Not set", null, (_, _) => RunActionKey(d)) { ShortcutKeyDisplayString = BundleKeys.Text(AxTaskKeys.ActionKey(d)) }, CommandMark.Act);
            _actionKeyItems[d] = item;
            _actionKeysMenu.DropDownItems.Add(item);
            AddKey(new Command($"Action key {d}", AxTaskKeys.ActionKey(d), () => RunActionKey(d)) { When = NotRenaming, Mark = CommandMark.Act });
        }

        CommandPalette.RefreshedOnOpening(_actionKeysMenu, RefreshActionKeyItems);
    }

    /// <summary>The nine items say the list's actions as the menu opens (RP-8.3), so that the palette and F1 list the user's names.</summary>
    private void RefreshActionKeyItems()
    {
        var keys = ActionKeys.Read(CurrentListDetails);
        for (var digit = ActionKeys.First; digit <= ActionKeys.Last; digit++)
        {
            _actionKeyItems[digit].Text = keys.TryGetValue(digit, out var action) ? action.Name.Replace("&", "&&", StringComparison.Ordinal) : "Not set";
        }
    }

    // ---- Ctrl+1 to Ctrl+9 (RP-8.1) ----

    private void RunActionKey(int digit)
    {
        if (_smart is not null || AtTop)
        {
            EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
            Announce("Action keys act in a list: open one first", true);
            return;
        }

        if (!ActionKeys.Read(CurrentListDetails).TryGetValue(digit, out var action))
        {
            EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
            Announce($"No action on Ctrl+{digit} in this list; Actions, Action keys sets one", true);
            return;
        }

        if (SelectedRow() is not { Tag: ItemFile item })
        {
            EventDoor.Raise(EventKind.Refused);
            Announce("Select an item first", true);
            return;
        }

        if (ActionKeys.AsksFirst(action) && !AcceptedOrAsked(digit, action))
        {
            return;
        }

        if (action.Set is { } set)
        {
            SetFields(item, set, action.Name);
        }
        else if (action.Move is { } move)
        {
            MoveItem(item, move, action.Name);
        }
        else if (action.Open is { } open)
        {
            OpenTarget(item, open);
        }
    }

    /// <summary>The key and action the window last asked about (TK-7a.3), as <see cref="ActionKeys.MarkText"/>; null when none waits for its second press.</summary>
    private string? _askedActionKey;

    /// <summary>
    /// Whether a key that moves or opens may act now (SPEC.md TK-7a.3; bundle repairs.md R-72): yes when this install
    /// wrote the action or the user accepted it before. Otherwise the first press says what the key now does in this
    /// list and does nothing; the same key pressed again, with the list's action still the same, accepts it, for good
    /// where the settings can be saved, and acts. A list whose <c>list.json</c> came from elsewhere or was rewritten
    /// so never moves an item or opens a program or an address on the first press.
    /// </summary>
    private bool AcceptedOrAsked(int digit, ListAction action)
    {
        if (_settings.AcceptsActionKey(SettingsPath, _folder, digit, action))
        {
            return true;
        }

        var asked = ActionKeys.MarkText(_folder, digit, action);
        if (_askedActionKey == asked)
        {
            _askedActionKey = null;
            if (_settings.AcceptActionKey(SettingsPath, _folder, digit, action))
            {
                SaveSettings();
            }

            return true;
        }

        _askedActionKey = asked;
        Announce(ActionKeys.AskText(digit, action), true);
        return false;
    }

    /// <summary>A Set action (RP-8.1): the fields written as one step.</summary>
    private void SetFields(ItemFile item, IReadOnlyDictionary<string, JsonNode?> set, string name)
    {
        if (!CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var changed = ActionKeys.ApplySet(item.Document, set);
        Commit(new Step([FileChange.Write(item.Path, item.Text, changed.ToText())], $"{name}: {ActionKeys.SpokenSet(set)}"), item.Path);
    }

    /// <summary>A Move action (RP-8.1): to the list at the relative path, the links following (IA-D3).</summary>
    private void MoveItem(ItemFile item, string move, string name)
    {
        var target = Path.GetFullPath(Path.Combine(_folder, move));
        var top = TopFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Directory.Exists(target) || !target.StartsWith(top, StringComparison.OrdinalIgnoreCase) || ItemFolder.IsArchive(TopFolder, target) || target.Equals(_folder, StringComparison.OrdinalIgnoreCase))
        {
            Announce($"No list to move to at {move}", true);
            return;
        }

        if (!CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var to = ItemFiles.FreePath(target, Path.GetFileNameWithoutExtension(item.Path), ".json");
        var parts = new List<FileChange> { FileChange.Move(item.Path, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), item.Path, to));
        Commit(new Step(parts, $"{name}: {ItemFolder.NameOf(item)} moved to {Path.GetFileName(target)}"), NeighbourKey());
    }

    /// <summary>An Open action (RP-8.1): an app on the item's file, or a field's web address in the browser.</summary>
    private void OpenTarget(ItemFile item, string open)
    {
        if (Dispatch.Apps.FirstOrDefault(known => known.Verb.Equals(open, StringComparison.OrdinalIgnoreCase)) is { } app)
        {
            // A shell or Claude Code never on a folder off this computer (RP-8.1, S-5's rule; 2026-11-09).
            if (ActionKeys.OpenRefusal(app.Verb, item.Path) is { } refused)
            {
                Announce(refused, true);
                return;
            }

            WindowMenu.Start(app.Verb, item.Path);
            return;
        }

        var index = item.Document.IndexOf(open);
        var value = index >= 0 ? FieldTypes.TextOf(item.Document.Fields[index].Value).Trim() : string.Empty;
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            OpenUrl(value);
            return;
        }

        Announce(index < 0 ? $"No field called {open}, and no app called that" : value.Length == 0 ? $"{open} is empty" : $"{open} holds no web address", true);
    }

    // ---- Actions, Action keys (RP-8.2) ----

    /// <summary>
    /// Actions, Action keys (RP-8.2): one form (bundle AX-8.10): the nine keys as a list, then the name, what the key
    /// does as a list of three, and what it sets, moves to or opens; Keep and Cancel. Moving through the keys fills the
    /// other fields with that key's action. An empty name removes the key. (A list of the keys and then up to three
    /// notices, one after the other, until 2026-11-14.) The rules are <see cref="ActionKeys.FromForm"/>, tested.
    /// </summary>
    private void EditActionKeys()
    {
        if (_smart is not null || AtTop)
        {
            Announce("Action keys belong to a list: open one first", true);
            return;
        }

        var folder = _folder;
        var keys = ActionKeys.Read(ItemFolder.ReadList(folder));
        var lines = Enumerable.Range(ActionKeys.First, ActionKeys.Last).Select(digit => ActionKeys.Spoken(digit, keys.GetValueOrDefault(digit))).ToList();
        var form = new FormControls();
        var key = form.AddList("Choose the key", lines, 0);
        var name = form.AddText("Type the action's name, or leave it empty to remove the key");
        var kind = form.AddList("Choose what the key does", ActionKeys.KindNames, 0);
        var value = form.AddLines("Type what it sets, one field per line as Field: value; or the list it moves to; or the field or app it opens", rows: 3);
        int Digit() => Math.Max(0, key.SelectedIndex) + ActionKeys.First;

        // The fields hold the action of the key the list is on: filled at first and whenever the list moves.
        void Fill()
        {
            var current = keys.GetValueOrDefault(Digit());
            name.Text = current?.Name ?? string.Empty;
            kind.SelectedIndex = ActionKeys.KindIndex(current);
            value.Text = ActionKeys.ValueText(current).ReplaceLineEndings("\r\n");
        }

        (ListAction? Action, string? Refused, bool InValue) Typed() =>
            ActionKeys.FromForm(name.Text, kind.SelectedIndex, value.Text, list => Directory.Exists(Path.Combine(TopFolder, list)));

        Fill();
        key.SelectedIndexChanged += (_, _) => Fill();
        ShowForm("Action keys",
            $"The Ctrl+number keys of {ListTitle()}. Up and Down choose a key, and the fields after it show what it does. Tab goes on to the action's name, as Context will show it on an item, to what the key does, and to its fields as Field: value lines, the list it moves to, or the field or app it opens. In that last box Enter makes a new line. Ctrl+Enter or the Keep button keeps the key. An empty name removes it. Escape cancels.",
            form, "&Keep",
            () => WriteActionKey(folder, Digit(), Typed().Action),
            () => Typed() is { Refused: { } refused } typed ? (typed.InValue ? value : name, refused) : null);
    }

    /// <summary>The list's details written with the key set or removed, one undo step.</summary>
    private void WriteActionKey(string folder, int digit, ListAction? action)
    {
        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        var before = Changes.ReadOrNull(listFile);
        var details = ItemFolder.ReadList(folder) ?? ItemDocument.NewList(TaskRules.TemplateOf(null));
        var changed = ActionKeys.With(details, digit, action);
        // TK-7a.3: an action written here is this install's own and is not asked about.
        if (action is not null && ActionKeys.AsksFirst(action) && _settings.AcceptActionKey(SettingsPath, folder, digit, action))
        {
            SaveSettings();
        }

        Commit(new Step([FileChange.Write(listFile, before, changed.ToText())], action is null ? $"Ctrl+{digit} cleared" : $"Ctrl+{digit}: {action.Name}, {action.Described}"), SelectedKey());
    }
}

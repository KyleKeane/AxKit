using System.Text.Json.Nodes;
using AxKit.Core;
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
            Sounds.Edge();
            Announce("Action keys act in a list: open one first", true);
            return;
        }

        if (!ActionKeys.Read(CurrentListDetails).TryGetValue(digit, out var action))
        {
            Sounds.Edge();
            Announce($"No action on Ctrl+{digit} in this list; Actions, Action keys sets one", true);
            return;
        }

        if (SelectedRow() is not { Tag: ItemFile item })
        {
            Sounds.Edge();
            Announce("Select an item first", true);
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
        ChooseFromList("Action keys", $"The Ctrl+number keys of {ListTitle()}: Enter on one sets or changes it.", "Keys", lines, 0, chosen => EditActionKey(folder, chosen + ActionKeys.First));
    }

    private void EditActionKey(string folder, int digit)
    {
        var current = ActionKeys.Read(ItemFolder.ReadList(folder)).GetValueOrDefault(digit);
        AskText($"Ctrl+{digit}", "The action's name, as Context will show it on an item. Empty removes the key.", "Name:", "Name", current?.Name ?? string.Empty, "&Next", typed =>
        {
            var name = typed.Trim();
            if (name.Length == 0)
            {
                WriteActionKey(folder, digit, null);
                return;
            }

            var kinds = new[] { "Set fields to values", "Move the item to a list", "Open a field's web address, or an app" };
            var start = current?.Kind switch { ListAction.MoveKind => 1, ListAction.OpenKind => 2, _ => 0 };
            ChooseFromList($"Ctrl+{digit}: {name}", "What the key does.", "What", kinds, start, kind =>
            {
                switch (kind)
                {
                    case 0:
                        AskText($"Ctrl+{digit}: {name} sets", "One field per line, as Field: value. Done: done or not done for a check; Kind: Bug.", "Fields:", "Fields",
                            current?.Set is { } set ? string.Join("\r\n", set.Select(pair => $"{pair.Key}: {FieldTypes.TextOf(pair.Value)}")) : string.Empty, "&Keep",
                            text => WriteActionKey(folder, digit, new ListAction(name, Set: ActionKeys.ParseSetLines(text))),
                            text => ActionKeys.ParseSetLines(text) is null ? "Type at least one line as Field: value" : null);
                        break;
                    case 1:
                        AskText($"Ctrl+{digit}: {name} moves to", "The list's name, a list in the top folder.", "List:", "List", current?.Move?.Replace("../", string.Empty, StringComparison.Ordinal) ?? string.Empty, "&Keep",
                            text => WriteActionKey(folder, digit, new ListAction(name, Move: "../" + text.Trim())),
                            text => Directory.Exists(Path.Combine(TopFolder, text.Trim())) ? null : $"No list called {text.Trim()} in the top folder");
                        break;
                    default:
                        AskText($"Ctrl+{digit}: {name} opens", "A field whose value is a web address, such as Issue, or an app for the item's file: json or file.", "Open:", "Open", current?.Open ?? string.Empty, "&Keep",
                            text => WriteActionKey(folder, digit, new ListAction(name, Open: text.Trim())),
                            text => text.Trim().Length == 0 ? "Type a field's title or an app" : null);
                        break;
                }
            });
        }, _ => null, required: false);
    }

    /// <summary>The list's details written with the key set or removed, one undo step.</summary>
    private void WriteActionKey(string folder, int digit, ListAction? action)
    {
        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        var before = Changes.ReadOrNull(listFile);
        var details = ItemFolder.ReadList(folder) ?? ItemDocument.NewList(TaskRules.TemplateOf(null));
        var changed = ActionKeys.With(details, digit, action);
        Commit(new Step([FileChange.Write(listFile, before, changed.ToText())], action is null ? $"Ctrl+{digit} cleared" : $"Ctrl+{digit}: {action.Name}, {action.Described}"), SelectedKey());
    }
}

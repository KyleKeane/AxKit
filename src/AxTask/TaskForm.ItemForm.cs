using AxKit.Core;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The item's form (docs/axTask/forms-framework.md AF-14.1, AF-14.2; the maintainer's message 8 of 2026-11-21: "the
/// item details for a task into a standard form that opens on enter"). Enter on an item opens one form with a
/// standard control for each field of its list's configuration, in its order: Tab between them, Ctrl+Enter saves
/// them all as one step for Ctrl+Z, Escape leaves and nothing is changed. Enter on a field in the detail pane opens
/// the same form with the focus in that field. The detail pane stays for reading, with its one-key changes (Space,
/// Right and Left). The rules, which control a field gets and what its value makes of the item, are
/// <c>AxTask.Core/ItemFormRules</c>; this file makes the controls and writes the change.
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>The item the form is for, from the navigator's selection: one that can be changed, outside the search's level (where an item is changed in its fields, and renamed only where it lives, TK-7b.4).</summary>
    private ItemFile? FormItem() =>
        _search is null && SelectedRow() is { Tag: ItemFile { ConfigProblem: null } item } ? item : null;

    private bool OnFormItem() => InNavigator() && NotRenaming() && FormItem() is not null;

    /// <summary>Whether Enter in the detail pane is on a field of such an item: any other line keeps its own Enter (the kept values, a list's details).</summary>
    private bool OnFormField() =>
        DetailPane.Focused && NotRenaming() && FormItem() is { } item && ShownDetail.Source is FileSource { Item: { } shown } && ReferenceEquals(shown, item) && SelectedField() is not null;

    /// <summary>Enter on an item in the navigator, and the Item menu's Open its form: the form, the focus in its first field; on anything else, what Enter does there.</summary>
    private void OpenFormOfSelected()
    {
        if (FormItem() is { } item)
        {
            OpenItemForm(item, null);
        }
        else
        {
            OpenSelected();
        }
    }

    /// <summary>Enter on a field in the detail pane, and the Field menu's Change: the form, the focus in that field; on any other line, the line's own Enter.</summary>
    private void OpenFormAtField()
    {
        if (FormItem() is { } item && ShownDetail.Source is FileSource { Item: { } shown } && ReferenceEquals(shown, item) && SelectedField() is { } field)
        {
            OpenItemForm(item, field);
        }
        else
        {
            ChangeField();
        }
    }

    private void OpenItemForm(ItemFile item, int? startField)
    {
        var name = ItemFolder.NameOf(item);
        var top = TopOf(item.Path);
        var controls = ItemFormRules.Controls(item.Document, link => ItemFolder.Resolve(item.Path, link) is { } path && ItemFolder.ReadItem(path) is { } linked ? ItemFolder.NameOf(linked) : string.Empty);
        var form = new FormControls();
        var made = new List<Control>();
        foreach (var control in controls)
        {
            switch (control.Kind)
            {
                case ItemFormRules.CheckKind:
                    var box = form.AddCheck(control.Name);
                    box.Checked = control.Checked;
                    made.Add(box);
                    break;
                case ItemFormRules.ListKind:
                    made.Add(form.AddList(control.Name, control.Options, control.Selected));
                    break;
                case ItemFormRules.LinesKind:
                    made.Add(form.AddLines(control.Name, rows: 4, control.Text));
                    break;
                default:
                    made.Add(form.AddText(control.Name, control.Text));
                    break;
            }
        }

        if (startField is { } start && controls.ToList().FindIndex(control => control.Field == start) is var at and > 0)
        {
            form.StartIn(made[at]);
        }

        // What the controls hold now, as the rules take it.
        List<FormValue> Values() => [.. made.Select(control => control switch
        {
            AxKitCheckBox check => new FormValue(string.Empty, check.Checked, 0),
            AxKitList list => new FormValue(string.Empty, false, list.SelectedIndex),
            _ => new FormValue(control.Text, false, 0),
        })];

        // A link is typed as the other item's title, looked for among the items of the item's own folder of lists.
        (string? Path, string? Refused) Link(string title)
        {
            var matches = ItemFolder.AllItems(top).Where(other => !other.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase)
                && ItemFolder.NameOf(other).Equals(title, StringComparison.OrdinalIgnoreCase)).ToList();
            return matches.Count switch
            {
                0 => (null, $"No item is called {title}"),
                1 => (ItemFolder.LinkTo(item.Path, matches[0].Path), null),
                _ => (null, $"{matches.Count} items are called {title}: rename one first"),
            };
        }

        (ItemDocument? Changed, int Control, string? Refused) Typed() => ItemFormRules.Apply(item.Document, controls, Values(), Today, Link);

        var text = $"The fields of {name}, in the order of its list. Tab moves from one to the next. Ctrl+Enter or the Save button saves them all as one step, which Ctrl+Z takes back. Escape leaves, and nothing is changed."
            + (form.EnterMakesNewLine ? " In a box of several lines Enter makes a new line." : string.Empty)
            + (item.KeptLine is { } kept ? $" {kept}: they are in the item's file and no field of its list." : string.Empty);
        ShowForm("Fields of " + name, text, form, "&Save",
            () => SaveItemForm(item, Typed().Changed),
            () => Typed() is { Refused: { } refused } typed ? (made[typed.Control], refused) : null);
    }

    /// <summary>
    /// The form saved: every field that changed, written as one step. A changed Title renames the item's file with
    /// it, the links to it following, as F2 does (IA-4.5, IA-D3).
    /// </summary>
    private void SaveItemForm(ItemFile item, ItemDocument? changed)
    {
        if (changed is null)
        {
            return;
        }

        var said = ItemFormRules.Said(item.Document, changed, field => SpokenValue(SelectedRow(), field));
        if (ReferenceEquals(changed, item.Document))
        {
            Announce(said, true);
            return;
        }

        if (!Changeable(item) || !CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var (parts, path) = ItemChangeParts(item, changed);
        Commit(new Step(parts, said), path);
    }

    /// <summary>
    /// The writes of a change to an item: its file with what changed, and, when the Title changed, the file renamed
    /// after it with every link to it written again (IA-4.5, IA-D3); and the item's path after them.
    /// </summary>
    private (List<FileChange> Parts, string Path) ItemChangeParts(ItemFile item, ItemDocument changed)
    {
        var parts = new List<FileChange> { FileChange.Write(item.Path, item.Text, item.TextWith(changed)) };
        var to = item.Path;
        if (changed.Title != item.Document.Title)
        {
            to = ItemFiles.FreePath(Path.GetDirectoryName(item.Path)!, ItemFiles.FileStem(changed.Title), ".json", own: item.Path);
            if (!to.Equals(item.Path, StringComparison.Ordinal))
            {
                parts.Add(FileChange.Move(item.Path, to));
                parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopOf(item.Path)), item.Path, to, skip: item.Path));
            }
        }

        return (parts, to);
    }
}

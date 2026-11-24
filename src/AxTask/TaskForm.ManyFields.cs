using AxKit.Core;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// Set fields and Clear fields on the selected items, or the current one when none is selected (SPEC.md TK-4.7a; the
/// maintainer's choice of 2026-11-24). The rules are <see cref="ManyFields"/>; each command writes through
/// <see cref="SetFields"/>, so all the items change as one step for Ctrl+Z.
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>
    /// The items the two commands act on, all of one list, since the form is that list's fields; null, with the
    /// reason said, when there are none, when they are of several lists, or when one cannot be changed.
    /// </summary>
    private List<ItemFile>? ItemsForFields()
    {
        var items = Targets();
        if (items.Count == 0)
        {
            Refuse("Choose an item, or select items with Space, first");
            return null;
        }

        if (items.Select(item => Path.GetDirectoryName(item.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            Refuse("The selected items are in several lists: select items of one list, whose fields the form shows");
            return null;
        }

        return items.All(Changeable) ? items : null;
    }

    /// <summary>Item → Set fields…: the form of the list's fields, every one starting as "leave"; what is filled in is written into every item.</summary>
    private void SetFieldsOfItems()
    {
        if (ItemsForFields() is not { } items)
        {
            return;
        }

        var first = items[0];
        var controls = ManyFields.Controls(first.Document);
        if (controls.Count == 0)
        {
            Refuse("This list has no fields but the title");
            return;
        }

        var top = TopOf(first.Path);
        (string? Path, string? Refused) Link(string title)
        {
            var matches = ItemFolder.AllItems(top).Where(other => ItemFolder.NameOf(other).Equals(title, StringComparison.OrdinalIgnoreCase)).ToList();
            return matches.Count switch
            {
                0 => (null, $"No item is called {title}"),
                1 => (ItemFolder.LinkTo(first.Path, matches[0].Path), null),
                _ => (null, $"{matches.Count} items are called {title}: rename one first"),
            };
        }

        var form = new FormControls();
        var made = new List<Control>();
        var values = new List<Func<FormValue>>();
        foreach (var control in controls)
        {
            Control added = control.Kind switch
            {
                ItemFormRules.ListKind => form.AddList(control.Name, control.Options, control.Selected),
                ItemFormRules.LinesKind => form.AddLines(control.Name, rows: 4),
                _ => form.AddText(control.Name),
            };
            made.Add(added);
            values.Add(added is AxKitList list ? () => new FormValue(string.Empty, false, list.SelectedIndex) : () => new FormValue(added.Text, false, 0));
        }

        (IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode?>? Set, int Control, string? Refused) Typed() =>
            ManyFields.Set(first.Document, controls, [.. values.Select(value => value())], Today, Link);

        var what = items.Count == 1 ? ItemFolder.NameOf(first) : ManyItems.Items(items.Count);
        var text = $"Set fields of {what}. What you fill in here is written into every one of them; an empty box, and a choice left on {ManyFields.Leave}, leave the field as each item has it. "
            + $"To empty fields, use Clear fields instead. {BundleKeys.Text(BundleKeys.MainAction)} or the Set button sets them all as one step, which {BundleKeys.Text(ItemKeys.Undo)} takes back. Escape leaves, and nothing is changed.";
        ShowForm("Set fields", text, form, "S&et", () =>
        {
            if (Typed().Set is not { } set)
            {
                return;
            }

            if (set.Count == 0)
            {
                Announce("Nothing changed", true);
                return;
            }

            SetFields([.. items.Select(item => (item, set))], "Fields set");
        }, () => Typed() is { Refused: { } refused, Control: var at } ? (made[at], refused) : null);
    }

    /// <summary>Item → Clear fields…: a list of the list's fields; the checked ones are emptied in every item.</summary>
    private void ClearFieldsOfItems()
    {
        if (ItemsForFields() is not { } items)
        {
            return;
        }

        var first = items[0];
        var titles = ManyFields.Clearable(first.Document);
        if (titles.Count == 0)
        {
            Refuse("This list has no fields but the title");
            return;
        }

        var form = new FormControls();
        var list = form.AddCheckList("Check the fields to empty: Space checks or unchecks", [.. titles.Select(title => (title, false))]);
        var what = items.Count == 1 ? ItemFolder.NameOf(first) : ManyItems.Items(items.Count);
        var text = $"Clear fields of {what}. The fields you check are emptied in every one of them: a text, a date or a choice made empty, a check unchecked. "
            + $"{BundleKeys.Text(BundleKeys.MainAction)} or the Clear button clears them as one step, which {BundleKeys.Text(ItemKeys.Undo)} takes back. Escape leaves, and nothing is changed.";
        ShowForm("Clear fields", text, form, "C&lear", () =>
        {
            var chosen = titles.Where((_, index) => list.IsChecked(index)).ToList();
            SetFields([.. items.Select(item => (item, ManyFields.Cleared(item.Document, chosen)))], "Fields cleared");
        }, () => titles.Where((_, index) => list.IsChecked(index)).Any() ? null : (list, "Check at least one field to empty"));
    }
}

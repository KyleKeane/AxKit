using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// What a row's form is made of (docs/axTask/forms-framework.md AF-14.1, AF-14.2): the item's name, its document,
/// the same one the detail pane shows, so that a field of the pane is the form's field of the same place, and what
/// a save does with the changed document and the words to say. A link's field needs <see cref="LinkTitle"/> and
/// <see cref="Link"/>; <see cref="Refuse"/> refuses a document before it is saved (a name taken), with the field
/// to go back to; <see cref="Note"/> adds words to the form's explanation.
/// </summary>
public sealed record ItemFormSource(string Name, ItemDocument Document, Action<ItemDocument, string> Save)
{
    public Func<string, string> LinkTitle { get; init; } = _ => string.Empty;

    public Func<string, (string? Path, string? Refused)> Link { get; init; } = _ => (null, "No link can be made here");

    public Func<ItemDocument, (string Field, string Said)?> Refuse { get; init; } = _ => null;

    public string? Note { get; init; }

    /// <summary>The form's title: "Fields of" and the name for an item there is; a new item's form names itself ("New template").</summary>
    public string? Title { get; init; }

    /// <summary>The name of a group of checks shown as one list (rule 19), an instruction.</summary>
    public string ChecksName { get; init; } = "Check the ones that apply: Space checks or unchecks";

    /// <summary>The default button: Save, or Add for a new item's form.</summary>
    public string Button { get; init; } = "&Save";
}

/// <summary>
/// The item form, one for every app of the item window (the maintainer, 2026-11-23: "this two windows axtask and
/// axtemplate and anything like them should be held in lock step using the same window template so that the
/// interactions are the same. a form is a form and a detail pane list is a detail pane list and the item list is an
/// item list"). An app gives a row's form with <see cref="FormOf"/>; then Enter on that row opens one form with a
/// standard control for each field, in the document's order, and Enter on a field of the detail pane opens the same
/// form with the focus in that field. Tab moves between the controls, Ctrl+Enter or Save saves them all, Escape
/// leaves and nothing changes. The controls and what their values make of the item are
/// <see cref="ItemFormRules"/>; each control is keyed by its field's name and number (<see cref="FormControls.Key"/>).
/// An app without forms keeps Enter as it was: into a list, and the field's own notice in the pane.
/// </summary>
public abstract partial class ItemForm
{
    /// <summary>The form of a row, or null for a row that has none (a list, a file that does not read): the app's.</summary>
    protected virtual ItemFormSource? FormOf(ItemRow row) => null;

    /// <summary>The day a date field's words (today, +3) count from.</summary>
    protected virtual DateOnly FormToday => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Enter on a row, and an app's Open its form: the row's form, the focus in its first field; on a row without one, Enter's own work (<see cref="OpenSelected"/>).</summary>
    protected void OpenFormOfSelected()
    {
        if (SelectedRow() is { } row && FormOf(row) is { } source)
        {
            OpenItemForm(source, null);
        }
        else
        {
            OpenSelected();
        }
    }

    /// <summary>Enter on a field in the detail pane, and the Field menu's Change: the form, the focus in that field; without a form, the field's own notice.</summary>
    protected void OpenFormAtField()
    {
        if (SelectedRow() is { } row && FormOf(row) is { } source && SelectedField() is { } field && field < source.Document.Fields.Count)
        {
            OpenItemForm(source, source.Document.Fields[field].Title);
        }
        else
        {
            ChangeField();
        }
    }

    /// <summary>The form of <paramref name="source"/>, started in the field named <paramref name="startField"/>, its text selected when asked.</summary>
    protected void OpenItemForm(ItemFormSource source, string? startField, bool selectContents = false)
    {
        var document = source.Document;
        var controls = ItemFormRules.Controls(document, source.LinkTitle);
        var form = new FormControls();
        // For each of the rules' controls, the control it is shown in and how its value is read back: a check of a
        // group (rule 19) is a line of the group's list.
        var made = new List<Control>();
        var values = new List<Func<FormValue>>();
        for (var i = 0; i < controls.Count; i++)
        {
            var control = controls[i];
            var run = 0;
            while (i + run < controls.Count && controls[i + run].Kind == ItemFormRules.CheckKind)
            {
                run++;
            }

            if (run >= 2)
            {
                // Two or more checks in a row: one list, Space checks a line, with Check all before it (the
                // maintainer's choice of 2026-11-23, "One list, Space checks").
                var group = controls.Skip(i).Take(run).ToList();
                FormCheckList? list = null;
                form.AddButton("Check &all", () => list?.SetAll(true));
                form.AddButton("U&ncheck all", () => list?.SetAll(false));
                list = form.AddCheckList(source.ChecksName, [.. group.Select(check => (check.Name, check.Checked))]);
                for (var line = 0; line < group.Count; line++)
                {
                    var at = line;
                    made.Add(list);
                    values.Add(() => new FormValue(string.Empty, list.IsChecked(at), 0));
                    form.Key(document.Fields[group[at].Field].Title, list);
                    form.Key(made.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), list);
                }

                if (startField is not null && group.Any(check => document.Fields[check.Field].Title == startField))
                {
                    list.SelectName(group.First(check => document.Fields[check.Field].Title == startField).Name);
                }

                i += run - 1;
                continue;
            }

            Control added = control.Kind switch
            {
                ItemFormRules.CheckKind => form.AddCheck(control.Name),
                ItemFormRules.ListKind => form.AddList(control.Name, control.Options, control.Selected),
                ItemFormRules.LinesKind => form.AddLines(control.Name, rows: 6, control.Text),
                _ => form.AddText(control.Name, control.Text),
            };
            if (added is AxKitCheckBox box)
            {
                box.Checked = control.Checked;
            }

            made.Add(added);
            values.Add(added switch
            {
                AxKitCheckBox check => () => new FormValue(string.Empty, check.Checked, 0),
                AxKitList choice => () => new FormValue(string.Empty, false, choice.SelectedIndex),
                _ => () => new FormValue(added.Text, false, 0),
            });
            form.Key(document.Fields[control.Field].Title, added);
            form.Key(made.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), added);
        }

        if (startField is not null)
        {
            form.StartIn(startField, selectContents);
        }

        // What the controls hold now, as the rules take them.
        List<FormValue> Values() => [.. values.Select(value => value())];

        (ItemDocument? Changed, int Control, string? Refused) Typed() => ItemFormRules.Apply(document, controls, Values(), FormToday, source.Link);

        (Control Field, string Said)? Refused()
        {
            var typed = Typed();
            if (typed.Refused is { } refused)
            {
                return (made[typed.Control], refused);
            }

            if (typed.Changed is { } changed && source.Refuse(changed) is { } why)
            {
                return (form.Field(why.Field) ?? made[0], why.Said);
            }

            return null;
        }

        var save = BundleKeys.Text(BundleKeys.MainAction);
        var text = $"The fields of {source.Name}. Tab moves from one to the next. {save} or the Save button saves them all as one step, which {BundleKeys.Text(ItemKeys.Undo)} takes back. Escape leaves, and nothing is changed."
            + (form.EnterMakesNewLine ? " In a box of several lines Enter makes a new line." : string.Empty)
            + (source.Note is { } note ? " " + note : string.Empty);
        ShowForm(source.Title ?? "Fields of " + source.Name, text, form, source.Button, () => SaveItemForm(source, Typed().Changed), Refused);
    }

    /// <summary>The form saved: nothing when nothing changed, said; otherwise the app's save with the words of what changed.</summary>
    private void SaveItemForm(ItemFormSource source, ItemDocument? changed)
    {
        if (changed is null)
        {
            return;
        }

        var said = ItemFormRules.Said(source.Document, changed);
        if (ReferenceEquals(changed, source.Document))
        {
            Announce(said, true);
            return;
        }

        source.Save(changed, said);
    }
}

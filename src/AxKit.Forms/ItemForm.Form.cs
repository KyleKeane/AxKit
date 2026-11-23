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
        var made = new List<Control>();
        foreach (var control in controls)
        {
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
            form.Key(document.Fields[control.Field].Title, added);
            form.Key((made.Count).ToString(System.Globalization.CultureInfo.InvariantCulture), added);
        }

        if (startField is not null)
        {
            form.StartIn(startField, selectContents);
        }

        // What the controls hold now, as the rules take them.
        List<FormValue> Values() => [.. made.Select(control => control switch
        {
            AxKitCheckBox check => new FormValue(string.Empty, check.Checked, 0),
            AxKitList list => new FormValue(string.Empty, false, list.SelectedIndex),
            _ => new FormValue(control.Text, false, 0),
        })];

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
        ShowForm("Fields of " + source.Name, text, form, "&Save", () => SaveItemForm(source, Typed().Changed), Refused);
    }

    /// <summary>The form saved: nothing when nothing changed, said; otherwise the app's save with the words of what changed.</summary>
    private void SaveItemForm(ItemFormSource source, ItemDocument? changed)
    {
        if (changed is null)
        {
            return;
        }

        var said = ItemFormRules.Said(source.Document, changed, field => SpokenValue(SelectedRow(), field));
        if (ReferenceEquals(changed, source.Document))
        {
            Announce(said, true);
            return;
        }

        source.Save(changed, said);
    }
}

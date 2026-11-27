using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// The field commands of an item app's detail pane (docs/axKit/item-apps.md IA-5) and the notices they and the apps
/// use: every change makes a new document of the one shown and hands it to the app's <c>SaveFields</c>.
/// </summary>
public abstract partial class ItemForm
{
    /// <summary>Space in the detail pane (IA-5.2): a check changed at once.</summary>
    protected void CheckField()
    {
        if (SelectedField() is { } index && _shown.Document!.Fields[index].Type == FieldTypes.Check)
        {
            var field = _shown.Document.Fields[index];
            var value = !FieldTypes.IsTrue(field.Value);
            ChangeShown(document => (document.WithValue(index, JsonValue.Create(value)), $"{field.Title}: {(value ? "done" : "not done")}"), index);
        }
        else
        {
            Announce("Not a check: Enter changes the field", true);
        }
    }

    /// <summary>Enter in the detail pane (IA-5.1): the field's notice, by its type; a line's own action.</summary>
    protected void ChangeField()
    {
        if (SelectedField() is not { } index)
        {
            if (SelectedLine() is { Enter: { } enter })
            {
                enter();
            }
            else
            {
                Announce(SelectedLine()?.Label ?? "Nothing selected", true);
            }

            return;
        }

        var field = _shown.Document!.Fields[index];
        var row = SelectedRow();
        if (row is { IsList: false } && field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
        {
            AskText("Change Title", string.Empty, "Title:", "Title", FieldTypes.TextOf(field.Value), "&Change", title => RenameItem(row, title.Trim()),
                typed => typed.Trim().Length == 0 ? "Type the item's title" : null);
            return;
        }

        if (row is not null && ChangeSpecial(row, index, field))
        {
            return;
        }

        switch (field.Type)
        {
            case FieldTypes.Check:
                CheckField();
                return;
            case FieldTypes.Choice:
                ChooseOption(index, field);
                return;
            case FieldTypes.LongText:
                ChangeLongText(index, field);
                return;
        }

        if (!FieldTypes.Known(field.Type))
        {
            // No key is named: this window is every item app's, and Ctrl+J is another command in axMCP (until
            // 2026-11-27 the sentence ended in ", Ctrl+J").
            Announce($"A field of type {field.Type}: change it in axJSON", true);
            return;
        }

        var initial = field.Value is JsonValue value && value.GetValueKind() == System.Text.Json.JsonValueKind.Number ? value.ToJsonString() : FieldTypes.TextOf(field.Value).Replace('T', ' ');
        if (field.Type == FieldTypes.Text)
        {
            initial = FieldTypes.TextOf(field.Value);
        }

        var hint = field.Type switch
        {
            FieldTypes.Date => "A date: 2026-10-30, 30 October 2026, today, tomorrow, +3 for three days on, or a weekday. Empty for none.",
            FieldTypes.DateTime => "A date, a space and a time: tomorrow 14:00. Empty for none.",
            FieldTypes.Number => "A number. Empty for none.",
            _ => string.Empty,
        };
        Parsed? parsed = null;
        AskText("Change " + field.Title, hint, field.Title + ":", field.Title, initial, "&Change",
            _ => SetValue(index, field, parsed!.Value),
            typed =>
            {
                parsed = FieldTypes.Parse(field.Type, typed, field.Options, DateOnly.FromDateTime(DateTime.Now));
                return parsed.Refused;
            }, required: false);
    }

    /// <summary>A field's new value, saved and said ("Due: 30 October 2026, Friday").</summary>
    protected void SetValue(int index, ItemField field, JsonNode? value)
    {
        var shown = field with { Value = value };
        ChangeShown(document => (document.WithValue(index, value), $"{field.Title}: {SpokenValue(SelectedRow(), shown)}"), index);
    }

    /// <summary>A choice (IA-4.2): a list of the options, the current one selected.</summary>
    private void ChooseOption(int index, ItemField field)
    {
        if (field.Options.Count == 0)
        {
            Announce("This choice has no options: add them in axJSON", true);
            return;
        }

        var current = FieldTypes.TextOf(field.Value);
        var start = Math.Max(0, field.Options.ToList().FindIndex(option => option.Equals(current, StringComparison.OrdinalIgnoreCase)));
        ChooseFromList("Change " + field.Title, $"Now {(current.Length == 0 ? "empty" : current)}.", field.Title, field.Options, start,
            chosen => SetValue(index, field, JsonValue.Create(field.Options[chosen])));
    }

    /// <summary>
    /// Right and Left in the detail pane (IA-5.6, bundle AX-8.10 rule 4): on a choice the next or the previous value,
    /// stopping at the ends; on a check Right checks and Left unchecks; saved, said once ("Priority: Low") and one step
    /// for Ctrl+Z, as every change. The edge sound and nothing else on any other field or line, at a choice's end, on
    /// a check already so, and always on a protected field (<see cref="FieldStep"/>).
    /// </summary>
    protected void StepField(int direction)
    {
        if (SelectedField() is { } index && _shown.Document!.Fields[index] is var field && FieldStep.Step(field, direction) is { } value)
        {
            SetValue(index, field, value);
        }
        else
        {
            EventDoor.Raise(EventKind.Edge);
        }
    }

    /// <summary>
    /// A notice with a list to choose from (a choice's options, a new field's type): the bundle's list, type-to-find
    /// included, holds the focus; Enter chooses the selected line, Escape leaves it.
    /// </summary>
    protected void ChooseFromList(string title, string text, string name, IReadOnlyList<string> lines, int start, Action<int> chosen)
    {
        var list = new AxKitList { AccessibleName = name, Font = Navigator.Font };
        list.Items.AddRange([.. lines]);
        list.SelectedIndex = Math.Clamp(start, 0, lines.Count - 1);
        ShowNotice(new Notice(title, text + " Up and Down move through the list, Enter chooses.",
        [
            new OverlayChoice("C&hoose", () => chosen(list.SelectedIndex), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Custom = list,
        });
        list.Select();
    }

    /// <summary>
    /// A long text (IA-4.2, IA-5.7): a field of several lines; Enter makes a new line, as in every text box; Ctrl+Enter
    /// keeps the text (the notice's main button, bundle AX-8.10), as Tab then Enter on Save does.
    /// </summary>
    private void ChangeLongText(int index, ItemField field)
    {
        var box = new AxKitTextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = false,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            AccessibleName = field.Title,
            Text = FieldTypes.TextOf(field.Value).ReplaceLineEndings("\r\n"),
            Font = Navigator.Font,
        };
        ShowNotice(new Notice("Change " + field.Title, $"Enter makes a new line. {BundleKeys.Text(BundleKeys.MainAction)} keeps the text; so does Tab, then Enter on Save. Escape leaves it as it was.",
        [
            new OverlayChoice("&Save", () => SetValue(index, field, FieldTypes.Parse(FieldTypes.LongText, box.Text, [], DateOnly.FromDateTime(DateTime.Now)).Value), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Custom = box,
            // The text says the keys: "Enter: Save." would be untrue in the box.
            KeyLine = false,
        });
        box.Select();
        box.Select(box.TextLength, 0);
    }

    /// <summary>
    /// Read in full (<see cref="ItemKeys.ReadAll"/>): in the navigator, the row and everything the detail pane shows of it, one after another: every field
    /// of an item, or, for a row whose details are lines and no fields (axAutomate's workflows and steps, a list in
    /// axTask), every line (since 2026-11-22; until then such a row was read alone, where axAutomate's guide and
    /// F1 text said every line). In the detail pane, the whole value of the field or the line you are on.
    /// </summary>
    protected void ReadAll()
    {
        if (DetailPane.Focused)
        {
            if (SelectedField() is { } index)
            {
                var field = _shown.Document!.Fields[index];
                Announce(field.Type == FieldTypes.LongText && FieldTypes.TextOf(field.Value) is { Length: > 0 } text
                    ? $"{field.Title}: {text.ReplaceLineEndings(". ")}"
                    : FieldLabel(field), true);
            }
            else if (SelectedLine() is { } line)
            {
                Announce(line.Label, true);
            }

            return;
        }

        switch (SelectedRow())
        {
            case { IsList: false } row when _shown.Document is { } document:
                Announce(row.Name + ". " + string.Join(". ", document.Fields.Where(field => !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
                    // A long text is read whole here; an empty one reads "empty" as every other empty field does ("Notes: " and nothing until 2026-11-14).
                    .Select(field => $"{field.Title}: {(field.Type == FieldTypes.LongText && FieldTypes.TextOf(field.Value) is { Length: > 0 } text ? text.ReplaceLineEndings(". ") : SpokenValue(row, field))}")), true);
                return;
            case { } other when Pane == SecondPane.Fields && HasDetailPane && _shown.Lines.Count > 0:
                Announce(other.Label + ". " + string.Join(". ", _shown.Lines.Select(line => line.Label.TrimEnd('.'))), true);
                return;
            case { } other:
                Announce(other.Label, true);
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }
    }

    /// <summary>A change to the document the detail pane shows, handed to the app with the field to select after.</summary>
    private void ChangeShown(Func<ItemDocument, (ItemDocument Document, string Said)?> change, int selectField)
    {
        if (_shown.Document is not { } document)
        {
            Announce("Nothing to change here", true);
            return;
        }

        if (change(document) is { } result)
        {
            SaveFields(_shown, result.Document, result.Said, selectField);
        }
    }
}

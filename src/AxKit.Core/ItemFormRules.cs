using System.Text.Json.Nodes;

namespace AxKit.Core;

/// <summary>
/// One control of an item's form (docs/axTask/forms-framework.md AF-14.1): which field it is for, its kind, its name
/// (an instruction, which NVDA reads as the control's name), and what it starts with: a text, a check, or a list of
/// options with one selected.
/// </summary>
public sealed record FormControl(int Field, string Kind, string Name, string Text, bool Checked, IReadOnlyList<string> Options, int Selected);

/// <summary>What a control holds when the form is saved: its text, whether it is checked, the line selected.</summary>
public sealed record FormValue(string Text, bool Checked, int Selected);

/// <summary>
/// The rules of an item's form (AF-14.1, the maintainer's message 8 of 2026-11-21: "the item details for a task into
/// a standard form that opens on enter"): one standard control for each field of the item as its list's
/// configuration gives it, in its order, and what the controls' values make of the item when the form is saved.
/// Pure: the window makes the controls and hands their values back. A control left as it was changes nothing of its
/// field, so a value the form cannot show as it is (a link whose item is gone, a value of the type the field had
/// before, AF-11.6) is never rewritten by a save that did not touch it.
/// </summary>
public static class ItemFormRules
{
    public const string TextKind = "text";
    public const string LinesKind = "lines";
    public const string CheckKind = "check";
    public const string ListKind = "list";

    /// <summary>The first line of a choice's list while the field holds none of its options: leaving it there leaves the field as it is.</summary>
    public const string NotChosen = "Not chosen";

    /// <summary>
    /// The form's controls, one per field in the document's order, the Title first. <paramref name="linkTitle"/>
    /// gives the title of the item a link's path points to, or empty when it points to none.
    /// </summary>
    public static IReadOnlyList<FormControl> Controls(ItemDocument item, Func<string, string> linkTitle)
    {
        var controls = new List<FormControl>();
        for (var i = 0; i < item.Fields.Count; i++)
        {
            var field = item.Fields[i];
            var title = field.Title.Length == 0 ? "the field without a title" : field.Title;
            var text = FieldTypes.PlainText(field.Value);
            if (field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
            {
                controls.Add(new FormControl(i, TextKind, "Type the title", text, false, [], 0));
                continue;
            }

            // A value its type does not hold (AF-11.6) is shown as the text it is, in a text box, whatever the type.
            var held = FieldTypes.Holds(field.Type, field.Value);
            switch (field.Type)
            {
                case FieldTypes.Check when held:
                    controls.Add(new FormControl(i, CheckKind, title, string.Empty, FieldTypes.IsTrue(field.Value), [], 0));
                    break;
                case FieldTypes.Choice when held && field.Options.Count > 0:
                    var chosen = field.Options.ToList().FindIndex(option => option.Equals(text, StringComparison.OrdinalIgnoreCase));
                    IReadOnlyList<string> options = chosen >= 0 ? field.Options : [NotChosen, .. field.Options];
                    controls.Add(new FormControl(i, ListKind, $"Choose {title}", string.Empty, false, options, Math.Max(0, chosen)));
                    break;
                case FieldTypes.LongText when held:
                    controls.Add(new FormControl(i, LinesKind, $"Type {title}; Enter makes a new line", text, false, [], 0));
                    break;
                case FieldTypes.Date when held:
                    controls.Add(new FormControl(i, TextKind, $"Type {title}: a date such as 2026-11-30, today, tomorrow, +3 or a weekday; empty for none", text, false, [], 0));
                    break;
                case FieldTypes.DateTime when held:
                    controls.Add(new FormControl(i, TextKind, $"Type {title}: a date, a space and a time, such as tomorrow 14:00; empty for none", text.Replace('T', ' '), false, [], 0));
                    break;
                case FieldTypes.Number when held:
                    controls.Add(new FormControl(i, TextKind, $"Type {title}, a number; empty for none", text, false, [], 0));
                    break;
                case FieldTypes.Link when held:
                    controls.Add(new FormControl(i, TextKind, $"Type the title of the item {title} links to; empty for none", text.Length == 0 ? string.Empty : linkTitle(text), false, [], 0));
                    break;
                default:
                    controls.Add(new FormControl(i, TextKind, held ? $"Type {title}" : $"Type {title}, kept as text: the field is now a {FieldTypes.Spoken(field.Type)}", text, false, [], 0));
                    break;
            }
        }

        return controls;
    }

    /// <summary>
    /// The item with the form's values. Each control that was changed gives its field a new value, taken as the
    /// field's own change takes it (<see cref="FieldTypes.Parse"/>: a date as 2026-11-30, today, +3 or a weekday); a
    /// link's typed title becomes a path through <paramref name="link"/>, which refuses a title no item or several
    /// items have. The first value refused stops it: the control's index and the words to say, and nothing changed.
    /// A document that is the one handed in means that nothing was changed.
    /// </summary>
    public static (ItemDocument? Changed, int Control, string? Refused) Apply(
        ItemDocument item, IReadOnlyList<FormControl> controls, IReadOnlyList<FormValue> values, DateOnly today, Func<string, (string? Path, string? Refused)> link)
    {
        var changed = item;
        for (var i = 0; i < controls.Count && i < values.Count; i++)
        {
            var control = controls[i];
            var value = values[i];
            var field = item.Fields[control.Field];
            switch (control.Kind)
            {
                case CheckKind:
                    if (value.Checked != control.Checked)
                    {
                        changed = changed.WithValue(control.Field, JsonValue.Create(value.Checked));
                    }

                    continue;
                case ListKind:
                    if (value.Selected != control.Selected && value.Selected >= 0 && value.Selected < control.Options.Count && control.Options[value.Selected] != NotChosen)
                    {
                        changed = changed.WithValue(control.Field, JsonValue.Create(control.Options[value.Selected]));
                    }

                    continue;
            }

            // A text box gives Windows line ends back for the line ends it was given: compared without them.
            var typed = value.Text.ReplaceLineEndings("\n");
            if (typed == control.Text.ReplaceLineEndings("\n"))
            {
                continue;
            }

            if (field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
            {
                var title = typed.Trim();
                if (title.Length == 0 || title.Contains('\n') || title.Contains('\r'))
                {
                    return (null, i, title.Length == 0 ? "Type the item's title" : "A title is one line");
                }

                changed = changed.WithTitle(title);
                continue;
            }

            if (field.Type == FieldTypes.Link && FieldTypes.Holds(field.Type, field.Value))
            {
                var title = typed.Trim();
                if (title.Length == 0)
                {
                    changed = changed.WithValue(control.Field, JsonValue.Create(string.Empty));
                    continue;
                }

                var (path, refused) = link(title);
                if (refused is not null || path is null)
                {
                    return (null, i, $"{field.Title}: {refused ?? "no item of that title"}");
                }

                changed = changed.WithValue(control.Field, JsonValue.Create(path));
                continue;
            }

            // A field of a type this version does not know takes its text as it is typed.
            var type = FieldTypes.Known(field.Type) ? field.Type : FieldTypes.Text;
            var parsed = FieldTypes.Parse(type, typed, field.Options, today);
            if (parsed.Refused is not null)
            {
                return (null, i, $"{field.Title}: {parsed.Refused}");
            }

            changed = changed.WithValue(control.Field, parsed.Value);
        }

        return (changed, -1, null);
    }

    /// <summary>
    /// What a save says, once: "Saved", or "Nothing changed" when no field did. The item and what changed are heard as
    /// the list reads the item the focus goes back to (the maintainer's choice, 2026-11-24: until then the title and
    /// every changed field were said here, and the list read them again).
    /// </summary>
    public static string Said(ItemDocument before, ItemDocument after)
    {
        for (var i = 0; i < after.Fields.Count && i < before.Fields.Count; i++)
        {
            if (!JsonNode.DeepEquals(before.Fields[i].Value, after.Fields[i].Value))
            {
                return "Saved";
            }
        }

        return "Nothing changed";
    }
}

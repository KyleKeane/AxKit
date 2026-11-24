using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// Fields set or cleared on several items at once, without any configuration (SPEC.md TK-4.7a; the maintainer's
/// choice of 2026-11-24): Set fields is a form of the list's fields without the Title, where what is left as it
/// started leaves each item's own value: an empty box, a choice on "Leave as it is", a check as a list of three lines
/// (Leave as it is, Checked, Not checked), since a check box has only two states. Emptying a field is the other
/// command, Clear fields: a list of the fields, the checked ones emptied on every item. Pure: the window makes the
/// controls (the item window's <see cref="ItemFormRules"/> kinds) and hands their values back.
/// </summary>
public static class ManyFields
{
    /// <summary>The first line of every choice and check of the Set fields form: left there, the field stays as each item has it.</summary>
    public const string Leave = "Leave as it is";

    public const string Checked = "Checked";

    public const string NotChecked = "Not checked";

    /// <summary>The Set fields form's controls, one per field of <paramref name="item"/> but the Title, in its order, every one starting as "leave".</summary>
    public static IReadOnlyList<FormControl> Controls(ItemDocument item)
    {
        var controls = new List<FormControl>();
        for (var i = 0; i < item.Fields.Count; i++)
        {
            var field = item.Fields[i];
            if (field.Title.Length == 0 || field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var title = field.Title;
            controls.Add(field.Type switch
            {
                FieldTypes.Check => new FormControl(i, ItemFormRules.ListKind, title, string.Empty, false, [Leave, Checked, NotChecked], 0),
                FieldTypes.Choice when field.Options.Count > 0 => new FormControl(i, ItemFormRules.ListKind, $"Choose {title}", string.Empty, false, [Leave, .. field.Options], 0),
                FieldTypes.LongText => new FormControl(i, ItemFormRules.LinesKind, $"Type {title}; empty leaves it as each item has it; Enter makes a new line", string.Empty, false, [], 0),
                FieldTypes.Date => new FormControl(i, ItemFormRules.TextKind, $"Type {title}: a date such as 2026-11-30, today, tomorrow, +3 or a weekday; empty leaves it as each item has it", string.Empty, false, [], 0),
                FieldTypes.DateTime => new FormControl(i, ItemFormRules.TextKind, $"Type {title}: a date, a space and a time, such as tomorrow 14:00; empty leaves it as each item has it", string.Empty, false, [], 0),
                FieldTypes.Number => new FormControl(i, ItemFormRules.TextKind, $"Type {title}, a number; empty leaves it as each item has it", string.Empty, false, [], 0),
                FieldTypes.Link => new FormControl(i, ItemFormRules.TextKind, $"Type the title of the item {title} links to; empty leaves it as each item has it", string.Empty, false, [], 0),
                _ => new FormControl(i, ItemFormRules.TextKind, $"Type {title}; empty leaves it as each item has it", string.Empty, false, [], 0),
            });
        }

        return controls;
    }

    /// <summary>
    /// What the form's values set, by field title; empty when everything was left. A typed value is taken as the
    /// item form takes it (<see cref="FieldTypes.Parse"/>), a link's title through <paramref name="link"/>; the first
    /// value refused stops it, with its control's index and the words to say.
    /// </summary>
    public static (IReadOnlyDictionary<string, JsonNode?>? Set, int Control, string? Refused) Set(
        ItemDocument item, IReadOnlyList<FormControl> controls, IReadOnlyList<FormValue> values, DateOnly today, Func<string, (string? Path, string? Refused)> link)
    {
        var set = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        for (var i = 0; i < controls.Count && i < values.Count; i++)
        {
            var control = controls[i];
            var field = item.Fields[control.Field];
            if (control.Kind == ItemFormRules.ListKind)
            {
                var chosen = values[i].Selected;
                if (chosen <= 0 || chosen >= control.Options.Count)
                {
                    continue;
                }

                set[field.Title] = field.Type == FieldTypes.Check
                    ? JsonValue.Create(control.Options[chosen] == Checked)
                    : JsonValue.Create(control.Options[chosen]);
                continue;
            }

            var typed = values[i].Text.ReplaceLineEndings("\n");
            if (typed.Trim().Length == 0)
            {
                continue;
            }

            if (field.Type == FieldTypes.Link)
            {
                var (path, refused) = link(typed.Trim());
                if (refused is not null || path is null)
                {
                    return (null, i, $"{field.Title}: {refused ?? "no item of that title"}");
                }

                set[field.Title] = JsonValue.Create(path);
                continue;
            }

            var type = FieldTypes.Known(field.Type) ? field.Type : FieldTypes.Text;
            var parsed = FieldTypes.Parse(type, typed, field.Options, today);
            if (parsed.Refused is not null)
            {
                return (null, i, $"{field.Title}: {parsed.Refused}");
            }

            set[field.Title] = parsed.Value;
        }

        return (set, -1, null);
    }

    /// <summary>The fields Clear fields lists: every field of <paramref name="item"/> but the Title, by its title.</summary>
    public static IReadOnlyList<string> Clearable(ItemDocument item) =>
        [.. item.Fields.Where(field => field.Title.Length > 0 && !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase)).Select(field => field.Title)];

    /// <summary>The empty value of each field named: a check not checked, anything else an empty text, as a field's own change empties it.</summary>
    public static IReadOnlyDictionary<string, JsonNode?> Cleared(ItemDocument item, IEnumerable<string> titles)
    {
        var set = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var title in titles)
        {
            var index = item.IndexOf(title);
            if (index < 0)
            {
                continue;
            }

            set[title] = item.Fields[index].Type == FieldTypes.Check ? JsonValue.Create(false) : JsonValue.Create(string.Empty);
        }

        return set;
    }
}

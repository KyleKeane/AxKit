using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// What makes an item a task (docs/axTask/SPEC.md TK-4): a check field titled Done (TK-D2), with a Due date; the
/// default fields of a new item and the row's summary; All tasks' order is its smart list's (`SmartList.AllTasks`, TK-5.1).
/// </summary>
public static class TaskRules
{
    public const string DoneField = "Done";
    public const string StartField = "Start";
    public const string DueField = "Due";

    /// <summary>TK-4.1: what a list without a template gives a new item.</summary>
    public static readonly IReadOnlyList<ItemField> DefaultTemplate =
    [
        new(DoneField, FieldTypes.Check, JsonValue.Create(false), []),
        new(StartField, FieldTypes.Date, JsonValue.Create(string.Empty), []),
        new(DueField, FieldTypes.Date, JsonValue.Create(string.Empty), []),
    ];

    /// <summary>The fields a list gives its items: its configuration's, or the default for a list that has no configuration yet.</summary>
    public static IReadOnlyList<ItemField> TemplateOf(ListConfig? list) => list?.ItemFields ?? DefaultTemplate;

    /// <summary>The index of the Done check; -1 for a plain item (TK-4.2).</summary>
    public static int DoneIndex(ItemDocument item)
    {
        var fields = item.Fields;
        for (var i = 0; i < fields.Count; i++)
        {
            if (fields[i].Type == FieldTypes.Check && fields[i].Title.Equals(DoneField, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool IsTask(ItemDocument item) => DoneIndex(item) >= 0;

    public static bool IsDone(ItemDocument item) => DoneIndex(item) is var i && i >= 0 && FieldTypes.IsTrue(item.Fields[i].Value);

    /// <summary>
    /// The fields of a list's items with a Done check first, or without one (TK-4.2's switch of a whole list). The
    /// switch is made in the list's configuration alone (AF-13.3): no item's file is touched, a Done an item holds is
    /// kept while the list is plain and shown again when it is a checklist again.
    /// </summary>
    public static IReadOnlyList<ItemField> TemplateWithDone(IReadOnlyList<ItemField> template, bool done)
    {
        var without = template.Where(field => !(field.Type == FieldTypes.Check && field.Title.Equals(DoneField, StringComparison.OrdinalIgnoreCase))).ToList();
        return done ? [new ItemField(DoneField, FieldTypes.Check, JsonValue.Create(false), []), .. without] : without;
    }

    /// <summary>The Due date, a date field or a date and time; null for none.</summary>
    public static DateOnly? Due(ItemDocument item)
    {
        var index = item.IndexOf(DueField);
        if (index < 0)
        {
            return null;
        }

        var field = item.Fields[index];
        return field.Type == FieldTypes.DateTime && FieldTypes.ReadDateTime(field.Value) is { } moment
            ? DateOnly.FromDateTime(moment)
            : FieldTypes.ReadDate(field.Value);
    }

    /// <summary>TK-4.3: ", done", ", due 30 October", ", overdue since 30 October"; empty for a plain item without a due date.</summary>
    public static string Summary(ItemDocument item, DateOnly today)
    {
        var done = IsDone(item);
        var parts = new List<string>();
        if (done)
        {
            parts.Add("done");
        }

        if (Due(item) is { } due)
        {
            parts.Add(!done && due < today ? "overdue since " + FieldTypes.ShortDate(due, today) : due == today ? "due today" : "due " + FieldTypes.ShortDate(due, today));
        }

        return parts.Count == 0 ? string.Empty : ", " + string.Join(", ", parts);
    }

}

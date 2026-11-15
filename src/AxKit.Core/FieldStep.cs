using System.Text.Json.Nodes;

namespace AxKit.Core;

/// <summary>
/// Right and Left on a field in a list of fields (docs/axKit/SPEC.md AX-8.10 rule 4, docs/axKit/item-apps.md IA-5.6):
/// on a choice the next and the previous value, stopping at the ends; on a check a direction, Right checks and Left
/// unchecks; nothing on any other kind of field, and never on a protected one. The pure rule: the window saves the
/// value, says it once and plays the edge sound where this gives none.
/// </summary>
public static class FieldStep
{
    /// <summary>A field whose value never changes on an arrow key: axPass's user name and password.</summary>
    public static bool Protected(string type) => type is FieldTypes.UserName or FieldTypes.Password;

    /// <summary>
    /// The field's value after Right (<paramref name="direction"/> 1) or Left (-1); null when the key changes nothing:
    /// a check already in that state, a choice at its first or last value or without options, any other type. A choice
    /// whose value is none of its options (empty) takes the first on Right.
    /// </summary>
    public static JsonNode? Step(ItemField field, int direction)
    {
        if (Protected(field.Type))
        {
            return null;
        }

        switch (field.Type)
        {
            case FieldTypes.Check:
                var check = direction > 0;
                return FieldTypes.IsTrue(field.Value) == check ? null : JsonValue.Create(check);
            case FieldTypes.Choice:
                var current = FieldTypes.TextOf(field.Value);
                var at = -1;
                for (var i = 0; i < field.Options.Count; i++)
                {
                    if (field.Options[i].Equals(current, StringComparison.OrdinalIgnoreCase))
                    {
                        at = i;
                        break;
                    }
                }

                var to = at + (direction > 0 ? 1 : -1);
                return to < 0 || to >= field.Options.Count ? null : JsonValue.Create(field.Options[to]);
            default:
                return null;
        }
    }

    /// <summary>What a change says, once: the field's name and its new value, "Priority: Low".</summary>
    public static string Said(string title, string value) => $"{(title.Length == 0 ? "untitled" : title)}: {value}";
}

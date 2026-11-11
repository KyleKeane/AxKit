using System.Text.Json.Nodes;

namespace AxTask.Core;

/// <summary>
/// One key of a smart list's <c>order</c> (docs/axTask/SPEC.md TK-5.2): a field's name alone, <c>"Due"</c>, or an
/// object that says how, <c>{ "field": "Priority", "by": "options", "descending": true }</c>. A choice sorts by its
/// options' order unless <c>by</c> is <c>text</c>; <c>descending</c> turns the order round, the items without the field
/// still last.
/// </summary>
public sealed record OrderKey(string Field, bool ByText = false, bool Descending = false)
{
    /// <summary>A key from the file; null when it is neither a name nor an object with a field and a known <c>by</c>.</summary>
    public static OrderKey? Read(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var name))
        {
            return name.Length > 0 ? new OrderKey(name) : null;
        }

        if (node is not JsonObject key || key["field"] is not JsonValue f || !f.TryGetValue<string>(out var field) || field.Length == 0)
        {
            return null;
        }

        var by = key["by"] is JsonValue b && b.TryGetValue<string>(out var text) ? text : "options";
        if (by is not ("options" or "text"))
        {
            return null;
        }

        var descending = key["descending"] is JsonValue d && d.TryGetValue<bool>(out var down) && down;
        return new OrderKey(field, by == "text", descending);
    }
}

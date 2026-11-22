using System.Text.Json.Nodes;

namespace AxKit.Core;

/// <summary>One field as the window shows it (docs/axKit/item-apps.md IA-4.1): its title, its type, its value, and a choice's options.</summary>
public sealed record ItemField(string Title, string Type, JsonNode? Value, IReadOnlyList<string> Options);

/// <summary>
/// What the item window shows and changes (IA-4.1): a list of fields in their order, each a title, a type and a
/// value. It lives in memory only and has no file shape of its own since 2026-11-21: an app makes it from what it
/// holds (axTask from an item's values read through its list's configuration, <see cref="ItemView"/>; axPass from a
/// KeePassXC entry; axMCP from a tool's arguments) and writes a change back in its own way. The files of the lists
/// are <see cref="FlatItem"/> and <see cref="ListConfig"/>. Every change returns a new document; the one made stays
/// as it was, for the check against the file on disk and for undo.
/// </summary>
public sealed class ItemDocument
{
    public const string ListFile = ListConfig.FileName;
    public const string TitleField = "Title";

    private readonly IReadOnlyList<ItemField> _fields;

    private ItemDocument(IReadOnlyList<ItemField> fields)
    {
        _fields = fields;
    }

    /// <summary>The fields in their order.</summary>
    public IReadOnlyList<ItemField> Fields => _fields;

    /// <summary>The Title field's text; empty when there is none.</summary>
    public string Title => IndexOf(TitleField) is var i && i >= 0 && _fields[i].Value is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;

    /// <summary>The document of these fields, in this order.</summary>
    public static ItemDocument Of(IEnumerable<ItemField> fields) => new([.. fields.Select(Own)]);

    /// <summary>A new item (IA-4.3): its Title, then the template's fields with their values.</summary>
    public static ItemDocument NewItem(string title, IReadOnlyList<ItemField> template) =>
        Of([new ItemField(TitleField, FieldTypes.Text, JsonValue.Create(title), []), .. template.Where(field => !field.Title.Equals(TitleField, StringComparison.OrdinalIgnoreCase))]);

    /// <summary>The index of the first field with this title (case ignored), -1 for none.</summary>
    public int IndexOf(string title)
    {
        for (var i = 0; i < _fields.Count; i++)
        {
            if (_fields[i].Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Whether field <paramref name="index"/> is still the one shown, by title and type (IA-5.5).</summary>
    public bool Holds(int index, ItemField shown) =>
        index >= 0 && index < _fields.Count && _fields[index].Title == shown.Title && _fields[index].Type == shown.Type;

    public ItemDocument WithValue(int index, JsonNode? value) => Change(fields => fields[index] = fields[index] with { Value = value?.DeepClone() });

    public ItemDocument WithTitle(string title)
    {
        var index = IndexOf(TitleField);
        return index >= 0
            ? WithValue(index, JsonValue.Create(title))
            : Change(fields => fields.Insert(0, new ItemField(TitleField, FieldTypes.Text, JsonValue.Create(title), [])));
    }

    /// <summary>A new field after <paramref name="after"/> (-1 for the end), empty (IA-4.2).</summary>
    public ItemDocument WithField(int after, string title, string type, IReadOnlyList<string> options) =>
        Change(fields =>
        {
            var field = new ItemField(title, type, FieldTypes.Empty(type), type == FieldTypes.Choice ? [.. options] : []);
            if (after < 0 || after >= fields.Count - 1)
            {
                fields.Add(field);
            }
            else
            {
                fields.Insert(after + 1, field);
            }
        });

    public ItemDocument WithoutField(int index) => Change(fields => fields.RemoveAt(index));

    public ItemDocument WithFieldTitle(int index, string title) => Change(fields => fields[index] = fields[index] with { Title = title });

    /// <summary>The field moved one place up or down; null at the end it would leave.</summary>
    public ItemDocument? WithFieldMoved(int index, int direction)
    {
        var to = index + direction;
        if (to < 0 || to >= _fields.Count)
        {
            return null;
        }

        return Change(fields =>
        {
            var field = fields[index];
            fields.RemoveAt(index);
            fields.Insert(to, field);
        });
    }

    private ItemDocument Change(Action<List<ItemField>> change)
    {
        var fields = _fields.ToList();
        change(fields);
        return new ItemDocument(fields);
    }

    /// <summary>A field with a value of its own: a value handed in may belong to a JSON document that changes later.</summary>
    private static ItemField Own(ItemField field) => field with { Value = field.Value?.DeepClone(), Options = [.. field.Options] };
}

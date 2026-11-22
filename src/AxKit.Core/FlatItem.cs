using System.Text.Json;
using System.Text.Json.Nodes;
namespace AxKit.Core;

/// <summary>
/// An item as its file holds it (docs/axTask/forms-framework.md section 13): one JSON object of names and values,
/// and <c>"schema"</c>, the path, relative to the file, of the configuration it is read through. Nothing of a field
/// but its value is here; its type, its options and its place are the configuration's (<see cref="ListConfig"/>).
///
/// The object is kept whole and in its order; every change returns a new item. axTask's lists
/// and the reports list (<c>Reports/ReportFiles</c>) are written in it.
/// </summary>
public sealed class FlatItem
{
    /// <summary>The one member that is no field: the link to the configuration. The one name a field cannot have (AF-13.2).</summary>
    public const string SchemaKey = "schema";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    private readonly JsonObject _root;

    private FlatItem(JsonObject root)
    {
        _root = root;
    }

    /// <summary>The configuration this item is read through, as the file names it: <c>list.json</c> for an item of a list.</summary>
    public string Schema => FieldTypes.TextOf(_root[SchemaKey]);

    /// <summary>The item's Title; empty when it has none.</summary>
    public string Title => Text(ItemDocument.TitleField);

    /// <summary>Every name and value of the item in the file's order, the link left out.</summary>
    public IReadOnlyList<KeyValuePair<string, JsonNode?>> Values =>
        [.. _root.Where(pair => !pair.Key.Equals(SchemaKey, StringComparison.Ordinal)).Select(pair => new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value))];

    /// <summary>Whether the item holds a value of this name, case ignored.</summary>
    public bool Has(string name) => KeyOf(name) is not null;

    /// <summary>The value of this name, case ignored; null when the item has none (or holds JSON's null).</summary>
    public JsonNode? Value(string name) => KeyOf(name) is { } key ? _root[key] : null;

    /// <summary>A value as its text; empty for none.</summary>
    public string Text(string name) => FieldTypes.TextOf(Value(name));

    /// <summary>Whether a JSON object is an item: it names the configuration it is read through.</summary>
    public static bool IsItem(JsonNode? node) =>
        node is JsonObject root && root[SchemaKey] is JsonValue value && value.TryGetValue<string>(out var schema) && schema.Trim().Length > 0;

    /// <summary>The item in a file's text; an <see cref="InvalidDataException"/> when it is not JSON, not an object, or names no configuration.</summary>
    public static FlatItem Parse(string text)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(text, documentOptions: Lenient);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(ex.LineNumber is { } line ? $"it is not JSON at line {line + 1}" : "it is not JSON", ex);
        }

        if (node is not JsonObject root)
        {
            throw new InvalidDataException("it is not a JSON object");
        }

        if (!IsItem(root))
        {
            throw new InvalidDataException($"it names no configuration to be read through (\"{SchemaKey}\")");
        }

        return new FlatItem(root);
    }

    /// <summary>
    /// A new item of a list (AF-13.1): the link, its Title, then each field of the configuration with the value a
    /// new item starts with.
    /// </summary>
    public static FlatItem New(string title, ListConfig config, string schema = ListConfig.FileName)
    {
        var root = new JsonObject { [SchemaKey] = schema, [ItemDocument.TitleField] = title };
        foreach (var field in config.ItemFields)
        {
            root[field.Title] = field.Value?.DeepClone() ?? FieldTypes.Empty(field.Type);
        }

        return new FlatItem(root);
    }

    /// <summary>The text written to the file: indented JSON, one member a line, with Windows line ends.</summary>
    public string ToText() => _root.ToJsonString(Indented).ReplaceLineEndings("\r\n") + "\r\n";

    /// <summary>
    /// The item with a value set. A name it holds keeps its place and its spelling; a new one is put where the
    /// configuration has it, after the fields before it that the item holds, so that a file stays in the list's
    /// order (AF-13.1); a name the configuration does not know goes last.
    /// </summary>
    public FlatItem With(string name, JsonNode? value, ListConfig? config = null)
    {
        if (name.Equals(SchemaKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The link to the configuration is no field.", nameof(name));
        }

        var root = (JsonObject)_root.DeepClone();
        if (KeyOf(name) is { } key)
        {
            root[key] = value?.DeepClone();
            return new FlatItem(root);
        }

        var at = root.Count;
        if (name.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
        {
            // The Title an item lacked goes where a Title is: first, after the link.
            at = root.Select(pair => pair.Key).ToList().IndexOf(SchemaKey) + 1;
        }
        else if (config is not null && config.IndexOf(name) is var index and >= 0)
        {
            // After the last member that is the link, the Title, or a field the configuration has before this one.
            at = 0;
            var position = 0;
            foreach (var (member, _) in root)
            {
                position++;
                if (member.Equals(SchemaKey, StringComparison.Ordinal) || member.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase)
                    || (config.IndexOf(member) is var before and >= 0 && before < index))
                {
                    at = position;
                }
            }
        }

        root.Insert(at, config is not null && config.IndexOf(name) is var named and >= 0 ? config.ItemFields[named].Title : name, value?.DeepClone());
        return new FlatItem(root);
    }

    /// <summary>The item with its link set: read through another configuration from now on.</summary>
    public FlatItem WithSchema(string schema)
    {
        var root = (JsonObject)_root.DeepClone();
        root[SchemaKey] = schema;
        return new FlatItem(root);
    }

    private string? KeyOf(string name) =>
        name.Equals(SchemaKey, StringComparison.OrdinalIgnoreCase) ? null : _root.Select(pair => pair.Key).FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>A value an item holds that its list's configuration does not name: kept in the file, not shown among the fields (AF-11.3).</summary>
public sealed record KeptValue(string Name, JsonNode? Value);

/// <summary>An item as it is shown: its Title, the configuration's fields with the item's values, and what is kept.</summary>
public sealed record ShownItem(string Title, IReadOnlyList<ItemField> Fields, IReadOnlyList<KeptValue> Kept)
{
    /// <summary>The last line of the item's fields when it keeps values, "2 more fields kept: Sprint, Old priority" (AF-11.4); null when it keeps none.</summary>
    public string? KeptLine => Kept.Count == 0
        ? null
        : $"{Kept.Count} more {(Kept.Count == 1 ? "field" : "fields")} kept: {string.Join(", ", Kept.Select(kept => kept.Name))}";
}

/// <summary>
/// Hide, keep, restore (docs/axTask/forms-framework.md AF-11.3, the maintainer's decision of 2026-11-21): which of an
/// item's values are shown, in which order, read through its list's configuration. Pure; nothing here changes a
/// file. A change of the configuration therefore never removes or rewrites a value: a field it no longer names is
/// kept, and is shown again with the value it had as soon as it is named again.
/// </summary>
public static class ItemView
{
    /// <summary>
    /// The item read through the configuration: each of its fields in its order, with the item's value, or the
    /// type's empty value where the item's file holds none (written when the field is first given a value); and
    /// every other value of the item as kept, in the file's order.
    /// </summary>
    public static ShownItem Of(FlatItem item, ListConfig config)
    {
        var fields = new List<ItemField>();
        foreach (var field in config.ItemFields)
        {
            fields.Add(field with { Value = item.Has(field.Title) ? item.Value(field.Title)?.DeepClone() : FieldTypes.Empty(field.Type) });
        }

        var kept = Others(item)
            .Where(pair => config.IndexOf(pair.Key) < 0)
            .Select(pair => new KeptValue(pair.Key, pair.Value?.DeepClone()))
            .ToList();
        return new ShownItem(item.Title, fields, kept);
    }

    /// <summary>
    /// The item with every name and value as a field of plain text, in the file's order: how it is shown, read-only,
    /// when the configuration it names is not there or does not read (AF-13.4). Nothing is guessed from a value: a
    /// check reads "true" or "false", a number as it is written.
    /// </summary>
    public static ShownItem Plain(FlatItem item) =>
        new(item.Title,
            [.. Others(item).Select(pair => new ItemField(pair.Key, FieldTypes.Text, JsonValue.Create(FieldTypes.PlainText(pair.Value)), []))],
            []);

    /// <summary>The document the item window shows for an item: its Title first, then the fields shown.</summary>
    public static ItemDocument Document(ShownItem shown) =>
        ItemDocument.Of([new ItemField(ItemDocument.TitleField, FieldTypes.Text, JsonValue.Create(shown.Title), []), .. shown.Fields]);

    /// <summary>
    /// The item's file after a change made in the window (AF-11.3): <paramref name="shown"/> is the document the item
    /// was shown as, <paramref name="changed"/> the one the change made. Only a value that changed is written, so a
    /// field shown empty because the file does not hold it stays out of the file until it is first given a value, and
    /// a value the configuration does not name is never touched. A field the change added is written too.
    /// </summary>
    public static FlatItem Changed(FlatItem item, ItemDocument shown, ItemDocument changed, ListConfig? config)
    {
        var result = item;
        foreach (var field in changed.Fields)
        {
            var before = shown.IndexOf(field.Title);
            if (before >= 0 && JsonNode.DeepEquals(shown.Fields[before].Value, field.Value))
            {
                continue;
            }

            result = result.With(field.Title, field.Value, config);
        }

        return result;
    }

    /// <summary>Every value of the item but its Title, in the file's order.</summary>
    private static IEnumerable<KeyValuePair<string, JsonNode?>> Others(FlatItem item) =>
        item.Values.Where(pair => !pair.Key.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase));
}

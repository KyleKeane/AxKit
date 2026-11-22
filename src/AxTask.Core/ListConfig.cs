using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// A list's configuration, its <c>list.json</c> in the shape of docs/axTask/forms-framework.md section 11
/// (<c>axkit-list 2</c>): the list's own details by name, the fields of its items in the order they are shown, and its
/// actions. It is the schema every item of the list is read through (section 13): an item's file holds values, and
/// everything else of a field is written here once.
///
/// The object is kept whole, so that what this version does not know is written back as it was read (IA-4). Every
/// change returns a new configuration. A member of the wrong kind is passed over and named in <see cref="Problems"/>;
/// the rest of the file counts (AF-11.7). Not used by a window yet: the item window still reads the earlier shape,
/// which <c>Conversion/EarlierFormat</c> turns into this one.
/// </summary>
public sealed class ListConfig
{
    public const string Format = "axkit-list 2";
    public const string FileName = "list.json";
    public const string FormatKey = "format";
    public const string DetailsKey = "details";
    public const string ItemFieldsKey = "item fields";
    public const string ActionsKey = "actions";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly JsonObject _root;

    private ListConfig(JsonObject root, IReadOnlyList<ItemField> itemFields, IReadOnlyList<string> problems)
    {
        _root = root;
        ItemFields = itemFields;
        Problems = problems;
    }

    /// <summary>The fields of the list's items, in the order they are shown after the Title (AF-11.2).</summary>
    public IReadOnlyList<ItemField> ItemFields { get; }

    /// <summary>What of the file was passed over, a sentence each (AF-11.7); empty for a file that reads whole.</summary>
    public IReadOnlyList<string> Problems { get; }

    /// <summary>The list's own details in the file's order, each a name and its value.</summary>
    public IReadOnlyList<KeyValuePair<string, JsonNode?>> Details =>
        _root[DetailsKey] is JsonObject details ? [.. details.Select(pair => new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value))] : [];

    /// <summary>A detail's text by its name, case ignored; empty when the list has none of that name.</summary>
    public string Detail(string name) =>
        Details.FirstOrDefault(pair => pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;

    /// <summary>The actions as the file holds them (section 3), or null: read by the actions' own code.</summary>
    public JsonArray? Actions => _root[ActionsKey] as JsonArray;

    /// <summary>A member of the file's root this class gives no name, as the file has it.</summary>
    public JsonNode? Extra(string key) => _root[key];

    /// <summary>The format a file's text names, or null when it is not a JSON object that names one: for telling an earlier shape from this one without reading it.</summary>
    public static string? FormatOf(string text)
    {
        try
        {
            return JsonNode.Parse(text, documentOptions: Lenient) is JsonObject root && root[FormatKey] is JsonValue value && value.TryGetValue<string>(out var format) ? format : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The configuration in a file's text. An <see cref="InvalidDataException"/> when the text is not JSON (its
    /// message names the line, AF-11.7), not an object, or not of this format; a member of the wrong kind is no
    /// exception but a <see cref="Problems">problem</see>.
    /// </summary>
    public static ListConfig Parse(string text)
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

        var format = root[FormatKey] is JsonValue named && named.TryGetValue<string>(out var given) ? given : null;
        if (format != Format)
        {
            throw new InvalidDataException(format is null ? "it names no format" : $"its format is {format}, not {Format}");
        }

        var problems = new List<string>();
        if (root[DetailsKey] is { } details && details is not JsonObject)
        {
            problems.Add($"\"{DetailsKey}\" is not an object of names and values, and is passed over");
        }

        if (root[ActionsKey] is { } actions && actions is not JsonArray)
        {
            problems.Add($"\"{ActionsKey}\" is not a list, and is passed over");
        }

        return new ListConfig(root, ReadItemFields(root[ItemFieldsKey], problems), problems);
    }

    /// <summary>A new configuration with these item fields, no details but empty Notes, and no actions: the file a list gets when it has none (AF-11.1).</summary>
    public static ListConfig New(IReadOnlyList<ItemField> itemFields)
    {
        var root = new JsonObject
        {
            [FormatKey] = Format,
            [DetailsKey] = new JsonObject { ["Notes"] = string.Empty },
            [ItemFieldsKey] = FieldsArray(itemFields),
            [ActionsKey] = new JsonArray(),
        };
        return new ListConfig(root, [.. itemFields], []);
    }

    /// <summary>The text written to the file: indented JSON with Windows line ends.</summary>
    public string ToText() => _root.ToJsonString(Indented).ReplaceLineEndings("\r\n") + "\r\n";

    /// <summary>The configuration with a detail set, added last when the list had none of that name.</summary>
    public ListConfig WithDetail(string name, JsonNode? value)
    {
        var root = (JsonObject)_root.DeepClone();
        if (root[DetailsKey] is not JsonObject details)
        {
            details = new JsonObject();
            root[DetailsKey] = details;
        }

        var existing = details.Select(pair => pair.Key).FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
        details[existing ?? name] = value?.DeepClone();
        return new ListConfig(root, ItemFields, Problems);
    }

    /// <summary>The configuration with these item fields in place of its own. No item's file is touched by it (AF-13.3).</summary>
    public ListConfig WithItemFields(IReadOnlyList<ItemField> itemFields)
    {
        var root = (JsonObject)_root.DeepClone();
        root[ItemFieldsKey] = FieldsArray(itemFields);
        return new ListConfig(root, [.. itemFields], Problems);
    }

    /// <summary>The configuration with a root member set, or removed with null.</summary>
    public ListConfig WithExtra(string key, JsonNode? value)
    {
        var root = (JsonObject)_root.DeepClone();
        if (value is null)
        {
            root.Remove(key);
        }
        else
        {
            root[key] = value.DeepClone();
        }

        return new ListConfig(root, ItemFields, Problems);
    }

    /// <summary>The index of the item field of this title, case ignored; -1 for none.</summary>
    public int IndexOf(string title)
    {
        for (var i = 0; i < ItemFields.Count; i++)
        {
            if (ItemFields[i].Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>A file a person edits may hold a comma too many and a comment: both are read past, as the earlier shape's reader did.</summary>
    private static readonly JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    /// <summary>
    /// The item fields read, each a title and a type, with the value a new item starts with and a choice's options.
    /// What cannot be a field is passed over with its sentence: an entry that is no object, one without a title, a
    /// second field of a title, and the two names a field cannot have (the Title, which every item has first, and
    /// the item's link to this file). A type this version does not know is kept and read as text.
    /// </summary>
    private static List<ItemField> ReadItemFields(JsonNode? node, List<string> problems)
    {
        var fields = new List<ItemField>();
        if (node is null)
        {
            return fields;
        }

        if (node is not JsonArray array)
        {
            problems.Add($"\"{ItemFieldsKey}\" is not a list, and is passed over");
            return fields;
        }

        var number = 0;
        foreach (var entry in array)
        {
            number++;
            if (entry is not JsonObject field)
            {
                problems.Add($"item field {number} is not an object, and is passed over");
                continue;
            }

            var title = FieldTypes.TextOf(field["title"]).Trim();
            if (title.Length == 0)
            {
                problems.Add($"item field {number} has no title, and is passed over");
                continue;
            }

            if (title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase) || title.Equals(FlatItem.SchemaKey, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"an item field cannot be named {title}, and is passed over");
                continue;
            }

            if (fields.Any(known => known.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add($"the item field {title} is there twice; the first counts");
                continue;
            }

            var type = FieldTypes.TextOf(field["type"]).Trim();
            if (type.Length == 0)
            {
                type = FieldTypes.Text;
            }
            else if (!FieldTypes.All.Contains(type))
            {
                problems.Add($"the item field {title} has the type {type}, which this version does not know; it is read as text");
            }

            var options = field["options"] is JsonArray given
                ? given.Select(option => option is JsonValue value && value.TryGetValue<string>(out var text) ? text : null).OfType<string>().ToList()
                : [];
            fields.Add(new ItemField(title, type, field["value"]?.DeepClone(), options));
        }

        return fields;
    }

    private static JsonArray FieldsArray(IReadOnlyList<ItemField> fields)
    {
        var array = new JsonArray();
        foreach (var field in fields)
        {
            var node = new JsonObject { ["title"] = field.Title, ["type"] = field.Type, ["value"] = field.Value?.DeepClone() ?? FieldTypes.Empty(field.Type) };
            if (field.Type == FieldTypes.Choice)
            {
                node["options"] = new JsonArray([.. field.Options.Select(option => (JsonNode?)JsonValue.Create(option))]);
            }

            array.Add(node);
        }

        return array;
    }
}

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxMCP.Core;

/// <summary>
/// A tool's arguments as the detail pane's fields, and back (SPEC.md MP-3.2, MP-3.3): the Description first, fixed,
/// then one field per property of the input schema with the item apps' type for it (<c>FieldTypes</c>), a required one
/// marked in its title; the fields' values back to the call's <c>arguments</c> object, typed as the schema says. A
/// prompt's arguments the same way, every one a text (MP-5.2).
/// </summary>
public static class ArgumentFields
{
    public const string DescriptionField = "Description";

    /// <summary>The one field of a tool whose schema has no properties (MP-3.3): its JSON, whole.</summary>
    public const string RawField = "Arguments (JSON)";

    private const string RequiredMark = " (required)";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>The fields for a tool: Description, then its arguments, each from its schema property.</summary>
    public static ItemDocument ForTool(ServerTool tool)
    {
        var fields = new JsonArray { Field(DescriptionField, FieldTypes.LongText, tool.Description) };
        var properties = tool.InputSchema["properties"] as JsonObject;
        var required = RequiredNames(tool.InputSchema);
        if (properties is null || properties.Count == 0)
        {
            if (HasObjectSchema(tool.InputSchema))
            {
                fields.Add(Field(RawField, FieldTypes.LongText, "{}"));
            }
        }
        else
        {
            foreach (var property in properties)
            {
                var schema = property.Value as JsonObject ?? new JsonObject();
                var title = property.Key + (required.Contains(property.Key) ? RequiredMark : string.Empty);
                var (type, options) = TypeOf(schema);
                fields.Add(Field(title, type, Default(schema, type), options));
            }
        }

        return ItemDocument.Parse(new JsonObject { ["format"] = ItemDocument.ItemFormat, ["fields"] = fields }.ToJsonString());
    }

    /// <summary>The fields for a prompt: Description, then one text per argument.</summary>
    public static ItemDocument ForPrompt(ServerPrompt prompt)
    {
        var fields = new JsonArray { Field(DescriptionField, FieldTypes.LongText, prompt.Description) };
        foreach (var argument in prompt.Arguments)
        {
            fields.Add(Field(argument.Name + (argument.Required ? RequiredMark : string.Empty), FieldTypes.Text, string.Empty));
        }

        return ItemDocument.Parse(new JsonObject { ["format"] = ItemDocument.ItemFormat, ["fields"] = fields }.ToJsonString());
    }

    /// <summary>The argument an argument field stands for: its title without the required mark.</summary>
    public static string ArgumentName(ItemField field) =>
        field.Title.EndsWith(RequiredMark, StringComparison.Ordinal) ? field.Title[..^RequiredMark.Length] : field.Title;

    public static bool IsRequired(ItemField field) => field.Title.EndsWith(RequiredMark, StringComparison.Ordinal);

    /// <summary>The first required argument left empty, for "path is required" (MP-3.4); null when every one has a value.</summary>
    public static string? MissingRequired(ItemDocument document) =>
        document.Fields.FirstOrDefault(field => IsRequired(field) && field.Type != FieldTypes.Check && IsEmptyText(field)) is { } field ? ArgumentName(field) : null;

    /// <summary>
    /// The fields back to the call's arguments (MP-3.3): a number as a number, a check as a boolean, a long text that
    /// holds JSON as that JSON (the raw field spread into the object), a text as a string; an empty optional argument
    /// is left out. A long text that is not JSON throws <see cref="JsonException"/>, which the window says.
    /// </summary>
    public static JsonObject ToArguments(ItemDocument document)
    {
        var arguments = new JsonObject();
        foreach (var field in document.Fields)
        {
            if (field.Title == DescriptionField)
            {
                continue;
            }

            if (field.Title == RawField)
            {
                if (JsonNode.Parse(FieldTypes.TextOf(field.Value).Trim() is { Length: > 0 } raw ? raw : "{}") is JsonObject whole)
                {
                    foreach (var pair in whole.ToList())
                    {
                        whole.Remove(pair.Key);
                        arguments[pair.Key] = pair.Value;
                    }
                }

                continue;
            }

            var name = ArgumentName(field);
            switch (field.Type)
            {
                case FieldTypes.Check:
                    arguments[name] = FieldTypes.IsTrue(field.Value);
                    break;
                case FieldTypes.Number:
                    if (field.Value is JsonValue number && number.GetValueKind() == JsonValueKind.Number)
                    {
                        arguments[name] = number.DeepClone();
                    }

                    break;
                case FieldTypes.LongText:
                    var text = FieldTypes.TextOf(field.Value).Trim();
                    if (text.Length > 0)
                    {
                        arguments[name] = JsonNode.Parse(text);
                    }

                    break;
                default:
                    var value = FieldTypes.TextOf(field.Value);
                    if (value.Length > 0)
                    {
                        arguments[name] = value;
                    }

                    break;
            }
        }

        return arguments;
    }

    /// <summary>A prompt's arguments: every value a string, the empty ones left out (MP-5.2).</summary>
    public static JsonObject ToPromptArguments(ItemDocument document)
    {
        var arguments = new JsonObject();
        foreach (var field in document.Fields.Where(field => field.Title != DescriptionField))
        {
            var value = FieldTypes.TextOf(field.Value);
            if (value.Length > 0)
            {
                arguments[ArgumentName(field)] = value;
            }
        }

        return arguments;
    }

    /// <summary>A schema property's field type and, for an enumeration, its options (MP-3.3).</summary>
    public static (string Type, IReadOnlyList<string> Options) TypeOf(JsonObject schema)
    {
        if (schema["enum"] is JsonArray values)
        {
            var options = values.Select(value => value is JsonValue v && v.TryGetValue<string>(out var text) ? text : value?.ToJsonString() ?? "null").ToList();
            return (FieldTypes.Choice, options);
        }

        var type = schema["type"] switch
        {
            JsonValue one when one.TryGetValue<string>(out var name) => name,
            // ["string", "null"]: the first that is not null.
            JsonArray several => several.Select(item => item is JsonValue v && v.TryGetValue<string>(out var name) ? name : null).FirstOrDefault(name => name is not null and not "null"),
            _ => null,
        };
        var format = schema["format"] is JsonValue f && f.TryGetValue<string>(out var formatName) ? formatName : null;
        return type switch
        {
            "string" when format == "date" => (FieldTypes.Date, []),
            "string" when format == "date-time" => (FieldTypes.DateTime, []),
            "string" => (FieldTypes.Text, []),
            "integer" or "number" => (FieldTypes.Number, []),
            "boolean" => (FieldTypes.Check, []),
            "array" or "object" => (FieldTypes.LongText, []),
            _ => (FieldTypes.LongText, []),
        };
    }

    private static JsonNode? Default(JsonObject schema, string type)
    {
        var given = schema["default"];
        return type switch
        {
            FieldTypes.Check => JsonValue.Create(given is JsonValue flag && flag.TryGetValue<bool>(out var on) && on),
            FieldTypes.Number => given is JsonValue number && number.GetValueKind() == JsonValueKind.Number ? number.DeepClone() : JsonValue.Create(string.Empty),
            FieldTypes.LongText => given is null ? JsonValue.Create(string.Empty) : JsonValue.Create(given.ToJsonString(Indented).ReplaceLineEndings("\r\n")),
            _ => JsonValue.Create(given is JsonValue text && text.TryGetValue<string>(out var value) ? value : given?.ToJsonString() ?? string.Empty),
        };
    }

    private static HashSet<string> RequiredNames(JsonObject schema)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (schema["required"] is JsonArray required)
        {
            foreach (var item in required)
            {
                if (item is JsonValue value && value.TryGetValue<string>(out var name))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    private static bool HasObjectSchema(JsonObject schema) =>
        schema["type"] is JsonValue type && type.TryGetValue<string>(out var name) && name == "object" || schema.Count > 0;

    private static bool IsEmptyText(ItemField field) =>
        field.Value is not JsonValue value || value.GetValueKind() == JsonValueKind.String && value.GetValue<string>().Trim().Length == 0;

    private static JsonObject Field(string title, string type, string value) => Field(title, type, JsonValue.Create(value), []);

    private static JsonObject Field(string title, string type, JsonNode? value, IReadOnlyList<string> options)
    {
        var field = new JsonObject { ["title"] = title, ["type"] = type, ["value"] = value };
        if (options.Count > 0)
        {
            field["options"] = new JsonArray([.. options.Select(option => JsonValue.Create(option))]);
            if (value is JsonValue v && v.TryGetValue<string>(out var chosen) && chosen.Length == 0)
            {
                field["value"] = options[0];
            }
        }

        return field;
    }
}

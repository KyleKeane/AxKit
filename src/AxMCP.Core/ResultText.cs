using System.Text;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxMCP.Core;

/// <summary>
/// What a server gave back, as the text of a notice (SPEC.md MP-3.5, MP-4.2, MP-5.2): a tool's content items in
/// order with a blank line between, an image or audio item as one line naming its kind and size, an embedded resource
/// as its text, "The tool reported an error" first when it did; a resource's contents; a prompt's messages as
/// paragraphs starting with their role.
/// </summary>
public static class ResultText
{
    public const string NoContent = "No content";
    public const string ErrorLine = "The tool reported an error";

    /// <summary>A <c>tools/call</c> result.</summary>
    public static string OfCall(JsonObject result)
    {
        var parts = new List<string>();
        if (result["isError"] is JsonValue flag && flag.TryGetValue<bool>(out var isError) && isError)
        {
            parts.Add(ErrorLine);
        }

        if (result["content"] is JsonArray content)
        {
            parts.AddRange(content.OfType<JsonObject>().Select(Item));
        }
        else if (result["structuredContent"] is JsonObject structured)
        {
            parts.Add(structured.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        return parts.Count == 0 ? NoContent : Join(parts);
    }

    /// <summary>A <c>resources/read</c> result.</summary>
    public static string OfResource(JsonObject result)
    {
        var parts = new List<string>();
        if (result["contents"] is JsonArray contents)
        {
            foreach (var item in contents.OfType<JsonObject>())
            {
                parts.Add(Text(item["text"]) ?? $"[blob, {Text(item["mimeType"]) ?? "unknown type"}, {Size(Text(item["blob"]))}]");
            }
        }

        return parts.Count == 0 ? NoContent : Join(parts);
    }

    /// <summary>A <c>prompts/get</c> result: "user: …", "assistant: …".</summary>
    public static string OfPrompt(JsonObject result)
    {
        var parts = new List<string>();
        if (Text(result["description"]) is { Length: > 0 } description)
        {
            parts.Add(description);
        }

        if (result["messages"] is JsonArray messages)
        {
            foreach (var message in messages.OfType<JsonObject>())
            {
                var role = Text(message["role"]) ?? "message";
                var content = message["content"] is JsonObject item ? Item(item) : string.Empty;
                parts.Add(role + ": " + content);
            }
        }

        return parts.Count == 0 ? NoContent : Join(parts);
    }

    /// <summary>One content item: its text, a line for an image or audio, a resource's text (MP-3.5).</summary>
    public static string Item(JsonObject item)
    {
        switch (Text(item["type"]))
        {
            case "text":
                return Text(item["text"]) ?? string.Empty;
            case "image":
            case "audio":
                return $"[{Text(item["type"])}, {Text(item["mimeType"]) ?? "unknown type"}, {Size(Text(item["data"]))}]";
            case "resource":
                var resource = item["resource"] as JsonObject;
                return Text(resource?["text"]) ?? $"[resource, {Text(resource?["uri"]) ?? "no uri"}]";
            case "resource_link":
                return $"[resource link, {Text(item["uri"]) ?? "no uri"}]";
            default:
                return item.ToJsonString();
        }
    }

    private static string Join(IEnumerable<string> parts) => string.Join("\r\n\r\n", parts.Select(part => part.ReplaceLineEndings("\r\n")));

    /// <summary>The size of base64 data as the bundle says sizes.</summary>
    private static string Size(string? base64)
    {
        if (base64 is null)
        {
            return "no data";
        }

        var padding = base64.EndsWith("==", StringComparison.Ordinal) ? 2 : base64.EndsWith('=') ? 1 : 0;
        return FileSize.Text(Math.Max(0, base64.Length * 3L / 4 - padding));
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}

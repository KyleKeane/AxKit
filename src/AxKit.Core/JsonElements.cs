using System.Text.Json;

namespace AxKit.Core;

/// <summary>What the readers of GitHub's answers ask of a parsed element (the update check, the reports), written once.</summary>
internal static class JsonElements
{
    /// <summary>The string property of an object, or null when the element is no object, has no such property or it is not a string.</summary>
    internal static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
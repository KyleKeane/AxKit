using System.Text.Json;

namespace AxKit.Core;

/// <summary>
/// What the readers of a parsed element ask of it (GitHub's answers, the reports' envelope, the automation channel's
/// requests, Claude Code's stream), written once: until 2026-11-24 the same reader was written in four places, and one
/// of them threw on an element that was no object (docs/axKit/repairs.json R-94).
/// </summary>
public static class JsonElements
{
    /// <summary>The string property of an object, or null when the element is no object, has no such property or it is not a string.</summary>
    public static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxKit.Core;

/// <summary>
/// The one reader of JSON a person may have written by hand (an item's file, a <c>list.json</c>, a smart list's
/// filter): the text is read whole and at once, and a name written twice in one object is a failure of the reading,
/// a <see cref="JsonException"/> like any other. .NET's own <c>JsonNode.Parse</c> reads lazily and lets such a file
/// through; the object then throws an <c>ArgumentException</c> at whoever asks it for a member first, which no
/// reader of a file catches: one item file with "Title" twice made every fill of its list fail (found 2026-11-27).
/// <c>JsonTextTests</c> holds the callers to it.
/// </summary>
public static class JsonText
{
    /// <summary>What a reader says of a file that names a member twice.</summary>
    public const string Twice = "it names a member twice";

    private static readonly JsonDocumentOptions Strict = new() { AllowDuplicateProperties = false };

    /// <summary>A file a person edits may hold a comma too many and a comment: both are read past.</summary>
    private static readonly JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip, AllowDuplicateProperties = false };

    private static readonly JsonDocumentOptions LenientWithTwice = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    private static readonly JsonDocumentOptions StrictWithTwice = new();

    /// <summary>
    /// The text as JSON; a <see cref="JsonException"/> when it is not JSON or names a member twice anywhere in it.
    /// <paramref name="lenient"/>: a comma too many and comments are read past.
    /// </summary>
    public static JsonNode? Parse(string text, bool lenient = false) =>
        JsonNode.Parse(text, documentOptions: lenient ? Lenient : Strict);

    /// <summary>
    /// Why <see cref="Parse"/> refused a text, in the words a list shows (AF-11.7, TK-4.6): "it names a member
    /// twice" when that is all that is wrong with it, "it is not JSON at line 12" otherwise.
    /// <paramref name="lenient"/> as it was given to <see cref="Parse"/>.
    /// </summary>
    public static string Why(string text, JsonException refused, bool lenient = false)
    {
        try
        {
            using var read = JsonDocument.Parse(text, lenient ? LenientWithTwice : StrictWithTwice);
            return Twice;
        }
        catch (JsonException)
        {
            return refused.LineNumber is { } line ? $"it is not JSON at line {line + 1}" : "it is not JSON";
        }
    }
}

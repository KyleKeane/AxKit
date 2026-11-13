namespace AxClaude.Core.Transcript;

/// <summary>
/// The user's words that keep a reply line silent (FR-7.4a, 2026-11-12): a line holding any of them, case not minded,
/// stays in the conversation and is not spoken. The words are typed into one field, separated by semicolons.
/// </summary>
public static class SpeechFilter
{
    /// <summary>The separator between the words in the field.</summary>
    public const char Separator = ';';

    /// <summary>The words of the field: each part between semicolons, trimmed, an empty one left out, a repeat once.</summary>
    public static List<string> Words(string? text) =>
        (text ?? string.Empty).Split(Separator)
            .Select(word => word.Trim())
            .Where(word => word.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>The words as the field shows them again.</summary>
    public static string Text(IEnumerable<string> words) => string.Join(Separator + " ", words);

    /// <summary>Whether a line holds one of the words, case not minded.</summary>
    public static bool Silences(string line, IReadOnlyList<string> words)
    {
        foreach (var word in words)
        {
            if (line.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

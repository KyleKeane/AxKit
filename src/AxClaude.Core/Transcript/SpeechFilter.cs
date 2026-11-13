using AxKit.Core.Transcript;

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

    /// <summary>
    /// The lines that are spoken: a line is its row and the rows joined to it (a long line wraps over several rows of
    /// the console, <see cref="Line.JoinedToPrevious"/>), tested as one text and silenced or spoken whole. Row by row,
    /// a word in the second row left the first spoken, and a word split by the wrap silenced nothing
    /// (docs/axKit/repairs.json R-20).
    /// </summary>
    public static List<Line> Spoken(IReadOnlyList<Line> lines, IReadOnlyList<string> words)
    {
        var spoken = new List<Line>();
        if (words.Count == 0)
        {
            spoken.AddRange(lines);
            return spoken;
        }

        for (var start = 0; start < lines.Count;)
        {
            var end = start + 1;
            while (end < lines.Count && lines[end].JoinedToPrevious)
            {
                end++;
            }

            // Both joinings: a wrap falls at a space, and inside a word too long for the row.
            var rows = lines.Skip(start).Take(end - start).Select(line => line.Text).ToList();
            if (!Silences(string.Join(" ", rows), words) && !Silences(string.Concat(rows), words))
            {
                for (var i = start; i < end; i++)
                {
                    spoken.Add(lines[i]);
                }
            }

            start = end;
        }

        return spoken;
    }

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

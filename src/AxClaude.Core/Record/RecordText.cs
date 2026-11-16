using System.Text;

namespace AxClaude.Core.Record;

/// <summary>
/// The comparison of a record's text with the rows a program draws for it (docs/axClaude/events-and-grounding.md
/// EG-3.7). Neutral: a program's own rules for what its screen changes are applied first (for Claude Code,
/// <c>ClaudeCodeTranscript.ScreenKey</c>), and both sides are then compared by their letters alone.
/// </summary>
public static class RecordText
{
    /// <summary>
    /// The letters and digits of the text, in lower case, in their order; everything else (spaces, line ends, marks,
    /// punctuation, the labels' colons) is left out, so that a reply's source and its wrapped rows on the screen give
    /// the same letters. A character outside the first plane of Unicode (an emoji) is left out on both sides alike.
    /// </summary>
    public static string Letters(ReadOnlySpan<char> text)
    {
        var letters = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                letters.Append(char.ToLowerInvariant(c));
            }
        }

        return letters.ToString();
    }
}

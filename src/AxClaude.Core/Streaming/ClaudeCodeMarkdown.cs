using System.Text.RegularExpressions;

namespace AxClaude.Core.Streaming;

/// <summary>
/// A reply's Markdown source made to read as Claude Code's screen shows the same reply in screen reader mode
/// (docs/axClaude/stream-mode.md SM-4.5, stage 7), so that a reply over the stream sounds as it does over the screen.
/// Claude Code's own rendering, one place for it (root CLAUDE.md rule 12); the rules are read off the timeline fixture
/// <c>long-reply</c>, which holds the source and the screen of one reply with every kind of mark (Claude Code
/// 2.1.31x): bold, italics and code spans lose their marks; a fence line goes and the code under it stays as written;
/// a table's header and rule go and each row is one line, "Lighthouse: Fastnet Rock. Location: Ireland."; a link
/// whose text is its address is the address; a quote loses its mark; lists stay as written. A heading keeps its marks
/// here, since the model makes it a heading by them and the view hides them when its option says so; only the marks
/// inside its text go. Pure, tested in <c>ClaudeCodeMarkdownTests</c>.
/// </summary>
public static partial class ClaudeCodeMarkdown
{
    // A code span: the text between single backticks, never touched by the other marks.
    [GeneratedRegex(@"`([^`\n]+)`")]
    private static partial Regex CodeSpan();

    [GeneratedRegex(@"\*\*(?=\S)(.+?)(?<=\S)\*\*|__(?=\S)(.+?)(?<=\S)__")]
    private static partial Regex Bold();

    // Italics by one star or one underscore, not inside a word, so that snake_case and 2*3*4 stay.
    [GeneratedRegex(@"(?<![\w*])\*(?=\S)([^*\n]+?)(?<=\S)\*(?![\w*])|(?<![\w_])_(?=\S)([^_\n]+?)(?<=\S)_(?![\w_])")]
    private static partial Regex Italic();

    // A link or an image: [text](address), ![text](address).
    [GeneratedRegex(@"!?\[([^\]\n]*)\]\(([^)\s]+)\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex Fence();

    [GeneratedRegex(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$")]
    private static partial Regex TableRule();

    /// <summary>The source's lines as the screen reads them.</summary>
    public static string[] Plain(IReadOnlyList<string> source)
    {
        var lines = new List<string>(source.Count);
        var inCode = false;
        string[]? header = null;
        for (var i = 0; i < source.Count; i++)
        {
            var line = source[i];
            if (Fence().IsMatch(line))
            {
                inCode = !inCode;
                continue;
            }

            if (inCode)
            {
                lines.Add(line);
                continue;
            }

            // A table: its header row, known by the rule under it, names the cells of every row after it.
            if (header is null && IsTableRow(line) && i + 1 < source.Count && TableRule().IsMatch(source[i + 1]))
            {
                header = Cells(line);
                i++;
                continue;
            }

            if (header is not null)
            {
                if (IsTableRow(line))
                {
                    lines.Add(TableRow(header, Cells(line)));
                    continue;
                }

                header = null;
            }

            var text = line.TrimStart().StartsWith('>') ? line.TrimStart()[1..].TrimStart() : line;
            lines.Add(Inline(text));
        }

        return [.. lines];
    }

    /// <summary>The marks inside a line taken away: code spans, bold, italics and links; the text of a code span is left as written.</summary>
    public static string Inline(string text)
    {
        var parts = CodeSpan().Split(text);
        // Split with a group gives the text between spans at even places and each span's text at odd ones.
        for (var i = 0; i < parts.Length; i += 2)
        {
            var part = Link().Replace(parts[i], match => match.Groups[1].Value.Length == 0 || match.Groups[1].Value == match.Groups[2].Value
                ? match.Groups[2].Value
                : $"{match.Groups[1].Value} ({match.Groups[2].Value})");
            part = Bold().Replace(part, match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
            parts[i] = Italic().Replace(part, match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
        }

        return string.Concat(parts);
    }

    private static bool IsTableRow(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length > 1 && trimmed.StartsWith('|') && trimmed.EndsWith('|');
    }

    private static string[] Cells(string line) =>
        [.. line.Trim().Trim('|').Split('|').Select(cell => Inline(cell.Trim()))];

    /// <summary>"Lighthouse: Pharos of Alexandria. Location: Egypt.": each cell after its column's name, each ended by a full stop.</summary>
    private static string TableRow(string[] header, string[] cells)
    {
        var said = new List<string>();
        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var name = i < header.Length ? header[i] : string.Empty;
            var ended = cell.Length > 0 && cell[^1] is '.' or '!' or '?' ? cell : cell + ".";
            said.Add(name.Length > 0 ? $"{name}: {ended}" : ended);
        }

        return string.Join(" ", said);
    }
}

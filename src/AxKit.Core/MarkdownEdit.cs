namespace AxKit.Core;

/// <summary>
/// One change <see cref="MarkdownEdit"/> makes to a text: what replaces which characters, where the caret stands
/// after, and what is said ("Heading 2", "Bold on"). <paramref name="Edge"/>: nothing could change (a heading of
/// level 6 made deeper), so the edge sound comes with the sentence, and the change is of no characters by no text.
/// </summary>
public sealed record MarkdownChange(int Start, int Length, string Text, int Caret, string Said, bool Edge = false);

/// <summary>
/// The format commands of a Markdown text (docs/axDown/SPEC.md AD-12, 2026-11-22; the maintainer's ask: "heading
/// toggles and other markdown editing quick keys"): each a pure function from the text and the caret or the
/// selection to one <see cref="MarkdownChange"/>, and each a toggle, so that the same command on the same text
/// takes the format away again. The line commands (a heading, a list item, a block quote) act on the caret's line
/// or on every line the selection touches; the inline ones (bold, italic, code) on the selection, or on the word
/// the caret is in, or, with neither, put the empty marks in with the caret between them. Lines end in \r\n or
/// \n; a change never touches a line's ending.
/// </summary>
public static class MarkdownEdit
{
    public const int DeepestHeading = 6;

    // ---- headings ----

    /// <summary>The caret's line made a heading of <paramref name="level"/> (1 to 6), another level changed to it, and that very level taken away: "Heading 2", "No heading".</summary>
    public static MarkdownChange Heading(string text, int caret, int level)
    {
        var line = HeadingLine.At(text, caret);
        return SetHeading(line, caret, line.Level == level ? 0 : Math.Clamp(level, 1, DeepestHeading));
    }

    /// <summary>One number sign more: a plain line becomes a heading of level 1, a heading one level deeper; at level 6 nothing changes and the level is said with the edge sound.</summary>
    public static MarkdownChange HeadingDeeper(string text, int caret)
    {
        var line = HeadingLine.At(text, caret);
        return line.Level >= DeepestHeading
            ? new MarkdownChange(caret, 0, string.Empty, caret, $"Heading {DeepestHeading}", Edge: true)
            : SetHeading(line, caret, line.Level + 1);
    }

    /// <summary>One number sign fewer: a heading one level shallower, a heading of level 1 a plain line; on a plain line nothing changes and "No heading" is said with the edge sound.</summary>
    public static MarkdownChange HeadingShallower(string text, int caret)
    {
        var line = HeadingLine.At(text, caret);
        return line.Level == 0
            ? new MarkdownChange(caret, 0, string.Empty, caret, "No heading", Edge: true)
            : SetHeading(line, caret, line.Level - 1);
    }

    /// <summary>The caret's line as a heading: where the line starts, how many characters come before its content (the indentation, the number signs and the blanks after them), and its level, 0 for a plain line.</summary>
    private readonly record struct HeadingLine(int Start, int Prefix, int Level)
    {
        public static HeadingLine At(string text, int caret)
        {
            var start = LineStart(text, caret);
            var end = LineEnd(text, start);
            var at = start;
            while (at < end && text[at] is ' ' or '\t')
            {
                at++;
            }

            var indent = at - start;
            var signs = 0;
            while (at + signs < end && text[at + signs] == '#')
            {
                signs++;
            }

            var after = at + signs;
            // A heading (CommonMark): up to three blanks, one to six number signs, then a blank or the line's end.
            if (indent > 3 || signs is 0 or > DeepestHeading || (after < end && text[after] is not (' ' or '\t')))
            {
                return new HeadingLine(start, indent, 0);
            }

            while (after < end && text[after] is ' ' or '\t')
            {
                after++;
            }

            return new HeadingLine(start, after - start, signs);
        }
    }

    private static MarkdownChange SetHeading(HeadingLine line, int caret, int level)
    {
        var prefix = level == 0 ? string.Empty : new string('#', level) + " ";
        // The caret keeps its place in the line's content; from inside the old marks it goes to the content's start.
        var inLine = caret - line.Start;
        var moved = line.Start + (inLine <= line.Prefix ? prefix.Length : inLine - line.Prefix + prefix.Length);
        return new MarkdownChange(line.Start, line.Prefix, prefix, moved, level == 0 ? "No heading" : $"Heading {level}");
    }

    // ---- bold, italic, code ----

    /// <summary>Two asterisks around the selection or the caret's word, or taken away from around it: "Bold on", "Bold off".</summary>
    public static MarkdownChange Bold(string text, int start, int length) => Emphasis(text, start, length, 2, "Bold");

    /// <summary>One asterisk around the selection or the caret's word, or taken away: "Italic on", "Italic off". Bold text made italic has three.</summary>
    public static MarkdownChange Italic(string text, int start, int length) => Emphasis(text, start, length, 1, "Italic");

    /// <summary>Backticks around the selection or the caret's word, or taken away: "Code on", "Code off". Text that holds a backtick itself gets two.</summary>
    public static MarkdownChange Code(string text, int start, int length)
    {
        var (from, to) = Target(text, start, length, '`');
        var marks = Math.Min(RunBefore(text, from, '`'), RunAfter(text, to, '`'));
        var caret = length == 0 ? Math.Clamp(start, from, to) : to;
        if (marks > 0)
        {
            return Unwrapped(text, from, to, marks, caret, "Code off");
        }

        return Wrapped(text, from, to, text.AsSpan(from, to - from).Contains('`') ? "``" : "`", caret, "Code on");
    }

    private static MarkdownChange Emphasis(string text, int start, int length, int width, string name)
    {
        var (from, to) = Target(text, start, length, '*');
        // The asterisks on both sides: two or three are bold, one or three italic.
        var marks = Math.Min(3, Math.Min(RunBefore(text, from, '*'), RunAfter(text, to, '*')));
        var on = width == 2 ? marks >= 2 : marks % 2 == 1;
        var caret = length == 0 ? Math.Clamp(start, from, to) : to;
        return on
            ? Unwrapped(text, from, to, width, caret, name + " off")
            : Wrapped(text, from, to, new string('*', width), caret, name + " on");
    }

    /// <summary>
    /// What an inline command acts on: the selection without the blanks at its ends, or with no selection the word
    /// the caret is in or at an end of; then without the marks of this kind at its own ends, so that a selection of
    /// "**word**" and one of "word" inside it are the same thing. Empty when the caret is in no word.
    /// </summary>
    private static (int From, int To) Target(string text, int start, int length, char mark)
    {
        var from = Math.Clamp(start, 0, text.Length);
        var to = Math.Clamp(start + Math.Max(0, length), from, text.Length);
        if (to == from)
        {
            while (from > 0 && IsWord(text[from - 1]))
            {
                from--;
            }

            while (to < text.Length && IsWord(text[to]))
            {
                to++;
            }

            return (from, to);
        }

        while (from < to && char.IsWhiteSpace(text[from]))
        {
            from++;
        }

        while (to > from && char.IsWhiteSpace(text[to - 1]))
        {
            to--;
        }

        while (from < to && text[from] == mark)
        {
            from++;
        }

        while (to > from && text[to - 1] == mark)
        {
            to--;
        }

        return (from, to);
    }

    private static bool IsWord(char c) => char.IsLetterOrDigit(c) || c is '_' or '\'' or '-';

    private static int RunBefore(string text, int at, char mark)
    {
        var count = 0;
        while (at - count > 0 && text[at - count - 1] == mark)
        {
            count++;
        }

        return count;
    }

    private static int RunAfter(string text, int at, char mark)
    {
        var count = 0;
        while (at + count < text.Length && text[at + count] == mark)
        {
            count++;
        }

        return count;
    }

    private static MarkdownChange Wrapped(string text, int from, int to, string marks, int caret, string said) =>
        new(from, to - from, marks + text[from..to] + marks, caret + marks.Length, said);

    private static MarkdownChange Unwrapped(string text, int from, int to, int width, int caret, string said) =>
        new(from - width, to - from + 2 * width, text[from..to], caret - width, said);

    // ---- list items and block quotes ----

    /// <summary>"- " put before the caret's line, or before every line the selection touches that is no list item yet; taken away when all of them are items: "List item on", "List item off, 3 lines".</summary>
    public static MarkdownChange ListItem(string text, int start, int length) =>
        Lines(text, start, length, "List item", quote: false);

    /// <summary>"> " put before the caret's line or every line the selection touches; taken away when all of them are quoted: "Block quote on", "Block quote off".</summary>
    public static MarkdownChange BlockQuote(string text, int start, int length) =>
        Lines(text, start, length, "Block quote", quote: true);

    private static MarkdownChange Lines(string text, int start, int length, string name, bool quote)
    {
        start = Math.Clamp(start, 0, text.Length);
        var last = Math.Clamp(start + Math.Max(0, length), start, text.Length);
        // A selection that ends at the start of a line does not touch that line.
        if (last > start && text[last - 1] == '\n')
        {
            last--;
            if (last > start && text[last - 1] == '\r')
            {
                last--;
            }
        }

        var first = LineStart(text, start);
        var end = LineEnd(text, last);

        // The lines of the block, each with where it starts; the endings between them stay as they are.
        var starts = new List<int>();
        for (var at = first; ; )
        {
            starts.Add(at);
            var lineEnd = LineEnd(text, at);
            if (lineEnd >= end)
            {
                break;
            }

            at = lineEnd + (text[lineEnd] == '\r' && lineEnd + 1 < text.Length && text[lineEnd + 1] == '\n' ? 2 : 1);
        }

        var single = starts.Count == 1;
        // Empty, or in a quote nothing but its sign: such a line is carried along and not counted.
        bool Blank(int at) => Content(text, at) == LineEnd(text, at)
            || (quote && MarkLength(text, at, quote: true) is var sign && sign > 0 && at + sign == LineEnd(text, at));
        bool Counts(int at) => single || !Blank(at);
        var counted = starts.Where(Counts).ToList();
        var on = counted.Count > 0 && counted.All(at => MarkLength(text, at, quote) > 0);

        var result = new System.Text.StringBuilder();
        var caret = start;
        for (var i = 0; i < starts.Count; i++)
        {
            var at = starts[i];
            var lineEnd = LineEnd(text, at);
            var next = i + 1 < starts.Count ? starts[i + 1] : end;
            var markAt = quote ? at : Content(text, at);
            var had = MarkLength(text, at, quote);
            var delta = 0;
            result.Append(text, at, markAt - at);
            if (on)
            {
                delta = -had;
            }
            else if (had == 0 && (Counts(at) || quote))
            {
                // Inside a quote of several lines an empty line gets its sign too, so that it stays one quote.
                var mark = quote ? (Blank(at) && !single ? ">" : "> ") : "- ";
                result.Append(mark);
                delta = mark.Length;
            }

            result.Append(text, markAt + (on ? had : 0), lineEnd - markAt - (on ? had : 0));
            result.Append(text, lineEnd, next - lineEnd);
            if (single)
            {
                // The caret keeps its place in the line; from inside a mark taken away it goes to where the mark was.
                caret = start <= markAt ? start : Math.Max(markAt, start + delta);
                if (!on && start == markAt)
                {
                    caret = start + delta;
                }
            }
        }

        var said = $"{name} {(on ? "off" : "on")}" + (single ? string.Empty : $", {counted.Count} lines");
        return new MarkdownChange(first, end - first, result.ToString(), single ? caret : first + result.Length, said);
    }

    /// <summary>Where a line's content starts, after its blanks.</summary>
    private static int Content(string text, int lineStart)
    {
        var at = lineStart;
        while (at < text.Length && text[at] is ' ' or '\t')
        {
            at++;
        }

        return at;
    }

    /// <summary>
    /// How many characters the line's mark takes, 0 when it has none: for a list item "-", "*" or "+" after the
    /// line's blanks with the blank after it; for a quote ">" at the line's start, after up to three blanks, with
    /// one blank after it. A quote's length counts from the line's start, blanks before the sign included.
    /// </summary>
    private static int MarkLength(string text, int lineStart, bool quote)
    {
        var end = LineEnd(text, lineStart);
        var at = Content(text, lineStart);
        if (at >= end)
        {
            return 0;
        }

        if (quote)
        {
            if (text[at] != '>' || at - lineStart > 3)
            {
                return 0;
            }

            var after = at + 1 < end && text[at + 1] == ' ' ? at + 2 : at + 1;
            return after - lineStart;
        }

        if (text[at] is not ('-' or '*' or '+'))
        {
            return 0;
        }

        return at + 1 == end ? 1 : text[at + 1] is ' ' or '\t' ? 2 : 0;
    }

    // ---- lines ----

    private static int LineStart(string text, int at)
    {
        at = Math.Clamp(at, 0, text.Length);
        while (at > 0 && text[at - 1] != '\n')
        {
            at--;
        }

        return at;
    }

    private static int LineEnd(string text, int at)
    {
        at = Math.Clamp(at, 0, text.Length);
        while (at < text.Length && text[at] is not ('\r' or '\n'))
        {
            at++;
        }

        return at;
    }
}

using System.Text.RegularExpressions;
using AxKit.Core.Transcript;

namespace AxClaude.Core.Transcript;

/// <summary>A key a screen of Claude's names on its hint row: what to send, the key's name, and what it does there.</summary>
public sealed record ScreenKey(string Send, string Name, string Action);

/// <summary>
/// A screen of Claude's that waits for keys named on a hint row rather than on a prompt row (docs/axClaude/claude-screens.md):
/// <c>/status</c>, <c>/tasks</c>, <c>/help</c>, <c>/goal</c> and the like. Its title, its lines, and the keys of its
/// hint row.
/// </summary>
public sealed record ClaudeScreen(string Title, IReadOnlyList<string> Lines, IReadOnlyList<ScreenKey> Keys)
{
    /// <summary>The screen row the screen starts on (its title or tab row), where the app marks it in the conversation.</summary>
    public int FirstRow { get; init; }

    /// <summary>A screen under a tab row (<c>/status</c>, <c>/permissions</c>), whose title is the tabs: it stays a list in the control area (SPEC.md FR-2.11).</summary>
    public bool Tabbed { get; init; }

    /// <summary>
    /// What tells one screen from the next: the title and the keys. The lines are left out, since some screens change
    /// by themselves (a task's running time counts up every second); after a key of the user's the same screen is
    /// shown again all the same (MainForm).
    /// </summary>
    public string Signature => Title + "\n" + string.Join(" ", Keys.Select(key => key.Send));

    /// <summary>The line Claude highlights with ❯, without the mark ("Send feedback", "Details:"); null when none is marked.</summary>
    public string? Highlighted => Lines.FirstOrDefault(line => line.StartsWith(ClaudeCodeTexts.HighlightMark))?[1..].Trim();

    /// <summary>A text row of /feedback's review that Claude has opened for editing (D33): the hint row names "cancel edit", and typed keys go to the end of the row's text.</summary>
    public bool Editing => Keys.Any(key => key.Action == ClaudeCodeTexts.ActionCancelEdit);
}

/// <summary>
/// Reads a waiting screen off the rows around Claude's cursor. The cursor sits on a hint row that names Escape
/// (<c>↑/↓ to select · Enter to view · Esc to close</c>, <c>Esc to dismiss</c>), with the screen's lines above it up to
/// a labelled row or two blank rows; or it sits on a tab row (<c>Settings  Status   Config   Usage   Stats</c>) with the
/// screen's lines below it down to such a hint row. At Claude's own prompt the cursor is on the <c>$</c> row, so
/// ordinary work never looks like a screen. The texts and patterns are <see cref="ClaudeCodeTexts"/>'s.
/// </summary>
public static class ScreenReader
{
    private const int MaxRows = 60;

    /// <summary>How far under the cursor the hint row of a field being edited is looked for.</summary>
    public const int EditRows = 8;

    /// <summary>The screen waiting around the cursor row, or null when the cursor is not on a hint or tab row.</summary>
    public static ClaudeScreen? Read(IReadOnlyList<string> rows, int cursorRow)
    {
        if (cursorRow < 0 || cursorRow >= rows.Count)
        {
            return null;
        }

        var cursorText = rows[cursorRow].Trim();

        // The last screen of /login (recorded with 2.1.323, 2026-11-13): "Logged in as …" over "Login successful.
        // Press Enter to continue…", the cursor at that row's end. Unseen, it left the message field in place, and
        // the Enter of the user's next message closed it: the message never reached Claude. The row's own words are
        // the title, the rows above it the lines, Enter the one key.
        if (PressEnterTitle(cursorText) is { } title)
        {
            var (lines, first) = LinesAbove(rows, cursorRow);
            return new ClaudeScreen(title, lines, [new ScreenKey("\r", ClaudeCodeTexts.KeyEnter, ClaudeCodeTexts.ActionContinue)]) { FirstRow = lines.Count > 0 ? first : cursorRow };
        }

        if (Keys(cursorText) is { } keys)
        {
            var (lines, first) = LinesAbove(rows, cursorRow);
            return lines.Count == 0 ? null : new ClaudeScreen(lines[0], lines.Skip(1).ToList(), keys) { FirstRow = first };
        }

        // /mobile leaves the cursor on the row under its hint row ("Then run /rc to continue this session from your
        // phone"), which belongs to the screen too. Never Claude's own prompt row.
        if (cursorRow > 0 && cursorText.Length > 0 && !LineClassifier.IsBarePrompt(cursorText) && !LineClassifier.IsDraftRow(rows[cursorRow])
            && Keys(rows[cursorRow - 1].Trim()) is { } keysAbove)
        {
            var (lines, first) = LinesAbove(rows, cursorRow - 1);
            lines.Add(cursorText);
            return new ClaudeScreen(lines[0], lines.Skip(1).ToList(), keysAbove) { FirstRow = lines.Count > 1 ? first : cursorRow };
        }

        // A field of /feedback's review being edited (D33, recorded with 2.1.302): the highlight on Details, Area or
        // Title puts the cursor at the end of the field's text, with the rest of the review and the hint row
        // "↑/↓ to move · Enter to next · ctrl+j to new line · Esc to cancel edit" under it. The screen is the review
        // with that hint row's keys; typed keys go into the field.
        if (cursorText.Length > 0 && !LineClassifier.IsBarePrompt(cursorText))
        {
            for (var r = cursorRow + 1; r < rows.Count && r - cursorRow <= EditRows; r++)
            {
                if (Keys(rows[r].Trim()) is not { } editKeys)
                {
                    continue;
                }

                if (!editKeys.Any(key => key.Action == ClaudeCodeTexts.ActionCancelEdit))
                {
                    break;
                }

                var (lines, first) = LinesAbove(rows, r);
                return lines.Count == 0 ? null : new ClaudeScreen(lines[0], lines.Skip(1).ToList(), editKeys) { FirstRow = first };
            }
        }

        if (ClaudeCodeTexts.TabRow().IsMatch(cursorText))
        {
            var lines = new List<string>();
            for (var r = cursorRow + 1; r < rows.Count && r - cursorRow <= MaxRows; r++)
            {
                var text = rows[r].Trim();
                if (Keys(text) is { } tabKeys)
                {
                    return new ClaudeScreen("Tabs: " + string.Join(", ", Tabs(cursorText)), lines, tabKeys) { FirstRow = cursorRow, Tabbed = true };
                }

                if (text.Length > 0)
                {
                    lines.Add(text);
                }
            }
        }

        return null;
    }

    /// <summary>Whether a row can be one that waits on Enter: the cheap test for every frame's end, without allocating.</summary>
    public static bool EndsWithPressEnter(string text)
    {
        var end = text.AsSpan().TrimEnd().TrimEnd(ClaudeCodeTexts.EllipsisCharacter).TrimEnd('.');
        return end.EndsWith(ClaudeCodeTexts.PressEnterToContinue, StringComparison.Ordinal);
    }

    /// <summary>
    /// The title of a screen whose cursor row waits on Enter: the row's words before "Press Enter to continue"
    /// ("Login successful"), or those words themselves when the row holds nothing else; null for any other row, and
    /// for a labelled row (<c>claude:</c>, <c>tool:</c>), which is Claude quoting the words, not waiting on them.
    /// </summary>
    public static string? PressEnterTitle(string text)
    {
        if (!EndsWithPressEnter(text) || LineClassifier.Classify(text, inPromptBlock: false) is not (LineKind.Plain or LineKind.Prompt))
        {
            return null;
        }

        var before = text[..text.LastIndexOf(ClaudeCodeTexts.PressEnterToContinue, StringComparison.Ordinal)].Trim().TrimEnd('.').Trim();
        return before.Length > 0 ? before : ClaudeCodeTexts.PressEnterToContinue;
    }

    /// <summary>A row of tab names: three or more, two spaces or more apart.</summary>
    public static bool IsTabRow(string text) => ClaudeCodeTexts.TabRow().IsMatch(text);

    /// <summary>The keys of a hint row that names Escape, or null when the row is no such hint row.</summary>
    public static IReadOnlyList<ScreenKey>? Keys(string text)
    {
        // After a feedback report is sent from /feedback: "done: Feedback sent (receipt …). Thanks!" over this row
        // (D33, recorded with 2.1.302). Enter stands for the key.
        if (text == ClaudeCodeTexts.AnyKeyToContinue)
        {
            return [new ScreenKey("\r", ClaudeCodeTexts.KeyEnter, ClaudeCodeTexts.ActionContinue)];
        }

        var keys = new List<ScreenKey>();
        foreach (var part in text.Split(ClaudeCodeTexts.PartSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var match = ClaudeCodeTexts.Hint().Match(part);
            if (!match.Success)
            {
                return null;
            }

            var action = match.Groups["action"].Value;
            var names = match.Groups["keys"].Value.Split(ClaudeCodeTexts.KeyJoin);
            // "⇧←/→": the Shift of the first arrow holds for the arrows after it.
            var shift = names[0].StartsWith(ClaudeCodeTexts.KeyShift);
            foreach (var name in names)
            {
                var key = shift && name is ClaudeCodeTexts.KeyLeft or ClaudeCodeTexts.KeyRight or ClaudeCodeTexts.KeyUp or ClaudeCodeTexts.KeyDown
                    ? ClaudeCodeTexts.KeyShift + name
                    : name;
                keys.Add(new ScreenKey(Send(key), Name(key), action));
            }
        }

        return keys.Any(key => key.Send == "\x1b") ? keys : null;
    }

    private static (List<string> Lines, int First) LinesAbove(IReadOnlyList<string> rows, int hintRow)
    {
        var lines = new List<string>();
        var blanks = 0;
        var first = hintRow;
        for (var r = hintRow - 1; r >= 0 && hintRow - r <= MaxRows; r--)
        {
            var text = rows[r].Trim();
            if (text.Length == 0)
            {
                if (++blanks == 2)
                {
                    break;
                }

                continue;
            }

            if (LineClassifier.IsChrome(text) || LineClassifier.IsBarePrompt(text)
                || LineClassifier.Classify(text, inPromptBlock: false) is not (LineKind.Plain or LineKind.Prompt))
            {
                break;
            }

            blanks = 0;
            lines.Add(text);
            first = r;
        }

        lines.Reverse();
        return (lines, first);
    }

    private static IEnumerable<string> Tabs(string row) =>
        Regex.Split(row, ClaudeCodeTexts.TabGapPattern).Select(tab => tab.Trim()).Where(tab => tab.Length > 0);

    /// <summary>What goes to Claude for a key its hint row names.</summary>
    private static string Send(string key) => key switch
    {
        ClaudeCodeTexts.KeyEsc => "\x1b",
        ClaudeCodeTexts.KeyEnter => "\r",
        ClaudeCodeTexts.KeySpace => " ",
        ClaudeCodeTexts.KeyTab => "\t",
        ClaudeCodeTexts.KeyLeft => "\x1b[D",
        ClaudeCodeTexts.KeyRight => "\x1b[C",
        ClaudeCodeTexts.KeyUp => "\x1b[A",
        ClaudeCodeTexts.KeyDown => "\x1b[B",
        ClaudeCodeTexts.KeyShiftLeft => "\x1b[1;2D",
        ClaudeCodeTexts.KeyShiftRight => "\x1b[1;2C",
        ClaudeCodeTexts.KeyShiftUp => "\x1b[1;2A",
        ClaudeCodeTexts.KeyShiftDown => "\x1b[1;2B",
        // "ctrl+j": the control character of the letter.
        { Length: 6 } when key.StartsWith(ClaudeCodeTexts.KeyCtrl, StringComparison.Ordinal) => ((char)(key[5] - 'a' + 1)).ToString(),
        _ => key,
    };

    /// <summary>The name the user hears for a key its hint row names.</summary>
    private static string Name(string key) => key switch
    {
        ClaudeCodeTexts.KeyEsc => "Escape",
        ClaudeCodeTexts.KeyLeft => "Left",
        ClaudeCodeTexts.KeyRight => "Right",
        ClaudeCodeTexts.KeyUp => "Up",
        ClaudeCodeTexts.KeyDown => "Down",
        [ClaudeCodeTexts.KeyShift, var arrow] => "Shift+" + Name(arrow.ToString()),
        { Length: 6 } when key.StartsWith(ClaudeCodeTexts.KeyCtrl, StringComparison.Ordinal) => "Ctrl+" + char.ToUpperInvariant(key[5]),
        _ => key,
    };
}

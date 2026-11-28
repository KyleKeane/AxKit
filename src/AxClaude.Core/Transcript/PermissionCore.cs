using AxKit.Core;
using AxKit.Core.Vt;

namespace AxClaude.Core.Transcript;

/// <summary>
/// The core of a permission question, short: the tool and what it acts on ("Write permission-test.txt", "Bash cat
/// settings.json", "Create file permission-test.txt"), which every answer of the question carries in the control
/// area, so that moving through the answers says which question they belong to (SPEC.md D33, Answers; the
/// maintainer's ask of 2026-11-26). Read once here for both sources: the screen's permission question by its title and
/// its rows (<see cref="OfScreen"/>, from <see cref="QuestionReader"/>), the stream's by the tool and the subject
/// the relay's question names (<see cref="Of"/>, from <c>StreamAsking</c>). The label shown changes, the key sent
/// does not.
///
/// Two kinds of target, and one rule for each (the third review of 2026-11-27, which asked that the rule close the
/// faults by how it is made, not by a list of cases):
/// 1. A file tool's target is a path. The label is the file's name, whole, spaces and apostrophes included, never
///    parted into words and never marked: said at every file, the mark would no longer be heard where it counts.
///    (Until that day a path was parted at its blanks like a command, and "C:\Work\My Documents\notes.txt"
///    was "My notes.txt, and more".)
/// 2. Anything else is read as a command: its first words, never more than <see cref="MaxWords"/> and
///    <see cref="MaxTarget"/> characters. <see cref="MoreMark"/> stands after it exactly when the words shown are
///    not the whole command as it is written, found by comparing the two: words past the third, what follows an
///    operator, a further line, a word that was shortened or cut, blanks that are not one space. So a label without
///    the mark is the command, character for character, and a label with it never reads as all that a yes runs.
/// A space that is not the plain one, and every character neither seen nor heard, is written as its code in the
/// label (<see cref="DisplayText.VisibleCommand"/>).
/// </summary>
public static class PermissionCore
{
    /// <summary>The most words of a command the core keeps.</summary>
    public const int MaxWords = 3;

    /// <summary>The most characters of a command the core keeps, cut at a word.</summary>
    public const int MaxTarget = 40;

    /// <summary>Words of a shell that end a command's start: what follows is where its output goes or the next command.</summary>
    private static readonly string[] Operators = [">", ">>", "<", "|", "||", "&", "&&", ";", "2>", "2>&1", "1>"];

    /// <summary>
    /// The core of a question read off the screen: for a permission question (its title "Permission Required: Create
    /// file"), the kind the title names and what its rows name ("Create file permission-test.txt", "Bash command
    /// echo hello-permission, and more", "Accessing workspace example"); null for any other question, and for plan
    /// approval, whose title is a question of its own ("Ready to code?") and whose first line names no target.
    /// A kind that asks about one file or folder, by its whole name (<see cref="ClaudeCodeTexts.PathKinds"/>), names
    /// it by its path: its first row, and the rows it wrapped onto when the screen is <paramref name="width"/>
    /// columns wide (a row that fills the width goes on in the next, unless that row starts with a blank or is
    /// Claude Code's closing question), joined as one path; a path that took more than one row is marked, since where
    /// it was cut is read off the screen, not known (the fourth review of 2026-11-27, L1: "Edit file Butt" for a
    /// path cut at "...\Butt|on.cs"). Without a width, the first row alone. Whether a row fills the width and the next
    /// starts with a blank is read off <paramref name="raw"/>, the same rows as the screen has them, only their right
    /// end trimmed, and by display width (the fifth review of 2026-11-27, L3: QuestionReader trims the text, so a
    /// preview's row never started with a blank there, and a path of wide characters never filled the width). Every other kind is read as a command, whatever its title says: all the rows between the
    /// title and Claude Code's own closing question (<see cref="ClaudeCodeTexts.ClosingQuestionStart"/>) are taken
    /// for what a yes runs, since nothing on the screen tells a further line of a command from Claude's description
    /// of it, so a second row marks the label (a mark too many costs a look at the question, a mark too few hides a
    /// command). Until the third review of 2026-11-27 only a kind that ended in "command" was read so, and a
    /// question titled "Monitor" with a command of two rows was labelled by its first.
    /// </summary>
    public static string? OfScreen(string title, IReadOnlyList<string> text, int width = 0, IReadOnlyList<string>? raw = null)
    {
        if (!title.StartsWith(ClaudeCodeTexts.PermissionRequired, StringComparison.Ordinal))
        {
            return null;
        }

        var kind = title[ClaudeCodeTexts.PermissionRequired.Length..].Trim().TrimEnd(':').Trim();
        if (kind.Length == 0 || kind.EndsWith('?'))
        {
            return null;
        }

        if (ClaudeCodeTexts.PathKinds.Contains(kind, StringComparer.Ordinal))
        {
            // The rows as the screen has them: QuestionReader trims the text, and a preview's row starts with a blank
            // only before that, and fills the width by its display width, a wide character two columns (L3).
            var screen = raw is not null && raw.Count == text.Count ? raw : text;
            var path = text.Count > 0 ? text[0] : string.Empty;
            var taken = 1;
            while (width > 0 && taken < text.Count && screen[taken] is { Length: > 0 } next && Wrapped(screen[taken - 1], next, width)
                && !char.IsWhiteSpace(next[0]) && !(next.StartsWith(ClaudeCodeTexts.ClosingQuestionStart, StringComparison.Ordinal) && next.EndsWith('?')))
            {
                path += text[taken];
                taken++;
            }

            return Of(kind, path, more: taken > 1, path: true);
        }

        var rows = text.ToList();
        if (rows.Count > 1 && rows[^1].StartsWith(ClaudeCodeTexts.ClosingQuestionStart, StringComparison.Ordinal) && rows[^1].EndsWith('?'))
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return Of(kind, string.Join("\n", rows));
    }

    /// <summary>
    /// The core of the tool <paramref name="tool"/> acting on <paramref name="target"/>: the tool, then the target
    /// as the class's summary says, a path (<paramref name="path"/>) by its name, anything else as a command by its
    /// first words; the tool alone when nothing is named. <see cref="MoreMark"/> stands after a command of which not
    /// all is shown, or when the caller knows that the call holds more than the target it names
    /// (<paramref name="more"/>: further lines of a command, which the stream's question cuts before this reads it),
    /// also after a tool alone.
    /// </summary>
    public static string Of(string tool, string target, bool more = false, bool path = false)
    {
        var (shown, left) = path ? (FileName(target), false) : Shorten(target);
        var named = shown.Length == 0 ? tool.Trim() : $"{tool.Trim()} {shown}";
        return DisplayText.VisibleCommand(left || more ? named + MoreMark : named);
    }

    /// <summary>
    /// Whether a row went on in the next because the width ended: it fills the width, or the next row's first character
    /// would not have fitted into what was left of it, as a wide character two columns wide in the last column (the
    /// sixth review of 2026-11-27, L4: a path of 39 columns on a screen 40 wide, whose next character was wide, was
    /// read as its first row alone).
    /// </summary>
    private static bool Wrapped(string row, string next, int width) =>
        Columns(row) is var used && (used >= width || used + Math.Max(0, Wcwidth.Of(next.EnumerateRunes().First().Value)) > width);

    /// <summary>How many columns of the console a row takes, as the screen counts them (<see cref="Wcwidth"/>): a wide character two, a mark none.</summary>
    private static int Columns(string row) => row.EnumerateRunes().Sum(rune => Math.Max(0, Wcwidth.Of(rune.Value)));

    /// <summary>After a command of which not all is shown: the label never reads as the whole of what is asked.</summary>
    public const string MoreMark = ", and more";

    /// <summary>
    /// A command in a few words, with <see cref="MoreMark"/> exactly when they are not the whole command as written:
    /// its first line; its first words up to a shell's operator, three at most; a word that is a path by its last
    /// part, an address by its host; and no more than <see cref="MaxTarget"/> characters, cut at a word. A word that
    /// holds a character a shell acts on (<see cref="OperatorCharacters"/>), or a blank that is not a plain space or
    /// a tab, is kept as it is written and never shortened: "echo x &gt;/etc/hosts" was "echo x hosts" once.
    /// </summary>
    public static string Short(string target)
    {
        var (shortened, left) = Shorten(target);
        return left && shortened.Length > 0 ? shortened + MoreMark : shortened;
    }

    /// <summary>
    /// A path by the name of its file or folder, whole: everything after the last separator, whatever it holds
    /// ("C:\Work\My Documents\notes.txt" is "notes.txt", "C:\Temp\example\" is "example", "it's here.txt" is
    /// itself). The first line that holds something, when there are several.
    /// </summary>
    public static string FileName(string target)
    {
        var line = target.Replace("\r\n", "\n").Split('\n').Select(part => part.Trim()).FirstOrDefault(part => part.Length > 0) ?? string.Empty;
        var trimmed = line.TrimEnd('\\', '/');
        var name = trimmed[(trimmed.LastIndexOfAny(['\\', '/']) + 1)..];
        return name.Length > 0 ? name : line;
    }

    /// <summary>
    /// Characters a shell acts on when they stand in a word outside quotes: where output goes or comes from, the next
    /// command, a variable or a command put in, an assignment, a group, a list in braces. A dollar sign and a
    /// backtick count inside double quotes too, where a shell still puts a command's output in. A word with one of
    /// them is shown as written.
    /// </summary>
    private const string OperatorCharacters = "<>|;&=$()`{}";

    /// <summary><see cref="Short"/>'s words, and whether they are anything but the whole command as it is written.</summary>
    private static (string Text, bool Left) Shorten(string target)
    {
        // The command as written, but for the blanks and line ends around it.
        var whole = target.Replace("\r\n", "\n").Trim();
        var line = whole.Split('\n').Select(part => part.Trim()).FirstOrDefault(part => part.Length > 0) ?? string.Empty;
        var all = Words(line);
        var words = new List<string>();
        foreach (var (word, acts) in all)
        {
            if (words.Count == MaxWords || Operators.Contains(word))
            {
                break;
            }

            words.Add(acts ? word : LastPart(word));
        }

        var shortened = string.Empty;
        foreach (var word in words)
        {
            var next = shortened.Length == 0 ? word : shortened + " " + word;
            if (next.Length > MaxTarget)
            {
                if (shortened.Length == 0)
                {
                    shortened = word[..MaxTarget];
                }

                break;
            }

            shortened = next;
        }

        // Compared, not worked out case by case: whatever made the words shown differ from what is written marks it.
        return (shortened, shortened != whole);
    }

    /// <summary>
    /// The words of a line, parted at spaces and tabs outside quotes, each with whether it is to be shown as written:
    /// it holds one of <see cref="OperatorCharacters"/> where a shell acts on it, or a blank of another kind (a
    /// no-break space, an em space), which parts nothing here and may part words in a shell.
    /// </summary>
    private static List<(string Word, bool Acts)> Words(string line)
    {
        var words = new List<(string, bool)>();
        var word = new System.Text.StringBuilder();
        var acts = false;
        var quote = '\0';
        foreach (var c in line)
        {
            if (quote == '\0' && c is ' ' or '\t')
            {
                if (word.Length > 0)
                {
                    words.Add((word.ToString(), acts));
                    word.Clear();
                    acts = false;
                }

                continue;
            }

            if (c is not (' ' or '\t') && (char.IsWhiteSpace(c) || char.IsControl(c)))
            {
                acts = true;
            }
            else if (quote == '\0' && c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == quote)
            {
                quote = '\0';
            }
            else if (quote == '\0' ? OperatorCharacters.Contains(c) : quote == '"' && c is '$' or '`')
            {
                acts = true;
            }

            word.Append(c);
        }

        if (word.Length > 0)
        {
            words.Add((word.ToString(), acts));
        }

        return words;
    }

    /// <summary>
    /// A word of a command that is a path by its last part ("C:\Temp\example" is "example", "src/a.cs" is "a.cs"),
    /// an address by its host; any other word as it is. A path has a separator after its first character: "/c" and
    /// "/home" are a switch and a word, kept ("cmd /c del x" named "c" until the third review of 2026-11-27).
    /// </summary>
    private static string LastPart(string word)
    {
        var unquoted = word.Trim('"', '\'');
        var scheme = unquoted.IndexOf("://", StringComparison.Ordinal);
        if (scheme > 0)
        {
            var rest = unquoted[(scheme + 3)..];
            var slash = rest.IndexOf('/');
            return slash > 0 ? rest[..slash] : rest;
        }

        var trimmed = unquoted.TrimEnd('\\', '/');
        var separator = trimmed.LastIndexOfAny(['\\', '/']);
        if (separator <= 0)
        {
            return word;
        }

        var last = trimmed[(separator + 1)..];
        return last.Length > 0 ? last : word;
    }
}

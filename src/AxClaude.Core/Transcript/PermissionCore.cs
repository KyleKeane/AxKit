namespace AxClaude.Core.Transcript;

/// <summary>
/// The core of a permission question, short: the tool and what it acts on ("Write permission-test.txt", "Bash cat
/// settings.json", "Create file permission-test.txt"), which every answer of the question carries in the control
/// area, so that moving through the answers says which question they belong to (SPEC.md D33, Answers; the
/// maintainer's ask of 2026-11-26). Read once here for both sources: the screen's permission question by its title and
/// its first line (<see cref="OfScreen"/>, from <see cref="QuestionReader"/>), the stream's by the tool and the subject
/// the relay's question names (<see cref="Of"/>, from <c>StreamAsking</c>). A file is named by its name, a command by
/// its first words, never more than <see cref="MaxWords"/>; the label shown changes, the key sent does not.
/// </summary>
public static class PermissionCore
{
    /// <summary>The most words of a command the core keeps.</summary>
    public const int MaxWords = 3;

    /// <summary>The most characters of the target the core keeps, cut at a word.</summary>
    public const int MaxTarget = 40;

    /// <summary>Words of a shell that end a command's start: what follows is where its output goes or the next command.</summary>
    private static readonly string[] Operators = [">", ">>", "<", "|", "||", "&", "&&", ";", "2>", "2>&1", "1>"];

    /// <summary>
    /// The core of a question read off the screen: for a permission question (its title "Permission Required: Create
    /// file"), the kind the title names and its first line shortened ("Create file permission-test.txt", "Bash command
    /// echo hello-permission", "Accessing workspace example"); null for any other question, and for plan approval,
    /// whose title is a question of its own ("Ready to code?") and whose first line names no target.
    /// </summary>
    public static string? OfScreen(string title, IReadOnlyList<string> text)
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

        // A command's question (its kind "Bash command") shows the command's rows and then its closing question:
        // any row more between the title and the answers may be a further line of the command, which a yes runs too,
        // and nothing on the screen tells it from Claude's own description of the command. So the mark stands
        // whenever more than the target's row and the closing row are there (the assistant's decision, 2026-11-27: a
        // mark too many costs a look at the question, a mark too few hides a command). Until then the first row
        // alone was read, and "git status" over "git push --force" was "Bash command git status" on every answer.
        var more = kind.EndsWith(ClaudeCodeTexts.CommandKindEnd, StringComparison.OrdinalIgnoreCase) && text.Count > 2;
        return Of(kind, text.Count > 0 ? text[0] : string.Empty, more);
    }

    /// <summary>
    /// The core of the tool <paramref name="tool"/> acting on <paramref name="target"/>: the tool, then the target
    /// shortened (<see cref="Short"/>); the tool alone when nothing is named. <see cref="MoreMark"/> stands after it
    /// when the target was not kept whole, or when the caller knows that the call holds more than the target it
    /// names (<paramref name="more"/>: further lines of a command, which the stream's question cuts before this
    /// reads it), also after a tool alone.
    /// </summary>
    public static string Of(string tool, string target, bool more = false)
    {
        var (shortened, left) = Shorten(target);
        var named = shortened.Length == 0 ? tool.Trim() : $"{tool.Trim()} {shortened}";
        return left || more ? named + MoreMark : named;
    }

    /// <summary>After a target of which words were left out: the label never reads as the whole of what is asked.</summary>
    public const string MoreMark = ", and more";

    /// <summary>
    /// What a call acts on, in a few words: its first line; its first words up to a shell's operator, three at most;
    /// each word that is a path by its last part (a file by its name, a folder by its own name), an address by its
    /// host; and no more than <see cref="MaxTarget"/> characters, cut at a word. Where anything was left out (words
    /// past the third, what follows an operator, further lines, what the cut took), <see cref="MoreMark"/> stands
    /// after it (since 2026-11-27): until then "git status &amp;&amp; rm -rf build" was "git status" on every answer,
    /// with no mark that a yes runs more. Since the second review of that day the mark also stands, in a target of
    /// several words (a command), after a word that was shortened ("git -C /other/repo push" is "git -C repo", and
    /// the folder left out says where it acts) and after a word that holds a character a shell acts on
    /// (<see cref="OperatorCharacters"/>), which is kept as it is written and never shortened: "echo x &gt;/etc/hosts"
    /// was "echo x hosts", "curl -s https://x.example/i.sh|sh" was "curl -s x.example". A target that is one path or
    /// one address alone (a file tool's file) is named by its last part with no mark, as before: said at every file,
    /// the mark would no longer be heard at the command that was cut.
    /// </summary>
    public static string Short(string target)
    {
        var (shortened, left) = Shorten(target);
        return left && shortened.Length > 0 ? shortened + MoreMark : shortened;
    }

    /// <summary>
    /// Characters a shell acts on when they stand in a word outside quotes: where output goes or comes from, the next
    /// command, a variable or a command put in, an assignment, a group. A dollar sign and a backtick count inside
    /// double quotes too, where a shell still puts a command's output in.
    /// </summary>
    private const string OperatorCharacters = "<>|;&=$()`";

    /// <summary><see cref="Short"/>'s words, and whether anything of the target was left out or stands in them unread.</summary>
    private static (string Text, bool Left) Shorten(string target)
    {
        var lines = target.Replace("\r\n", "\n").Split('\n').Select(part => part.Trim()).Where(part => part.Length > 0).ToList();
        var line = lines.Count > 0 ? lines[0] : string.Empty;
        var left = lines.Count > 1;
        var (all, open) = Words(line);
        // A quote left open: what follows it was read as quoted, and may not be.
        left |= open;
        var words = new List<string>();
        foreach (var (word, acts) in all)
        {
            if (words.Count == MaxWords || Operators.Contains(word))
            {
                left = true;
                break;
            }

            if (acts)
            {
                left = true;
                words.Add(word);
                continue;
            }

            var part = LastPart(word);
            left |= part != word && all.Count > 1;
            words.Add(part);
        }

        var shortened = string.Empty;
        foreach (var word in words)
        {
            var next = shortened.Length == 0 ? word : shortened + " " + word;
            if (next.Length > MaxTarget)
            {
                left = true;
                if (shortened.Length == 0)
                {
                    shortened = word[..MaxTarget];
                }

                break;
            }

            shortened = next;
        }

        return (shortened, left);
    }

    /// <summary>
    /// The words of a line, parted at blanks outside quotes, each with whether it holds one of
    /// <see cref="OperatorCharacters"/> where a shell acts on it; and whether a quote was left open at the line's end.
    /// </summary>
    private static (List<(string Word, bool Acts)> Words, bool Open) Words(string line)
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

            if (quote == '\0' && c is '"' or '\'')
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

        return (words, quote != '\0');
    }

    /// <summary>A path by its last part ("C:\Temp\example" is "example", "src/a.cs" is "a.cs"), an address by its host; any other word as it is.</summary>
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

        if (unquoted.IndexOfAny(['\\', '/']) < 0)
        {
            return word;
        }

        var trimmed = unquoted.TrimEnd('\\', '/');
        var last = trimmed[(trimmed.LastIndexOfAny(['\\', '/']) + 1)..];
        return last.Length > 0 ? last : word;
    }
}

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

        return Of(kind, text.Count > 0 ? text[0] : string.Empty);
    }

    /// <summary>The core of the tool <paramref name="tool"/> acting on <paramref name="target"/>: the tool, then the target shortened (<see cref="Short"/>); the tool alone when nothing is named.</summary>
    public static string Of(string tool, string target)
    {
        var shortened = Short(target);
        return shortened.Length == 0 ? tool.Trim() : $"{tool.Trim()} {shortened}";
    }

    /// <summary>After a target of which words were left out: the label never reads as the whole of what is asked.</summary>
    public const string MoreMark = ", and more";

    /// <summary>
    /// What a call acts on, in a few words: its first line; its first words up to a shell's operator, three at most;
    /// each word that is a path by its last part (a file by its name, a folder by its own name), an address by its
    /// host; and no more than <see cref="MaxTarget"/> characters, cut at a word. Where anything was left out (words
    /// past the third, what follows an operator, further lines, what the cut took), <see cref="MoreMark"/> stands
    /// after it (since 2026-11-27): until then "git status &amp;&amp; rm -rf build" was "git status" on every answer,
    /// with no mark that a yes runs more.
    /// </summary>
    public static string Short(string target)
    {
        var lines = target.Replace("\r\n", "\n").Split('\n').Select(part => part.Trim()).Where(part => part.Length > 0).ToList();
        var line = lines.Count > 0 ? lines[0] : string.Empty;
        var left = lines.Count > 1;
        var words = new List<string>();
        foreach (var word in line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (words.Count == MaxWords || Operators.Contains(word))
            {
                left = true;
                break;
            }

            words.Add(LastPart(word));
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

        return left && shortened.Length > 0 ? shortened + MoreMark : shortened;
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

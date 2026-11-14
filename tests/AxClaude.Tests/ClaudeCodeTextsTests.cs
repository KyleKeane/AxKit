namespace AxClaude.Tests;

/// <summary>
/// Claude Code's own texts are written in one place, <c>ClaudeCodeTexts</c> (root CLAUDE.md rule 12, repair R-28):
/// when Claude Code changes a word, that file is adjusted and nothing else. Read off the sources: one of the most
/// characteristic texts inside a string literal of any other file of axClaude's two source folders fails here.
/// Comments are not looked at, since they quote Claude's rows to explain the code: a line that starts with two
/// slashes is skipped, and on any other line only what stands between double quotes counts, up to a comment that
/// starts outside a string. A block comment or a raw string over several lines is not understood: each of its lines
/// is read as a line of code (the F1 text is such a string, and is allowed by name).
/// </summary>
public sealed class ClaudeCodeTextsTests
{
    /// <summary>The table, the one file allowed to hold the texts.</summary>
    private static readonly string Table = Path.Combine("src", "AxClaude.Core", "Transcript", "ClaudeCodeTexts.cs");

    /// <summary>The other files allowed to hold a watched text, with why.</summary>
    private static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // The F1 text tells the user which lines c and p stop at, by the label as it stands in the conversation: words
        // for the user, matched against nothing. It is changed by hand should Claude Code ever rename the label.
        [Path.Combine("src", "AxClaude", "HelpText.cs")] = "help text for the user",
    };

    /// <summary>The texts watched: labels, prompt rows and hint rows that only Claude Code prints, never words of axKit's own.</summary>
    private static readonly string[] Watched =
    [
        "claude:", "you:", "tool:", "thinking:",
        "Enter selection", "Enter y/n", "Select with numbers", "Enter text for", "Enter a number to change", "Permission Required",
        "Any key to continue", "Press Enter to continue", "Esc to", "to cancel edit",
        "esc to interrupt", "shift+tab to cycle", "ctrl+o to", "to send now", "to manage",
        "Screen Reader Mode", "Feedback drafts", "Review your answers", ": Dismiss", "Don't ask again",
    ];

    [Fact]
    public void Claude_Codes_texts_are_written_in_the_table_alone()
    {
        var root = RepositoryRoot();
        var offenders = new List<string>();
        foreach (var folder in new[] { Path.Combine(root, "src", "AxClaude.Core"), Path.Combine(root, "src", "AxClaude") })
        {
            foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file);
                if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || relative.Equals(Table, StringComparison.OrdinalIgnoreCase) || Allowed.ContainsKey(relative))
                {
                    continue;
                }

                var number = 0;
                foreach (var line in File.ReadLines(file))
                {
                    number++;
                    foreach (var literal in StringLiterals(line))
                    {
                        foreach (var text in Watched)
                        {
                            if (literal.Contains(text, StringComparison.Ordinal))
                            {
                                offenders.Add($"{relative}:{number}: \"{text}\"");
                            }
                        }
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, "A text of Claude Code's outside ClaudeCodeTexts, where it belongs (rule 12): " + string.Join("; ", offenders));
    }

    /// <summary>The list above is real: the table's file names every text watched, as a constant, inside a pattern or in an entry's comment.</summary>
    [Fact]
    public void Every_watched_text_is_in_the_table()
    {
        var table = File.ReadAllText(Path.Combine(RepositoryRoot(), Table));
        // A regex in the table writes "+" as "\+".
        var plain = table.Replace(@"\+", "+");
        Assert.All(Watched, text => Assert.Contains(text, plain));
    }

    [Theory]
    [InlineData("var a = \"claude:\"; // \"tool:\"", "claude:")]
    [InlineData("    // text.StartsWith(\"claude:\")", "")]
    [InlineData("    /// <summary>The <c>\"you:\"</c> row.</summary>", "")]
    [InlineData("Find(@\"C:\\a\"\"b\", \"x\\\"y // z\") // \"no\"", "C:\\a\"\"b|x\\\"y // z")]
    [InlineData("var c = '\"'; var s = \"after\";", "after")]
    [InlineData("Is('\\'', '\\u00A0', \"after\")", "after")]
    public void String_literals_are_found_and_comments_left_out(string line, string expected)
    {
        Assert.Equal(expected, string.Join("|", StringLiterals(line)));
    }

    /// <summary>
    /// What stands between double quotes on one line of C#, escapes left as written: plain strings with backslash
    /// escapes, verbatim strings with doubled quotes, a quote as a character ('"') passed over; nothing of a line
    /// that is a comment, and nothing after a comment that starts outside a string.
    /// </summary>
    private static List<string> StringLiterals(string line)
    {
        var found = new List<string>();
        if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            return found;
        }

        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                break;
            }

            if (c == '\'')
            {
                // A character: 'x', '"', '\'' or ' ': past what it holds, then to its closing quote.
                i += i + 1 < line.Length && line[i + 1] == '\\' ? 3 : 2;
                while (i < line.Length && line[i] != '\'')
                {
                    i++;
                }

                i++;
                continue;
            }

            if (c != '"')
            {
                i++;
                continue;
            }

            var verbatim = i > 0 && (line[i - 1] == '@' || (line[i - 1] == '$' && i > 1 && line[i - 2] == '@'));
            var start = ++i;
            while (i < line.Length)
            {
                if (verbatim && line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                if (!verbatim && line[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (line[i] == '"')
                {
                    break;
                }

                i++;
            }

            found.Add(line[start..Math.Min(i, line.Length)]);
            i++;
        }

        return found;
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AxKit.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("AxKit.sln not found above the test directory.");
    }
}

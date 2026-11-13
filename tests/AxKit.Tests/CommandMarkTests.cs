namespace AxKit.Tests;

/// <summary>
/// The marks of the windows' commands (docs/axKit/app-kit.md AK-2, docs/axKit/mcp.md MC-3.1, MC-7.3, MC-D7): an AI tool
/// runs a command on its own word unless it is marked act, when the user is asked first. A forgotten mark would let a
/// command run unasked, so every command made in <c>src</c> states its mark, and the commands that start a program,
/// change what lies outside the window or bring in what the AI tools could not read are pinned as act. Read off the
/// sources, as <see cref="ListTests"/> does.
/// </summary>
public sealed class CommandMarkTests
{
    /// <summary>Commands that must stay act, by file and label as written, each with why (the review of 2026-11-11).</summary>
    private static readonly (string File, string Label, string Why)[] PinnedAct =
    [
        (Path.Combine("src", "AxFile", "FileForm.cs"), "&Open", "a file opens in its program, which may be the file itself run"),
        (Path.Combine("src", "AxFile", "FileForm.cs"), "&Delete", "deletes"),
        (Path.Combine("src", "AxFile", "FileForm.cs"), "&Paste", "copies or moves files"),
        (Path.Combine("src", "AxFile", "FileForm.cs"), "Windows men&u...", "the shell's menu runs anything"),
        (Path.Combine("src", "AxHelp", "HelpForm.cs"), "&Open the conversation", "starts Claude Code in an axClaude window (MC-6.5's NeverOpened)"),
        (Path.Combine("src", "AxHelp", "HelpForm.cs"), "Put a picture &file into the conversation...", "starts Claude Code in an axClaude window"),
        (Path.Combine("src", "AxKit.Forms", "TextEditorForm.Viewer.cs"), "Read the clipboard's &picture", "brings in the clipboard, which the AI tools could not read (MC-7.6)"),
        (Path.Combine("src", "AxGit", "GitForm.cs"), "&Stage or unstage", "changes the repository"),
        (Path.Combine("src", "AxGit", "GitForm.cs"), "Stage &all", "changes the repository"),
        (Path.Combine("src", "AxGit", "GitForm.cs"), "&Unstage all", "changes the repository"),
        (Path.Combine("src", "AxStart", "StartForm.cs"), "&Start", "starts a program"),
        (Path.Combine("src", "AxStart", "StartForm.cs"), "Start as &administrator", "starts a program"),
        (Path.Combine("src", "AxClaude", "MainForm.Ui.cs"), "&New session...", "starts Claude Code"),
        (Path.Combine("src", "AxClaude", "MainForm.Ui.cs"), "Force Claude to res&tart", "starts Claude Code"),
        (Path.Combine("src", "AxTerminal", "TerminalForm.Ui.cs"), "&New session...", "starts a shell"),
        (Path.Combine("src", "AxTerminal", "TerminalForm.Ui.cs"), "Res&tart shell", "starts a shell"),
        (Path.Combine("src", "AxMCP", "MCPForm.cs"), "&Call, or fetch the prompt", "runs a server's tool"),
        (Path.Combine("src", "AxCalc", "CalcForm.cs"), "&Copy answer", "writes the user's clipboard"),
        (Path.Combine("src", "AxFile", "FileForm.cs"), "Copy pat&h", "writes the user's clipboard"),
        (Path.Combine("src", "AxProcess", "ProcessForm.cs"), "&Copy line", "writes the user's clipboard"),
    ];

    [Fact]
    public void Every_command_states_its_mark()
    {
        var root = LayoutTests.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var (at, span) in Commands(text))
            {
                if (!span.Contains("Mark = CommandMark.", StringComparison.Ordinal))
                {
                    offenders.Add($"{relative}:{LineOf(text, at)}: {span.Split('\n')[0].Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A command without its Mark (an AI tool would run it unasked, MC-7.3): " + string.Join("; ", offenders));
    }

    [Fact]
    public void Commands_that_act_outside_their_window_stay_marked_act()
    {
        var root = LayoutTests.RepositoryRoot();
        foreach (var (file, label, why) in PinnedAct)
        {
            var text = File.ReadAllText(Path.Combine(root, file));
            var spans = Commands(text).Where(command => command.Span.Contains($"new Command(\"{label}\"", StringComparison.Ordinal)).ToList();
            Assert.True(spans.Count > 0, $"{file}: no command {label}");
            Assert.All(spans, command => Assert.True(command.Span.Contains("Mark = CommandMark.Act", StringComparison.Ordinal), $"{file}: {label} must be marked act: {why}"));
        }
    }

    /// <summary>
    /// The menu items made outside the command list that do something on a click and state no mark
    /// (<c>CommandPalette.Marked</c>), counted on 2026-11-13 (docs/axKit/app-kit.md AK-2.5). Such an item is act for the
    /// AI tools, which ask the user before running it: safe, but a question more than needed. The number may only go
    /// down: a new item is a command or states its mark, and an item converted lowers it here.
    /// </summary>
    private const int UnmarkedItemsAtMost = UnmarkedItemsCounted;

    private const int UnmarkedItemsCounted = 93;

    [Fact]
    public void Menu_items_without_a_mark_never_grow_in_number()
    {
        var root = LayoutTests.RepositoryRoot();
        var unmarked = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("new ToolStripMenuItem(", StringComparison.Ordinal) && line.Contains("(_, _) =>", StringComparison.Ordinal)
                    && !line.Contains("CommandPalette.Marked(", StringComparison.Ordinal) && !line.Contains("NeverForAiTools(", StringComparison.Ordinal))
                {
                    unmarked.Add($"{relative}:{i + 1}");
                }
            }
        }

        Assert.True(unmarked.Count <= UnmarkedItemsAtMost, $"{unmarked.Count} menu items without a mark, at most {UnmarkedItemsAtMost} allowed (AK-2.5): make the new one a command or give it CommandPalette.Marked. " + string.Join("; ", unmarked));
    }

    /// <summary>Every <c>new Command(…)</c> of a source with its object initializer, if it has one, and where it starts.</summary>
    private static IEnumerable<(int At, string Span)> Commands(string text)
    {
        const string Start = "new Command(";
        for (var at = text.IndexOf(Start, StringComparison.Ordinal); at >= 0; at = text.IndexOf(Start, at + 1, StringComparison.Ordinal))
        {
            var end = Closing(text, at + Start.Length - 1, '(', ')');
            var next = end + 1;
            while (next < text.Length && char.IsWhiteSpace(text[next]))
            {
                next++;
            }

            if (next < text.Length && text[next] == '{')
            {
                end = Closing(text, next, '{', '}');
            }

            yield return (at, text[at..(end + 1)]);
        }
    }

    /// <summary>The index of the bracket that closes the one at <paramref name="open"/>, string and character literals skipped.</summary>
    private static int Closing(string text, int open, char opening, char closing)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '"' or '\'')
            {
                i++;
                while (i < text.Length && text[i] != c)
                {
                    if (text[i] == '\\')
                    {
                        i++;
                    }

                    i++;
                }

                continue;
            }

            if (c == opening)
            {
                depth++;
            }
            else if (c == closing && --depth == 0)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"No closing {closing} after index {open}");
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;
}

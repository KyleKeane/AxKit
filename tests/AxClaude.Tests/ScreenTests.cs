using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>Screens of Claude's that wait on a key-hint row or a tab row rather than a prompt row (docs/axClaude/claude-screens.md, D33). The rows are the ones Claude Code 2.1.281 drew.</summary>
public class ScreenTests
{
    [Fact]
    public void The_tasks_recording_waits_on_its_hint_row()
    {
        var model = TestHelpers.ParseFixture("tasks-screen-claude2.1.281-240x50.vt");

        var screen = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.True(model.PromptPending);
        Assert.Null(model.PendingQuestion);
        Assert.Equal("Background", screen.Title);
        Assert.Equal(["No tasks currently running"], screen.Lines);
        Assert.Equal(["Up", "Down", "Enter", "Escape"], screen.Keys.Select(k => k.Name));
        Assert.Equal(["\x1b[A", "\x1b[B", "\r", "\x1b"], screen.Keys.Select(k => k.Send));
        Assert.Equal(["select", "select", "view", "close"], screen.Keys.Select(k => k.Action));
    }

    [Fact]
    public void A_tab_screen_takes_its_lines_from_below_the_tab_row()
    {
        string[] rows =
        [
            "you: /status",
            "Settings  Status   Config   Usage   Stats",
            "Version: 2.1.281",
            "",
            "Model: Default (Opus 5.5 with 1M context)",
            "Esc to cancel",
        ];
        var screen = ScreenReader.Read(rows, 1)!;
        Assert.Equal("Tabs: Settings, Status, Config, Usage, Stats", screen.Title);
        Assert.Equal(["Version: 2.1.281", "Model: Default (Opus 5.5 with 1M context)"], screen.Lines);
        Assert.Equal(["Escape"], screen.Keys.Select(k => k.Name));
    }

    [Fact]
    public void Hint_rows_name_their_keys()
    {
        string[] autocompact = ["you: /autocompact", "Auto-compact window", "Current setting: 1m tokens", "Select auto-compact window:  auto", "←/→ to adjust · Enter to apply · Esc to cancel"];
        Assert.Equal(["Left", "Right", "Enter", "Escape"], ScreenReader.Read(autocompact, 4)!.Keys.Select(k => k.Name));

        string[] details = ["you: /tasks", "Shell details", "Status: running", "← to go back · Esc/Enter/Space to close · x to stop"];
        var shell = ScreenReader.Read(details, 3)!;
        Assert.Equal("Shell details", shell.Title);
        Assert.Equal(["Left", "Escape", "Enter", "Space", "x"], shell.Keys.Select(k => k.Name));
        Assert.Equal("stop", shell.Keys[^1].Action);

        string[] goal = ["you: /goal", "Goal", "No goal set", "/goal <condition> to set one", "Esc to dismiss"];
        Assert.Equal(["No goal set", "/goal <condition> to set one"], ScreenReader.Read(goal, 4)!.Lines);
    }

    [Fact]
    public void The_prompt_and_rows_without_escape_are_no_screen()
    {
        string[] idle = ["claude: done", "auto mode on (shift+tab to cycle)", "$"];
        Assert.Null(ScreenReader.Read(idle, 2));
        Assert.Null(ScreenReader.Keys("Enter to confirm"));
        Assert.Null(ScreenReader.Keys("Showing detailed transcript · ctrl+o to toggle"));
        Assert.Null(ScreenReader.Keys("Then run /rc to continue this session from your phone"));

        // A hint row above the prompt row does not make the prompt a screen.
        Assert.Null(ScreenReader.Read(["Esc to close", "$"], 1));
    }

    [Fact]
    public void Mobile_leaves_the_cursor_under_its_hint_row()
    {
        // Recorded with Claude Code 2.1.282.
        string[] rows =
        [
            "you: /mobile",
            "Get the Claude app on your phone",
            "Visit on your phone:  claude.ai/mobile",
            "Esc to close",
            "Then run /rc to continue this session from your phone",
        ];
        var screen = ScreenReader.Read(rows, 4)!;
        Assert.Equal("Get the Claude app on your phone", screen.Title);
        Assert.Equal(["Visit on your phone:  claude.ai/mobile", "Then run /rc to continue this session from your phone"], screen.Lines);
        Assert.Equal(["Escape"], screen.Keys.Select(k => k.Name));
        Assert.Equal(1, screen.FirstRow);
    }

    [Fact]
    public void Login_ends_on_a_screen_that_waits_on_enter()
    {
        // Recorded with Claude Code 2.1.323 (2026-11-13), the address as the redacting dump shows it. Unseen, this screen took the Enter of
        // the user's next message, which never reached Claude.
        string[] rows =
        [
            "you: /login",
            "Logged in as <email>",
            "Login successful. Press Enter to continue…",
            "",
        ];
        var screen = ScreenReader.Read(rows, 2)!;
        Assert.Equal("Login successful", screen.Title);
        Assert.Equal(["Logged in as <email>"], screen.Lines);
        Assert.Equal([("Enter", "continue", "\r")], screen.Keys.Select(k => (k.Name, k.Action, k.Send)));
        Assert.Equal(1, screen.FirstRow);
        // A list, not a notice: Enter is its key, and Enter on any line continues.
        Assert.False(ScreenActions.IsInformation(screen));
        Assert.Equal("\r", ScreenActions.EnterOnLine(screen)!.Send);

        // The same frame through the model: the screen waits, so the message field gives way to it.
        const string Esc = "\x1b";
        var model = new SessionModel(240, 50);
        TestHelpers.Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[Hyou: /login\r\nLogged in as <email>\r\nLogin successful. Press Enter to continue…{Esc}[?25h");
        model.EndFrame();
        Assert.True(model.PromptPending);
        Assert.Equal("Login successful", model.PendingScreen!.Title);

        // The words alone, three full stops for the ellipsis, are a screen too; Claude quoting them in a reply is not.
        Assert.Equal("Press Enter to continue", ScreenReader.Read(["you: /x", "Press Enter to continue..."], 1)!.Title);
        Assert.Null(ScreenReader.Read(["you: hi", "claude: It says Press Enter to continue…"], 1));
        Assert.Null(ScreenReader.Read(["you: hi", "Then run /rc to continue this session from your phone"], 1));
    }

    [Fact]
    public void The_permissions_screen_names_the_slash_key_since_2_1_327()
    {
        // Recorded with Claude Code 2.1.327 (2026-11-17), the day the version came. Its hint row names the key "/"
        // itself, "/ to search", where the pattern of a hint's keys knew "/" only as what joins two keys. The row was
        // taken for no hint row, so the screen was no screen: /permissions showed nothing in the window and said
        // nothing (found by the live script 12-questions.txt). The rows are those of the frame.
        string[] rows =
        [
            "you: /permissions",
            "Permissions  Recently blocked   Allow   Ask   Deny   Auto mode   Workspace",
            "Claude Code won't ask for these. Rules off in auto mode aren't listed.",
            "Search…",
            "1. Add a new rule…",
            "↓ to navigate · ←/→ to switch · / to search · Esc to close",
        ];
        var screen = ScreenReader.Read(rows, 1)!;
        Assert.Equal("Tabs: Permissions, Recently blocked, Allow, Ask, Deny, Auto mode, Workspace", screen.Title);
        Assert.True(screen.Tabbed);
        Assert.Equal(["Claude Code won't ask for these. Rules off in auto mode aren't listed.", "Search…", "1. Add a new rule…"], screen.Lines);

        // Since 2026-11-25 the search's key is offered as "Search the rules (/)", which sends the slash; the search row it
        // moves to is a screen of its own (the permissions-search fixture, below). The keys are the hint row's, in its order.
        Assert.Equal([("Down", "navigate"), ("Left", "switch"), ("Right", "switch"), ("/", "search"), ("Escape", "close")], screen.Keys.Select(k => (k.Name, k.Action)));
        Assert.Equal("/", screen.Keys[3].Send);
        Assert.Equal("Search the rules (/)", ScreenActions.KeyItem(screen.Keys[3]));
        Assert.True(ScreenActions.Closes(ScreenActions.LeaveKey(screen)));

        // The row of 2.1.326, which named no such key, reads as it did.
        Assert.Equal(["Left", "Right", "Down", "Escape"], ScreenReader.Keys("←/→ to switch · ↓ to select · Esc to cancel")!.Select(k => k.Name));

        // The slash alone is a key; between two keys it joins them, as ever. A row without Escape is no hint row still.
        Assert.Equal(["/", "Escape"], ScreenReader.Keys("/ to search · Esc to close")!.Select(k => k.Name));
        Assert.Equal(["Escape", "Enter", "Space"], ScreenReader.Keys("Esc/Enter/Space to close")!.Select(k => k.Name));
        Assert.Null(ScreenReader.Keys("/ to search"));
        Assert.Null(ScreenReader.Keys("Esc// to close"));
    }

    [Fact]
    public void The_permissions_recording_of_2_1_327_is_a_screen_until_it_is_closed()
    {
        const string fixture = "permissions-claude2.1.327-240x50.vt";
        var model = new SessionModel(240, 50);

        // Up to the frame that draws the screen: the command's echo, the tab row with the cursor on the tab chosen,
        // and the rows under it down to the hint row.
        Replay(model, fixture, 0, 1216);
        var screen = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.True(model.PromptPending);
        Assert.Null(model.PendingQuestion);
        Assert.Equal("Tabs: Permissions, Recently blocked, Allow, Ask, Deny, Auto mode, Workspace", screen.Title);
        Assert.Equal(["Claude Code won't ask for these. Rules off in auto mode aren't listed.", "Search…", "1. Add a new rule…"], screen.Lines);
        Assert.Equal(["Down", "Left", "Right", "/", "Escape"], screen.Keys.Select(k => k.Name));

        // Right: the next tab. Claude Code draws only the rows that changed, and it is the same screen with other lines.
        Replay(model, fixture, 1626, 1626);
        var next = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.Equal(screen.Signature, next.Signature);
        Assert.Equal(["Claude Code will always ask for confirmation before using these tools.", "Search…", "1. Add a new rule…"], next.Lines);

        // Escape, and then /exit: nothing waits any more.
        Replay(model, fixture, 1831, int.MaxValue);
        Assert.Null(model.PendingScreen);
        Assert.False(model.PromptPending);
        Assert.Contains("Permissions dialog dismissed", TestHelpers.Visible(model));
    }

    [Fact]
    public void The_permissions_search_is_a_screen_being_searched()
    {
        // Recorded with Claude Code 2.1.332 (2026-11-25) in a folder whose settings allow three rules: /permissions, then
        // "/", "g", "i" and "t" (the last two echoed onto the row without a frame), Down, Up, "z" twice, Escape three
        // times. Until 2026-11-25 the search row read as no screen, and the message field came back over a field of
        // Claude Code's that took the next message as its text.
        const string fixture = "permissions-search-claude2.1.332-240x50.vt";
        const string tabs = "Tabs: Permissions, Recently blocked, Allow, Ask, Deny, Auto mode, Workspace";
        var model = new SessionModel(240, 50);

        // The screen as it opens, the search offered as an item.
        Replay(model, fixture, 0, 2064);
        var screen = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.False(screen.Searching);
        Assert.Equal(["Claude Code won't ask before using allowed tools.", "Search…", "1. Add a new rule…", "2. Bash(git status)", "3. Bash(npm test)", "4. WebSearch"], screen.Lines);
        Assert.Contains("Search the rules (/)", ScreenActions.Items(screen));

        // "/": only the hint row changes, and the cursor moves to the search row. A screen being searched, nothing typed:
        // the window shows a field. Escape goes back, Down goes into the rules.
        Replay(model, fixture, 2501, 2501);
        var empty = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.True(empty.Searching);
        Assert.False(empty.SearchTyped);
        Assert.True(model.PromptPending);
        Assert.Equal(tabs, empty.Title);
        Assert.Equal(["Search…", "1. Add a new rule…", "2. Bash(git status)", "3. Bash(npm test)", "4. WebSearch"], empty.Lines);
        Assert.Equal([("Down", "navigate"), ("Escape", "go back")], empty.Keys.Select(k => (k.Name, k.Action)));
        Assert.False(ScreenActions.IsInformation(empty));
        Assert.False(ScreenActions.Closes(ScreenActions.LeaveKey(empty)));
        Assert.Equal("\x1b[B", ScreenActions.SearchDownKey(empty)!.Send);

        // "git": the rules that match, read as the screen's list; Enter on a line goes into them, Escape clears.
        Replay(model, fixture, 2562, 2669);
        var typed = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.True(typed.SearchTyped);
        Assert.Equal(["Search: git", "1. Bash(git status)"], typed.Lines);
        Assert.Equal([("Down", "navigate"), ("Escape", "clear")], typed.Keys.Select(k => (k.Name, k.Action)));
        Assert.Equal("\x1b[B", ScreenActions.EnterOnLine(typed)!.Send);
        Assert.Equal(["Search: git", "1. Bash(git status)", "Navigate (Down)", "Clear (Escape)"], ScreenActions.Items(typed));

        // Down: Claude Code asks for the rule by its number, a question and no screen.
        Replay(model, fixture, 2670, 2670);
        Assert.Null(model.PendingScreen);
        Assert.True(model.PromptPending);

        // "zz" (Up drew nothing, and the first key typed in the list began a new search): no rule matches, and the hint
        // row names Escape alone, so nothing goes into the rules from here.
        Replay(model, fixture, 2818, 2900);
        var none = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.True(none.SearchTyped);
        Assert.Equal(["Search: zz", "No rules match \"zz\""], none.Lines);
        Assert.Equal([("Escape", "clear")], none.Keys.Select(k => (k.Name, k.Action)));
        Assert.Null(ScreenActions.SearchDownKey(none));
        Assert.Null(ScreenActions.EnterOnLine(none));

        // Escape: the text cleared, every rule again, the field again.
        Replay(model, fixture, 2974, 2974);
        var cleared = Assert.IsType<ClaudeScreen>(model.PendingScreen);
        Assert.True(cleared.Searching);
        Assert.False(cleared.SearchTyped);
        Assert.Equal("go back", cleared.Keys[^1].Action);

        // Escape twice more: back to the rules, asked for by number, then the screen dismissed; nothing waits.
        Replay(model, fixture, 3122, int.MaxValue);
        Assert.Null(model.PendingScreen);
        Assert.False(model.PromptPending);
        Assert.Contains("Permissions dialog dismissed", TestHelpers.Visible(model));
    }

    [Fact]
    public void A_search_row_without_its_hint_row_is_no_screen()
    {
        // The row's words alone prove nothing: a hint row whose Escape goes back or clears must stand under it.
        Assert.Null(ScreenReader.Read(["claude: here", "Search: git", "1. Bash(git status)", "Esc to close"], 1));
        Assert.Null(ScreenReader.Read(["claude: here", "Search: git"], 1));
        Assert.True(ScreenReader.IsSearchRow("Search…"));
        Assert.True(ScreenReader.IsSearchRow("Search: a b"));
        Assert.False(ScreenReader.IsSearchRow("Search the web"));

        // Without a tab row above it, the search row is the title.
        var screen = ScreenReader.Read(["Search: x", "No rules match \"x\"", "Esc to clear"], 0)!;
        Assert.Equal("Search: x", screen.Title);
        Assert.Equal(0, screen.FirstRow);
    }

    /// <summary>Feeds the chunks of a recording from one offset through another, the frames cut as the app's quiet rule cuts them, and ends the last frame.</summary>
    private static void Replay(SessionModel model, string fixture, int from, int through)
    {
        var bytes = TestHelpers.Fixture(fixture);
        var previous = -1;
        foreach (var chunk in TestHelpers.Chunks(fixture).Where(chunk => chunk.Offset >= from && chunk.Offset <= through))
        {
            if (previous >= 0 && chunk.Ms - previous >= 100)
            {
                model.EndFrame();
            }

            model.Feed(bytes.AsSpan(chunk.Offset, chunk.Length));
            previous = chunk.Ms;
        }

        model.EndFrame();
    }
}

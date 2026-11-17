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

        // The search's key is read and not offered: it opens a field that takes typed text, which the list of a
        // screen cannot give (claude-screens.md). The other keys are the hint row's, in its order.
        Assert.Equal([("Down", "navigate"), ("Left", "switch"), ("Right", "switch"), ("Escape", "close")], screen.Keys.Select(k => (k.Name, k.Action)));
        Assert.True(ScreenActions.Closes(ScreenActions.LeaveKey(screen)));
        Assert.Null(ScreenActions.LetterKey(screen, '/'));

        // The row of 2.1.326, which named no such key, reads as it did.
        Assert.Equal(["Left", "Right", "Down", "Escape"], ScreenReader.Keys("←/→ to switch · ↓ to select · Esc to cancel")!.Select(k => k.Name));

        // The slash alone is a key; between two keys it joins them, as ever. A row without Escape is no hint row still.
        Assert.Equal(["Escape"], ScreenReader.Keys("/ to search · Esc to close")!.Select(k => k.Name));
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
        Assert.Equal(["Down", "Left", "Right", "Escape"], screen.Keys.Select(k => k.Name));

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

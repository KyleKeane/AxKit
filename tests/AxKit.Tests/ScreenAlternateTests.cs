using AxKit.Core.Transcript;
using AxKit.Core.Vt;
using Screen = AxKit.Core.Vt.Screen;

namespace AxKit.Tests;

/// <summary>
/// The alternate screen (docs/axTerminal/SPEC.md AT-9.1, AT-9.6): a full-screen program writes on a buffer of its
/// own, and the main screen's rows, their lines and the transcript stay as they were until it switches back.
/// </summary>
public sealed class ScreenAlternateTests
{
    private const string Esc = "\x1b";

    /// <summary>A store with no screen of its own: the test drives a screen by hand.</summary>
    private sealed class Store : LineStore
    {
        public List<string> Rewritten { get; } = [];

        public override void AddSystemLine(string text)
        {
        }

        protected override void CompleteFrame()
        {
        }

        protected override void TextChanged(Line line, string text)
        {
            Rewritten.Add(text);
            base.TextChanged(line, text);
        }
    }

    [Fact]
    public void The_main_screen_and_its_lines_are_untouched_while_the_alternate_one_is_in_use()
    {
        var store = new Store();
        var screen = new Screen(20, 4, store) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        parser.Feed("prompt> edit\r\n");
        screen.PullRowText();
        var lines = store.Lines.Select(l => l.Text).ToList();
        store.Rewritten.Clear();
        Assert.Equal(1, screen.CursorRow);
        Assert.Null(ScreenPage.Read(screen));

        // In: the program draws, scrolls past the bottom row and erases, all on its own buffer.
        parser.Feed($"{Esc}[?1049h{Esc}[HFile  Edit\r\n2\r\n3\r\n4\r\n5\r\n6{Esc}[2J{Esc}[1;1Hmenu");
        screen.PullRowText();
        Assert.True(screen.Alternate);
        Assert.Equal(lines, store.Lines.Select(l => l.Text).ToList());
        Assert.Empty(store.Rewritten);
        Assert.Equal("prompt> edit", screen.LineAt(0)!.Text);
        Assert.Equal(1, screen.CursorRow); // the main screen's, where it stood
        var page = ScreenPage.Read(screen)!;
        Assert.Equal("menu", page.Rows[0]);
        Assert.Equal((0, 4), (page.CursorRow, page.CursorColumn));

        // Out: the main screen as it was, the cursor back where 1049 saved it.
        parser.Feed($"{Esc}[?1049l");
        Assert.False(screen.Alternate);
        Assert.Null(ScreenPage.Read(screen));
        parser.Feed("after");
        screen.PullRowText();
        Assert.Equal("prompt> edit", screen.LineAt(0)!.Text);
        Assert.Equal("after", screen.LineAt(1)!.Text);
        Assert.Equal(["prompt> edit", "after"], store.Lines.Select(l => l.Text).ToList());
    }

    [Fact]
    public void Until_a_window_shows_the_page_a_full_screen_program_writes_on_the_main_rows_as_before()
    {
        // KeepsAlternateApart is off by default: the conversation keeps what the program draws, as it did before the
        // screen view, rather than nothing while no window shows the page (2026-11-25).
        var store = new Store();
        var screen = new Screen(20, 4, store);
        var parser = new VtParser(screen);
        parser.Feed($"prompt> edit\r\n{Esc}[?1049hmenu");
        screen.PullRowText();
        Assert.False(screen.Alternate);
        Assert.Null(ScreenPage.Read(screen));
        Assert.Contains(store.Lines, line => line.Text.Contains("menu", StringComparison.Ordinal));
    }

    [Fact]
    public void The_cursor_homed_and_the_screen_erased_are_not_raised_on_the_alternate_screen()
    {
        var screen = new Screen(20, 4, new Store()) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        var homed = 0;
        var cleared = 0;
        screen.Homed += () => homed++;
        screen.Cleared += () => cleared++;
        parser.Feed($"{Esc}[?1049h{Esc}[H{Esc}[2J{Esc}[3J{Esc}c");
        // ESC c leaves the alternate screen first and is then a reset of the main one, as in xterm.
        Assert.False(screen.Alternate);
        Assert.Equal(0, homed);
        Assert.Equal(1, cleared);
        parser.Feed($"{Esc}[?47h{Esc}[2J{Esc}[?47l{Esc}[2J");
        Assert.Equal(2, cleared);
    }

    [Fact]
    public void The_switch_is_kept_until_the_frame_ends()
    {
        var screen = new Screen(20, 4, new Store()) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        var seen = new List<bool>();
        screen.FrameEnd += () => seen.Add(screen.AlternateChangedSinceFrameEnd);
        parser.Feed($"{Esc}[?1049h{Esc}[?25h");
        Assert.False(screen.AlternateChangedSinceFrameEnd);
        parser.Feed($"x{Esc}[?25h");
        parser.Feed($"{Esc}[?1049l{Esc}[?25h");
        Assert.Equal([true, false, true], seen);
        // A switch to the screen already in use is nothing.
        parser.Feed($"{Esc}[?1049l");
        Assert.False(screen.AlternateChangedSinceFrameEnd);
    }

    [Fact]
    public void Only_1049_clears_the_alternate_screen_on_the_way_in_and_1047_on_the_way_out()
    {
        var screen = new Screen(20, 3, new Store()) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        parser.Feed($"{Esc}[?47hkept{Esc}[?47l{Esc}[?47h");
        Assert.Equal("kept", ScreenPage.Read(screen)!.Rows[0]);
        parser.Feed($"{Esc}[?47l{Esc}[?1049h");
        Assert.Equal(string.Empty, ScreenPage.Read(screen)!.Rows[0]);
        parser.Feed($"{Esc}[Hagain{Esc}[?1047l{Esc}[?47h");
        Assert.Equal(string.Empty, ScreenPage.Read(screen)!.Rows[0]);
    }

    [Fact]
    public void The_cursor_is_put_back_by_1049_and_left_where_it_is_by_47()
    {
        var screen = new Screen(20, 4, new Store()) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        parser.Feed($"{Esc}[2;3H{Esc}[?1049h{Esc}[4;1H{Esc}[?1049lx");
        screen.PullRowText();
        Assert.Equal("  x", screen.LineAt(1)!.Text);
        parser.Feed($"{Esc}[?47h{Esc}[4;1H{Esc}[?47l");
        Assert.Equal(3, screen.CursorRow);
    }

    /// <summary>AT-9.10: a program that ended on the alternate screen is left as on ?1049l, by the model's word; on the main screen it is nothing.</summary>
    [Fact]
    public void Leaving_the_alternate_screen_by_hand_is_the_switch_back()
    {
        var screen = new Screen(20, 4, new Store()) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        parser.Feed($"before{Esc}[2;3H{Esc}[?1049h{Esc}[4;1Hpage");
        screen.LeaveAlternate();
        Assert.False(screen.Alternate);
        Assert.True(screen.AlternateChangedSinceFrameEnd);
        Assert.Null(ScreenPage.Read(screen));
        parser.Feed("x");
        screen.PullRowText();
        Assert.Equal("before", screen.LineAt(0)!.Text);
        Assert.Equal("  x", screen.LineAt(1)!.Text);
        screen.LeaveAlternate();
        Assert.False(screen.Alternate);
    }

    /// <summary>
    /// AT-9.10: a program that ended on the alternate screen takes what it set with it: its scroll region, its
    /// background and its reverse video. Until 2026-11-27 they stayed, so that the shell's output scrolled inside
    /// the dead program's region and the next page was drawn in its colours.
    /// </summary>
    [Fact]
    public void Leaving_the_alternate_screen_by_hand_lets_go_of_what_the_program_set()
    {
        var screen = new Screen(20, 4, new Store()) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        parser.Feed($"before{Esc}[?1049h{Esc}[2;3r{Esc}[41m{Esc}[7m");
        screen.LeaveAlternate();

        // The whole screen scrolls again: a line feed on the last row moves the first row off.
        parser.Feed($"{Esc}[4;1H\nlast");
        screen.PullRowText();
        Assert.NotEqual("before", screen.LineAt(0)?.Text);
        Assert.Equal("last", screen.LineAt(3)!.Text);

        // The next program's page is drawn on the default background: nothing of it is marked.
        parser.Feed($"{Esc}[?47h{Esc}[Hab");
        Assert.Empty(ScreenPage.Read(screen)!.Marked);
    }

    /// <summary>AT-9.10: the colour questions are answered only where the screen answers them and while the alternate screen is in use.</summary>
    [Fact]
    public void A_colour_question_is_answered_on_the_alternate_screen_only_and_only_when_asked_for()
    {
        var answers = new List<string>();
        var quiet = new Screen(20, 4, new Store()) { KeepsAlternateApart = true };
        quiet.Answer += answers.Add;
        new VtParser(quiet).Feed($"{Esc}[?1049h{Esc}]11;?\a");
        Assert.Empty(answers);

        var screen = new Screen(20, 4, new Store()) { KeepsAlternateApart = true, AnswersQueries = true };
        screen.Answer += answers.Add;
        var parser = new VtParser(screen);
        parser.Feed($"{Esc}]11;?\a");
        Assert.Empty(answers);
        parser.Feed($"{Esc}[?1049h{Esc}]4;1;?\a{Esc}]10;?{Esc}\\");
        Assert.Equal([$"{Esc}]4;1;rgb:c5c5/0f0f/1f1f\a", $"{Esc}]10;rgb:cccc/cccc/cccc\a"], answers);
    }
}

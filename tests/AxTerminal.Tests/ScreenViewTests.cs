using AxKit.Core.Vt;
using AxTerminal.Core;

namespace AxTerminal.Tests;

/// <summary>
/// The screen view of a full-screen program (SPEC.md AT-9): the keys as a terminal sends them (<see cref="ScreenKeys"/>),
/// the page as the view's text with its rows replaced one by one (<see cref="ScreenView"/>), the marked run said once
/// when it moves (<see cref="ScreenMarks"/>), and the recording of Windows' edit walked through as the window walks it.
/// </summary>
public sealed class ScreenViewTests
{
    private const string Esc = TestHelpers.Esc;

    // Windows' virtual key codes, the numbers of WinForms' Keys.
    private const int Up = 0x26, Down = 0x28, Left = 0x25, Home = 0x24, Delete = 0x2E, PageDown = 0x22, F1 = 0x70, F5 = 0x74, F10 = 0x79, F12 = 0x7B;
    private const int Enter = 0x0D, Tab = 0x09, Back = 0x08, Escape = 0x1B, Space = 0x20, A = 0x41, F = 0x46, Q = 0x51, D1 = 0x31;

    [Fact]
    public void The_arrows_and_editing_keys_go_as_xterm_sends_them_with_the_modifiers_as_its_parameter()
    {
        Assert.Equal($"{Esc}[A", ScreenKeys.Sequence(Up, false, false, false));
        Assert.Equal($"{Esc}[B", ScreenKeys.Sequence(Down, false, false, false));
        Assert.Equal($"{Esc}[H", ScreenKeys.Sequence(Home, false, false, false));
        Assert.Equal($"{Esc}[1;5D", ScreenKeys.Sequence(Left, true, false, false));
        Assert.Equal($"{Esc}[1;2A", ScreenKeys.Sequence(Up, false, true, false));
        Assert.Equal($"{Esc}[3~", ScreenKeys.Sequence(Delete, false, false, false));
        Assert.Equal($"{Esc}[6;5~", ScreenKeys.Sequence(PageDown, true, false, false));
    }

    [Fact]
    public void The_function_keys_go_as_xterm_sends_them()
    {
        Assert.Equal($"{Esc}OP", ScreenKeys.Sequence(F1, false, false, false));
        Assert.Equal($"{Esc}[1;2P", ScreenKeys.Sequence(F1, false, true, false));
        Assert.Equal($"{Esc}[15~", ScreenKeys.Sequence(F5, false, false, false));
        Assert.Equal($"{Esc}[21~", ScreenKeys.Sequence(F10, false, false, false));
        Assert.Equal($"{Esc}[24;5~", ScreenKeys.Sequence(F12, true, false, false));
    }

    [Fact]
    public void Ctrl_and_Alt_with_a_letter_are_a_control_character_and_Escape_with_the_letter()
    {
        Assert.Equal("\u0011", ScreenKeys.Sequence(Q, true, false, false));
        Assert.Equal("\u0001", ScreenKeys.Sequence(A, true, true, false));
        Assert.Equal($"{Esc}f", ScreenKeys.Sequence(F, false, false, true));
        Assert.Equal($"{Esc}F", ScreenKeys.Sequence(F, false, true, true));
        Assert.Equal($"{Esc}1", ScreenKeys.Sequence(D1, false, false, true));
        // Ctrl+Alt is AltGr on many keyboards: its character comes as typed.
        Assert.Null(ScreenKeys.Sequence(Q, true, false, true));
    }

    [Fact]
    public void Enter_Tab_Backspace_and_Escape_are_their_characters_and_a_typed_key_is_none()
    {
        Assert.Equal("\r", ScreenKeys.Sequence(Enter, false, false, false));
        Assert.Equal("\t", ScreenKeys.Sequence(Tab, false, false, false));
        Assert.Equal($"{Esc}[Z", ScreenKeys.Sequence(Tab, false, true, false));
        Assert.Equal("\u007f", ScreenKeys.Sequence(Back, false, false, false));
        Assert.Equal(Esc, ScreenKeys.Sequence(Escape, false, false, false));
        Assert.Null(ScreenKeys.Sequence(A, false, false, false));
        Assert.Null(ScreenKeys.Sequence(A, false, true, false));
        Assert.Null(ScreenKeys.Sequence(Space, false, false, false));
        Assert.Equal("x", ScreenKeys.Character("x", alt: false));
        Assert.Equal($"{Esc}.", ScreenKeys.Character(".", alt: true));
    }

    [Fact]
    public void A_row_and_column_stand_in_the_text_and_back()
    {
        string[] rows = ["ab", string.Empty, "cdef"];
        Assert.Equal("ab\r\n\r\ncdef", ScreenView.Whole(rows));
        Assert.Equal(0, ScreenView.Index(rows, 0, 0));
        Assert.Equal(4, ScreenView.Index(rows, 1, 0));
        Assert.Equal(8, ScreenView.Index(rows, 2, 2));
        // A column past the row's end, in its trailing spaces, stands at the end.
        Assert.Equal(2, ScreenView.Index(rows, 0, 40));
        Assert.Equal((2, 2), ScreenView.Position(rows, 8));
        Assert.Equal((0, 2), ScreenView.Position(rows, 2));
        Assert.Equal((1, 0), ScreenView.Position(rows, 4));
    }

    /// <summary>
    /// AT-9.5 (2026-11-27): a selection made in reading the screen is still there after the program redraws: both
    /// its ends stay on their rows and columns, in the direction the user made it.
    /// </summary>
    [Fact]
    public void A_selection_keeps_both_its_ends_over_a_change_of_the_rows()
    {
        string[] shown = ["12:00:01", "alpha beta", "gamma"];
        string[] next = ["12:00:02 busy", "alpha beta", "gamma"];
        // "beta" selected forwards: from row 1, column 6 to row 1, column 10; the first row grew by five.
        var anchor = ScreenView.Index(shown, 1, 6);
        var caret = ScreenView.Index(shown, 1, 10);
        Assert.Equal((anchor + 5, caret + 5), ScreenView.Kept(shown, next, anchor, caret));
        // Backwards, the caret before the anchor: still backwards.
        Assert.Equal((caret + 5, anchor + 5), ScreenView.Kept(shown, next, caret, anchor));
        // Nothing selected: the caret alone, on its row and column.
        Assert.Equal((anchor + 5, anchor + 5), ScreenView.Kept(shown, next, anchor, anchor));
        // An end past its row's new end stands at that end; one on a row that is gone, on the last row.
        string[] shorter = ["12:00:03", "alpha"];
        Assert.Equal((ScreenView.Index(shorter, 1, 5), ScreenView.Index(shorter, 1, 3)), ScreenView.Kept(shown, shorter, anchor, ScreenView.Index(shown, 2, 3)));
    }

    [Fact]
    public void Only_the_rows_that_changed_are_edits_the_last_first()
    {
        string[] shown = ["ab", "cd", "ef"];
        string[] next = ["aX", "cd", "efgh"];
        var edits = ScreenView.Edits(shown, next);
        Assert.Equal([new RowEdit(8, 2, "efgh"), new RowEdit(0, 2, "aX")], edits);
        Assert.Equal(ScreenView.Whole(next), Apply(ScreenView.Whole(shown), edits));
        Assert.Empty(ScreenView.Edits(shown, shown));
    }

    [Fact]
    public void The_program_is_named_by_the_commands_first_word()
    {
        Assert.Equal("edit", ScreenView.ProgramName("edit notes.txt"));
        Assert.Equal("edit", ScreenView.ProgramName(@"C:\Windows\System32\edit.exe notes.txt"));
        Assert.Equal("vim", ScreenView.ProgramName("\"C:\\Program Files\\Vim\\vim.exe\" a.txt"));
        Assert.Equal("the program", ScreenView.ProgramName(null));
        Assert.Equal("the program", ScreenView.ProgramName(TerminalModel.UnrecordedCommand));
        Assert.Equal("Screen of edit", ScreenView.Name("edit"));
        Assert.Equal("Keys to edit", ScreenView.KeysWords("edit"));
    }

    [Fact]
    public void A_marked_run_is_said_once_when_it_moves_and_again_after_it_was_gone()
    {
        var marks = new ScreenMarks();
        Assert.Equal("New File, Ctrl+N", marks.Said(Page(new MarkedRun(2, 1, 22, "New File    Ctrl+N"))));
        // A redraw that leaves it, its text changed or not: nothing.
        Assert.Null(marks.Said(Page(new MarkedRun(2, 1, 22, "New File    Ctrl+N"))));
        Assert.Null(marks.Said(Page(new MarkedRun(2, 1, 22, "New File    Ctrl+M"))));
        // Down in the menu: the next entry.
        Assert.Equal("Open File, Ctrl+O", marks.Said(Page(new MarkedRun(3, 1, 22, "Open File    Ctrl+O"))));
        // The menu closed and opened again: said again.
        Assert.Null(marks.Said(Page()));
        Assert.Equal("Open File, Ctrl+O", marks.Said(Page(new MarkedRun(3, 1, 22, "Open File    Ctrl+O"))));
        // A run of blanks says nothing.
        Assert.Null(marks.Said(Page(new MarkedRun(5, 4, 3, string.Empty))));
    }

    /// <summary>
    /// The recording of Windows' edit (AT-9.7) through the shell's own model, as the window walks it at each frame's
    /// end: the page is there while edit holds the alternate screen, the view's text made of edits of only the rows
    /// that changed is always the page's, the cursor stands in it, and what is said by itself is the File menu's
    /// selected entry and the unsaved-changes dialog's button in focus, each once. Nothing of edit reaches the transcript.
    /// </summary>
    [Fact]
    public void Windows_edit_walked_through_says_the_menus_entry_and_the_dialogs_button_once_each()
    {
        const string fixture = "fullscreen-edit1.2.1-120x30.vt";
        var said = new List<string>();
        var marks = new ScreenMarks();
        IReadOnlyList<string>? shown = null;
        var text = string.Empty;
        var pages = 0;
        var opened = 0;
        var closed = 0;
        TerminalModel? watched = null;
        var model = TestHelpers.Replay(fixture, [], subscribe: m =>
        {
            watched = m;
            m.Changed += () =>
            {
                if (watched!.Page is not { } page)
                {
                    if (shown is not null)
                    {
                        closed++;
                    }

                    shown = null;
                    return;
                }

                pages++;
                if (shown is null)
                {
                    opened++;
                    marks.Reset();
                    text = ScreenView.Whole(page.Rows);
                }
                else
                {
                    text = Apply(text, ScreenView.Edits(shown, page.Rows));
                }

                shown = page.Rows;
                Assert.Equal(ScreenView.Whole(page.Rows), text);
                var caret = ScreenView.Index(page.Rows, page.CursorRow, page.CursorColumn);
                Assert.InRange(caret, 0, text.Length);
                if (marks.Said(page) is { } words)
                {
                    said.Add(words);
                }
            };
        });

        Assert.True(pages > 3, $"{pages} pages");
        Assert.Equal(1, opened);
        Assert.Equal(1, closed);
        Assert.Equal(["New File, Ctrl+N", "[Save]"], said);
        Assert.Null(model.Page);
        Assert.DoesNotContain(model.Lines, line => line.Text.Contains("File  Edit", StringComparison.Ordinal));
    }

    /// <summary>
    /// Windows' console after a full-screen program leaves the alternate screen (recorded 2026-11-25 with PowerShell
    /// running edit): the cursor hidden and homed, every row of the main screen written again with an erase to its
    /// end, the cursor shown. The repaint lands on the lines already there: no line of the banner or of the prompt
    /// comes again in the output, and the program's page never enters the transcript.
    /// </summary>
    [Fact]
    public void The_consoles_repaint_after_the_alternate_screen_adds_no_line()
    {
        string Prompt(string? code) =>
            $"{Esc}]133;D{(code is null ? string.Empty : ";" + code)}{TestHelpers.Bel}{Esc}]133;A{TestHelpers.Bel}{Esc}]9;9;\"C:\\src\"{TestHelpers.Bel}PS C:\\src> {Esc}]133;B{TestHelpers.Bel}";
        var model = new TerminalModel(40, 6);
        TestHelpers.Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[HBanner line{Esc}[?25h\r\n\r\n" + Prompt(null));
        model.EndFrame();
        Assert.True(model.Send("edit notes.txt"));
        TestHelpers.Feed(model, "edit notes.txt\r\n");
        TestHelpers.Feed(model, $"{Esc}[?1049h{Esc}[?25l{Esc}[H File  Edit{Esc}[2;1H1 │ First line{Esc}[?25h");
        model.EndFrame();
        Assert.NotNull(model.Page);
        var before = model.Lines.Where(line => !line.Hidden).Select(line => line.Text).ToList();

        TestHelpers.Feed(model, $"{Esc}[?1049l{Esc}[?25l{Esc}[HBanner line{Esc}[K\r\n{Esc}[K\r\nPS C:\\src> edit notes.txt{Esc}[K\r\n{Esc}[K\r\n{Esc}[K\r\n{Esc}[K{Esc}[4;1H{Esc}[?25h");
        model.EndFrame();
        Assert.Null(model.Page);
        var after = model.Lines.Where(line => !line.Hidden).Select(line => line.Text).ToList();
        Assert.Equal(before, after);
        Assert.Single(after, text => text == "Banner line");
        Assert.DoesNotContain(after, text => text.StartsWith("PS C:", StringComparison.Ordinal) || text.Contains("File  Edit", StringComparison.Ordinal));

        TestHelpers.Feed(model, Prompt("0"));
        model.EndFrame();
        Assert.False(model.Running);
    }

    /// <summary>
    /// A reset (<c>ESC c</c>) from a full-screen program leaves the alternate screen and clears the rows of the main
    /// one (AT-4.4): the lines those rows had keep their text. Until 2026-11-27 the reset told the model after the
    /// switch, the model took it for the console's repaint (AT-9.9) and kept the rows on their lines, and the erase
    /// blanked what had been read. In one frame with the switch or in a later one.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_reset_on_the_alternate_screen_blanks_no_line_already_read(bool frameEndsBetween)
    {
        var model = new TerminalModel(40, 6);
        TestHelpers.Feed(model, "Banner line\r\nsecond line\r\n");
        model.EndFrame();
        TestHelpers.Feed(model, $"{Esc}[?1049h{Esc}[HFile  Edit" + (frameEndsBetween ? $"{Esc}[?25h" : string.Empty));
        if (frameEndsBetween)
        {
            model.EndFrame();
        }

        TestHelpers.Feed(model, $"{Esc}c");
        model.EndFrame();
        TestHelpers.Feed(model, "after\r\n");
        model.EndFrame();

        var lines = model.Lines.Where(line => !line.Hidden).Select(line => line.Text).ToList();
        Assert.Contains("Banner line", lines);
        Assert.Contains("second line", lines);
        Assert.Contains("after", lines);
    }

    /// <summary>
    /// A program that ends on the alternate screen, without switching back (killed, or crashed; AT-9.10): the shell's
    /// own mark brings the main screen back, so the page is gone (the window closes the view as on the switch back),
    /// the command ends with its code, and the next prompt and what follows reach the transcript, not the page.
    /// </summary>
    [Fact]
    public void A_program_that_ends_on_the_alternate_screen_gives_way_at_the_shells_mark()
    {
        string Prompt(string? code) =>
            $"{Esc}]133;D{(code is null ? string.Empty : ";" + code)}{TestHelpers.Bel}{Esc}]133;A{TestHelpers.Bel}PS C:\\src> {Esc}]133;B{TestHelpers.Bel}";
        var model = new TerminalModel(40, 6);
        int? finished = null;
        model.CommandFinished += code => finished = code;
        TestHelpers.Feed(model, "Banner line\r\n" + Prompt(null));
        model.EndFrame();
        Assert.True(model.Send("edit notes.txt"));
        TestHelpers.Feed(model, "edit notes.txt\r\n");
        TestHelpers.Feed(model, $"{Esc}[?1049h{Esc}[H File  Edit{Esc}[2;1H1 │ First line{Esc}[?25h");
        model.EndFrame();
        Assert.NotNull(model.Page);

        // No ESC[?1049l: the shell's prompt comes straight after.
        TestHelpers.Feed(model, "\r\n" + Prompt("1") + $"{Esc}[?25h");
        model.EndFrame();
        Assert.Null(model.Page);
        Assert.False(model.Running);
        Assert.Equal(1, finished);
        Assert.Equal("PS C:\\src>", model.Prompt);

        Assert.True(model.Send("echo after"));
        TestHelpers.Feed(model, "echo after\r\nafter\r\n" + Prompt("0") + $"{Esc}[?25h");
        model.EndFrame();
        Assert.Null(model.Page);
        Assert.Contains(model.Lines, line => !line.Hidden && line.Text == "after");
        Assert.DoesNotContain(model.Lines, line => line.Text.Contains("File  Edit", StringComparison.Ordinal));
    }

    /// <summary>
    /// The colour questions of Windows' edit, as recorded (AT-9.7: the sixteen colours, OSC 4, in two strings), are
    /// answered at once with a plain black background and light text (AT-9.10), one answer per colour, in xterm's form;
    /// a question on the main screen is never answered, since the shell's line editor would take it as typed text.
    /// </summary>
    [Fact]
    public void Windows_edits_colour_questions_are_answered_with_black_and_light_text()
    {
        var answers = new List<string>();
        TestHelpers.Replay("fullscreen-edit1.2.1-120x30.vt", [], subscribe: m => m.Answer += answers.Add);
        Assert.Equal(16, answers.Count);
        Assert.Equal($"{Esc}]4;0;rgb:0000/0000/0000{TestHelpers.Bel}", answers[0]);
        Assert.Equal($"{Esc}]4;7;rgb:cccc/cccc/cccc{TestHelpers.Bel}", answers[7]);
        Assert.Equal($"{Esc}]4;15;rgb:f2f2/f2f2/f2f2{TestHelpers.Bel}", answers[15]);

        var model = new TerminalModel(40, 6);
        var mainAnswers = new List<string>();
        model.Answer += mainAnswers.Add;
        TestHelpers.Feed(model, $"{Esc}]11;?{TestHelpers.Bel}{Esc}]4;0;?{TestHelpers.Bel}");
        Assert.Empty(mainAnswers);
        TestHelpers.Feed(model, $"{Esc}[?1049h{Esc}]11;?{TestHelpers.Bel}");
        Assert.Equal([$"{Esc}]11;rgb:0000/0000/0000{TestHelpers.Bel}"], mainAnswers);
    }

    [Fact]
    public void The_places_and_find_say_why_they_wait_while_the_screen_shows()
    {
        Assert.Equal("Screen of edit. The terminal and the command field come back when edit leaves the whole screen", ScreenView.PlacesWords("edit"));
        Assert.Equal("Find works in reading the screen: Ctrl+Space turns to it", ScreenView.FindWaitsWords("Ctrl+Space"));
    }

    private static ScreenPage Page(params MarkedRun[] runs) => new([string.Empty], 0, 0, runs);

    private static string Apply(string text, IReadOnlyList<RowEdit> edits)
    {
        foreach (var edit in edits)
        {
            text = text[..edit.Start] + edit.Text + text[(edit.Start + edit.OldLength)..];
        }

        return text;
    }
}

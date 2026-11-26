using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Core.Vt;
using AxKit.Forms;
using AxTerminal.Core;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the screen view of a full-screen program (SPEC.md AT-9) ----
    //
    // While a program holds the alternate screen (Windows' edit, a pager, an editor), its page stands in place of the
    // terminal and the field, in one read-only text (ScreenBox). Keys to the program, the edit mode, sends every key
    // there but Ctrl+Space, F1 and Alt+F4, and the caret stands where the program's cursor stands; Ctrl+Space, the
    // bundle's mode key, turns to reading the screen, the navigation mode, and back. At each frame's end the rows that
    // changed are replaced and a marked run that moved is said (ScreenMarks). When the program leaves the alternate
    // screen, or the shell ends, the terminal and the field come back as they were, with the focus where it was.

    private readonly ScreenBox _screen = new() { TabIndex = 2 };

    /// <summary>The rows the view shows; null while no program holds the screen.</summary>
    private IReadOnlyList<string>? _screenRows;

    /// <summary>The program's cursor on the page shown, for the caret in keys to the program.</summary>
    private (int Row, int Column) _screenCursor;

    private readonly ScreenMarks _screenMarks = new();

    /// <summary>The program's name, from the command that started it: "edit".</summary>
    private string _screenProgram = string.Empty;

    /// <summary>Whether the terminal had the focus when the view opened, so that it gets it back; the field otherwise.</summary>
    private bool _terminalBeforeScreen;

    /// <summary>A program holds the screen and its page is shown.</summary>
    private bool ScreenShown => _screenRows is not null;

    /// <summary>Called once from the constructor: the view joins the window, hidden, its keys to the shell.</summary>
    private void SetUpScreen()
    {
        _screen.Typed += sequence =>
        {
            if (ProcessRunning)
            {
                Write(sequence);
            }
        };
        _screen.ModeKey += ToggleScreenMode;
        Controls.Add(_screen);
    }

    /// <summary>
    /// At each frame's end and each change of the transcript (<see cref="OnTranscriptChanged"/>): the view opens when a
    /// program has switched to the alternate screen, follows its page, and closes when it has switched back or the
    /// shell has ended (AT-9.1, AT-9.5).
    /// </summary>
    private void FollowScreen()
    {
        var page = ProcessRunning ? _model.Page : null;
        if (page is null)
        {
            CloseScreen();
            return;
        }

        if (!ScreenShown)
        {
            OpenScreen(page);
        }
        else
        {
            ShowPage(page);
        }

        // AT-9.4: a marked run said once when it moved, after the rows and the caret, so that NVDA's reading of the caret's line comes first.
        if (_screenMarks.Said(page) is { } said)
        {
            Announce(EventKind.ProgramSaid, said, false);
        }
    }

    private void OpenScreen(ScreenPage page)
    {
        _screenProgram = ScreenView.ProgramName(_model.RunningCommand);
        _terminalBeforeScreen = Transcript.Focused;
        _screenMarks.Reset();
        _screen.KeysToProgram = true;
        _screen.AccessibleName = ScreenView.Name(_screenProgram);
        _screenRows = page.Rows;
        _screenCursor = (page.CursorRow, page.CursorColumn);
        _screen.Text = ScreenView.Whole(page.Rows);
        PlaceScreenCaret();
        Log.Info("A program took the whole screen: the screen view opened");
        if (!NoticeOpen)
        {
            var active = ContainsFocus;
            Transcript.Visible = false;
            Input.Visible = false;
            Area.Visible = false;
            _screen.Visible = true;
            _screen.BringToFront();
            FocusOrKeep(_screen, active);
        }

        UpdateStatus();
    }

    /// <summary>AT-9.5: only the rows that changed are replaced, with the drawing held; the caret follows the program's cursor in keys to the program and stays on its row and column in reading the screen.</summary>
    private void ShowPage(ScreenPage page)
    {
        var edits = ScreenView.Edits(_screenRows!, page.Rows);
        var before = ScreenView.Position(_screenRows!, _screen.SelectionStart);
        var cursorMoved = _screenCursor != (page.CursorRow, page.CursorColumn);
        _screenCursor = (page.CursorRow, page.CursorColumn);
        if (edits.Count == 0 && !cursorMoved)
        {
            return;
        }

        _screen.SuspendDrawing(() =>
        {
            foreach (var edit in edits)
            {
                _screen.Select(edit.Start, edit.OldLength);
                _screen.SelectedText = edit.Text;
            }

            _screenRows = page.Rows;
            if (_screen.KeysToProgram)
            {
                PlaceScreenCaret();
            }
            else
            {
                _screen.Select(ScreenView.Index(page.Rows, before.Row, before.Column), 0);
            }
        });
    }

    /// <summary>The control gets the focus when the window has it; otherwise it is the one that gets it when the window is activated again.</summary>
    private void FocusOrKeep(Control control, bool active)
    {
        if (active)
        {
            control.Focus();
        }
        else
        {
            ActiveControl = control;
        }
    }

    /// <summary>The caret where the program's cursor stands (AT-9.3).</summary>
    private void PlaceScreenCaret()
    {
        if (_screenRows is { } rows)
        {
            _screen.Select(ScreenView.Index(rows, _screenCursor.Row, _screenCursor.Column), 0);
            _screen.ScrollToCaret();
        }
    }

    /// <summary>The program left the screen, or the shell ended: the terminal and the field as they were, the focus where it was.</summary>
    private void CloseScreen()
    {
        if (!ScreenShown)
        {
            return;
        }

        var active = ContainsFocus;
        _screenRows = null;
        Log.Info("The program left the whole screen: the screen view closed");
        if (!NoticeOpen)
        {
            ShowAfterNotice();
            FocusOrKeep(_terminalBeforeScreen ? Transcript : BottomControl, active);
        }

        _screen.Visible = false;
        _screen.Text = string.Empty;

        UpdateStatus();
    }

    /// <summary>
    /// Ctrl+Space in the view (AT-9.3): keys to the program, the edit mode, or reading the screen, the navigation mode;
    /// said in two words and shown in the status bar. Back in keys to the program the caret goes to the cursor.
    /// </summary>
    private void ToggleScreenMode()
    {
        _screen.KeysToProgram = !_screen.KeysToProgram;
        if (_screen.KeysToProgram)
        {
            PlaceScreenCaret();
        }

        Announce(EventKind.Changed, ScreenModeWords, true);
        UpdateStatus();
    }

    /// <summary>"Keys to edit" or "Reading the screen", for the status bar and the switch.</summary>
    private string ScreenModeWords => _screen.KeysToProgram ? ScreenView.KeysWords(_screenProgram) : ScreenView.ReadingWords;

    // ---- the window base's hooks while the view shows ----

    protected override void HideForNotice()
    {
        base.HideForNotice();
        _screen.Visible = false;
    }

    /// <summary>The view comes back after a notice while the program holds the screen; the terminal and the field otherwise.</summary>
    protected override void ShowAfterNotice()
    {
        if (ScreenShown)
        {
            _screen.Visible = true;
            _screen.BringToFront();
            return;
        }

        base.ShowAfterNotice();
    }

    /// <summary>The view takes the focus after a notice while it shows.</summary>
    protected override Control FocusTarget => ScreenShown ? _screen : base.FocusTarget;

    /// <summary>Announcements are raised on the view while it shows, the control NVDA reads the window by then.</summary>
    protected override Control AnnounceSource => ScreenShown ? _screen : base.AnnounceSource;
}

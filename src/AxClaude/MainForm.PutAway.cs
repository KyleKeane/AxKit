using System.Runtime.InteropServices;
using AxClaude.Core.Flow;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;
using static AxKit.Forms.User32;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- a window that hides on closing (FR-9.6, --hide-on-close and --hidden; axHelp's conversation) ----
    //
    // With --hide-on-close, Alt+F4, the close command and Close all windows put the window away: hidden, Claude Code
    // running on, everything in it as it was. A start of the same program hands it "show" or a "message"
    // (TakeHandOver), which shows that same window at once, the focus in the message field; "end" asks it to close,
    // and Session, End Claude and close this window closes it. While it is put away the window says nothing, plays
    // nothing and takes nothing up: what Claude waits on, a command offered, a notice raised and a Claude that stopped
    // are all acted on or said when the window is brought up, as if they had just happened.

    private const int WM_ACTIVATE = 0x0006;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    /// <summary>
    /// The one place that decides whether a turn's end is heard while the window is put away (a decision still the
    /// user's, 2026-11-15): false, the put-away window is silent and "Done" with its chime comes when it is brought
    /// up; true, the Done chime alone also plays at the moment the turn ends, nothing spoken.
    /// </summary>
    private static readonly bool DoneChimeWhilePutAway = false;

    /// <summary>The one rule of the foreground while hidden, the hotkey windows' (<see cref="ForegroundReturn"/>).</summary>
    private readonly ForegroundReturn _front = new();

    /// <summary>The window is put away: hidden by its close, or not yet shown after a hidden start.</summary>
    private bool _putAway;

    /// <summary>The close under way is for good: End Claude and close this window, or an "end" with nothing to lose.</summary>
    private bool _ending;

    /// <summary>
    /// The program that keeps this window asked it to end and it stayed (the user kept it): from now a close closes,
    /// as in any axClaude window, since nothing may be left to bring a hidden window back. A "show" or a "message"
    /// says the keeper is there again.
    /// </summary>
    private bool _keeperLeft;

    /// <summary>The screen settled while the window was put away: what waits is acted on when the window is shown.</summary>
    private bool _attentionWaits;

    /// <summary>
    /// The core said Done while the window was put away (EG-5.6, <see cref="FlowDone"/>): one "Done" is owed, for
    /// however many turns ended meanwhile, and the settle after <see cref="BringUp"/> says it, unless a question
    /// waits then, which takes its place as it takes a bell's.
    /// </summary>
    private bool _doneOwed;

    /// <summary>Claude stopped while the window was put away: said when it is brought up.</summary>
    private bool _stoppedWhilePutAway;

    /// <summary>Put away, the window announces nothing and the AI tools do not see it (the window base reads this).</summary>
    protected override bool PutAway => _putAway;

    /// <summary>What the window would have said while put away: only that Claude stopped is kept, for the bringing up.</summary>
    protected override void SaidWhilePutAway(string text)
    {
        if (text.StartsWith(StoppedText, StringComparison.Ordinal))
        {
            _stoppedWhilePutAway = true;
        }
    }

    /// <summary>A hidden start's window was never where the user put one: its place and state are not saved until it has been brought up once.</summary>
    protected override bool SavesWindowPlace => _stateWhenBroughtUp is null;

    /// <summary>
    /// A hidden start (--hidden with --hide-on-close): the first showing is without activation, so that the start
    /// never takes the foreground; <see cref="OnShown"/> hides the window at once. Shown and hidden, not never shown:
    /// every control is made and laid out as in any start, so the conversation fills as it does in a window on view.
    /// </summary>
    protected override bool ShowWithoutActivation => _options.StartsHidden && _putAway;

    /// <summary>
    /// For that one showing the window is a tool window and no app window, which Windows keeps out of the taskbar and
    /// out of Alt+Tab, so that the moment between its showing and its hiding leaves no entry there;
    /// <see cref="BringUp"/> gives it its own style back before it is first seen. Read before the constructor has
    /// run (the base asks for the size of the frame), hence the null check.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            if (_options is { StartsHidden: true } && _putAway && _stateWhenBroughtUp is not null)
            {
                parameters.ExStyle = (parameters.ExStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
            }

            return parameters;
        }
    }

    /// <summary>Alt+F4, the close command, Close all windows: the window is put away. A close for good, the update's and Windows' own go on to the console window's closing.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_options.HideOnClose && !_ending && !_keeperLeft && !CloseWasConfirmed && !ClosingForUpdate && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            PutAwayNow();
            return;
        }

        // A Claude Code that runs over its stream is ended at once when the window closes for good, and never left to
        // the wait that would end it later, which dies with this process (MainForm.Stream.cs, StopStream).
        _windowCloses = true;
        base.OnFormClosing(e);
        _windowCloses = !e.Cancel;
        if (e.Cancel)
        {
            // The closing question keeps the window (Keep working): the next Alt+F4 hides again.
            _ending = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_ACTIVATE && (m.WParam & 0xFFFF) != 0 && _putAway && IsHandleCreated && !IsWindowVisible(Handle))
        {
            // The hidden window must never hold the keyboard (docs/axStart/SPEC.md AS-3.4): Windows activates a hidden
            // window of a process that hid one or started hidden.
            Log.Info("Activated while put away; the front given back");
            BeginInvoke(() => _front.GiveBack(Handle));
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// The window put away: hidden, silent (announcements dropped, the sounds and the music muted, reply speech that
    /// waited dropped), the settings saved since the process may now run for days, and the foreground given back by
    /// the rule. Claude Code and the conversation are untouched; no question is asked, since nothing ends.
    /// </summary>
    private void PutAwayNow()
    {
        Log.Info("Put away on closing; Claude runs on");
        _putAway = true;
        EventDoor.Muted = true;
        _speechHold.Stop();
        _heldLines.Clear();
        // The core drops what it held for speech, as the lines above drop this code's (MainForm.Flow.cs).
        FlowActed(UserAction.PutAway);
        SaveSettings();
        Hide();
        _front.GiveBack(Handle);
    }

    /// <summary>The attention timer's tick while the window is put away, and the core's Done then (<see cref="FlowDone"/>): nothing is acted on; it waits for <see cref="BringUp"/>.</summary>
    private void SettledWhilePutAway()
    {
        _attentionWaits = true;
        if (DoneChimeWhilePutAway && DoneBySettle && WaitSignature is null && _settings.SoundOnBell)
        {
            EventDoor.TurnDoneThroughMute();
        }
    }

    /// <summary>
    /// The window brought up (a "show" or a "message" hand-over): the same window made visible when it was put away,
    /// nothing rebuilt or read out again; in front; and the focus in the message field, or in what stands in its
    /// place while Claude waits on an answer, wherever it was before. A notice on view keeps the focus. What happened
    /// while the window was put away is said and asked now, in this order: a notice raised meanwhile is shown again as
    /// on its arrival; "Claude stopped" when Claude stopped; then, after the settle, what Claude waits on as a question
    /// that has just arrived, or "Done" when a turn ended; then a command an AI tool offered, with its caution.
    /// </summary>
    private void BringUp()
    {
        _front.Remember();
        if (_putAway)
        {
            Log.Info("Brought up again");
            _putAway = false;
            EventDoor.Muted = false;
            Opacity = 1;
            if (_stateWhenBroughtUp is { } state)
            {
                // A hidden start's first showing: its own frame back (CreateParams) and its own saved state (the constructor).
                _stateWhenBroughtUp = null;
                var style = (long)GetWindowLongPtrW(Handle, GWL_EXSTYLE);
                SetWindowLongPtrW(Handle, GWL_EXSTYLE, (nint)((style & ~WS_EX_TOOLWINDOW) | WS_EX_APPWINDOW));
                SetWindowPos(Handle, 0, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
                WindowState = state;
            }

            Show();
            ShowNoticeRaisedWhilePutAway();
            if (_stoppedWhilePutAway)
            {
                _stoppedWhilePutAway = false;
                if (!ProcessRunning)
                {
                    Announce(StoppedText + $". {BundleKeys.Text(AxClaudeKeys.RestartClaude)} starts it again", false);
                }
            }

            if (_attentionWaits)
            {
                _attentionWaits = false;
                _attention.Stop();
                _attention.Start();
            }

            // An offered command that waited (MainForm.Mcp.cs): asked now, heard with its caution (bundle AX-11.12).
            ShowOfferWhenIdle();
        }

        WindowMenu.Activate(Handle);
        if (!NoticeOpen)
        {
            BottomControl.Focus();
        }
    }

    /// <summary>Said when the window is brought up with a message typed and not sent in its field (FR-9.6).</summary>
    internal const string UnsentMessage = "Unsent message in the field";

    /// <summary>
    /// After a "show" bring-up: a message typed and not sent stays in the field (the user, 2026-11-15: kept, and said,
    /// so that it is no surprise): the recall chime and a short line after NVDA has read the field. Not after a
    /// "message" hand-over, which says what it added itself.
    /// </summary>
    private void SayUnsentMessage()
    {
        if (!NoticeOpen && BottomControl == Input && Input.Text.Trim().Length > 0)
        {
            EventDoor.Raise(EventKind.BroughtUp);
            Announce(UnsentMessage, false);
        }
    }

    /// <summary>Session, End Claude and close this window: the close for good, with the console window's question while Claude is in the middle of something (FR-1.7).</summary>
    private void EndForGood()
    {
        _ending = true;
        Close();
    }

    /// <summary>
    /// The "end" hand-over (axHelp quits): a request to close, never a forced close. A window with nothing to lose
    /// (Claude idle, no work in the background, nothing typed, no offered command waiting, no notice) closes at once.
    /// Any other is brought up and asks: FR-1.7's question while Claude works, waits for an answer or runs something
    /// in the background; a question of its own for a message typed and not sent or an offered command not answered.
    /// Kept, the window stays on view and closes from then on as any axClaude window does.
    /// </summary>
    private void EndRequested()
    {
        var busy = ClaudeBusy || ClaudeBackground is not null;
        var draft = Input.Text.Trim().Length > 0;
        var offer = _offerShown is not null || _offers.Count > 0;
        _keeperLeft = true;
        if (!busy && !draft && !offer && !NoticeOpen)
        {
            Log.Info("Ended by the program that keeps it");
            _ending = true;
            CloseConfirmed();
            return;
        }

        Log.Info("Asked to end by the program that keeps it, with something to lose: brought up to ask");
        BringUp();
        if (busy)
        {
            EndForGood();
            return;
        }

        if (NoticeOpen)
        {
            // A notice is on view already and is the user's to answer first; the window stays, closing as any other.
            return;
        }

        var lost = draft && offer ? "The message you typed is not sent, and a command offered to you is not answered; both would be lost."
            : draft ? "The message you typed is not sent and would be lost."
            : "A command offered to you is not answered and would be lost.";
        ShowNotice(Notice.Ask("Close this conversation?",
            $"The program that keeps this window has quit and asked it to close. {lost}\n" +
            "Kept open, it stays a window of its own, and Alt+F4 closes it.",
            "&Close anyway", () =>
            {
                _ending = true;
                CloseConfirmed();
            }, "&Keep it open", null, cancelIsDefault: true) with { Unprompted = true });
    }

    [DllImport("user32.dll")]
    private static extern nint GetWindowLongPtrW(nint hWnd, int index);

    [DllImport("user32.dll")]
    private static extern nint SetWindowLongPtrW(nint hWnd, int index, nint value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint after, int x, int y, int width, int height, uint flags);
}

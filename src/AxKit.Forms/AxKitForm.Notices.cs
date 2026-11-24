using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- notices: the window's own dialogs, drawn inside the window (AX-7.2, AX-6.4) ----
    //
    // The one focus rule (docs/axKit/SPEC.md AX-6.5): when the first notice of a chain opens, the control that has the
    // focus is remembered; a notice opened from a notice's button, or shown while one is open, continues the chain;
    // when the last one closes, that control gets the focus back if it is still there and visible, and the window's
    // FocusTarget otherwise. Nothing else decides where the focus goes, and no window writes its own version.

    /// <summary>The control that had the focus when the first notice of a chain opened, and gets it back when the last one closes; null between chains.</summary>
    private Control? _focusBeforeNotice;

    /// <summary>Inside a notice button's action: a notice the action opens continues the chain, so nothing is remembered again and the hooks fire once.</summary>
    private bool _chaining;

    /// <summary>The crash handler's report, shown inside the window like every other notice.</summary>
    public void ShowError(string title, string message) => ShowNotice(Notice.Plain(title, message) with { Unprompted = true });

    /// <summary>
    /// The one place a notice is shown; callers pass only its content. It adds the chime of a notice the user did
    /// not open, the focus in the notice, the hidden main controls, the blocked menu and window shortcuts, and
    /// Enter and Escape on the default and cancel buttons; the panel's Populate adds the key line made from the
    /// buttons and the hold of an unprompted notice. A notice shown while another is open replaces it. The axKit
    /// menu, in this project, calls it for the About sections.
    /// </summary>
    protected internal void ShowNotice(Notice notice)
    {
        _paletteSpeak.Stop();
        // AX-8.12: a notice that arrives while a key is tested ends the test, so that its keys answer the notice.
        EndKeyTest();
        if (PutAway)
        {
            // Unheard and unseen now: shown again when the window is brought up (ShowNoticeRaisedWhilePutAway).
            _noticeWhilePutAway = true;
        }

        Recent.Add(RecentActionKind.Notice, notice.Title, DateTime.UtcNow);
        if (notice.Unprompted && !notice.Silent && NoticeChime)
        {
            // The notice says which kind of event it is (Notice.Kind); each of the three has the notice sound today,
            // and each is raised by its name, so that the door knows which it was.
            switch (notice.Kind)
            {
                case EventKind.Failed:
                    EventDoor.Raise(EventKind.Failed);
                    break;
                case EventKind.Pairing:
                    EventDoor.Raise(EventKind.Pairing);
                    break;
                default:
                    EventDoor.Raise(EventKind.NoticeByItself);
                    break;
            }
        }

        if (!NoticeOpen && !_chaining)
        {
            _focusBeforeNotice = FocusedControl();
            OnNoticeOpened(_focusBeforeNotice);
        }

        NoticeOpen = true;
        Overlay.Populate(notice);
        PaintTheme(Overlay);
        Overlay.Visible = true;
        Overlay.BringToFront();
        AcceptButton = Overlay.DefaultButton;
        CancelButton = Overlay.CancelButton;
        Overlay.FocusStart();
        HideForNotice();
        if (MainMenuStrip is { } menu)
        {
            menu.Enabled = false;
        }

        PerformLayout();
    }

    private void OnNoticeChoice(OverlayChoice choice)
    {
        if (choice.StaysOpen)
        {
            choice.Action?.Invoke();
            return;
        }

        CloseNotice(choice.Action);
    }

    /// <summary>Closes the notice, runs <paramref name="then"/>, and gives the focus back unless the action opened the next notice of the chain or closed the window.</summary>
    protected internal void CloseNotice(Action? then)
    {
        if (!NoticeOpen)
        {
            return;
        }

        _paletteSpeak.Stop();
        NoticeOpen = false;
        ShowAfterNotice();
        if (RequestOverWindow)
        {
            // An AI's request still covers the window (its explanation just closed): the sections stay hidden under it.
            HideForNotice();
        }

        if (MainMenuStrip is { } menu)
        {
            menu.Enabled = true;
        }

        AcceptButton = null;
        CancelButton = null;
        PerformLayout();
        var outer = _chaining;
        _chaining = true;
        try
        {
            then?.Invoke();
        }
        finally
        {
            _chaining = outer;
        }

        // The action opened the next notice (the update's progress, an error, the changed-on-disk question after
        // Save): that one is showing now and the place from before carries over to it. A close inside an outer
        // close's action leaves the end of the chain to the outer one.
        if (IsDisposed || NoticeOpen || outer)
        {
            return;
        }

        OnNoticeClosed();
        var back = _focusBeforeNotice;
        _focusBeforeNotice = null;
        if (ActiveControl is null || Overlay.Contains(ActiveControl))
        {
            (back is { IsDisposed: false, Visible: true, Enabled: true } ? back : FocusTarget).Select();
        }

        Overlay.Visible = false;
    }

    /// <summary>The innermost control with the focus, outside the notice and the menu bar; null when none has it.</summary>
    private Control? FocusedControl()
    {
        var control = ActiveControl;
        while (control is ContainerControl { ActiveControl: { } inner })
        {
            control = inner;
        }

        return control is null || control is ToolStrip || Overlay.Contains(control) ? null : control;
    }

    /// <summary>
    /// After a paste into an editable text (AX-7.3, 2026-10-25): the paste is the control's own, and this says it
    /// once the control has done it, "Pasted" or "Pasted, 3 lines" (<see cref="PasteWords"/>); the text itself is never
    /// read out, since it may be a password. A menu item that pastes calls it the same way.
    /// </summary>
    protected internal void SayPasteAfter(TextBoxBase box)
    {
        string? text = null;
        try
        {
            if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
            }
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // The clipboard is held by another program for a moment: the paste is the control's, so say it plainly.
            text = string.Empty;
        }

        var said = PasteWords(text, box.Multiline);
        BeginInvoke(() => Announce(said, true));
    }

    /// <summary>What a paste says: "Pasted"; "Pasted, 3 lines" for several into a text of several lines; "Nothing to paste as text" when the clipboard holds none (a picture, files).</summary>
    public static string PasteWords(string? text, bool multiline)
    {
        if (text is null)
        {
            return "Nothing to paste as text";
        }

        var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n').Length;
        return multiline && lines > 1 ? $"Pasted, {lines} lines" : "Pasted";
    }

    /// <summary>A failed command's reason: one announcement, "Failed: ...", when it is one line; a notice with all of it when it is more (axGIT AG-7, axFile AF-4.5).</summary>
    protected internal void Failed(string title, string text)
    {
        if (text.Contains('\n'))
        {
            ShowNotice(Notice.Plain(title, text) with { Unprompted = true, Kind = EventKind.Failed });
        }
        else
        {
            Announce("Failed: " + text, true);
        }
    }

    /// <summary>
    /// A notice with one field (item-apps IA-5.1; axGIT's Commit and New branch, axFile's Filter, New folder and Add a
    /// program, and axMedia's Go to time since 2026-11-09): <paramref name="refused"/>, when given, is asked before the button
    /// acts, its reason said and the notice kept; then <paramref name="act"/> with the text, the notice closed. A field
    /// that may be left empty passes <paramref name="required"/> false; <paramref name="emptyMessage"/> is what an empty
    /// required field says; <paramref name="hidden"/> makes a password field; <paramref name="cancel"/> is the cancel's
    /// label where &C is the button's.
    /// </summary>
    protected internal void AskText(string title, string text, string label, string name, string initial, string button, Action<string> act, Func<string, string?>? refused = null, bool required = true, string emptyMessage = "Type something first", bool hidden = false, string cancel = "&Cancel")
    {
        ShowNotice(new Notice(title, text,
        [
            new OverlayChoice(button, () => act(Overlay.InputText), IsDefault: true),
            new OverlayChoice(cancel, IsCancel: true),
        ])
        {
            Input = new OverlayInput(label, name, initial, emptyMessage, Required: required, Hidden: hidden),
            Accepts = refused is null ? null : () =>
            {
                if (refused(Overlay.InputText) is { } why)
                {
                    Announce(why, true);
                    return false;
                }

                return true;
            },
        });
    }

    /// <summary>
    /// A form (AX-8.10): a notice whose text is the form's explanation, with the window's <paramref name="controls"/>
    /// under it in Tab order, then <paramref name="button"/>, the default button, and Cancel. Whatever asks more than
    /// one thing for one action is asked this way, once, never as one question after another. The focus starts in
    /// the first control, ready to type, with the form's sound (AX-7.11), and after NVDA has read that control the
    /// window says the title and the one word "form": "Move 2 items, form". The explanation is not said at opening
    /// (the maintainer's wish of 2026-11-22, "just the essential info when the cursor moves"): Ctrl+Q reads it, and
    /// Shift+Tab from the first control goes to it. <paramref name="sayText"/> is for the two forms whose text is
    /// not how a form is used but what will happen, which must be heard: what a report sends and to whom, and what
    /// an action written in a file will do (bundle rule 11); there the text is said after the word. Enter
    /// presses the default button from a one-line box, a list or a check box, and makes a new line in a box of several
    /// lines (the text then says the keys itself, and no line made from the buttons follows it); Ctrl+Enter presses
    /// the default button from any control (<see cref="BundleKeys.MainAction"/>); Escape cancels and nothing is kept.
    /// <paramref name="missing"/>, asked before the button acts, names the control that still needs something and
    /// what to say: the focus goes there with the edge sound, and the form stays open.
    /// </summary>
    protected internal void ShowForm(string title, string text, FormControls controls, string button, Action act, Func<(Control Field, string Said)?>? missing = null, Action? cancelled = null, bool sayText = false)
    {
        // TP-5.5: the templates on Shift+F10 in its text fields, in an app whose templates have a check; off until
        // NVDA is heard reading the menu (three passes of 2026-11-24 heard nothing open), so that a form's field keeps
        // Windows' own menu meanwhile.
        if (FormTemplatesOn)
        {
            OfferTemplatesInForm(controls);
        }
        // AX-8.10 rules 15 and 18: the keys every form has, at the end of its explanation, from the key table.
        ShowNotice(new Notice(title, text + " " + FormControls.KeysLine,
        [
            new OverlayChoice(button, act, IsDefault: true),
            new OverlayChoice("&Cancel", cancelled, IsCancel: true),
        ])
        {
            Custom = controls,
            KeyLine = !controls.EnterMakesNewLine,
            Accepts = missing is null ? null : () =>
            {
                if (missing() is not { } lacking)
                {
                    return true;
                }

                EventDoor.Raise(EventKind.Refused);
                Announce(lacking.Said, true);
                lacking.Field.Select();
                return false;
            },
        });
        // The notice's own FocusStart put the focus in the form's first control; here, once that has happened and
        // NVDA reads the control, the title and the word, queued after it and never over it.
        BeginInvoke(() =>
        {
            if (NoticeOpen && ReferenceEquals(Overlay.Shown?.Custom, controls))
            {
                if (!controls.ContainsFocus)
                {
                    controls.FocusFirst();
                }

                Announce(sayText ? $"{title}, form. {text}" : $"{title}, form", false);
            }
        });
    }

    /// <summary>An error the user must see: logged and shown as a notice; a console window adds a system line.</summary>
    protected virtual void Fail(string message)
    {
        Log.Error(message);
        ShowError("Error", message);
    }

    /// <summary>
    /// The window is put away: hidden by its own close and running on, to be shown again as it was (axClaude's window
    /// that hides on closing, docs/axClaude/SPEC.md FR-9.6, 2026-11-15; false for every other window, a hotkey window
    /// included, which speaks while hidden: axHelp's "The screenshot could not be taken"). While it is, the window
    /// announces nothing (<see cref="Announce"/>, <see cref="AnnounceBefore"/>: dropped, not kept for later) and the
    /// AI tools do not see it (<see cref="McpChannel"/> answers that it is hidden, so <c>axkit_windows</c> leaves it out).
    /// </summary>
    protected internal virtual bool PutAway => false;

    /// <summary>An announcement dropped because the window is put away: the window keeps what the user must still hear when it is brought up (axClaude: that Claude stopped); nothing by default.</summary>
    protected virtual void SaidWhilePutAway(string text)
    {
    }

    /// <summary>A notice was raised while the window was put away, unheard and unseen.</summary>
    private bool _noticeWhilePutAway;

    /// <summary>
    /// For a window brought up again: a notice raised while it was put away, and still showing, is shown again as on
    /// its arrival, with its chime and the focus where a notice puts it, so that NVDA reads it; true when there was one.
    /// </summary>
    protected bool ShowNoticeRaisedWhilePutAway()
    {
        var raised = _noticeWhilePutAway;
        _noticeWhilePutAway = false;
        if (!raised || !NoticeOpen || Overlay.Shown is not { } notice)
        {
            return false;
        }

        // Chained, so that the notice is not taken for one replacing itself (its cancel must not run, R-14).
        _chaining = true;
        try
        {
            ShowNotice(notice);
        }
        finally
        {
            _chaining = false;
        }

        return true;
    }

    /// <summary>
    /// What was announced before the window was first activated, said on the activation, in order (AX-7.1): NVDA
    /// ignores the announcements of a window that is not in front, and a window started by a program in the background
    /// (a hand-over, the test runner) comes up behind until it is activated; what it said as it opened was lost (heard
    /// by the NVDA pass, 2026-11-01). Null once the window has been activated.
    /// </summary>
    private List<(string Text, bool Interrupt)>? _beforeActivation = [];

    /// <summary>Half a second after the first activation: NVDA's foreground change and the notifications reach it by different roads, and a notification raised at the activation itself still found the window in the background.</summary>
    private readonly System.Windows.Forms.Timer _activationSay = new() { Interval = 500 };

    /// <summary>
    /// Announcements that do not interrupt, raised within <see cref="JoinMs"/> of the one before, go as one (AX-7.3):
    /// NVDA drops a notification that reaches it while the one before still waits its turn (the NVDA pass of 2026-11-01:
    /// a shell's "Ready" after its banner, a player's "Playing" after "Cannot play"). An interrupting one goes at once,
    /// with what waited in front of it, so that nothing is lost.
    /// </summary>
    private readonly List<string> _joining = [];

    private readonly System.Windows.Forms.Timer _joinSay = new() { Interval = JoinMs };

    private const int JoinMs = 300;

    private long _lastRaiseTick = long.MinValue / 2;

    /// <summary>Speaks without moving the focus (AX-7.3): on the notice while one shows, since the window's controls are hidden then, and on <see cref="AnnounceSource"/> otherwise; before the first activation, kept for it; close on the heels of the one before, joined to it.</summary>
    protected internal bool Announce(string text, bool interrupt)
    {
        // A window put away says nothing; what it would have said goes to SaidWhilePutAway, which keeps what matters.
        if (PutAway)
        {
            SaidWhilePutAway(text);
            return false;
        }

        // For a report (docs/axKit/reports.md RP-4.2): a window whose announcements may carry the user's content records
        // its commands and notices alone (RecordsAnnouncements).
        if (RecordsAnnouncements)
        {
            Recent.Add(RecentActionKind.Said, text, DateTime.UtcNow);
        }

        if (_beforeActivation is { } held)
        {
            // The last few only: a window left behind for an hour says what is recent, not its whole backlog.
            held.Add((text, interrupt));
            if (held.Count > 4)
            {
                held.RemoveAt(0);
            }

            return true;
        }

        if (interrupt)
        {
            _joinSay.Stop();
            var whole = _joining.Count == 0 ? text : Sentence(_joining.Append(text));
            _joining.Clear();
            return Raise(whole, true);
        }

        if (_joining.Count > 0 || Environment.TickCount64 - _lastRaiseTick < JoinMs)
        {
            // The timer runs from the first piece that waits, never restarted: a steady trickle is said every third of
            // a second, not held until it stops ("Ready. Sent. before. Done" came out as one, 2026-11-01).
            _joining.Add(text);
            if (!_joinSay.Enabled)
            {
                _joinSay.Start();
            }

            return true;
        }

        return Raise(text, false);
    }

    /// <summary>
    /// Words said ahead of what comes next (docs/axKit/mcp.md MC-7.5, "Claude ran Word wrap"): held to be joined to the
    /// next announcement within <see cref="JoinMs"/>, an interrupting one included, so that the command's own words never
    /// cut them off; said alone when nothing follows.
    /// </summary>
    protected void AnnounceBefore(string text)
    {
        if (PutAway)
        {
            return;
        }

        if (RecordsAnnouncements)
        {
            Recent.Add(RecentActionKind.Said, text, DateTime.UtcNow);
        }

        if (_beforeActivation is { } held)
        {
            held.Add((text, false));
            if (held.Count > 4)
            {
                held.RemoveAt(0);
            }

            return;
        }

        _joining.Add(text);
        if (!_joinSay.Enabled)
        {
            _joinSay.Start();
        }
    }

    /// <summary>Wires the two timers; called once from the constructor.</summary>
    private void WireAnnouncing()
    {
        _joinSay.Tick += (_, _) =>
        {
            _joinSay.Stop();
            if (_joining.Count > 0)
            {
                var whole = Sentence(_joining);
                _joining.Clear();
                Raise(whole, false);
            }
        };
        _activationSay.Tick += (_, _) =>
        {
            _activationSay.Stop();
            if (_beforeActivation is { } held)
            {
                _beforeActivation = null;
                if (held.Count > 0)
                {
                    // As one notification: raised one after another, an interrupting one would cancel the one before it
                    // (review, 2026-11-02); it interrupts when any of them would have.
                    Raise(Sentence(held.Select(h => h.Text)), held.Any(h => h.Interrupt));
                }
            }
        };
    }

    private bool Raise(string text, bool interrupt)
    {
        _lastRaiseTick = Environment.TickCount64;
        return NoticeOpen ? Overlay.Announce(text, interrupt) : Announcer.Raise(AnnounceSource, text, interrupt);
    }

    /// <summary>Several announcements as one, the last with its own ending (<see cref="SaidTogether.Sentence"/>).</summary>
    private static string Sentence(IEnumerable<string> pieces) => SaidTogether.Sentence(pieces);

    /// <summary>The first activation: what waited for it is said half a second on, so that NVDA has the window in front when the notifications arrive.</summary>
    private void SayWhatWaitedForActivation()
    {
        if (_beforeActivation is not null && !_activationSay.Enabled)
        {
            _activationSay.Start();
        }
    }
}

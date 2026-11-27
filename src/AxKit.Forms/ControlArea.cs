namespace AxKit.Forms;

/// <summary>
/// The control area (D33): the message field's place at the bottom of the window, which holds one control in the
/// field's stead while Claude waits on something: a list (a question's answers, with check boxes when it takes several;
/// /config's settings; a screen's lines and keys) or a one-line field for the user's own words. Every question gets a
/// new control, so the focus lands on a new control and NVDA reads its name, the question, before the item, as it
/// does whenever the focus comes back from the conversation. Enter chooses and Escape goes back; Tab and Shift+Tab
/// move to the conversation, as from the message field (MainForm). A line over the control shows the name on screen.
///
/// The keys that answer (docs/axKit/SPEC.md AX-6.4; <see cref="AxKit.Core.KeyHold"/>, where the hold's rule is
/// written once). In a list these are Enter, Escape, Space and everything typed (a letter, a digit, a punctuation
/// mark, with Shift or without); in a field Enter and Escape, since what is typed there only stands in the field
/// until Enter. The keys that only move (the arrows, Page Up and Down, Home, End) are never held. Five things hold
/// for the keys that answer, whoever shows the control:
/// 1. One hold for each control, new with the control. For a moment after a control appears (a second) its
///    answering keys do nothing: they were meant for the message field or for the question before. After its own
///    answer goes, the window lengthens that same control's hold (<see cref="Hold"/>; three seconds in axClaude), so
///    that a second Enter does not send the answer twice. The control shown next starts with a hold of its own and
///    inherits nothing: what protects it is its own moment, and what was left of the answer's seconds only kept the
///    user waiting at a list they had already heard. A hold is never shortened and nothing ends one early. Every
///    control holds so, whatever came before it; the one exception is a control the user opened with a command of
///    their own a moment ago (Go to line, the recent commands), whose caller says <c>hold: false</c> in those words,
///    and <c>ControlAreaHoldTests</c> lists the callers that may. A window that shows a question for a program
///    (axClaude) never passes anything but <c>hold: true</c>: that the user answered a moment ago does not make the
///    next question heard.
/// 2. The hold counts again from when the focus arrives (<see cref="FocusArrived"/>). A control shown with
///    <c>hold: true</c> starts its moment when it is shown and again each time it gets the focus, whoever moved it:
///    the window's own timer, a notice that closes over it, the window becoming the active one, a menu that closes,
///    the user's own focus key. NVDA says the question when the focus reaches it, and until then a key was meant for
///    the place the focus was in. A control shown with <c>hold: false</c> starts no hold then either.
/// 3. A repeat never answers. A key that answers and arrives as a key's repeat (the key was down already) does
///    nothing, held or not, and without a sound: a repeat is not a press, and a key held down longer than any hold
///    would otherwise answer the question that came meanwhile. In a list that is every key named above, a screen's
///    own letter and a digit among them; in a field Enter and Escape. The arrows repeat, and so does what is typed
///    into a field.
/// 4. A held key is heard. A fresh press of an answering key that the hold takes raises
///    <see cref="AxKit.Core.Events.EventKind.Refused"/> at the door, each time, so that a key that did nothing is
///    never silence: whatever a held list takes, Shift with a letter and a punctuation mark included.
/// 5. The hold is asked where the answer is taken (<see cref="Takes"/>). Each callback of the caller's (the choice,
///    Escape, a letter of the list's own, a typed character, a field's Enter) passes that one gate after the key was
///    read: held, it is heard and nothing happens. The reading of the keys keeps rule 3; the gate makes a key path
///    that was missed there harmless.
/// </summary>
public sealed class ControlArea : Panel
{
    private readonly Label _name = new();

    /// <summary>The hold of the control shown: its own moment from when it was shown and from when the focus came to it, and what <see cref="Hold"/> started since. New with every control, so that nothing is inherited.</summary>
    private AxKit.Core.KeyHold _hold = new();

    /// <summary>Whether the control shown arrived by itself: shown with <c>hold: true</c>, or opened by a command run for an AI tool (<see cref="HoldAsArrived"/>). Its moment then starts again each time the focus comes to it.</summary>
    private bool _arrived;

    /// <summary>The clock of the hold, in milliseconds: the system's, or a test's own.</summary>
    private readonly Func<long> _clock;

    /// <summary>What makes a held key heard: the refusal raised at the door, or a test's own count of it.</summary>
    private readonly Action _heldKeyHeard;

    /// <summary>Whether the key message the shown control was handed last is a repeat (<see cref="NoteKey"/>); taken once by the key's own event.</summary>
    private bool _repeat;

    public ControlArea()
        : this(() => Environment.TickCount64, AnswerKeys.HeldBack)
    {
    }

    /// <summary>
    /// An area whose hold runs by <paramref name="clock"/> (milliseconds) and whose held keys call
    /// <paramref name="heldKeyHeard"/>, for a test of the rules with a clock and a count of its own, and no sound.
    /// </summary>
    public ControlArea(Func<long> clock, Action heldKeyHeard)
    {
        _clock = clock;
        _heldKeyHeard = heldKeyHeard;
        Visible = false;
        Dock = DockStyle.Bottom;
        TabStop = false;
        _name.Dock = DockStyle.Top;
        _name.AutoEllipsis = true;
        _name.UseMnemonic = false;
        Controls.Add(_name);
    }

    /// <summary>The control shown, which takes the focus; null while the message field is in its place.</summary>
    public Control? Current { get; private set; }

    /// <summary>What the focus lands on: the list's selected item, or nothing for the field.</summary>
    public string CurrentItem => Current is ListBox { SelectedItem: string item } ? item : string.Empty;

    /// <summary>
    /// Whether the control shown arrived by itself (the class's summary, 2): false for one the user opened with a
    /// command of their own, and while the area shows none. The window asks it as the control closes: the field that
    /// gets the focus back from a question that arrived by itself holds its Enter for a moment
    /// (<see cref="ConversationForm.CloseArea"/>), and from a list the user opened it does not.
    /// </summary>
    public bool Arrived => Current is not null && _arrived;

    /// <summary>The height of the message field, which the area never goes below.</summary>
    [System.ComponentModel.DefaultValue(60)]
    public int MinimumHeight { get; set; } = 60;

    /// <summary>
    /// A list named <paramref name="name"/>, starting on item <paramref name="start"/>. With <paramref name="ticked"/>
    /// it has check boxes, those items ticked. Enter calls <paramref name="choose"/> with the focused item and the
    /// ticked ones, Escape calls <paramref name="escape"/>. The list takes the focus when <paramref name="focus"/>.
    /// <paramref name="hold"/> is true for everything but a list the user opened with a command of their own a moment
    /// ago (the class's summary): the list then ignores the answering keys for a moment after it is shown and after
    /// the focus comes to it. A plain letter goes to <paramref name="letter"/> first, when given: true takes the key
    /// (a screen's own key such as d to discard, from anywhere in the list), false leaves it to the list's jump to the
    /// item that starts with it. With <paramref name="typed"/> (axClaude's question, D33, 2026-11-26) every typed
    /// character but Space (a letter, a digit, a punctuation mark) calls it in place of the list's jump: nothing
    /// jumps to an item by typing then, not even an answer's own key (a number, <c>y</c>, <c>n</c>; the maintainer's
    /// word of 2026-11-26, "every key reads the question"), and the digits of <see cref="ListKeys"/> are off; Page Up,
    /// Page Down, Home, End and the arrows stay. Space stays the list's own: it ticks an answer of a list that takes
    /// several (AX-8.16).
    /// </summary>
    public void ShowList(string name, IReadOnlyList<string> items, int start, IReadOnlyList<int>? ticked, bool focus, bool hold,
        Action<int, IReadOnlyList<int>> choose, Action escape, Func<char, bool>? letter = null, Action? typed = null)
    {
        // What a program asks is shown with every character that is neither seen nor heard written out as its code
        // (DisplayText, the one table): the name and the answers are what the user decides on. Here, so that it
        // holds for every window's question (axClaude's screen source put its rows in as they came until 2026-11-27).
        name = AxKit.Core.DisplayText.VisibleLine(name);
        items = [.. items.Select(AxKit.Core.DisplayText.VisibleLine)];
        // A plain list box, a checked one for several answers: NVDA reads either as a list, the answer and its place.
        // The area's own classes of them, which only note whether a key is a repeat (NoteKey).
        ListBox list = ticked is null ? new AreaList(this) : new AreaCheckedList(this);
        list.IntegralHeight = false;
        list.AccessibleName = name;
        list.Dock = DockStyle.Fill;
        // AX-7.11, framework FW-3.8: the focus arrived in the answers: a list entered. Its field is the text box,
        // which says so itself. A question a program asks has its own sound first, and this one then stays silent.
        list.Enter += (_, _) => (FindForm() as AxKitForm)?.SoundOnEntry(list, AxKit.Core.Events.EventKind.ListEntered);
        list.Items.AddRange([.. items]);
        if (list is CheckedListBox boxes)
        {
            foreach (var i in ticked!)
            {
                boxes.SetItemChecked(i, true);
            }
        }

        if (items.Count > 0)
        {
            list.SelectedIndex = Math.Clamp(start, 0, items.Count - 1);
        }

        list.KeyDown += (_, e) =>
        {
            // Asked once: it marks the key handled and decides whether it may act (asked twice until 2026-10-23).
            var answering = Answering(e);
            if (answering && e.KeyCode == Keys.Enter)
            {
                if (list.SelectedIndex >= 0 && Takes(list))
                {
                    choose(list.SelectedIndex, list is CheckedListBox checkedList ? checkedList.CheckedIndices.Cast<int>().ToList() : []);
                }
            }
            else if (answering && e.KeyCode == Keys.Escape)
            {
                if (Takes(list))
                {
                    escape();
                }
            }
            else if (!e.Handled && letter is not null && e.Modifiers == Keys.None && e.KeyCode is >= Keys.A and <= Keys.Z
                && (!Takes(list) || letter((char)('a' + (e.KeyCode - Keys.A)))))
            {
                // The screen's own letter, or one the gate held back: either way it is not the list's to jump with.
                e.Handled = e.SuppressKeyPress = true;
            }
            else if (!e.Handled && (typed is null || e.KeyCode is not (>= Keys.D0 and <= Keys.D9)))
            {
                // Not while the area holds typed keys (Answering): they were meant for the message field. With typed,
                // a digit is a typed character like any other (below), so its jump of ListKeys is off.
                ListKeys.Handle(list, e);
            }
        };
        list.KeyPress += (_, e) =>
        {
            // The note of the character's own message, taken once.
            var repeat = _repeat;
            _repeat = false;
            if (e.Handled || char.IsControl(e.KeyChar) || char.IsWhiteSpace(e.KeyChar))
            {
                return;
            }

            if (Holding)
            {
                // A character whose key the key's own event did not take (a punctuation mark): held like every typed
                // key, and heard like them. It was meant for the message field, and never reaches the list's jump.
                e.Handled = true;
                if (!repeat)
                {
                    _heldKeyHeard();
                }

                return;
            }

            if (typed is null)
            {
                // The list box's own jump to the item that starts with it.
                return;
            }

            // Handled, the character never reaches the list box's own jump. A repeat reads nothing again.
            e.Handled = true;
            if (!repeat && Takes(list))
            {
                typed();
            }
        };

        var old = Place(list, name);
        Finish(list, old, focus, hold);
    }

    /// <summary>
    /// A one-line field named <paramref name="name"/>: Enter calls <paramref name="enter"/> with its text, Escape
    /// <paramref name="escape"/>. <paramref name="hold"/> as for a list (the class's summary): true for everything but
    /// a field the user opened with a command of their own a moment ago; Enter and Escape then do nothing for the
    /// field's moment, and what is typed goes into the field all the same.
    /// </summary>
    public void ShowField(string name, bool focus, bool hold, Action<string> enter, Action escape)
    {
        // As a list's name: nothing unseen in what the user is asked (DisplayText).
        name = AxKit.Core.DisplayText.VisibleLine(name);
        var field = new AreaField(this) { AccessibleName = name, Dock = DockStyle.Top };
        field.KeyDown += (_, e) =>
        {
            var answering = Answering(e);
            if (answering && e.KeyCode == Keys.Enter)
            {
                if (Takes(field))
                {
                    enter(field.Text);
                }
            }
            else if (answering && e.KeyCode == Keys.Escape)
            {
                if (Takes(field))
                {
                    escape();
                }
            }
        };
        Finish(field, Place(field, name), focus, hold);
        _fieldEnter = enter;
    }

    /// <summary>What Enter in the field shown does; null while the area shows no field.</summary>
    private Action<string>? _fieldEnter;

    /// <summary>
    /// The field shown, typed into and entered as the user's Enter would (docs/axKit/mcp.md MC-7.9: the text an AI tool's
    /// run of a command carries, for the field that command opened, never one that was there before); false when the
    /// area shows no field. No key of the user's and no answer of theirs: it passes no hold (the run holds the field
    /// it opened against the user's keys, <see cref="HoldAsArrived"/>, and fills it in the same breath).
    /// </summary>
    public bool Fill(string text)
    {
        if (Current is not TextBoxBase field || _fieldEnter is not { } enter)
        {
            return false;
        }

        field.Text = text;
        enter(text);
        return true;
    }

    /// <summary>The area empties and hides; the window has put the message field back.</summary>
    public void Clear()
    {
        Current?.Dispose();
        Current = null;
        _fieldEnter = null;
        _arrived = false;
        Visible = false;
    }

    /// <summary>
    /// The answering keys of the control shown do nothing for this long: its answer is on its way, and a second Enter
    /// would send it twice. Only ever longer, and only this control's: the one shown next has a hold of its own.
    /// </summary>
    public void Hold(int milliseconds) => _hold.Start(_clock(), milliseconds);

    /// <summary>
    /// The control shown counts as one that arrived by itself, though its caller showed it with <c>hold: false</c>:
    /// a command run for an AI tool opened it, not a key of the user's (<see cref="AxKitForm"/>'s run for the AI
    /// tools). Its answering keys do nothing for a moment from now, and again from when the focus comes to it.
    /// </summary>
    public void HoldAsArrived()
    {
        _arrived = true;
        _hold.Start(_clock());
    }

    /// <summary>Whether the answering keys of the control shown are held now.</summary>
    public bool Holding => _hold.Holding(_clock());

    /// <summary>
    /// The focus came to <paramref name="control"/>: when it is the control shown and it arrived by itself, its
    /// moment starts again (the class's summary, 2). The control's own GotFocus, which Windows raises for every way
    /// the focus arrives: a move inside the window, and the window becoming the active one with the control in it.
    /// </summary>
    private void FocusArrived(Control control)
    {
        if (_arrived && ReferenceEquals(Current, control))
        {
            _hold.Start(_clock());
        }
    }

    /// <summary>
    /// The one gate every answer of the area passes, after its key was read (the class's summary, 5): whether
    /// <paramref name="control"/> may hand its answer on. Not while its hold runs, which is heard; and never once it
    /// is no longer the control shown.
    /// </summary>
    private bool Takes(Control control)
    {
        if (!ReferenceEquals(Current, control))
        {
            return false;
        }

        if (!Holding)
        {
            return true;
        }

        _heldKeyHeard();
        return false;
    }

    /// <summary>
    /// The shown control's own class hands every key message here before the key's event is raised: whether it is a
    /// repeat of a key that was down already (<see cref="AnswerKeys.IsRepeat"/>). The event takes the note once
    /// (<see cref="Answering"/>, a list's typed character), so that a key event raised any other way counts as a press.
    /// </summary>
    private void NoteKey(in Message message) => _repeat = AnswerKeys.IsRepeat(message);

    /// <summary>The area's list: a plain list box that tells the area whether the key it is handed is a repeat.</summary>
    private sealed class AreaList(ControlArea area) : ListBox
    {
        protected override bool ProcessKeyMessage(ref Message m)
        {
            area.NoteKey(m);
            return base.ProcessKeyMessage(ref m);
        }
    }

    /// <summary>The area's list with check boxes, as <see cref="AreaList"/>.</summary>
    private sealed class AreaCheckedList(ControlArea area) : CheckedListBox
    {
        protected override bool ProcessKeyMessage(ref Message m)
        {
            area.NoteKey(m);
            return base.ProcessKeyMessage(ref m);
        }
    }

    /// <summary>The area's field: the bundle's text box, which tells the area whether the key it is handed is a repeat.</summary>
    private sealed class AreaField(ControlArea area) : AxKitTextBox
    {
        protected override bool ProcessKeyMessage(ref Message m)
        {
            area.NoteKey(m);
            return base.ProcessKeyMessage(ref m);
        }
    }

    /// <summary>The height for the rows the control shows, from the message field's height up to half the window.</summary>
    public void FitHeight()
    {
        if (Current is null || Parent is null)
        {
            return;
        }

        var row = TextRenderer.MeasureText("Wg", Font).Height + 6;
        var rows = Current is ListBox list ? list.Items.Count : 1;
        _name.Height = row;
        Height = Math.Clamp(row * (rows + 1) + 8, MinimumHeight, Math.Max(MinimumHeight, Parent.ClientSize.Height / 2));
    }

    /// <summary>
    /// Enter and Escape are the control's own (and are kept from the edit control or the list) unless the control's
    /// hold takes them: then they do nothing, and in a list neither do Space and the typed letters and digits, with
    /// Shift or without, which tick an answer, jump to one or are a screen's own key. In a field what is typed goes
    /// into the field, held or not: it answers nothing before Enter, and the words typed right after "Your words
    /// next" must not lose their start. A repeat of any of these keys never acts, held or not, and says nothing (but
    /// in a field, where a repeated character is text); a fresh press the hold takes is heard (the class's summary, 3
    /// and 4). True when the key is Enter or Escape and may act.
    /// </summary>
    private bool Answering(KeyEventArgs e)
    {
        // The note of the key message this event came from, taken once.
        var repeat = _repeat;
        _repeat = false;
        var inList = Current is ListBox;
        var answering = e.KeyCode is Keys.Enter or Keys.Escape && e.Modifiers == Keys.None;
        var typed = inList && (e.KeyCode == Keys.Space
            || ((e.Modifiers & (Keys.Control | Keys.Alt)) == Keys.None && e.KeyCode is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9 or >= Keys.NumPad0 and <= Keys.NumPad9));
        if (repeat && (answering || typed))
        {
            // A repeat is not a press: a key held down never answers, however long.
            e.Handled = e.SuppressKeyPress = true;
            return false;
        }

        if (Holding && (answering || typed))
        {
            e.Handled = e.SuppressKeyPress = true;
            _heldKeyHeard();
            return false;
        }

        if (answering)
        {
            e.Handled = e.SuppressKeyPress = true;
        }

        return answering;
    }

    /// <summary>Puts the new control over the old one; returns the old one.</summary>
    private Control? Place(Control control, string name)
    {
        // Enter and Escape reach the control's KeyDown rather than the window's default and cancel buttons. Only the
        // plain keys (KeyData, not KeyCode): with a modifier they belong to the window, which skips ProcessCmdKey for
        // an input key (a window's key with Escape or Enter in it, such as axClaude's Ctrl+Shift+Enter, must stay a command from anywhere).
        control.PreviewKeyDown += (_, e) => e.IsInputKey |= e.KeyData is Keys.Enter or Keys.Escape;
        control.GotFocus += (_, _) => FocusArrived(control);
        var old = Current;
        _name.Text = name;
        Visible = true;
        Controls.Add(control);
        control.BringToFront();
        Current = control;
        FitHeight();
        return old;
    }

    /// <summary>
    /// The new control takes the focus before the old one goes, so the focus never passes through the conversation on
    /// the way. The control gets a hold of its own: with <paramref name="hold"/> its answering keys do nothing for
    /// the hold's moment from now, and again from when the focus comes to it (<see cref="FocusArrived"/>); without,
    /// it starts with none and the focus starts none. (Until 2026-11-27 the area had one hold, which a control shown
    /// without one cleared; axClaude showed the question after an answer so, and a doubled Enter answered it unheard:
    /// the class's summary.)
    /// </summary>
    private void Finish(Control control, Control? old, bool focus, bool hold)
    {
        _hold = new AxKit.Core.KeyHold();
        _repeat = false;
        _arrived = hold;
        if (hold)
        {
            _hold.Start(_clock());
        }

        if (focus)
        {
            control.Focus();
        }

        old?.Dispose();
    }
}

namespace AxKit.Forms;

/// <summary>
/// The control area (D33): the message field's place at the bottom of the window, which holds one control in the
/// field's stead while Claude waits on something: a list (a question's answers, with check boxes when it takes several;
/// /config's settings; a screen's lines and keys) or a one-line field for the user's own words. Every question gets a
/// new control, so the focus lands on a new control and NVDA reads its name, the question, before the item, as it
/// does whenever the focus comes back from the conversation. Enter chooses and Escape goes back; Tab and Shift+Tab
/// move to the conversation, as from the message field (MainForm). A line over the control shows the name on screen.
/// For a moment after a control appears, and after an answer goes, Enter, Space, Escape and typed keys do nothing:
/// they were meant for the message field, or the answer is on its way.
/// </summary>
public sealed class ControlArea : Panel
{
    private readonly Label _name = new();
    private readonly KeyHold _hold = new();

    public ControlArea()
    {
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

    /// <summary>The height of the message field, which the area never goes below.</summary>
    [System.ComponentModel.DefaultValue(60)]
    public int MinimumHeight { get; set; } = 60;

    /// <summary>
    /// A list named <paramref name="name"/>, starting on item <paramref name="start"/>. With <paramref name="ticked"/>
    /// it has check boxes, those items ticked. Enter calls <paramref name="choose"/> with the focused item and the
    /// ticked ones, Escape calls <paramref name="escape"/>. The list takes the focus when <paramref name="focus"/>;
    /// <paramref name="hold"/> makes it ignore the answering keys for a moment. A plain letter goes to
    /// <paramref name="letter"/> first, when given: true takes the key (a screen's own key such as d to discard, from
    /// anywhere in the list), false leaves it to the list's jump to the item that starts with it. With
    /// <paramref name="typed"/> (axClaude's question, D33, 2026-11-26) every typed character but Space (a letter, a
    /// digit, a punctuation mark) calls it in place of the list's jump: nothing jumps to an item by typing then, not
    /// even an answer's own key (a number, <c>y</c>, <c>n</c>; the maintainer's word of 2026-11-26, "every key reads
    /// the question"), and the digits of <see cref="ListKeys"/> are off; Page Up, Page Down, Home, End and the arrows
    /// stay. Space stays the list's own: it ticks an answer of a list that takes several (AX-8.16).
    /// </summary>
    public void ShowList(string name, IReadOnlyList<string> items, int start, IReadOnlyList<int>? ticked, bool focus, bool hold,
        Action<int, IReadOnlyList<int>> choose, Action escape, Func<char, bool>? letter = null, Action? typed = null)
    {
        // A plain list box, a checked one for several answers: NVDA reads either as a list, the answer and its place.
        var list = ticked is null ? new ListBox() : new CheckedListBox();
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
                if (list.SelectedIndex >= 0)
                {
                    choose(list.SelectedIndex, list is CheckedListBox checkedList ? checkedList.CheckedIndices.Cast<int>().ToList() : []);
                }
            }
            else if (answering && e.KeyCode == Keys.Escape)
            {
                escape();
            }
            else if (!e.Handled && letter is not null && e.Modifiers == Keys.None && e.KeyCode is >= Keys.A and <= Keys.Z
                && letter((char)('a' + (e.KeyCode - Keys.A))))
            {
                e.Handled = e.SuppressKeyPress = true;
            }
            else if (!e.Handled && (typed is null || e.KeyCode is not (>= Keys.D0 and <= Keys.D9)))
            {
                // Not while the area holds typed keys (Answering): they were meant for the message field. With typed,
                // a digit is a typed character like any other (below), so its jump of ListKeys is off.
                ListKeys.Handle(list, e);
            }
        };
        if (typed is not null)
        {
            list.KeyPress += (_, e) =>
            {
                if (e.Handled || char.IsControl(e.KeyChar) || char.IsWhiteSpace(e.KeyChar))
                {
                    return;
                }

                // Handled, the character never reaches the list box's own jump. While the area holds its keys it was
                // meant for the message field, and does nothing.
                e.Handled = true;
                if (!_hold.Holding)
                {
                    typed();
                }
            };
        }

        var old = Place(list, name);
        Finish(list, old, focus, hold);
    }

    /// <summary>A one-line field named <paramref name="name"/>: Enter calls <paramref name="enter"/> with its text, Escape <paramref name="escape"/>.</summary>
    public void ShowField(string name, bool focus, bool hold, Action<string> enter, Action escape)
    {
        var field = new AxKitTextBox { AccessibleName = name, Dock = DockStyle.Top };
        field.KeyDown += (_, e) =>
        {
            var answering = Answering(e);
            if (answering && e.KeyCode == Keys.Enter)
            {
                enter(field.Text);
            }
            else if (answering && e.KeyCode == Keys.Escape)
            {
                escape();
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
    /// area shows no field.
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
        Visible = false;
    }

    /// <summary>The answering keys do nothing for this long: an answer is on its way, and a second Enter would send it twice.</summary>
    public void Hold(int milliseconds) => _hold.Start(milliseconds);

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
    /// Enter, Escape, Space and typed keys are the control's own (and are kept from the edit control or the list) unless
    /// the area holds them: then they do nothing. True when the key may act.
    /// </summary>
    private bool Answering(KeyEventArgs e)
    {
        var answering = e.KeyCode is Keys.Enter or Keys.Escape && e.Modifiers == Keys.None;
        if (_hold.Holding && (answering || e.KeyCode == Keys.Space || (e.Modifiers == Keys.None && e.KeyCode is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)))
        {
            e.Handled = e.SuppressKeyPress = true;
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
        var old = Current;
        _name.Text = name;
        Visible = true;
        Controls.Add(control);
        control.BringToFront();
        Current = control;
        FitHeight();
        return old;
    }

    /// <summary>The new control takes the focus before the old one goes, so the focus never passes through the conversation on the way.</summary>
    private void Finish(Control control, Control? old, bool focus, bool hold)
    {
        if (hold)
        {
            _hold.Start();
        }
        else
        {
            _hold.Clear();
        }

        if (focus)
        {
            control.Focus();
        }

        old?.Dispose();
    }
}

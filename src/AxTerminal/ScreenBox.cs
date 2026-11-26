using AxKit.Forms;
using AxTerminal.Core;

namespace AxTerminal;

/// <summary>
/// The screen view's text (SPEC.md AT-9.2, AT-9.3): the page of a full-screen program, read-only, one line per row,
/// no wrapping. While <see cref="KeysToProgram"/> every key goes to the program as a terminal sends it
/// (<see cref="ScreenKeys"/>), ahead of the window's commands, but the bundle's mode key (Ctrl+Space, which turns to
/// reading the screen and back, <see cref="ModeKey"/>), F1 and Alt+F4; a key that types a character sends the
/// character. Otherwise it is a text to read like any other. The window fills the text and places the caret.
/// </summary>
internal sealed class ScreenBox : AxKitTextBox
{
    private const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_CHAR = 0x0102, WM_SYSCHAR = 0x0106, WM_CONTEXTMENU = 0x007B;

    /// <summary>The first half of a character outside the basic plane, waiting for its second.</summary>
    private char? _highSurrogate;

    public ScreenBox()
    {
        Multiline = true;
        ReadOnly = true;
        WordWrap = false;
        ScrollBars = ScrollBars.Both;
        AcceptsTab = false;
        HideSelection = false;
        BackColor = SystemColors.Window;
        ForeColor = SystemColors.WindowText;
        Dock = DockStyle.Fill;
        Visible = false;
    }

    /// <summary>Whether the keys go to the program (true) or stay in the text to read it (false).</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool KeysToProgram { get; set; } = true;

    /// <summary>What a key sends to the program, while the keys go there.</summary>
    public event Action<string>? Typed;

    /// <summary>The bundle's mode key in the view, either way: the window turns between the two.</summary>
    public event Action? ModeKey;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == BundleKeys.ToggleMode)
        {
            ModeKey?.Invoke();
            return true;
        }

        if (!KeysToProgram || keyData is BundleKeys.Shortcuts or (Keys.Alt | Keys.F4))
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        var sequence = ScreenKeys.Sequence((int)(keyData & Keys.KeyCode), keyData.HasFlag(Keys.Control), keyData.HasFlag(Keys.Shift), keyData.HasFlag(Keys.Alt));
        if (sequence is not null)
        {
            Typed?.Invoke(sequence);
            return true;
        }

        // A key that types: its character follows (WndProc). Nothing of the window sees it.
        return false;
    }

    /// <summary>A character with Alt held comes as a system character: it is the view's, not a menu's mnemonic.</summary>
    protected override bool IsInputChar(char charCode) => KeysToProgram || base.IsInputChar(charCode);

    protected override void WndProc(ref Message m)
    {
        if (KeysToProgram && Visible)
        {
            switch (m.Msg)
            {
                case WM_CHAR or WM_SYSCHAR:
                    SendCharacter((char)(m.WParam.ToInt64() & 0xFFFF), alt: m.Msg == WM_SYSCHAR);
                    return;
                case WM_KEYDOWN or WM_KEYUP or WM_CONTEXTMENU:
                    // The keys are the program's: the edit control's own moves, copy and menu stay out of it.
                    return;
            }
        }

        base.WndProc(ref m);
    }

    private const int WM_SETREDRAW = 0x000B;

    /// <summary>The rows replaced and the caret placed with the drawing held, so that a frame is painted once (AT-9.5).</summary>
    public void SuspendDrawing(Action change)
    {
        User32.SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            change();
        }
        finally
        {
            User32.SendMessage(Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            Invalidate();
        }
    }

    private void SendCharacter(char c, bool alt)
    {
        if (char.IsHighSurrogate(c))
        {
            _highSurrogate = c;
            return;
        }

        var typed = _highSurrogate is { } high && char.IsLowSurrogate(c) ? new string([high, c]) : c.ToString();
        _highSurrogate = null;
        Typed?.Invoke(ScreenKeys.Character(typed, alt));
    }
}

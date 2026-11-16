using System.Runtime.InteropServices;
using AxKit.Core;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- the key test (docs/axKit/SPEC.md AX-8.12): Help, Diagnostics, Test a key ----
    //
    // While it runs, every key message for this window is taken off the message loop before any control, menu or
    // command sees it (KeyTestFilter), said as the key's name and what it does here (AxKit.Core/KeyTest), and never
    // carried out. A key that is not said did not reach axKit: the keyboard, Windows, NVDA or another program took it.

    /// <summary>The run of the key test, or null while none runs.</summary>
    private KeyTestSession? _keyTest;

    private KeyTestFilter? _keyTestFilter;

    /// <summary>The keys the menus show, read once as the test starts: reading them fills the lists that fill as they open.</summary>
    private List<KeyTestCommand> _keyTestMenuKeys = [];

    /// <summary>Whether the key test runs: every key is said and none is carried out.</summary>
    protected internal bool KeyTestRunning => _keyTest is not null;

    /// <summary>
    /// What a key does that the focused control handles itself and no command of the list holds, where the window
    /// knows (a transcript's quick keys): the words after the key's name, "moves to the next heading"; null, the
    /// default, leaves it to what a control of that kind does with the key.
    /// </summary>
    protected virtual string? KeyTestOwn(Keys key) => null;

    /// <summary>Help, Diagnostics, Test a key: starts the test and says how it works and how it ends. Not offered while a notice shows, as no menu command is.</summary>
    private void StartKeyTest()
    {
        if (_keyTest is not null || NoticeOpen)
        {
            return;
        }

        _keyTestMenuKeys = KeysOfTheMenus();
        _keyTest = new KeyTestSession();
        _keyTestFilter = new KeyTestFilter(this);
        Application.AddMessageFilter(_keyTestFilter);
        Announce(KeyTest.Started, true);
    }

    /// <summary>
    /// Ends the test: Escape, the window no longer in front or hidden, a notice arriving, the window closing.
    /// The keys are the window's again, and "Key test ended" is said at once unless the window is going.
    /// </summary>
    private void EndKeyTest(bool say = true)
    {
        if (_keyTest is null)
        {
            return;
        }

        _keyTest = null;
        if (_keyTestFilter is { } filter)
        {
            Application.RemoveMessageFilter(filter);
            _keyTestFilter = null;
        }

        if (say)
        {
            Announce(KeyTest.Ended, true);
        }
    }

    /// <summary>The window is no longer in front: the test ends, since its keys go elsewhere now.</summary>
    protected override void OnDeactivate(EventArgs e)
    {
        EndKeyTest();
        base.OnDeactivate(e);
    }

    /// <summary>A window hidden while its test runs (a hotkey window's own hotkey) ends the test.</summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible)
        {
            EndKeyTest();
        }

        base.OnVisibleChanged(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        EndKeyTest(say: false);
        base.OnFormClosed(e);
    }

    /// <summary>A key went down while the test runs, once per press: its line said, or the test ended on Escape.</summary>
    private void KeyTestDown(Keys key)
    {
        if (_keyTest is not { } test)
        {
            return;
        }

        var code = (int)(key & Keys.KeyCode);
        var press = new KeyTestPress(code, key.HasFlag(Keys.Control), key.HasFlag(Keys.Shift), key.HasFlag(Keys.Alt), KeyTestName(key));
        var modifiers = KeyTest.IsWindowsKey(code) ? "Windows" : BundleKeys.Text(key & Keys.Modifiers).TrimEnd('.');
        // "Ctrl+Shift+None": the modifiers alone, without the key code's name.
        modifiers = modifiers.EndsWith("+None", StringComparison.Ordinal) ? modifiers[..^5] : modifiers == "None" ? string.Empty : modifiers;
        // A text in navigation mode (AX-8.13) says what its keys do there first: a jump letter, a refused key.
        var (said, ends) = test.Down(press, modifiers, () => KeyTestInMode(key) is { } inMode
            ? $"{press.Text}: {inMode}"
            : KeyTest.Describe(press, KeyTestCommands(), KeyTestBundle(), KeyTestFocusNow(), KeyTestOwn(key)));
        if (ends)
        {
            EndKeyTest();
        }
        else if (said is not null)
        {
            Announce(said, true);
        }
    }

    /// <summary>A key came up while the test runs: a modifier pressed and released alone is named.</summary>
    private void KeyTestUp(int code)
    {
        if (_keyTest?.Up(code) is { } said)
        {
            Announce(said, true);
        }
    }

    /// <summary>A character that arrived with no key press of its own (a program that sends text): said, not typed.</summary>
    private void KeyTestCharacter(char character)
    {
        if (_keyTest is not null && !char.IsControl(character))
        {
            Announce($"{(character == ' ' ? "Space" : character.ToString())}: a character sent without a key press; it types the character", true);
        }
    }

    /// <summary>
    /// A key as the test names it: the bundle's spelling (<see cref="BundleKeys.Text"/>, without a number key's full
    /// stop), with a plain name for the keys no table holds and Windows names oddly.
    /// </summary>
    private static string KeyTestName(Keys key)
    {
        var text = BundleKeys.Text(key).TrimEnd('.');
        var own = (key & Keys.KeyCode) switch
        {
            Keys.PageDown => "PageDown",
            Keys.PageUp => "PageUp",
            Keys.Capital => "CapsLock",
            Keys.Oemtilde => "Backquote",
            Keys.OemOpenBrackets => "LeftBracket",
            Keys.OemCloseBrackets => "RightBracket",
            Keys.OemPipe or Keys.OemBackslash => "Backslash",
            Keys.OemQuotes => "Quote",
            Keys.PrintScreen => "PrintScreen",
            Keys.Scroll => "ScrollLock",
            _ => null,
        };
        return own is null ? text : text[..(text.LastIndexOf('+') + 1)] + own;
    }

    /// <summary>The window's commands with a key, in the order they are asked, each with whether its key acts now; then the keys the menus show for what is no command of the list, and the bar's menus by their letters.</summary>
    private List<KeyTestCommand> KeyTestCommands()
    {
        var keys = new List<KeyTestCommand>();
        foreach (var command in _commands)
        {
            if (command.Key != Keys.None)
            {
                // Under the name its menu item has, which carries the submenu ("Next, Heading"), where the command has an item.
                var text = KeyTestName(command.Key);
                var plain = MenuText.Plain(command.Label);
                var shown = _keyTestMenuKeys.FirstOrDefault(menu => string.Equals(menu.Key, text, StringComparison.OrdinalIgnoreCase) && menu.Label.EndsWith(plain, StringComparison.Ordinal))?.Label;
                keys.Add(new KeyTestCommand(text, shown ?? plain, command.When?.Invoke() ?? true));
            }
        }

        var ofCommands = keys.Select(key => key.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        keys.AddRange(_keyTestMenuKeys.Where(key => !ofCommands.Contains(key.Key)));
        return keys;
    }

    /// <summary>Every menu item that shows a key, by that key, and "Alt+H: opens the Help menu" for each menu of the bar.</summary>
    private List<KeyTestCommand> KeysOfTheMenus()
    {
        var keys = new List<KeyTestCommand>();
        if (MainMenuStrip is not { } bar)
        {
            return keys;
        }

        foreach (var entry in CommandPalette.Entries(bar))
        {
            if (entry.Key is { Length: > 0 } key)
            {
                // An item of a submenu is named with it, "Next, Heading" and "Previous, Heading": the name alone is both.
                keys.Add(new KeyTestCommand(key.TrimEnd('.'), entry.Path.Count > 1 ? $"{entry.Path[^1]}, {entry.Name}" : entry.Name));
            }
        }

        foreach (ToolStripItem menu in bar.Items)
        {
            var text = menu.Text ?? string.Empty;
            var at = text.IndexOf('&');
            if (at >= 0 && at + 1 < text.Length)
            {
                keys.Add(new KeyTestCommand("Alt+" + char.ToUpperInvariant(text[at + 1]), $"opens the {MenuText.Plain(text)} menu"));
            }
        }

        return keys;
    }

    /// <summary>The keys the window base handles itself in every window, before any command (<see cref="ProcessCmdKey"/>).</summary>
    private static readonly Keys[] HandledByTheBase =
    [
        BundleKeys.LargerText, BundleKeys.LargerTextNumpad, BundleKeys.SmallerText, BundleKeys.SmallerTextNumpad, BundleKeys.SayStatus,
        BundleKeys.Shortcuts, BundleKeys.CommandPalette, BundleKeys.Report, BundleKeys.AskClaude, BundleKeys.Save,
        BundleKeys.ToggleMode, BundleKeys.AskClaudeAlso, BundleKeys.ContextMenu,
    ];

    /// <summary>The bundle's keys (<see cref="BundleKeys.All"/>): one the base handles applies everywhere, under its menu item's name where a menu shows it; the others mean something only through a window's command or the focused control.</summary>
    private List<KeyTestCommand> KeyTestBundle()
    {
        var keys = new List<KeyTestCommand>();
        foreach (var (name, key) in BundleKeys.All)
        {
            var text = KeyTestName(key);
            var shown = _keyTestMenuKeys.FirstOrDefault(menu => string.Equals(menu.Key, text, StringComparison.OrdinalIgnoreCase))?.Label;
            var handled = HandledByTheBase.Contains(key);
            keys.Add(new KeyTestCommand(text, handled ? shown ?? name : name, handled));
        }

        return keys;
    }

    /// <summary>What kind of control has the focus now.</summary>
    private static KeyTestFocus KeyTestFocusNow() => Control.FromChildHandle(User32.GetFocus()) switch
    {
        TextBoxBase { ReadOnly: true } => KeyTestFocus.ReadOnlyText,
        TextBoxBase { Multiline: true } => KeyTestFocus.Text,
        TextBoxBase => KeyTestFocus.OneLineText,
        ListBox => KeyTestFocus.List,
        CheckBox => KeyTestFocus.CheckBox,
        ButtonBase => KeyTestFocus.Button,
        _ => KeyTestFocus.Other,
    };

    /// <summary>
    /// The key test's catch: on the message loop while the test runs, ahead of every control, menu and command of
    /// the window. It takes every keyboard message for the window or a control in it, so that nothing is typed, no
    /// command runs, Alt and F10 open no menu and Alt+F4 closes nothing, and hands the window each press once (a held
    /// key's repeats are taken and not said). The automation channel's keys arrive the same way, posted to the
    /// focused control, with their modifiers already set by the channel's own filter, which was added first.
    /// </summary>
    private sealed class KeyTestFilter(AxKitForm form) : IMessageFilter
    {
        private const int WM_KEYFIRST = 0x0100;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_KEYLAST = 0x0109;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg is < WM_KEYFIRST or > WM_KEYLAST || form.IsDisposed || !form.IsHandleCreated
                || (m.HWnd != form.Handle && !IsChild(form.Handle, m.HWnd)))
            {
                return false;
            }

            switch (m.Msg)
            {
                case WM_KEYDOWN or WM_SYSKEYDOWN when (m.WParam.ToInt64() & 0xFFFF) == (long)Keys.Packet:
                    // A character a program sent with no key of its own (SendInput's Unicode input, as the NVDA tier
                    // types): its key-down is VK_PACKET, and the character itself comes only as the WM_CHAR that
                    // TranslateMessage makes of it. Translated here, since the key-down is taken, so that the
                    // character is said as one sent without a key press; the packet is never named as a key.
                    var message = new MSG { hwnd = m.HWnd, message = (uint)m.Msg, wParam = m.WParam, lParam = m.LParam };
                    TranslateMessage(ref message);
                    break;
                case WM_KEYDOWN or WM_SYSKEYDOWN:
                    // Bit 30: the key was down already, a repeat of a held key.
                    if ((m.LParam.ToInt64() & (1L << 30)) == 0)
                    {
                        var code = (Keys)(m.WParam.ToInt64() & 0xFFFF);
                        form.KeyTestDown(code | Control.ModifierKeys);
                    }

                    break;
                case WM_KEYUP or WM_SYSKEYUP when (m.WParam.ToInt64() & 0xFFFF) == (long)Keys.Packet:
                    break;
                case WM_KEYUP or WM_SYSKEYUP:
                    form.KeyTestUp((int)(m.WParam.ToInt64() & 0xFFFF));
                    break;
                case WM_CHAR:
                    form.KeyTestCharacter((char)(m.WParam.ToInt64() & 0xFFFF));
                    break;
            }

            return true;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsChild(nint parent, nint window);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public nint hwnd;
            public uint message;
            public nint wParam;
            public nint lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TranslateMessage(ref MSG message);
    }
}

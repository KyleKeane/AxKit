using System.Runtime.InteropServices;
using AxKit.Core;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// The window of a background app that a global hotkey brings up (docs/axStart/SPEC.md AS-1, AS-3.4, AS-4; from
/// axStart's StartForm on its second use, axClip, 2026-10-23): the process runs with the window hidden, the hotkey
/// registered on the window's handle shows it in front, and the window hides again on Escape, Alt+F4 and the focus
/// going to another program. Only Quit ends the process. What is here moved unchanged: the first showing that only
/// makes the handle, the hotkey and its notice, the showing and hiding with the one rule of the foreground, the hide
/// when another program comes to the front, and the search for the running window. The hotkey, Start with Windows
/// and Say the first match are the app's <see cref="HotkeySettings"/>, read here (2026-11-09); the app fills in the
/// window's size and its controls on showing (<see cref="PrepareShowing"/>, <see cref="AfterShowing"/>).
/// </summary>
public abstract class HotkeyForm : AxKitForm
{
    private const int WM_HOTKEY = 0x0312;
    private const int WM_ACTIVATE = 0x0006;
    private const int HotkeyId = 1;
    private const int MOD_NOREPEAT = 0x4000;

    /// <summary>How long after a start the foreground is left to the program started before the rule of <see cref="GiveForegroundBack"/> is applied (AS-3.4).</summary>
    private const int StartSettleMs = 3000;

    private readonly bool _hidden;
    private readonly HotkeySettings _settings;
    private Hotkey? _registered;

    /// <summary>
    /// The window the foreground goes back to whenever this process holds it while the window is hidden (AS-3.4): the
    /// window of another program that was in front when this one last showed, or, before any showing, when the process
    /// started. The one place the rule reads; <see cref="ShowWindowInFront"/> the one place that sets it.
    /// </summary>
    private nint _giveBackTo = GetForegroundWindow();

    private Hotkey? _captured;
    private bool _capturing;
    private bool _quitting;

    /// <summary>
    /// Options, General's Start with Windows for a background app: the toggle that writes the Run key through
    /// <see cref="SignInStart.Apply"/>, or, in the Store's copy, where the package's startup task decides and Windows
    /// lists it under Settings, Apps, Startup (docs/axKit/SPEC.md AX-13.4), an item that opens that page and says so;
    /// an app without an entry of its own in the package (AX-13.2: the eight are taken; axHelp) has no
    /// startup task there, and its item says how it is started instead.
    /// </summary>
    protected ToolStripMenuItem StartWithWindowsItem()
    {
        if (!PackageIdentity.IsPackaged)
        {
            return Toggle("&Start with Windows", _settings.StartWithWindows, v =>
            {
                _settings.StartWithWindows = v;
                SignInStart.Apply(AppName, AppVerb, v);
            });
        }

        if (Dispatch.ByName(AppName) is { StoreEntry: false })
        {
            return new ToolStripMenuItem("&Start with Windows (not in the Store's copy)", null, (_, _) =>
                Announce($"The Store's copy has no sign-in start for {AppName}: the Store allows eight apps with startup tasks, and they are taken. Start {AppName} from any axKit window, Window menu, New window; it then keeps running until Quit", true));
        }

        return new ToolStripMenuItem("&Start with Windows (Windows Settings)...", null, (_, _) =>
        {
            Announce(SignInStart.OpenWindowsStartupSettings()
                ? $"Windows Settings, Startup apps, is open: turn axKit {AppName} on or off there"
                : "Windows Settings could not be opened. Settings, Apps, Startup is where axKit " + AppName + " is turned on or off", true);
        });
    }

    /// <summary>Options, Hotkey... (AS-4.2): the notice that takes the next key.</summary>
    protected ToolStripMenuItem HotkeyItem() => new("&Hotkey...", null, (_, _) => ShowHotkeyNotice());

    /// <summary>Options, General's Say the first match while typing (AS-3.2, CP-2.3).</summary>
    protected ToolStripMenuItem SayFirstMatchItem() => Toggle("Say the first &match while typing", _settings.SayFirstMatch, v => _settings.SayFirstMatch = v);

    /// <summary>The end of the app's own menu: a separator, Hide with the app's key (AS-3.4: hidden at once, whatever the field holds; the next showing starts empty), and Quit, the one close that ends the process (AS-1.4).</summary>
    protected void AddHideAndQuit(ToolStripMenuItem menu, Keys hideKey)
    {
        menu.DropDownItems.Add(new ToolStripSeparator());
        Add(menu, new Command("&Hide", hideKey, () =>
        {
            Log.Info("Hidden: Hide");
            HideWindow();
        }) { Mark = CommandMark.Change });
        menu.DropDownItems.Add(new ToolStripMenuItem($"&Quit {AppName}", null, (_, _) => Quit()));
    }

    /// <param name="hidden">Started with --hidden, as at sign-in: the window stays hidden until the hotkey.</param>
    protected HotkeyForm(string appName, HotkeySettings settings, string settingsPath, string? settingsError, bool hidden)
        : base(appName, settings, settingsPath, settingsError)
    {
        _hidden = hidden;
        _settings = settings;
        // The title is the app's name alone: the second start finds the hidden window by it (FindRunning).
        Text = appName;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;

        // AS-3.5: the focus gone to another program hides the window, as the Start menu does.
        Deactivate += (_, _) => BeginInvoke(() => HideIfAnotherProgramIsInFront());

        // AS-3.1, CP-2.1: the caret is always in the field; the list's keys are the field's (AS-3.3).
        SearchField.Dock = DockStyle.Top;
        SearchField.TabIndex = 0;
        SearchField.TextChanged += (_, _) => OnTyped();

        // The matches: for the eyes and NVDA's object navigation, never the focus (AS-D2).
        Matches.Dock = DockStyle.Fill;
        Matches.IntegralHeight = false;
        Matches.TabStop = false;
        Matches.GotFocus += (_, _) => SearchField.Focus();

        _speak.Tick += (_, _) =>
        {
            _speak.Stop();
            SayFirst();
        };
    }

    // ---- what the app fills in ----

    /// <summary>The hotkey of the app's settings.</summary>
    protected Hotkey CurrentHotkey => _settings.Key;

    /// <summary>A new hotkey into the app's settings, saved.</summary>
    private void KeepHotkey(Hotkey key)
    {
        _settings.HotkeyText = key.ToString();
        SaveSettings();
    }

    /// <summary>Before the window shows: its bounds on <paramref name="area"/>, the working area of the screen of the program that had the focus; the field emptied unless <paramref name="keepText"/>.</summary>
    protected abstract void PrepareShowing(Rectangle area, bool keepText);

    /// <summary>After the window came to the front: the focus into its control, a fresh read.</summary>
    protected abstract void AfterShowing();

    /// <summary>As the window hides: the app's timers stopped.</summary>
    protected virtual void OnHiding()
    {
    }

    /// <summary>Whether the hotkey is registered now, for the diagnostics.</summary>
    protected bool HotkeyRegistered => _registered is not null;

    /// <summary>The window of the program that was in front when this one last showed: where a paste goes (axClip).</summary>
    protected nint ForegroundBefore => _giveBackTo;

    /// <summary>How many matches the list shows now (none: the list holds one line that says so).</summary>
    protected abstract int MatchCount { get; }

    /// <summary>What a match says when the selection reaches it: "Notepad, 1 of 3".</summary>
    protected abstract string SpokenMatch(int index);

    /// <summary>What is said when nothing matches: the line the list shows in place of the matches.</summary>
    protected abstract string NothingSaid { get; }


    /// <summary>The matches made again from the field's text, through <see cref="ShowMatches"/>.</summary>
    protected abstract void Refill();

    // ---- the search field over the matches (AS-3, CP-2; from axStart on its second use, axClip, 2026-10-24) ----

    /// <summary>The pause in the typing after which the first match is said (AS-3.2).</summary>
    private const int SpeakDelayMs = 300;

    private readonly System.Windows.Forms.Timer _speak = new() { Interval = SpeakDelayMs };

    /// <summary>The one field, which always has the caret (AS-3.1, CP-2.1); the app names it.</summary>
    protected AxKitTextBox SearchField { get; } = new();

    /// <summary>The matches under the field, never focused (AS-D2); the app names it in <see cref="ShowMatches"/>.</summary>
    protected ListBox Matches { get; } = new();

    /// <summary>The list made from <paramref name="labels"/>, or the one line <paramref name="nothing"/> when there are none, named <paramref name="name"/>, the selection on <paramref name="index"/>.</summary>
    protected void ShowMatches(IReadOnlyList<string> labels, string nothing, string name, int index)
    {
        Matches.BeginUpdate();
        Matches.Items.Clear();
        Matches.Items.AddRange(labels.Select(label => (object)label).ToArray());
        if (labels.Count == 0)
        {
            Matches.Items.Add(nothing);
        }

        Matches.EndUpdate();
        Matches.AccessibleName = name;
        Matches.SelectedIndex = index;
    }

    /// <summary>
    /// The keys that move through the matches with the caret kept in the field (AS-3.3, CP-2.4), as commands of the
    /// window's list (app-kit AK-2): the app's next and previous, Page Down, Page Up, and the app's first and last. The
    /// app calls this first in its constructor, so that these keys come before its own, as the switch had them.
    /// </summary>
    protected void AddMatchKeys(Keys next, Keys previous, Keys first, Keys last)
    {
        AddKey(new Command("Next match", next, () => SelectMatch(Matches.SelectedIndex + 1)) { Mark = CommandMark.Read });
        AddKey(new Command("Previous match", previous, () => SelectMatch(Matches.SelectedIndex - 1)) { Mark = CommandMark.Read });
        AddKey(new Command("Page down", BundleKeys.PageDown, () => SelectMatch(Math.Min(Matches.SelectedIndex + MatchPage, MatchCount - 1))) { Mark = CommandMark.Read });
        AddKey(new Command("Page up", BundleKeys.PageUp, () => SelectMatch(Math.Max(Matches.SelectedIndex - MatchPage, 0))) { Mark = CommandMark.Read });
        AddKey(new Command("First match", first, () => SelectMatch(0)) { Mark = CommandMark.Read });
        AddKey(new Command("Last match", last, () => SelectMatch(MatchCount - 1)) { Mark = CommandMark.Read });
    }

    /// <summary>How many matches the list shows at once: the step of Page Down and Page Up.</summary>
    private int MatchPage => Math.Max(1, Matches.ClientSize.Height / Math.Max(1, Matches.ItemHeight));

    private void OnTyped()
    {
        Refill();
        _speak.Stop();
        if (_settings.SayFirstMatch && SearchField.Text.Trim().Length > 0)
        {
            _speak.Start();
        }
    }

    /// <summary>The first match and the count, or that nothing matches, after the typing paused (AS-3.2).</summary>
    private void SayFirst()
    {
        if (SearchField.Text.Trim().Length == 0)
        {
            return;
        }

        Announce(MatchCount > 0 && Matches.SelectedIndex >= 0 ? SpokenMatch(Matches.SelectedIndex) : NothingSaid, true);
    }

    /// <summary>The selection moved to <paramref name="target"/> and said; at an end the edge earcon first, and the match it stays on said all the same, so that the last thing heard is the position and not the typed text (AS-3.3).</summary>
    private void SelectMatch(int target)
    {
        _speak.Stop();
        if (MatchCount == 0)
        {
            Sounds.Edge();
            Announce(NothingSaid, true);
            return;
        }

        target = Math.Clamp(target, 0, MatchCount - 1);
        if (target == Matches.SelectedIndex)
        {
            Sounds.Edge();
        }
        else
        {
            Matches.SelectedIndex = target;
        }

        Announce(SpokenMatch(target), true);
    }

    // ---- the window ----

    /// <summary>
    /// For a starting process (AS-1.2): the hidden window of another process of this very program file
    /// (<see cref="ThisProgram.Runs"/>) with the title <paramref name="title"/>, which a second start hands "show" to;
    /// 0 when none. The program file, not the process name (2026-11-09): the user's own axHelp from one build and a
    /// test run's from another are two programs, and the test's hand-over must never show the user's window.
    /// </summary>
    public static nint FindRunning(string title)
    {
        var process = (uint)Environment.ProcessId;
        for (var window = FindWindowExW(0, 0, null, title); window != 0; window = FindWindowExW(0, window, null, title))
        {
            GetWindowThreadProcessId(window, out var owner);
            if (owner != process && ThisProgram.Runs(owner))
            {
                return window;
            }
        }

        return 0;
    }

    /// <summary>
    /// The first showing, which Application.Run asks for before the handle exists (AS-1.1): the handle is made, so that
    /// the hotkey and the hand-over reach the window, and nothing shows; <see cref="OnHandleCreated"/> then shows the
    /// window itself unless it started with --hidden. Every later showing (<see cref="ShowWindowInFront"/>) finds the handle made and goes through.
    /// </summary>
    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated)
        {
            CreateHandle();
            value = false;
        }

        base.SetVisibleCore(value);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // AS-1.3: the Run key as the setting says, on every start (the installed program alone writes it; the Store's copy never).
        SignInStart.Apply(AppName, AppVerb, _settings.StartWithWindows);
        if (!Register(CurrentHotkey))
        {
            // AS-4.3: never a window that cannot be reached. Posted: not inside the handle's creation, where Show would
            // re-enter SetVisibleCore, whose first call only makes the handle.
            BeginInvoke(() =>
            {
                ShowWindowInFront();
                ShowNotice(Notice.Plain("Hotkey", $"The hotkey {CurrentHotkey} is taken by another program. Options, Hotkey, to choose another.") with { Unprompted = true });
            });
        }
        else if (!_hidden)
        {
            // Posted for the same reason: Show inside the handle's creation would re-enter SetVisibleCore.
            BeginInvoke(() => ShowWindowInFront());
        }

        // The app's second hotkey (axHelp's screenshot, docs/axHelp/SPEC.md HE-4): taken or not, logged; the app says it.
        if (SecondHotkey is { } second)
        {
            _secondRegistered = RegisterHotKey(Handle, SecondHotkeyId, (uint)(second.Modifiers | MOD_NOREPEAT), (uint)second.Key);
            Log.Info(_secondRegistered ? $"Second hotkey {second} registered" : $"The second hotkey {second} could not be registered");
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_registered is not null)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = null;
        }

        if (_secondRegistered)
        {
            UnregisterHotKey(Handle, SecondHotkeyId);
            _secondRegistered = false;
        }

        base.OnHandleDestroyed(e);
    }

    // ---- what an app may do with its hotkeys (axHelp, 2026-11-10) ----

    private const int SecondHotkeyId = 2;
    private bool _secondRegistered;

    /// <summary>
    /// The hotkey pressed while the window is not in front: true when the app did the hotkey's work itself and the
    /// window stays as it is (axHelp brings its conversation up instead, HE-2.1); false, the default, shows the window.
    /// </summary>
    protected virtual bool OnHotkey() => false;

    /// <summary>A second hotkey the app registers beside its own (axHelp's screenshot, HE-4.1), null for none; read once, when the window's handle is made.</summary>
    protected virtual Hotkey? SecondHotkey => null;

    /// <summary>The second hotkey pressed, from any program.</summary>
    protected virtual void OnSecondHotkey()
    {
    }

    /// <summary>Whether the second hotkey is registered, for the diagnostics and the window's text.</summary>
    protected bool SecondHotkeyRegistered => _secondRegistered;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // AS-1.4: Alt+F4 and Close all windows hide the window; Quit, the update's own close (AX-3.2) and Windows' own
        // end end the process.
        if (!_quitting && !ClosingForUpdate && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideWindow();
        }

        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_ACTIVATE && (m.WParam & 0xFFFF) != 0 && !IsWindowVisible(Handle))
        {
            // AS-3.4: the hidden window must never hold the keyboard. Windows activated it when axStart started hidden
            // and when it hid, and the user's typing went into a window nobody could see (2026-10-22).
            Log.Info("Activated while hidden; the front given back");
            BeginInvoke(GiveForegroundBack);
        }

        if (m.Msg == WM_HOTKEY && m.WParam == HotkeyId)
        {
            if (_capturing)
            {
                // The hotkey pressed in Hotkey…: the key in use chosen again.
                CaptureKey(CurrentHotkey);
            }
            else if (Visible && ContainsFocus)
            {
                Log.Info("Hidden: the hotkey");
                HideWindow();
            }
            else if (!OnHotkey())
            {
                ShowWindowInFront();
            }

            return;
        }

        if (m.Msg == WM_HOTKEY && m.WParam == SecondHotkeyId)
        {
            OnSecondHotkey();
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>A second start's "show" (AS-1.2), through the base: the window shown with the focus in its field once the sender has its answer.</summary>
    protected override Action? TakeHandOver(HandOver message) => message.Verb == AppMessage.Show ? () => ShowWindowInFront() : null;

    // ---- showing and hiding (AS-3.4, AS-4.1) ----

    /// <summary>The window in front, centred by the app on the screen of the program that had the focus, with the field empty (unless <paramref name="keepText"/>: a start that failed) and the caret in it.</summary>
    protected void ShowWindowInFront(bool keepText = false)
    {
        if (Visible && ContainsFocus)
        {
            return;
        }

        // AS-3.4: the window the foreground goes back to when this one hides, when it is another program's (this
        // process's own windows, after a start that failed or a hide Windows answered with one of them, are not it).
        var before = GetForegroundWindow();
        GetWindowThreadProcessId(before, out var owner);
        if (owner != (uint)Environment.ProcessId)
        {
            _giveBackTo = before;
        }

        PrepareShowing(Screen.FromHandle(before).WorkingArea, keepText);
        Log.Info("Shown");
        Show();
        WindowMenu.Activate(Handle);
        AfterShowing();
    }

    /// <summary>
    /// The window hidden, and the foreground given back by the rule of <see cref="GiveForegroundBack"/>, unless
    /// <paramref name="giveBack"/> is off: after a start, the program started takes the front itself, and a process
    /// no longer in front could not start it in front.
    /// </summary>
    protected void HideWindow(bool giveBack = true)
    {
        _speak.Stop();
        OnHiding();
        if (NoticeOpen)
        {
            CloseNotice(null);
        }

        _capturing = false;
        Hide();
        if (giveBack)
        {
            GiveForegroundBack();
        }
    }

    /// <summary>
    /// The one rule of the foreground (AS-3.4): while the window is hidden, this process never keeps it. Whenever the
    /// process holds it with the window hidden (Windows activates a hidden window of the process that hid one or that
    /// started hidden; a start brought nothing up), the window of <see cref="_giveBackTo"/> gets it, allowed because
    /// this process is in front at that moment. Called as the window hides, on an activation while hidden, and once
    /// three seconds after a start; nothing happens when another program has the front.
    /// </summary>
    protected void GiveForegroundBack()
    {
        if (IsWindowVisible(Handle))
        {
            return;
        }

        GetWindowThreadProcessId(GetForegroundWindow(), out var owner);
        if (owner == (uint)Environment.ProcessId && _giveBackTo != 0 && IsWindow(_giveBackTo))
        {
            SetForegroundWindow(_giveBackTo);
        }
    }

    /// <summary>After a start (AS-3.4): a program without a window, or a declined administrator prompt, leaves the front to this hidden process; the rule gives it back.</summary>
    protected async Task GiveForegroundBackLaterAsync()
    {
        await Task.Delay(StartSettleMs);
        GiveForegroundBack();
    }

    /// <summary>
    /// The focus went elsewhere (AS-3.5): the window hides when another program took the front, as the Start menu
    /// does; it stays for a picker of its own (the font) and for NVDA's own windows (its menu, its settings), since
    /// NVDA is how the window is seen, not another program (decided 2026-10-23).
    /// </summary>
    private void HideIfAnotherProgramIsInFront()
    {
        if (!Visible || NoticeOpen)
        {
            return;
        }

        GetWindowThreadProcessId(GetForegroundWindow(), out var owner);
        if (owner == (uint)Environment.ProcessId)
        {
            return;
        }

        // Which process took the front, by its program name only, for the case of a hide nobody asked for.
        var name = ProcessName(owner);
        if (name.StartsWith("nvda", StringComparison.OrdinalIgnoreCase))
        {
            Log.Info("NVDA came to the front; the window stays");
            return;
        }

        Log.Info($"Hidden: {name} came to the front");
        HideWindow();
    }

    private static string ProcessName(uint processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return "a program that has ended";
        }
    }

    /// <summary>The app's Quit item: the one close that ends the process (AS-1.4).</summary>
    protected void Quit()
    {
        _quitting = true;
        Close();
    }

    // ---- the hotkey (AS-4) ----

    /// <summary>The hotkey registered in place of the one before; false, with nothing registered, when another program holds it.</summary>
    private bool Register(Hotkey key)
    {
        if (_registered is not null)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = null;
        }

        if (!RegisterHotKey(Handle, HotkeyId, (uint)(key.Modifiers | MOD_NOREPEAT), (uint)key.Key))
        {
            Log.Info($"The hotkey {key} could not be registered");
            return false;
        }

        _registered = key;
        Log.Info($"Hotkey {key} registered");
        return true;
    }

    /// <summary>Options → Hotkey… (AS-4.2): a field that takes the next key pressed with its modifiers; Enter keeps it.</summary>
    protected void ShowHotkeyNotice()
    {
        _captured = null;
        _capturing = true;
        ShowNotice(new Notice("Hotkey",
            $"Press the key that brings {AppName} up, with Ctrl, Alt or the Windows key, then Enter. Now: {CurrentHotkey}.",
            [
                new OverlayChoice("&Save", () => ApplyHotkey(_captured), IsDefault: true),
                new OverlayChoice("&Cancel", () => _capturing = false, IsCancel: true),
            ])
        {
            Input = new OverlayInput("Press the new key:", "Press the new key", CurrentHotkey.ToString(), "Press a key first", KeyHook: OnHotkeyField),
        });
    }

    /// <summary>A key in the Hotkey field: taken as the new hotkey, or refused with the reason; Enter, Escape and Tab stay the notice's.</summary>
    private bool OnHotkeyField(Keys keyData)
    {
        var code = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if ((modifiers == Keys.None && code is Keys.Return or Keys.Escape or Keys.Tab) || keyData == (Keys.Shift | Keys.Tab))
        {
            return false;
        }

        if (code is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            return true;
        }

        var flags = ((modifiers & Keys.Control) != 0 ? Hotkey.Control : 0)
            | ((modifiers & Keys.Alt) != 0 ? Hotkey.Alt : 0)
            | ((modifiers & Keys.Shift) != 0 ? Hotkey.Shift : 0)
            | (WindowsKeyDown() ? Hotkey.Windows : 0);
        CaptureKey(new Hotkey(flags, (int)code));
        return true;
    }

    private void CaptureKey(Hotkey key)
    {
        if (key.Refusal() is { } refusal)
        {
            Announce(refusal, true);
            return;
        }

        _captured = key;
        Overlay.SetInputText(key.ToString());
        Announce(key.ToString(), true);
    }

    private void ApplyHotkey(Hotkey? key)
    {
        _capturing = false;
        var old = CurrentHotkey;
        if (key is null || key == old)
        {
            Announce($"Hotkey {old}", true);
            return;
        }

        if (!Register(key))
        {
            Register(old);
            Announce($"{key} is taken by another program", true);
            return;
        }

        KeepHotkey(key);
        Announce($"Hotkey {key}", true);
    }

    private static bool WindowsKeyDown() => GetKeyState(0x5B) < 0 || GetKeyState(0x5C) < 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);


    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);


    [DllImport("user32.dll")]
    private static extern short GetKeyState(int key);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowExW(nint parent, nint after, string? className, string title);
}

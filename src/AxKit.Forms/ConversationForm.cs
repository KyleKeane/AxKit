using AxKit.Core;
using AxKit.Core.Transcript;

namespace AxKit.Forms;

/// <summary>
/// The conversation window (docs/axKit/framework.md, section 4.1): a text that grows, shown as a transcript with
/// single-key navigation (<see cref="TranscriptView"/>), a multi-line field at the bottom (or the control area in
/// its place), and a status bar with two labels; with the focus keys between the two places, find, the bookmarks,
/// the Context menu on a line, the notice hooks and the font. It holds the shape alone and knows no program: what
/// the text is and what the field's Enter does are the app's. Split out of <see cref="ConsoleForm"/> on 2026-11-22
/// (framework step 9), unchanged, so that a window without a program in a pseudo console can have the shape:
/// <see cref="ConsoleForm"/> derives from it and adds the program (axClaude, axTerminal), and axCalc derives from
/// it directly, its text the answers given. The app's model is the bundle's <see cref="LineStore"/>, which a
/// window without a console fills with lines of its own.
/// </summary>
public abstract class ConversationForm : AxKitForm
{
    private readonly TranscriptKeys _keys;
    private readonly string _transcriptName;

    /// <summary>
    /// The field's name over it, for the eyes (2026-11-08: a sighted helper took the white field under the white view
    /// for more of the conversation), as the control area shows the question over its control. NVDA names the field by
    /// its AccessibleName and never reads this line for it; the line comes and goes with the field.
    /// </summary>
    private readonly Label _inputName = new();

    /// <param name="model">The app's model on the bundle's <see cref="LineStore"/>, which the window shows; the app keeps its own typed reference.</param>
    /// <param name="keys">The window's keys for the view, from its table.</param>
    /// <param name="transcriptName">What NVDA calls the view: "Conversation", "Terminal", "Answers".</param>
    /// <param name="inputName">What NVDA calls the field: "Message to Claude", "Command", "Expression".</param>
    /// <param name="showMarkers">Whether the view shows the # markers of its Input and Output lines, from the app's settings.</param>
    protected ConversationForm(string appName, WindowSettings settings, string settingsPath, string? settingsError, LineStore model, TranscriptKeys keys, string transcriptName, string inputName, bool showMarkers)
        : base(appName, settings, settingsPath, settingsError)
    {
        Model = model;
        _keys = keys;
        _transcriptName = transcriptName;
        Text = appName;
        Size = new Size(1000, 700);
        MinimumSize = new Size(600, 400);

        Model.Changed += OnModelChanged;

        Transcript = new TranscriptView(keys, transcriptName) { ShowMarkers = showMarkers, Dock = DockStyle.Fill, TabIndex = 1 };
        Transcript.EscapePressed += () => BottomControl.Focus();
        Transcript.BookmarkToggled += ToggleBookmark;

        Input.AccessibleName = inputName;
        Input.PlaceholderText = inputName;
        Input.Multiline = true;
        Input.AcceptsReturn = true;
        Input.AcceptsTab = false;
        Input.WordWrap = true;
        Input.ScrollBars = ScrollBars.Vertical;
        Input.Dock = DockStyle.Bottom;
        Input.TabIndex = 0;
        // A frame, a colour of its own and the name line over it, so that the active field is told from the read-only
        // view on screen (both are the white Window colour otherwise); none of this is anything to NVDA.
        Input.BorderStyle = BorderStyle.FixedSingle;
        // AX-8.13: the field's mode is in the status bar, after the program's state.
        Input.ModeChanged += (_, _) => UpdateStatus();
        _inputName.Text = inputName;
        _inputName.AutoEllipsis = true;
        _inputName.UseMnemonic = false;
        _inputName.TextAlign = ContentAlignment.MiddleLeft;
        _inputName.Padding = new Padding(4, 0, 0, 0);
        _inputName.Dock = DockStyle.Bottom;
        Input.VisibleChanged += (_, _) => _inputName.Visible = Input.Visible;
        // Their colours are the theme's classes field and band (docs/axKit/theme.md TH-2), painted by the window base
        // with every other control; the defaults are the tints of 2026-11-08.
        ThemePainter.Mark(Input, ThemePainter.Field);
        ThemePainter.Mark(_inputName, ThemePainter.Band);
        Area.ControlAdded += (_, e) =>
        {
            if (e.Control is { } added)
            {
                PaintTheme(added);
            }
        };

        // What the program waits on, or a list of the window's own, takes the field's place (axClaude D33), one control at a time.
        Area.TabIndex = 0;
        SizeChanged += (_, _) => FitBottom();

        // The bundle's status bar pair (no AccessibleName, no Spring; StatusLayout keeps both inside the strip).
        (StateLabel, PlaceLabel) = StatusLayout.Pair(StatusBar);

        // The window has two places, the view and the control at the bottom (the field, or the control area in its
        // place): every focus key moves between them, so a hidden control never takes the focus (app-kit AK-2).
        foreach (var key in new[] { Keys.Tab, Keys.Shift | Keys.Tab, BundleKeys.NextControl, BundleKeys.PreviousControl, BundleKeys.NextControlAlso })
        {
            AddKey(new Command("Switch between the view and the field", key, SwitchPlace) { Mark = CommandMark.Open });
        }

        // The overlay is hidden until a notice shows; it then fills the space of the hidden view and field. The
        // window adds its menu items to MenuBar after this constructor.
        Controls.Add(Overlay);
        Controls.Add(Transcript);
        // Docked from the last added: the name line, added before the field, lands above it.
        Controls.Add(_inputName);
        Controls.Add(Input);
        Controls.Add(Area);
        Controls.Add(StatusBar);
        Controls.Add(MenuBar);
        Transcript.BringToFront();
        MainMenuStrip = MenuBar;
    }

    /// <summary>The app's model, as the base sees it: the lines, the system lines, the bookmarks.</summary>
    protected LineStore Model { get; }

    /// <summary>The read-only transcript with its single-key navigation.</summary>
    protected TranscriptView Transcript { get; }

    /// <summary>The multi-line field at the bottom: the message to Claude, the command to the shell, the expression.</summary>
    protected AxKitTextBox Input { get; } = new();

    /// <summary>Actions, Insert template puts a template into the field at the bottom, never sent (docs/axTemplate/SPEC.md TP-5.2, TP-5.4).</summary>
    protected override AxKitTextBox? TemplateField => Input;

    /// <summary>What takes the field's place: Claude's question, the recent commands.</summary>
    protected ControlArea Area { get; } = new();

    /// <summary>The menu bar; the window adds its menus.</summary>
    protected MenuStrip MenuBar { get; } = new();

    protected StatusStrip StatusBar { get; } = new();

    /// <summary>The left status label: the program's state.</summary>
    protected ToolStripStatusLabel StateLabel { get; }

    /// <summary>The right status label: the folder, or the shell's prompt.</summary>
    protected ToolStripStatusLabel PlaceLabel { get; }

    /// <summary>The control at the bottom of the window: the field, or the control area's list or field in its place.</summary>
    protected Control BottomControl => Area.Current ?? Input;

    // ---- what the window fills in ----

    /// <summary>The window's own pass after the transcript changed (a frame ended, a line went in), after the view and the status bar: the tick, the speech, the attention.</summary>
    protected abstract void OnTranscriptChanged();

    /// <summary>The status bar's two labels for the moment, through <see cref="SetStatus"/>.</summary>
    protected abstract void UpdateStatus();

    /// <summary>The window's own keys that are no command (axClaude's Ctrl+O guard), after read to the end and before the command list.</summary>
    protected virtual bool ProcessConsoleKey(Keys keyData) => false;

    /// <summary>After the view took the changed lines and the status bar followed, before the window's own pass: the console window's music.</summary>
    protected virtual void OnModelSynced()
    {
    }

    // ---- the window base's hooks, filled in for every conversation window ----

    protected override void HideForNotice()
    {
        Transcript.Visible = false;
        Input.Visible = false;
        Area.Visible = false;
    }

    /// <summary>The view, and the field or the control area, whichever was in place when the notice closes.</summary>
    protected override void ShowAfterNotice()
    {
        Transcript.Visible = true;
        Input.Visible = Area.Current is null;
        Area.Visible = Area.Current is not null;
    }

    /// <summary>An AI's request is a list in the control area, in the field's place (MC-7.3); the field comes back with the focus when it is answered.</summary>
    protected override bool ShowOfferedRequest(OfferedRequest request) =>
        ShowOfferedRequestIn(Area, request, () => Input.Visible = false, () =>
        {
            Input.Visible = !NoticeOpen;
            Input.Focus();
        });

    /// <summary>The bottom of the window, whichever control is there when the notice closes and the one that had the focus is gone (an answered question's list).</summary>
    protected override Control FocusTarget => BottomControl;

    /// <summary>
    /// MC-6.5: the view is the console window's main text, the conversation or the terminal, wherever the focus is: the
    /// focus is nearly always in the field at the bottom, and until 2026-11-15 the AI tool read that field (an empty
    /// message) and never the conversation. The field the focus is in is named beside it (<c>McpChannel</c>).
    /// </summary>
    internal override TextBoxBase? McpMainText(TextBoxBase? focused) => Transcript;

    /// <summary>Everything is announced on the view, the control NVDA reads the window by.</summary>
    protected override Control AnnounceSource => Transcript;

    /// <summary>A reader keeps their line while the notice hides the view (axClaude FR-3.3): the notice opened from the view.</summary>
    protected override void OnNoticeOpened(Control? focusBefore) => Transcript.KeepCaret = ReferenceEquals(focusBefore, Transcript);

    /// <summary>Cleared after the notice's action, so that a system line the action adds does not move a reader's caret.</summary>
    protected override void OnNoticeClosed() => Transcript.KeepCaret = false;

    protected override void ApplyFont(Font font)
    {
        Transcript.Font = font;
        Input.Font = font;
        _inputName.Font = new Font(font, FontStyle.Bold);
        _inputName.Height = TextRenderer.MeasureText("Wg", font).Height + 6;
        Area.Font = font;
        FitBottom();
    }

    /// <summary>
    /// The bottom of the window after the font or the size changed: the field is three lines of its font and never more
    /// than a third of the window, so a larger text makes the lines taller, the view keeps the rest, and a message past
    /// the field's rows wraps and scrolls (the bundle's text size rule); the control area never goes below the field's
    /// height, and never past half the window by its own rule.
    /// </summary>
    private void FitBottom()
    {
        var line = TextRenderer.MeasureText("Wg", Input.Font).Height;
        Input.Height = Math.Max(line + 8, Math.Min(line * 3 + 8, ClientSize.Height / 3));
        Area.MinimumHeight = Input.Height;
        Area.FitHeight();
    }

    /// <summary>The update's steps as system lines in the transcript (axClaude FR-1.10).</summary>
    protected override void UpdateNoted(string message) => Model.AddSystemLine(message);

    /// <summary>A settings file that could not be read is a system line, not a notice over the start of the program.</summary>
    protected override void SettingsNotRead(string error) =>
        Model.AddSystemLine(SettingsSeal.IsRefusal(error) ? error : SettingsFile.Heading(error) + " " + error);

    /// <summary>A sentence about the start is a system line (AX-9.5a).</summary>
    protected override void StartNote(string sentence) => Model.AddSystemLine(sentence);

    /// <summary>A settings file that could not be written is a system line too (AX-5.9).</summary>
    protected override void SettingsNotSaved(string sentence) => Model.AddSystemLine(sentence);

    /// <summary>An error the user must see: logged, a system line in the transcript, the status, and the notice (axClaude FR-13.1).</summary>
    protected sealed override void Fail(string message)
    {
        Log.Error(message);
        Model.AddSystemLine(message);
        UpdateStatus();
        ShowError("Error", message);
    }

    /// <summary>The focus starts in the field (axClaude FR-6.1, axTerminal AT-2.2); a notice shown by what follows takes it from here and gives it back.</summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Input.Focus();
    }

    // ---- keys and focus (axClaude FR-6, axTerminal AT-2.2) ----

    /// <summary>
    /// The keys that are no command: read to the end, a pattern of a modifier pair with any quick key letter, then the
    /// window's own (<see cref="ProcessConsoleKey"/>). The focus keys, find, the bookmark, the music and the window's
    /// menu keys are its command list (app-kit AK-2), which the base runs after this.
    /// </summary>
    protected sealed override bool ProcessWindowKey(Keys keyData)
    {
        // Ctrl+Alt with a quick key letter reads that kind of line to the end, from anywhere (axClaude FR-5, D36).
        if ((keyData & Keys.Modifiers) == _keys.ReadToEnd && Transcript.ReadToEnd(keyData & Keys.KeyCode))
        {
            return true;
        }

        return ProcessConsoleKey(keyData);
    }

    /// <summary>
    /// The key test's words (AX-8.12) for the keys that are no command of the list: Ctrl+Alt with a quick key letter
    /// from anywhere, and in the view its quick keys, the bookmark letter, the line number and the way back.
    /// </summary>
    protected override string? KeyTestOwn(Keys key)
    {
        var code = key & Keys.KeyCode;
        var modifiers = key & Keys.Modifiers;
        var quick = _keys.QuickKeys.FirstOrDefault(k => k.Key == code);
        if (modifiers == _keys.ReadToEnd && quick is not null)
        {
            return $"reads every {quick.Name} from the caret's line to the end";
        }

        if (Input.Focused && key == Keys.Enter)
        {
            // The field's Enter is each window's own handler (a message to Claude, a command line to the shell).
            return "sends what the field holds";
        }

        if (!Transcript.Focused || modifiers is not (Keys.None or Keys.Shift))
        {
            return null;
        }

        if (quick is not null)
        {
            return $"moves to the {(modifiers == Keys.Shift ? "previous" : "next")} {quick.Name}";
        }

        if (modifiers == Keys.Shift)
        {
            return null;
        }

        return code == _keys.BookmarkToggle ? "bookmarks this line, or takes its bookmark away"
            : code == _keys.LineNumber ? "says the line number"
            : code == _keys.JumpBack ? "goes back to the line before the jump"
            : null;
    }

    /// <summary>Tab, Shift+Tab and the bundle's focus keys: the view, or the control at the bottom when the view has the focus.</summary>
    private void SwitchPlace()
    {
        if (Transcript.Focused)
        {
            BottomControl.Focus();
        }
        else
        {
            Transcript.Focus();
        }
    }

    /// <summary>
    /// Alt+1, Alt+2 and the Navigate menu (axClaude FR-6.5, D26): the control takes the focus, and NVDA announces the
    /// change. When it has the focus already, its name is spoken instead, so the key also answers "where am I".
    /// </summary>
    protected void GoTo(Control control)
    {
        if (control.Focused)
        {
            Announce(control.AccessibleName ?? string.Empty, true);
            return;
        }

        control.Focus();
    }

    /// <summary>The field comes back in the control area's place, with the focus when the area had it.</summary>
    protected virtual void CloseArea()
    {
        if (Area.Current is null)
        {
            return;
        }

        var focus = Area.ContainsFocus;
        Input.Visible = !NoticeOpen;
        if (focus)
        {
            Input.Focus();
        }

        Area.Clear();
    }

    /// <summary>m, Ctrl+Shift+K or Navigate → Bookmark this line (axClaude FR-3.9): the model adds or removes the bookmark line and the result is spoken.</summary>
    private void ToggleBookmark(Line line)
    {
        if (Model.ToggleBookmark(line) is not ({ } bookmark, var added))
        {
            Announce("Line gone", true);
            return;
        }

        Announce($"{bookmark.Text} {(added ? "added" : "removed")}", true);
    }

    // ---- the transcript and the status bar ----

    /// <summary>The model changed: the view takes the lines, the status bar follows (and the console window's music), then the window's own pass.</summary>
    private void OnModelChanged()
    {
        Transcript.Sync(Model.Lines);
        UpdateStatus();
        OnModelSynced();
        OnTranscriptChanged();
    }

    /// <summary>The two status labels, fitted into the strip when either changed (axClaude §6.2).</summary>
    protected void SetStatus(string state, string place)
    {
        if (Input.NavigationMode)
        {
            // AX-8.13: the field in navigation mode, said with the state wherever the status is read (F8 too).
            state += ", field in " + TextModes.StatusPart;
        }

        if (StateLabel.Text == state && PlaceLabel.Text == place)
        {
            return;
        }

        StateLabel.Text = state;
        PlaceLabel.Text = place;
        StatusLayout.Fit(StatusBar, StateLabel, PlaceLabel);
    }

    // ---- find (axClaude FR-3.7, axTerminal AT-4.5) ----

    /// <summary>Ctrl+F: the bundle's Find notice over the view's search. Find next closes it and searches from the caret; an empty field keeps it open.</summary>
    protected void FindDialog() => ShowFind(text => Transcript.Find(text, backward: false));

    /// <summary>F3 and Shift+F3: the last text again from the caret; the Find notice when there is none yet.</summary>
    protected void FindNext(bool backward)
    {
        if (FindText.Length == 0)
        {
            FindDialog();
            return;
        }

        Transcript.Find(FindText, backward);
    }

    // ---- the menu items every conversation window has, for the window to place ----

    /// <summary>Find…, Find next and Find previous as commands of the Navigate menu (axClaude FR-3.7).</summary>
    protected void AddFindItems(ToolStripMenuItem menu)
    {
        Add(menu, new Command("&Find...", BundleKeys.Find, FindDialog) { Mark = CommandMark.Read });
        Add(menu, new Command("Find &next", BundleKeys.FindNext, () => FindNext(backward: false)) { Mark = CommandMark.Read });
        Add(menu, new Command("Find &previous", BundleKeys.FindPrevious, () => FindNext(backward: true)) { Mark = CommandMark.Read });
    }

    /// <summary>Next bookmark and Previous bookmark for the Navigation menu (axClaude FR-3.9), with the view's letters named; Bookmark this line is Context's (<see cref="CreateTranscriptContext"/>).</summary>
    protected void AddBookmarkItems(ToolStripMenuItem menu)
    {
        var view = _transcriptName.ToLowerInvariant();
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Next bookmar&k", null, (_, _) => Transcript.JumpToBookmark(backward: false)) { ShortcutKeyDisplayString = $"{BundleKeys.Text(_keys.Bookmark.Key)} in the {view}" }, CommandMark.Open));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Pre&vious bookmark", null, (_, _) => Transcript.JumpToBookmark(backward: true)) { ShortcutKeyDisplayString = $"Shift+{BundleKeys.Text(_keys.Bookmark.Key)} in the {view}" }, CommandMark.Open));
    }

    /// <summary>
    /// The bar's Context menu of a conversation window (AX-9.11, 2026-11-11): what acts on the line the transcript's
    /// caret is on and the selection: the line handed to the window (<paramref name="chooseText"/>: axClaude's quote
    /// into the message, axTerminal's copy into the field), Bookmark this line (a command with its key), Copy the
    /// selection. Shift+F10, the Applications key and a right click in the transcript and the field open it
    /// (<see cref="AxKitForm.ContextFromControl"/>).
    /// </summary>
    protected ToolStripMenuItem CreateTranscriptContext(string chooseText)
    {
        var view = _transcriptName.ToLowerInvariant();
        var menu = new ToolStripMenuItem();
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(chooseText, null, (_, _) => Transcript.ChooseLine()) { ShortcutKeyDisplayString = $"Enter in the {view}" }, CommandMark.Change));
        var bookmark = Add(menu, new Command("&Bookmark this line", _keys.BookmarkLine, Transcript.ToggleBookmark) { Mark = CommandMark.Change });
        bookmark.ShortcutKeyDisplayString = $"{BundleKeys.Text(_keys.BookmarkLine)}, or {BundleKeys.Text(_keys.BookmarkToggle)} in the {view}";
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Copy the selection", null, (_, _) => Transcript.Copy()) { ShortcutKeyDisplayString = $"Ctrl+C in the {view}" }, CommandMark.Act));
        ContextFromControl(Transcript);
        ContextFromControl(Input);
        return menu;
    }
}

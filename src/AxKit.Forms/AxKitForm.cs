using System.Diagnostics;
using AxKit.Core;
using AxKit.Core.Audio;
using AxKit.Core.Events;
using AxKit.Core.Mcp;
using AxKit.Core.Updates;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// The window every app of the bundle is built on (docs/axKit/SPEC.md AX-6.5): what the windows share and no app
/// copies. One class in partial files, one per section: this file (the hooks a window fills in, the menus every
/// window has, the keys, the window's place, the settings, the ways out to the system), <c>AxKitForm.Notices.cs</c>
/// (the notice flow with its one focus rule, and <see cref="Announce"/>), <c>AxKitForm.Find.cs</c> (find in a text
/// box, a list, a transcript and a notice's text), <c>AxKitForm.Fonts.cs</c> (the text font with its size keys and
/// picker), <c>AxKitForm.Palette.cs</c> (the command palette, AX-10) and <c>AxKitForm.Commands.cs</c> (the window's
/// commands as data, for the automation channel). The services with a flow of their own are
/// plain classes the window holds, each made in the constructor with the window as its one collaborator
/// (docs/axKit/repairs.json R-15, 2026-11-14): <see cref="UpdateFlow"/> (AX-3), <see cref="ProgramInstaller"/>
/// (AX-2.9), <see cref="ReportFlow"/>, <see cref="AiHelpFlow"/> and <see cref="AiToolsSetup"/>; the partial file of
/// each (<c>AxKitForm.Updates.cs</c>, <c>.Install.cs</c>, <c>.Report.cs</c>, <c>.Ai.cs</c>, <c>.Mcp.cs</c>) keeps the
/// members a window calls, the menu items and the hooks. A window derives from it, adds its controls (the
/// <see cref="Overlay"/> first, so that it docks over the rest), and fills in the hooks listed under "what the window
/// fills in". It grew out of axDown's window (the editor shell) on the third copy, axGIT (2026-10-18); the notice and
/// focus core was written again on 2026-10-23 (the review of that day, in the git history).
/// </summary>
public abstract partial class AxKitForm : Form
{
    private const int SC_KEYMENU = 0xF100;
    private const int VK_MENU = 0x12;

    /// <summary>How long after an activation with Alt held a menu-bar Alt is taken as that Alt's release (see <see cref="WndProc"/>).</summary>
    private const int AltCarriedOverMs = 1500;

    /// <summary>Until when (<see cref="Environment.TickCount64"/>) a menu-bar Alt is the release of an Alt held as the window was activated; 0 when none was.</summary>
    private long _altCarriedOverUntil;

    /// <summary>Why the settings file could not be read, reported once the window shows; null when it was read.</summary>
    private readonly string? _settingsError;

    /// <param name="settingsError">What the settings loader reported for a file it could not read; the defaults are in use then (AX-5.2).</param>
    protected AxKitForm(string appName, WindowSettings settings, string settingsPath, string? settingsError = null)
    {
        AppName = appName;
        Settings = settings;
        SettingsPath = settingsPath;
        _settingsError = settingsError;
        // The services the window holds, each a plain class with this window as its one collaborator (R-15).
        _installer = new ProgramInstaller(this);
        _updates = new UpdateFlow(this);
        _reports = new ReportFlow(this);
        _ai = new AiHelpFlow(this);
        _aiTools = new AiToolsSetup(this);
        // AX-6.6a: the window's keys from an open drop-down of its menus.
        AddMenuKeyFilter();
        // With the role Window, NVDA speaks the window when it opens ("notes.md - axDown window") before the focused
        // control; without it the window is a nameless pane to NVDA and the app is never named (AX-7.1).
        AccessibleRole = AccessibleRole.Window;
        MinimumSize = new Size(500, 300);
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.WindowsDefaultLocation;
        Overlay.Chosen += OnNoticeChoice;
        // AX-7.9: every notice's text moves by headings (h, 1 to 6), so a key onto one plays the heading sound.
        FollowHeadings(Overlay.BodyText);
        WireAnnouncing();
        // The theme file now, before the window's own constructor, which may take a display setting from it (TH-6);
        // painted in OnLoad once the controls exist. A Windows app theme change repaints every class from the theme's
        // defaults (docs/axKit/theme.md TH-3).
        LoadTheme();
        SystemColorsChanged += (_, _) => PaintTheme(this);
        _paletteSpeak.Tick += (_, _) => SayFirstPaletteMatch();
        // Both reach GitHub: act for the AI tools (docs/axKit/app-kit.md AK-2.5).
        CommandPalette.Marked(_checkItem, CommandMark.Act).Click += (_, _) => CheckForUpdates(manual: true);
        CommandPalette.Marked(_updateItem, CommandMark.Act).Click += (_, _) => OfferKnownUpdate();
    }

    /// <summary>The app's name, which ends the title of its windows and names them in the Window menu.</summary>
    protected internal string AppName { get; }

    /// <summary>The app's name as the user meets it (AX-11.2): "axClaude, access layer for Claude Code" for an access layer, the name alone otherwise; window titles end with it.</summary>
    protected string AppTitle => Dispatch.ByName(AppName)?.Title ?? AppName;

    /// <summary>
    /// The title (AX-11.2): the app's title first, then what the window holds, so that NVDA says the app before the
    /// place as the focus lands on a new window ("axFile folder C:\work\music", the user's ask of 2026-11-23);
    /// the app's title alone for nothing.
    /// </summary>
    protected void SetTitle(string? held) => Text = held is null ? AppTitle : $"{AppTitle} {held}";

    /// <summary>What a window that holds a folder gives <see cref="SetTitle"/>: the word "folder" and the folder's full path.</summary>
    protected static string FolderTitle(string folder) => $"folder {folder}";

    /// <summary>The app's settings as the base needs them: the font, the update check and the window's place.</summary>
    protected WindowSettings Settings { get; }

    protected string SettingsPath { get; }

    /// <summary>The notices panel (AX-7.2); the window adds it to its controls first, so that it docks over the rest.</summary>
    protected internal OverlayPanel Overlay { get; } = new();

    /// <summary>A notice holds the window: the main controls are hidden, the menu and the shortcuts wait.</summary>
    protected internal bool NoticeOpen { get; private set; }

    // ---- what the window fills in ----

    /// <summary>The F1 text.</summary>
    protected abstract string Shortcuts { get; }

    /// <summary>Hides the main controls while a notice shows; <see cref="ShowAfterNotice"/> brings them back.</summary>
    protected abstract void HideForNotice();

    protected abstract void ShowAfterNotice();

    /// <summary>
    /// The control the focus goes to after a notice when the control that had it before is gone or hidden (an
    /// answered question's list, the message field behind a question), and the one announcements are raised on
    /// unless <see cref="AnnounceSource"/> says otherwise.
    /// </summary>
    protected abstract Control FocusTarget { get; }

    /// <summary>The control <see cref="Announce"/> raises on while no notice shows; the focus target unless the window says otherwise (a transcript, the list that has the focus).</summary>
    protected virtual Control AnnounceSource => FocusTarget;

    /// <summary>Whether an unprompted notice plays the chime; a window with a sound setting says.</summary>
    protected virtual bool NoticeChime => true;

    /// <summary>The first notice of a chain opened, with <paramref name="focusBefore"/> the control that had the focus (null when none had it); the next notice of the same chain does not call here.</summary>
    protected virtual void OnNoticeOpened(Control? focusBefore)
    {
    }

    /// <summary>The last notice of a chain closed, before the focus goes back.</summary>
    protected virtual void OnNoticeClosed()
    {
    }

    /// <summary>Gives the window's controls the text font; the notice gets it from the base.</summary>
    protected abstract void ApplyFont(Font font);

    /// <summary>
    /// How the installed new version starts this window again after Update and restart (AX-3.2): the app's verb and
    /// the path in hand when there is one, every window's case but axClaude's (<c>--continue</c> after a sent message).
    /// </summary>
    protected virtual IReadOnlyList<string> RestartArguments() => PathInHand is { Length: > 0 } path ? [AppVerb, path] : [AppVerb];

    /// <summary>What Update and restart does to this window, in the update notice: "closes axDown (asking first…), installs it and starts it again on the same file".</summary>
    protected virtual string UpdateRestartText => $"closes {AppName}, installs it and starts it again";

    /// <summary>A step of the update worth a line of the window's own (axClaude writes it into the conversation); nothing by default.</summary>
    protected virtual void UpdateNoted(string message)
    {
    }

    /// <summary>The window's own lines of Copy diagnostics, after the version line.</summary>
    protected virtual IEnumerable<string> DiagnosticsLines => [];

    /// <summary>The window's own keys, from its table; true when the key was handled. Called when no notice shows and after the common keys.</summary>
    protected virtual bool ProcessWindowKey(Keys keyData) => false;

    /// <summary>Ctrl+S, the bundle's save key (AX-8.2): the window saves what it holds; a window with nothing to save says so, which is the default.</summary>
    protected virtual void SaveCommand() => Announce("Nothing to save here", true);

    /// <summary>
    /// What the window does with a hand-over from a starting process of its app (<see cref="AppMessage"/>, AM-1.3):
    /// the action, run after the sender has its answer, so that the sender never waits on a read; null when the
    /// window does not take the message, which is the default (the sender then opens a window of its own).
    /// </summary>
    protected virtual Action? TakeHandOver(HandOver message) => null;

    private string? _startNote;

    private bool _startUpDone;

    /// <summary>
    /// A sentence about the start itself, said once the window shows (AX-9.5a, R-64: a remembered path on the network
    /// that was not opened, <see cref="LocalPaths.AtStart"/>): kept until the first showing when the window is still
    /// being made, said at once after it. Null says nothing.
    /// </summary>
    protected void SayAtStart(string? sentence)
    {
        if (sentence is null)
        {
            return;
        }

        if (_startUpDone)
        {
            StartNote(sentence);
        }
        else
        {
            _startNote = sentence;
        }
    }

    /// <summary>How <see cref="SayAtStart"/> says it: one announcement that interrupts nothing, unless the window says it another way (the console windows write a system line).</summary>
    protected virtual void StartNote(string sentence) => Announce(sentence, false);

    /// <summary>The settings file could not be read and the defaults are in use, or some of its values were not understood and the rest is in use (AX-5.3; <c>SettingsFile.Heading</c> says which): a notice at the first showing, unless the window says it another way (the console windows write a system line).</summary>
    protected virtual void SettingsNotRead(string error) =>
        ShowNotice(Notice.Plain("Settings", SettingsSeal.IsRefusal(error) ? error : $"{SettingsFile.Heading(error)}\n{error}") with { Unprompted = true });

    // ---- the menus every window has (AX-6.5, AX-9) ----

    /// <summary>
    /// The eight menus of every window's bar, in their order, each with its letter (AX-9.1, B-19; the user's word of
    /// 2026-11-11): Session, Actions, Navigation, Context, Options, Window, axKit, Help. <see cref="PlaceMenuBar"/>
    /// names them; a test fails on a bar placed another way.
    /// </summary>
    public static readonly IReadOnlyList<string> BarMenus = ["&Session", "&Actions", "&Navigation", "&Context", "&Options", "&Window", "ax&Kit", "&Help"];

    /// <summary>
    /// Puts the window's bar together, the one way every window does (AX-9.1, B-19): its Session, Actions, Navigation,
    /// Context and Options menus, named here whatever the window called them, then Window, axKit and the window's Help
    /// (<see cref="CreateHelpMenu"/>). Actions, Navigation and Context are never empty: one with nothing yet holds one
    /// enabled item that says so. Context is the window's <see cref="ContextBarMenu"/> where it has one, which Shift+F10,
    /// the Applications key and a right click open (<see cref="OpenBarMenu"/> with <see cref="ContextBarMenu.Letter"/>).
    /// </summary>
    protected void PlaceMenuBar(MenuStrip bar, ToolStripMenuItem session, ToolStripMenuItem actions, ToolStripMenuItem navigation, ToolStripMenuItem context, ToolStripMenuItem options, ToolStripMenuItem help)
    {
        ToolStripMenuItem[] own = [session, actions, navigation, context, options];
        for (var i = 0; i < own.Length; i++)
        {
            own[i].Text = BarMenus[i];
        }

        // TP-5: a window with a field to write in gets the templates for it at the end of its Actions.
        if (TemplateField is not null)
        {
            if (actions.DropDownItems.Count > 0)
            {
                actions.DropDownItems.Add(new ToolStripSeparator());
            }

            actions.DropDownItems.Add(CreateInsertTemplateMenu([], InsertIntoTemplateField));
        }

        SayWhenEmpty(actions, "No actions here yet");
        SayWhenEmpty(navigation, "Nothing to move to here yet");
        SayWhenEmpty(context, "Nothing for the focus here yet");
        var window = CreateWindowMenu();
        window.Text = BarMenus[5];
        help.Text = BarMenus[7];
        bar.Items.Clear();
        bar.Items.AddRange([.. own, window, CreateAxKitMenu(), help]);
        // AX-9.1: never deeper than the bar's menu, a submenu and an item; a deeper one is a defect, logged where it was made.
        foreach (var path in MenuShape.TooDeep(bar.Items))
        {
            Log.Error($"A menu item deeper than {MenuShape.MaxDepth}: {path}");
        }
    }

    /// <summary>
    /// Shift+F10, the Applications key and a right click on a control whose own context menu Windows would show (an
    /// edit control's: Undo through its own buffer, AD-D1) open the bar's Context menu instead (AX-9.11, 2026-11-11: one
    /// behaviour everywhere). The control gets a context menu that never shows: its opening is cancelled and Context
    /// opened as Alt and C open it, once the keys that asked are up.
    /// </summary>
    protected void ContextFromControl(Control control)
    {
        var stand = new ContextMenuStrip();
        // An empty strip never raises Opening; the item is never seen.
        stand.Items.Add(new ToolStripMenuItem(BarMenus[3]));
        stand.Opening += (_, e) =>
        {
            e.Cancel = true;
            BeginInvoke(() => OpenBarMenu(ContextBarMenu.Letter));
        };
        control.ContextMenuStrip = stand;
    }

    /// <summary>A bar menu with no items gets one enabled item that says <paramref name="said"/> when chosen (AX-9.1: a menu is never empty, WinForms skipping a disabled item).</summary>
    private void SayWhenEmpty(ToolStripMenuItem menu, string said)
    {
        if (menu.DropDownItems.Count == 0)
        {
            menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&" + said, null, (_, _) => Announce(said, true)), CommandMark.Read));
        }
    }

    /// <summary>The Window menu (AX-9.1): Open windows, Recently closed, Close all windows; the window puts it after Options, which follows its own menus (Navigate last of them).</summary>
    private WindowMenu CreateWindowMenu() => new(this);

    /// <summary>
    /// The axKit menu (AX-9.1; the user's order of 2026-11-10: Navigate, Options, Window, axKit, Help): what is the
    /// bundle's rather than the app's, each a submenu: Claude (AI help, <c>AxKitForm.Ai.cs</c>), AI tools (MC-6.1,
    /// <c>AxKitForm.Mcp.cs</c>) and Theme (the theme file, TH-4, <c>AxKitForm.Theme.cs</c>). A new bundle-wide setting or
    /// action joins here, never an app's Options.
    /// </summary>
    private ToolStripMenuItem CreateAxKitMenu()
    {
        var menu = new ToolStripMenuItem(BarMenus[6]);
        // AX-9.2: the bundle's apps first, the access layers after them (moved from the Window menu, 2026-11-23).
        var (apps, layers) = WindowMenu.CreateAppMenus();
        menu.DropDownItems.Add(apps);
        menu.DropDownItems.Add(layers);
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add(CreateClaudeMenu());
        menu.DropDownItems.Add(CreateMcpMenu());
        menu.DropDownItems.Add(CreateThemeMenu());
        // AX-7.12: the user's own sounds, a WAV file named as a sound in place of the built-in one.
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Open the &sounds folder", null, (_, _) => OpenSoundsFolder()), CommandMark.Open));
        if (!PackageIdentity.IsPackaged)
        {
            // AX-3.1 (the maintainer's answer of 2026-11-24, "One switch for all"): the update check at start is the
            // bundle's, one switch for every app, read as the menu opens since another window may have changed it.
            // The Store's copy makes no check at start (AX-13.3), so it has no switch. Act for the AI tools: on, it is a
            // network contact at every start (docs/axKit/repairs.json R-04).
            var check = CommandPalette.Marked(new ToolStripMenuItem("Check for &updates at start", null, (_, _) => ToggleBundleUpdateCheck()), CommandMark.Act);
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(check);
            CommandPalette.RefreshedOnOpening(menu, () => check.Checked = BundleSettings.Load().CheckForUpdates);
        }

        return menu;
    }

    /// <summary>
    /// axKit, Open the sounds folder (AX-7.12): the folder made when it is missing, with the file that lists the names a
    /// sound file may have written again, and shown in axFile.
    /// </summary>
    private void OpenSoundsFolder()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.SoundsFolder);
            File.WriteAllText(Path.Combine(AppPaths.SoundsFolder, CustomSounds.NamesFile), CustomSounds.NamesText());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Failed("Sounds folder", "The sounds folder could not be made: " + ex.Message);
            return;
        }

        WindowMenu.Start(Dispatch.AxFile, AppPaths.SoundsFolder);
        Announce("The sounds folder, in axFile: a WAV file named as a sound plays in its place from the next start; sound names.txt lists the names", true);
    }

    /// <summary>axKit, Check for updates at start: the bundle's one switch turned over and saved, its new state said.</summary>
    private void ToggleBundleUpdateCheck()
    {
        var settings = BundleSettings.Load();
        settings.CheckForUpdates = !settings.CheckForUpdates;
        try
        {
            settings.Save(BundleSettings.DefaultPath);
            Announce(MenuText.CheckSaid("Check for updates at start", settings.CheckForUpdates), true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Failed("Check for updates at start", "The bundle's settings could not be written: " + ex.Message);
        }
    }

    /// <summary>
    /// The "Show current folder in" submenu of a window (AX-9.10; 2026-10-24 one builder for every window): the
    /// window's File Explorer item, then every app of <see cref="Dispatch.Apps"/> that is offered there
    /// (<see cref="App.ShowIn"/>) but the window's own, in the bundle's order, each a new window on the file or folder
    /// <paramref name="place"/> gives (a folder's app gets a file's folder, <see cref="Dispatch.PathFor"/>).
    /// <paramref name="place"/> says itself why there is none and returns null; a path that is gone is said as
    /// <paramref name="missing"/> (<see cref="FolderMissing"/> or <see cref="FileMissing"/>) and starts nothing.
    /// </summary>
    protected ToolStripMenuItem CreateShowInMenu(string text, ToolStripMenuItem explorer, Func<string?> place, string missing)
    {
        var menu = new ToolStripMenuItem(text);
        menu.DropDownItems.Add(explorer);
        foreach (var app in Dispatch.Apps.Where(app => app.ShowIn && !string.Equals(app.Name, AppName, StringComparison.OrdinalIgnoreCase)))
        {
            menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.AppLabel(app), null, (_, _) =>
            {
                if (place() is { } path && !SaidMissing(path, missing) && Dispatch.PathFor(app, path, Directory.Exists(path)) is { } target)
                {
                    WindowMenu.Start(app.Verb, target);
                }
            }), WindowMenu.OpenMark(app.Verb)));
        }

        return menu;
    }

    /// <summary>What a Show in command says of a folder that is gone (AX-9.10): one wording in every window since 2026-11-09, axClaude's before.</summary>
    protected const string FolderMissing = "Folder missing";

    /// <summary>What a Show in command says of a file that is gone (AX-9.10).</summary>
    protected const string FileMissing = "File missing";

    /// <summary>Whether <paramref name="path"/> is gone, then said as <paramref name="missing"/>: a Show in command on it starts nothing (AX-9.10).</summary>
    protected bool SaidMissing(string path, string missing)
    {
        if (Path.Exists(path))
        {
            return false;
        }

        Announce(missing, true);
        return true;
    }

    /// <summary>
    /// A menu item whose key the window handles itself: the item only shows the key (AX-8), and holds it as a value
    /// for the window's list of its commands (<see cref="CommandPalette.Keyed"/>, docs/axKit/user-workflows.md WF-2.1).
    /// </summary>
    protected static ToolStripMenuItem Item(string text, Action action, Keys key, CommandMark mark)
    {
        var item = CommandPalette.Marked(new ToolStripMenuItem(text, null, (_, _) => action()), mark);
        if (key != Keys.None)
        {
            item.ShortcutKeyDisplayString = BundleKeys.Text(key);
            CommandPalette.Keyed(item, key);
        }

        return item;
    }

    /// <summary>A checked menu item for a setting: flipping it applies the value and saves the settings.</summary>
    protected ToolStripMenuItem Toggle(string text, bool initial, Action<bool> apply, CommandMark mark = CommandMark.Change)
    {
        // A setting is a change (docs/axKit/app-kit.md AK-2.5); one whose effect reaches past the app's own settings (the
        // Run key, a network contact, a file in the user's repository, a check the user relies on) says act.
        var item = CommandPalette.Marked(new ToolStripMenuItem(text) { CheckOnClick = true, Checked = initial }, mark);
        item.CheckedChanged += (_, _) =>
        {
            apply(item.Checked);
            SaveSettings();
        };

        return item;
    }

    /// <summary>The last item of every first menu (AX-6): Exit, closing the window as Alt+F4 does, which is Windows' own key.</summary>
    protected ToolStripMenuItem CreateExitItem() => CommandPalette.Marked(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" }, CommandMark.Act); // Ends the window, and with it what the window runs (Claude Code, a shell, a player).

    /// <summary>Options → Text (AX-6.5): the window's own items first, then what every window has: Say new line (AX-7.8), Sound on headings (AX-7.9), Font…, Larger text, Smaller text, Windows text size.</summary>
    protected ToolStripMenuItem CreateTextMenu(params ToolStripItem[] own)
    {
        var menu = new ToolStripMenuItem("&Text");
        menu.DropDownItems.AddRange(own);
        if (own.Length > 0)
        {
            menu.DropDownItems.Add(new ToolStripSeparator());
        }

        menu.DropDownItems.Add(CreateSayNewLineItem(menu));
        menu.DropDownItems.Add(CreateHeadingSoundItem(menu));
        menu.DropDownItems.Add(CreateOpeningSoundsItem(menu));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Font...", null, (_, _) => ChooseFont()), CommandMark.Change));
        menu.DropDownItems.Add(Item("&Larger text", () => ChangeTextSize(1), BundleKeys.LargerText, CommandMark.Change));
        menu.DropDownItems.Add(Item("S&maller text", () => ChangeTextSize(-1), BundleKeys.SmallerText, CommandMark.Change));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Windows &text size", null, (_, _) => UseSystemFont()), CommandMark.Change));
        return menu;
    }

    /// <summary>
    /// Options → General: the window's own general items. The update check at start was its first item until
    /// 2026-11-24; it is the axKit menu's now, one switch for the bundle (AX-3.1).
    /// </summary>
    protected ToolStripMenuItem CreateGeneralMenu(params ToolStripItem[] own)
    {
        var menu = new ToolStripMenuItem("&General");
        menu.DropDownItems.AddRange(own);
        // A window with no item of its own would show an empty General: the menus and the command list pass over an
        // item that is not Available (found by the Store copy's NVDA pass, 2026-11-14).
        menu.Available = menu.DropDownItems.Count > 0;
        return menu;
    }

    /// <summary>Options → Developer: Open settings file, the window's own items. Copy diagnostics was here until 2026-11-15 and is Help, Diagnostics now.</summary>
    protected ToolStripMenuItem CreateDeveloperMenu(params ToolStripItem[] own)
    {
        var menu = new ToolStripMenuItem("De&veloper");
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Open settings &file", null, (_, _) => OpenSettingsFile()), CommandMark.Open));
        menu.DropDownItems.AddRange(own);
        return menu;
    }

    /// <summary>
    /// Help → Diagnostics (AX-9.1, 2026-11-15, the user's submenu): what finds out why something does not work, the
    /// same in every window: Test a key (AX-8.12, <c>AxKitForm.KeyTest.cs</c>) and Copy diagnostics (from Options,
    /// Developer). Test a key is act for the AI tools: it takes the window's keys until the user ends it, so the user
    /// is asked first.
    /// </summary>
    private ToolStripMenuItem CreateDiagnosticsMenu()
    {
        var menu = new ToolStripMenuItem("&Diagnostics");
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Test a key", null, (_, _) => StartKeyTest()), CommandMark.Act));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Copy diagnostics", null, (_, _) => CopyDiagnostics()), CommandMark.Act));
        return menu;
    }

    /// <summary>
    /// Help (AX-9.1; 5.7.0, 2026-11-10: the user's help in one menu): Command palette… (Ctrl+Shift+P, AX-10),
    /// Keyboard shortcuts (F1, the base's key), User guide, the app's pages in the browser; Send a report… (F4), Your
    /// reports (docs/axKit/reports.md RP-2.2) and Diagnostics (<see cref="CreateDiagnosticsMenu"/>); the window's Check for updates and Update to items (AX-3.1) and About,
    /// the bundle's sections (AX-9.9, <see cref="BundleAbout"/>), each shown as a notice of this window.
    /// </summary>
    protected ToolStripMenuItem CreateHelpMenu(Func<string> userGuide, string resourcesLabel, IEnumerable<(string Label, string Url)> pages)
    {
        // Kept for a question about this window (docs/axKit/ai-help.md AH-6.2), which gives Claude the guide to answer from.
        _userGuide = userGuide;
        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(Item("Command &palette...", ShowCommandPalette, BundleKeys.CommandPalette, CommandMark.Read));
        help.DropDownItems.Add(Item("&Keyboard shortcuts", ShowShortcuts, BundleKeys.Shortcuts, CommandMark.Read));
        help.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("User &guide", null, (_, _) => ShowNotice(Notice.Plain("User guide", userGuide()))), CommandMark.Read));
        var resources = new ToolStripMenuItem(resourcesLabel);
        foreach (var (label, url) in pages)
        {
            resources.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(label, null, (_, _) => OpenUrl(url)), CommandMark.Act));
        }

        help.DropDownItems.Add(resources);
        help.DropDownItems.Add(new ToolStripSeparator());
        // Reports (RP-2.2): the report itself, with its bundle key shown and held as a value (WF-2.1), and the list of
        // them in axTask.
        help.DropDownItems.Add(CommandPalette.Keyed(CommandPalette.Marked(new ToolStripMenuItem("&Send a report...", null, (_, _) => ShowReport())
        {
            ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Report),
        }, CommandMark.Open), BundleKeys.Report));
        help.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Your reports", null, (_, _) => OpenReports()), CommandMark.Act));
        help.DropDownItems.Add(CreateDiagnosticsMenu());
        help.DropDownItems.Add(new ToolStripSeparator());
        help.DropDownItems.Add(_checkItem);
        help.DropDownItems.Add(_updateItem);
        var about = new ToolStripMenuItem("&About");
        about.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Version", null, (_, _) => ShowNotice(Notice.Plain(BundleAbout.VersionTitle, BundleAbout.Version(LatestReleaseState)))), CommandMark.Read));
        about.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Licence", null, (_, _) => ShowNotice(Notice.Plain(BundleAbout.LicenceTitle, BundleAbout.LicenceWithAgreements(Consent.Read(AppPaths.ConsentFile))))), CommandMark.Read));
        about.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Attribution", null, (_, _) => ShowNotice(Notice.Plain(BundleAbout.AttributionTitle, BundleAbout.Attribution))), CommandMark.Read));
        about.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Privacy", null, (_, _) => ShowNotice(Notice.Plain(BundleAbout.PrivacyTitle, BundleAbout.Privacy()))), CommandMark.Read));
        about.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Third-party &notices", null, (_, _) => ShowNotice(Notice.Plain(BundleAbout.NoticesTitle, BundleAbout.Notices()))), CommandMark.Read));
        about.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("axKit &guide", null, (_, _) => ShowNotice(Notice.Plain(BundleAbout.GuideTitle, BundleAbout.Guide()))), CommandMark.Read));
        about.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("axKit on Git&Hub", null, (_, _) => OpenUrl(UpdateCheck.RepositoryPage)), CommandMark.Act));
        help.DropDownItems.Add(about);
        return help;
    }

    /// <summary>
    /// The one one-shot timer of every window (docs/axKit/repairs.json R-17): <paramref name="action"/> runs on the
    /// window's thread after <paramref name="milliseconds"/>, once, and never in a window that has closed meanwhile;
    /// the timer is disposed either way.
    /// </summary>
    protected void After(int milliseconds, Action action)
    {
        var timer = new System.Windows.Forms.Timer { Interval = Math.Max(1, milliseconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            if (!IsDisposed)
            {
                action();
            }
        };
        timer.Start();
    }

    /// <summary>
    /// Shift+F10, the Applications key and a right click (docs/axFile/SPEC.md AF-D13; from axFile on its second use,
    /// axProcess, 2026-10-24): the menu bar's menu with the mnemonic <paramref name="letter"/>, opened as Alt and the
    /// letter open it, so that NVDA reads its first item; a context menu of its own never read its first item the first
    /// time it opened, however it was shown (2026-10-23). It waits until the keys that asked are up: F10 is itself the
    /// menu key, and with Shift down the letter is Shift and the letter; sent while they were held, Windows only beeped.
    /// </summary>
    protected void OpenBarMenu(char letter)
    {
        var waited = 0;
        var timer = new System.Windows.Forms.Timer { Interval = 20 };
        timer.Tick += (_, _) =>
        {
            waited += timer.Interval;
            // Ctrl and the period key too (BundleKeys.ContextMenu, AX-8.15): with Ctrl down the letter is no letter.
            if (IsDown(VK_SHIFT) || IsDown(VK_F10) || IsDown(VK_APPS) || IsDown(VK_CONTROL) || IsDown(VK_OEM_PERIOD))
            {
                if (waited < 3000)
                {
                    return;
                }
            }

            timer.Dispose();
            // Alt, then the letter, as the user would press them: the bar in menu mode, then the menu's letter, which
            // opens it from the keyboard with its first item read. A mnemonic in the Alt message itself opened the first menu.
            PostMessage(Handle, WM_SYSCOMMAND, SC_KEYMENU, 0);
            PostMessage(Handle, WM_KEYDOWN, char.ToUpperInvariant(letter), 0);
            PostMessage(Handle, WM_KEYUP, char.ToUpperInvariant(letter), unchecked((nint)0xC0000001));
        };
        timer.Start();
    }

    private static bool IsDown(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    private const int VK_SHIFT = 0x10;
    private const int VK_F10 = 0x79;
    private const int VK_APPS = 0x5D;
    private const int VK_CONTROL = 0x11;
    private const int VK_OEM_PERIOD = 0xBE;
    private const int WM_CONTEXTMENU = 0x007B;

    /// <summary>The menu of the focused text field of a notice, its first item selected, at the caret; false when the field has none of its own (TP-5.5: a form's field with its templates).</summary>
    private bool ShowFieldMenu()
    {
        if (Control.FromChildHandle(User32.GetFocus()) is not TextBoxBase { ContextMenuStrip: { } strip } box || box.FindForm() != this)
        {
            return false;
        }

        // Once the keys that asked are up, as OpenBarMenu waits: F10 is itself the menu key, and its release closed a
        // menu shown at once (the second NVDA pass of 2026-11-24 heard nothing open).
        var waited = 0;
        var timer = new System.Windows.Forms.Timer { Interval = 20 };
        timer.Tick += (_, _) =>
        {
            waited += timer.Interval;
            if ((IsDown(VK_SHIFT) || IsDown(VK_F10) || IsDown(VK_APPS) || IsDown(VK_CONTROL) || IsDown(VK_OEM_PERIOD)) && waited < 3000)
            {
                return;
            }

            timer.Dispose();
            if (box.IsDisposed || !box.Focused)
            {
                return;
            }

            // Filled again from the templates as read now (the strip's Opening, which a native menu never raises), then
            // shown as Windows' own popup menu, which NVDA reads (NativeMenu): the strip shown itself was heard by none
            // of three NVDA passes.
            (strip.Tag as Action)?.Invoke();
            var caret = box.GetPositionFromCharIndex(Math.Min(box.SelectionStart, Math.Max(0, box.TextLength - 1)));
            var at = box.PointToScreen(new Point(Math.Max(0, caret.X), Math.Max(0, caret.Y) + box.Font.Height));
            NativeMenu.Choose(this, strip.Items.OfType<ToolStripMenuItem>().ToList(), at);
        };
        timer.Start();
        return true;
    }

    /// <summary>Windows' own key for the context menu, which the windows' tables hold for their lists; <see cref="BundleKeys.ContextMenu"/> runs what it runs.</summary>
    private const Keys ContextMenuOfWindows = Keys.Shift | Keys.F10;

    // ---- the window ----

    /// <summary>The theme onto the controls before the first paint (docs/axKit/theme.md TH-3.1).</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        PaintTheme(this);
    }

    /// <summary>The first showing: a settings file that could not be read is reported, then the update check runs when the setting says (AX-3.1).</summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AutomationChannel.StartIfRequested(this);
        StartMcp();
        StartUpWork();
    }

    /// <summary>What the automation channel reports for <c>state</c> (docs/axKit/automation.md): the title, the notice, the status bar's texts.</summary>
    internal object AutomationState() => new
    {
        title = Text,
        notice = NoticeOpen ? Overlay.Shown?.Title : null,
        noticeText = NoticeOpen ? Overlay.Shown?.Text : null,
        process = Environment.ProcessId,
        // Whether the window is on view: false for a window put away by its close (axClaude FR-9.6).
        visible = Visible,
        status = StatusTexts(),
    };

    /// <summary>
    /// The texts of the window's status bar, its labels in order: the one place that reads them, for Say the status
    /// (<see cref="SayStatus"/>), the automation channel and the AI tools. Every kind of window puts a
    /// <see cref="StatusStrip"/> among its controls (<see cref="StatusLayout.Pair"/>); a window without one gives none.
    /// </summary>
    internal string[] StatusTexts() =>
        Controls.OfType<StatusStrip>().SelectMany(strip => strip.Items.Cast<ToolStripItem>()).Select(item => item.Text ?? string.Empty).ToArray();

    /// <summary>
    /// Say the status (AX-7.10, <see cref="BundleKeys.SayStatus"/>, F8, and Window, Say the status): the status bar's
    /// texts as one interrupting announcement of the window's own, as NVDA+End would read them, on the notice while
    /// one shows. NVDA finds a status bar by a lookup on the screen that can miss ("blank" over a full bar, the user,
    /// 2026-11-15); this never depends on it.
    /// </summary>
    protected internal void SayStatus() => Announce(StatusWords(StatusTexts()), true);

    /// <summary>What Say the status says when the window has no status bar, or one with no text.</summary>
    public const string NoStatus = "No status";

    /// <summary>"Claude: ready. C:\work": the status bar's texts in order, each without its own trailing full stop, parted by one so that they are heard as separate parts; <see cref="NoStatus"/> for none.</summary>
    public static string StatusWords(IEnumerable<string> texts)
    {
        var parts = texts.Select(text => text.Trim().TrimEnd('.').TrimEnd()).Where(text => text.Length > 0).ToList();
        return parts.Count == 0 ? NoStatus : string.Join(". ", parts);
    }

    private void StartUpWork()
    {
        if (_settingsError is { } error)
        {
            SettingsNotRead(error);
        }

        _startUpDone = true;
        if (_startNote is { } note)
        {
            _startNote = null;
            StartNote(note);
        }

        // Never in a test run (docs/axKit/automation.md): a network call, and a logged error when it fails. Never in the
        // Store's copy, which the Microsoft Store keeps current and which asks GitHub only on Check for updates (AX-13.3).
        if (!BundleInfo.IsDevelopmentBuild && !AutomationChannel.Requested && !PackageIdentity.IsPackaged && BundleSettings.Load().CheckForUpdates)
        {
            CheckForUpdates(manual: false);
        }
    }

    /// <summary>
    /// What the window holds, for the bundle's Recently closed list (AX-9.4), read as it closes: the path of its file
    /// or folder; an empty text for an app that holds none, listed by its name alone (the default, so that a new app is
    /// in the list without a line of its own); null for a window that holds nothing to open again (an untitled text).
    /// </summary>
    protected virtual string? PathInHand => string.Empty;

    /// <summary>The window joins Recently closed, the place and the settings are saved, and the installed new version started, unless the window cancels its closing before calling here.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!e.Cancel)
        {
            if (PathInHand is { } held)
            {
                RecentlyClosed.Remember(AppName, held);
            }

            if (SavesWindowPlace)
            {
                SaveWindow();
            }

            SaveSettings(say: false);
            StartInstalledVersion();
        }

        base.OnFormClosing(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        _altCarriedOverUntil = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 ? Environment.TickCount64 + AltCarriedOverMs : 0;
        base.OnActivated(e);
        SayWhatWaitedForActivation();
        PaintThemeIfChanged();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    /// <summary>The window's client area as its UI Automation rectangle, so that NVDA+End finds the status bar.</summary>
    protected override AccessibleObject CreateAccessibilityInstance() => new WindowAccessibleObject(this);

    protected override void WndProc(ref Message m)
    {
        // Alt alone and F10 open the menu bar through SC_KEYMENU; while a notice holds the keyboard they do nothing.
        // Alt+Space (the system menu, lParam is the space character) still works.
        if (m.Msg == WM_SYSCOMMAND && ((long)m.WParam & 0xFFF0) == SC_KEYMENU && ((long)m.LParam & 0xFFFF) == 0
            && (NoticeOpen || Environment.TickCount64 < _altCarriedOverUntil))
        {
            // The second case: the window came to the front while Alt was held, as Alt+F4 on another program leaves
            // it, and Alt's release then opened this window's menu bar (the user, 2026-10-23, on closing axFile:
            // "alt+f4 lands me on the session menu"). That Alt was pressed for the other window.
            _altCarriedOverUntil = 0;
            return;
        }

        if (AppMessage.Receive(ref m) is { } handOver && TakeHandOver(handOver) is { } apply)
        {
            // Answered at once and applied after: the starting process waits on the answer, never on a read (AM-1.3).
            m.Result = 1;
            BeginInvoke(() =>
            {
                if (!IsDisposed)
                {
                    apply();
                }
            });
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// The common keys (AX-8.2): the text size keys and Say the status (F8, AX-7.10) work everywhere, a notice included; while a notice shows, its text
    /// takes the find keys (AX-7.2) and every other shortcut waits (not calling the base skips the menu shortcuts;
    /// Enter and Escape reach the notice's buttons through AcceptButton and CancelButton); F1 shows the shortcuts,
    /// Ctrl+Shift+P the palette, Ctrl+S saves; then the window's own keys.
    /// </summary>
    /// <summary>
    /// The keys of the whole window, which need no selection and no caret (AX-6.6a): F1, the palette, F4, asking
    /// Claude, Ctrl+S and the app keys. They work from the window and from an open menu.
    /// </summary>
    private static bool IsWindowKey(Keys keyData) =>
        keyData is BundleKeys.Shortcuts or BundleKeys.CommandPalette or BundleKeys.Report or BundleKeys.AskClaude or BundleKeys.AskClaudeAlso or BundleKeys.Save
        || BundleKeys.AppOfLaunchKey(keyData) is not null;

    /// <summary>
    /// F1 (AX-7.2a): the app's own text, then every key of the window made from its command list as it stands
    /// (<see cref="KeyReference"/>), so that no key the window has is missing from what F1 reads.
    /// </summary>
    private void ShowShortcuts() => ShowNotice(Notice.Plain("Keyboard shortcuts", ShortcutsText()));

    /// <summary>The whole F1 text: <see cref="Shortcuts"/> and the section made from the commands; Ask about this window reads it too.</summary>
    protected internal string ShortcutsText() => (Shortcuts.TrimEnd('\r', '\n') + "\n\n" + KeyReference.Section(CommandRows().Commands)).ReplaceLineEndings("\r\n");

    /// <summary>A key of the whole window run (<see cref="IsWindowKey"/>): true when it was one.</summary>
    private bool RunWindowKey(Keys keyData)
    {
        if (IsWindowKey(keyData))
        {
            WriteKey(keyData, keyData == BundleKeys.AskClaudeAlso ? BundleKeys.AskClaude : keyData);
        }

        switch (keyData)
        {
            case BundleKeys.Shortcuts:
                ShowShortcuts();
                return true;
            case BundleKeys.CommandPalette:
                ShowCommandPalette();
                return true;
            case BundleKeys.Report:
                ShowReport();
                return true;
            case BundleKeys.AskClaude:
            case BundleKeys.AskClaudeAlso:
                AskAboutThisWindow();
                return true;
            case BundleKeys.Save:
                SaveCommand();
                return true;
        }

        // AX-8.18 rule 6: Ctrl with a function key opens a standalone app, Ctrl+Shift with one an access layer, from
        // every window, as the axKit menu's Apps and Access layers do.
        if (BundleKeys.AppOfLaunchKey(keyData) is { } launched)
        {
            WindowMenu.Start(launched.Verb, null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// AX-6.6a with an open drop-down (the maintainer, 2026-11-24: "ctrl+function does not work when focus is on menu
    /// of window"): WinForms hands the keys of an open drop-down to the drop-down, a window of its own whose keys never
    /// reach the form's <see cref="ProcessCmdKey"/> (which sees them only while the bar itself has them). This filter of
    /// the thread's messages sees a key before the drop-down does: a key of the whole window closes the menus and runs
    /// once they have closed, F8 says the status with the menu still open, and every other key goes on to the menu.
    /// One filter for the process, added with the first window.
    /// </summary>
    private sealed class MenuKeyFilter : IMessageFilter
    {
        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg is not (WmKeyDown or WmSysKeyDown) || Control.FromHandle(m.HWnd) is not ToolStripDropDown dropDown || WindowOf(dropDown) is not { } form)
            {
                return false;
            }

            var keyData = (Keys)(int)m.WParam | Control.ModifierKeys;
            return form.TakeMenuKey(dropDown, keyData);
        }

        /// <summary>The axKit window whose menu bar the drop-down hangs from, through its owner items; null for any other.</summary>
        private static AxKitForm? WindowOf(ToolStripDropDown dropDown)
        {
            ToolStrip? strip = dropDown;
            while (strip is ToolStripDropDown { OwnerItem.Owner: { } owner })
            {
                strip = owner;
            }

            return strip?.FindForm() as AxKitForm;
        }
    }

    private static bool _menuKeyFilterAdded;

    /// <summary>The filter added once, from the first window's constructor.</summary>
    private static void AddMenuKeyFilter()
    {
        if (!_menuKeyFilterAdded)
        {
            _menuKeyFilterAdded = true;
            Application.AddMessageFilter(new MenuKeyFilter());
        }
    }

    /// <summary>A key pressed in an open drop-down of this window's bar: true when it was a key of the whole window and is taken.</summary>
    private bool TakeMenuKey(ToolStrip dropDown, Keys keyData)
    {
        if (keyData == BundleKeys.SayStatus)
        {
            SayStatus();
            return true;
        }

        if (!IsWindowKey(keyData))
        {
            return false;
        }

        CloseMenus(dropDown);
        BeginInvoke(() => RunWindowKey(keyData));
        return true;
    }

    /// <summary>Every open drop-down of the bar closed, from the one the key came to up to the top one, so that the menu mode ends.</summary>
    private static void CloseMenus(ToolStrip strip)
    {
        var top = strip as ToolStripDropDown;
        while (top?.OwnerItem?.Owner is ToolStripDropDown parent)
        {
            top = parent;
        }

        top?.Close(ToolStripDropDownCloseReason.Keyboard);
        if ((top?.OwnerItem?.Owner ?? strip) is MenuStrip bar)
        {
            // The bar itself left: its item no longer selected, so that the menu mode ends with the drop-down.
            foreach (ToolStripItem item in bar.Items)
            {
                if (item.Selected && item is ToolStripMenuItem menu)
                {
                    menu.HideDropDown();
                }
            }
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.LargerText:
            case BundleKeys.LargerTextNumpad:
                WriteKey(keyData, BundleKeys.LargerText);
                ChangeTextSize(1);
                return true;
            case BundleKeys.SmallerText:
            case BundleKeys.SmallerTextNumpad:
                WriteKey(keyData, BundleKeys.SmallerText);
                ChangeTextSize(-1);
                return true;
            case BundleKeys.SayStatus when FromHandle(msg.HWnd) is not ToolStrip:
                // AX-7.10: information and nothing else, so it works while a notice shows too, as the text size keys
                // do (the status is still true behind the notice); an open menu keeps the key.
                WriteKey(keyData, keyData);
                SayStatus();
                return true;
            case BundleKeys.ContextMenu when FromHandle(msg.HWnd) is not ToolStrip:
                // AX-8.15: what Shift+F10 does where the focus is. A command of the window on Shift+F10 (a list's
                // Context menu), never while a notice holds the window's commands; otherwise Windows' own request
                // for a context menu, to the focused control, as Windows makes it for Shift+F10: the control's
                // stand-in opens the bar's Context menu (ContextFromControl), a plain edit control shows its own
                // menu, and a control with neither does nothing. Posted, so the menu's loop starts after this key.
                if (NoticeOpen && ShowFieldMenu())
                {
                    return true;
                }

                if (NoticeOpen || !(ProcessWindowKey(ContextMenuOfWindows) || RunCommandKey(ContextMenuOfWindows)))
                {
                    var focus = User32.GetFocus();
                    User32.PostMessage(focus, WM_CONTEXTMENU, focus, -1);
                }

                return true;
        }

        // AX-8.13: the mode key, for the control the key is addressed to, in a notice too (a notice's field has the
        // modes); navigation mode's own keys reach the text first (AxKitTextBox.ProcessCmdKey, TakeNavigationKey).
        if (TextModeKey(FromHandle(msg.HWnd), keyData))
        {
            return true;
        }

        // AX-7.3 (2026-10-25, the user's ask): a paste into an editable text is said, since the edit control's own
        // paste makes no sound and NVDA says nothing. The edit control's keys, not the bundle's: the paste is the
        // control's, and axFile's list keeps Ctrl+V for files.
        if (keyData is (Keys.Control | Keys.V) or (Keys.Shift | Keys.Insert)
            && Control.FromChildHandle(User32.GetFocus()) is TextBoxBase { ReadOnly: false, Enabled: true } box && box.FindForm() == this)
        {
            SayPasteAfter(box);
        }

        if (NoticeOpen)
        {
            if (Overlay.HasSearchableText)
            {
                switch (keyData)
                {
                    case BundleKeys.Find:
                        ShowFindInNotice();
                        return true;
                    case BundleKeys.FindNext:
                        FindInNotice(1);
                        return true;
                    case BundleKeys.FindPrevious:
                        FindInNotice(-1);
                        return true;
                }
            }

            if (keyData == BundleKeys.MainAction)
            {
                // AX-8.10 rule 3: the notice's main action, a form's Send or the main button, as Enter presses it.
                Overlay.PressMain();
                return true;
            }

            if (keyData == OverlayPanel.ReadText)
            {
                // AX-7.11: the notice's text on demand, a form's explanation among them, which is not said at opening.
                Overlay.SayText();
                return true;
            }

            // TP-5.5: a form field's own menu (its templates) opened by the window, at the caret, for Shift+F10 and the
            // Applications key: the key does not reach the field as a request for its menu while the notice holds it
            // (the NVDA pass of 2026-11-24 heard nothing open).
            if (keyData is ContextMenuOfWindows or Keys.Apps && ShowFieldMenu())
            {
                return true;
            }

            return false;
        }

        // In menu mode (Alt, a menu's name selected on the bar) WinForms routes the keys to the menu bar, whose
        // ProcessCmdKey hands them up to the window: they are the menu's (AX-6.6), or Right in axFile's list opened
        // the entry instead of moving to the next menu (2026-10-22); F1 opened the shortcuts over an open menu and
        // disabled the bar under it (2026-10-23).
        if (FromHandle(msg.HWnd) is ToolStrip strip)
        {
            // AX-6.6a (the maintainer, 2026-11-23: "any hotkey that does not need a selection or caret should work when
            // focus is on the menu"): a key of the whole window works from an open menu too. The menu closes first and
            // the key runs once it has, so that nothing opens over a menu (the F1 fault of 2026-10-23). F8 only says.
            if (keyData == BundleKeys.SayStatus)
            {
                SayStatus();
                return true;
            }

            if (IsWindowKey(keyData))
            {
                CloseMenus(strip);
                BeginInvoke(() => RunWindowKey(keyData));
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        if (RunWindowKey(keyData))
        {
            return true;
        }

        // The window's own keys first (a rename box's, a state's), then the keys of its command list (app-kit AK-2.3).
        if (ProcessWindowKey(keyData) || RunCommandKey(keyData))
        {
            return true;
        }

        if (keyData == BundleKeys.MainAction)
        {
            // AX-8.10 rule 3: no main action where the focus is (no command of the window took the key): the edge
            // sound and nothing else, never a line break in a text or a control's own meaning.
            EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ---- the window's commands (docs/axKit/app-kit.md AK-2) ----

    private readonly List<Command> _commands = [];

    /// <summary>A command as an item of <paramref name="menu"/> showing its key, and its key handled with it (AK-2.3); the item, for a window that keeps it.</summary>
    protected ToolStripMenuItem Add(ToolStripMenuItem menu, Command command)
    {
        var item = Add(command);
        menu.DropDownItems.Add(item);
        return item;
    }

    /// <summary>
    /// A command with its key handled, and its item for a menu the window places itself (the first item of
    /// <see cref="CreateShowInMenu"/>, a context menu). One record placed in several menus (axProcess's Details in each
    /// view's menu) is one command: it is listed once.
    /// </summary>
    protected ToolStripMenuItem Add(Command command)
    {
        if (!_commands.Any(known => ReferenceEquals(known, command)))
        {
            _commands.Add(command);
        }

        // The item carries the command's mark, for the AI tools, which read the menus (docs/axKit/app-kit.md AK-2.5).
        var item = Item(command.Label, () => Run(command), command.Key, command.Mark);
        _commandItems.AddOrUpdate(item, command);
        return item;
    }

    /// <summary>
    /// The window's last actions for a report (docs/axKit/reports.md RP-4.1): every command run, by its name and
    /// never with what the user typed (<see cref="RecentActions.CommandText"/>): a command of the list by its key or
    /// its item (<c>Run</c>), any other menu item as it is picked, by the user, the palette, a script or an AI tool
    /// (<see cref="WritePick"/>), and the keys of the whole window (<see cref="WriteKey"/>); what the window said; and
    /// the notices it showed; twenty at most.
    /// </summary>
    protected internal RecentActions Recent { get; } = new();

    /// <summary>The menu items made from a <see cref="Command"/>: <c>Run</c> writes them, so their pick is not written twice.</summary>
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ToolStripItem, Command> _commandItems = [];

    /// <summary>
    /// A menu item picked, from the one place every pick passes (<see cref="CommandPalette"/>'s <c>WritePicks</c>),
    /// written into <see cref="Recent"/> by its menus' names and its own: "Word wrap, Alt+Z", "Session, Open recent
    /// file, entry 2". An item of a <see cref="Command"/> is written by <c>Run</c> instead.
    /// </summary>
    internal void WritePick(ToolStripMenuItem item)
    {
        if (!_commandItems.TryGetValue(item, out _))
        {
            Recent.Add(RecentActionKind.Command, RecentActions.CommandText(item.Text ?? string.Empty, CommandPalette.MenusOf(item), CommandPalette.KeyTextOf(item)), DateTime.UtcNow);
        }
    }

    /// <summary>
    /// A key of the whole window pressed (F1, the palette, F4, Ask about this window, Ctrl+S, an app's launch key, the
    /// text size keys, F8), written into <see cref="Recent"/> by the name of the menu item that shows the key
    /// (<paramref name="shown"/>, the key a further key stands for) and the key pressed; a launch key with no item by
    /// the app's title.
    /// </summary>
    private void WriteKey(Keys pressed, Keys shown)
    {
        var name = ItemShowing(MainMenuStrip?.Items, shown)?.Text ?? BundleKeys.AppOfLaunchKey(pressed)?.Title;
        if (name is not null)
        {
            Recent.Add(RecentActionKind.Command, RecentActions.CommandText(name, [], BundleKeys.Text(pressed)), DateTime.UtcNow);
        }

        static ToolStripMenuItem? ItemShowing(ToolStripItemCollection? items, Keys key)
        {
            foreach (var item in items?.OfType<ToolStripMenuItem>() ?? [])
            {
                if (CommandPalette.KeyOf(item) == key && !item.HasDropDownItems)
                {
                    return item;
                }

                if (item.HasDropDownItems && ItemShowing(item.DropDownItems, key) is { } found)
                {
                    return found;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Whether what the window says goes into <see cref="Recent"/> (RP-4.2). Not for axPass and axClip, whose
    /// announcements may be a field's value or a clipboard entry (the MCP's never-read apps), and not for the console
    /// windows, whose spoken replies and output are the user's conversation (<c>ConsoleForm</c>): those record their
    /// commands and notices alone.
    /// </summary>
    protected virtual bool RecordsAnnouncements => !McpTools.NeverRead.Contains(AppVerb);

    /// <summary>Runs a command and records it: "Find, Ctrl+F", the label without its mnemonic and the key as the menu shows it.</summary>
    private void Run(Command command)
    {
        Recent.Add(RecentActionKind.Command, RecentActions.CommandText(command.Label, [], command.Key == Keys.None ? null : BundleKeys.Text(command.Key)), DateTime.UtcNow);
        command.Run();
    }

    /// <summary>
    /// The commands added as keys alone (<see cref="AddKey"/>), which are in <see cref="_commands"/> too: what tells a
    /// key with no menu item from a command's key when the window lists its commands as data (<see cref="CommandRows"/>).
    /// </summary>
    private readonly List<Command> _keyOnly = [];

    /// <summary>A command that is a key and no menu item (Tab between a window's panes, a list's Enter).</summary>
    protected void AddKey(Command command)
    {
        _commands.Add(command);
        _keyOnly.Add(command);
    }

    /// <summary>
    /// Whether a command added so far has the key: a shared piece that adds its keys after the app's own asks, so
    /// that it does not list a command on a key the app gave to another (axAutomate's F5 runs, and the item
    /// window's Refresh stood on it in the listing, where a script's or an AI tool's "Refresh" then started a run;
    /// found 2026-11-17).
    /// </summary>
    protected bool KeyTaken(Keys key) => _commands.Any(command => command.Key == key);

    /// <summary>
    /// Whether a command added so far would take the key from a later one that acts where <paramref name="when"/>
    /// says: it has the key and acts anywhere, or acts in that very place (the same condition). The later command
    /// would never run, which is what <see cref="CommandProblems"/> names. A shared piece that adds its keys after
    /// the app's own leaves such a key to the app (axFile's own Enter, Delete and F2 in its folder list, on the
    /// item window's condition, framework step 13).
    /// </summary>
    protected bool KeyTakenWhere(Keys key, Func<bool>? when) =>
        _commands.Any(command => command.Key == key && (command.When is null || command.When == when));

    /// <summary>
    /// What is wrong with the window's command list (app-kit AK-2.4), for the automated checks' <c>check commands</c>:
    /// a command that never runs because an earlier one has its key everywhere (no <see cref="Command.When"/>), and an
    /// entry without its mark. Empty when all is well.
    /// </summary>
    internal List<string> CommandProblems()
    {
        var problems = new List<string>();
        for (var i = 0; i < _commands.Count; i++)
        {
            var command = _commands[i];
            var key = command.Key;
            if (key == Keys.None)
            {
                continue;
            }

            // Every key is in the F1 text by construction since 2026-11-23 (KeyReference, AX-7.2a): the check that the
            // hand-written part named each one is gone, so that part may shrink to what the commands do not say.
            var text = BundleKeys.Text(key).TrimEnd('.');

            // An earlier command takes the key everywhere, or wherever this one would act (the same condition): the
            // listing would name this command on a key that does the other (axAutomate's Refresh on F5, 2026-11-17).
            if (_commands.Take(i).FirstOrDefault(earlier => earlier.Key == key && (earlier.When is null || earlier.When == command.When)) is { } shadow)
            {
                problems.Add($"{Plain(command.Label)} ({text}) never runs: {Plain(shadow.Label)} takes the key {(shadow.When is null ? "everywhere" : "wherever it would act")}");
            }
        }

        // Every entry of the built window states its mark (docs/axKit/repairs.json R-11, R-37): asked of the menus as
        // they stand, the lists that fill themselves included, so an item made in a way the source tests do not see
        // is found here.
        if (MainMenuStrip is { } bar)
        {
            // An item withheld from the AI tools (NeverForAiTools) has said what it is to them: never theirs.
            foreach (var entry in CommandPalette.Entries(bar).Where(entry => CommandPalette.ForAiTools(entry.Item) && !CommandPalette.StatesMark(entry.Item)))
            {
                problems.Add($"{entry.Name} ({entry.Menu}) states no mark: give it one where it is made, as a Command or with CommandPalette.Marked (docs/axKit/app-kit.md AK-2.5)");
            }
        }

        // Rule 1 (AX-7): every control the keyboard reaches has a name NVDA says, asked of the built window as it stands,
        // a notice's controls and the ones a window shows later included once they exist (docs/axKit/repairs.json R-98,
        // 2026-11-25: until then no check held it). A button or a check box with its own text is named by it; a hidden
        // control is reached by no key (a notice's field and check box are named by each notice as it opens).
        foreach (var control in Unnamed(this))
        {
            problems.Add($"A {control.GetType().Name} ({control.Name}) has no accessible name: give it one where it is made (root CLAUDE.md rule 1)");
        }

        return problems;

        static string Plain(string label) => label.Replace("&", string.Empty, StringComparison.Ordinal).TrimEnd('.');

        static IEnumerable<Control> Unnamed(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                var named = !string.IsNullOrWhiteSpace(control.AccessibleName)
                    || (control is ButtonBase && !string.IsNullOrWhiteSpace(control.Text));
                if (!named && control.Visible && control.TabStop && control is TextBoxBase or ListBox or ComboBox or TreeView or ButtonBase)
                {
                    yield return control;
                }

                foreach (var inner in Unnamed(control))
                {
                    yield return inner;
                }
            }
        }
    }

    /// <summary>The first command with the key whose <see cref="Command.When"/> holds, run; true when there was one.</summary>
    private bool RunCommandKey(Keys keyData)
    {
        if (keyData == Keys.None)
        {
            return false;
        }

        foreach (var command in _commands)
        {
            if (command.Key == keyData && (command.When?.Invoke() ?? true))
            {
                Run(command);
                return true;
            }
        }

        return false;
    }

    /// <summary>Runs a command's task from the UI thread and turns an escaped exception into a notice rather than a crash: an exception out of an awaited task would otherwise be unobserved.</summary>
    protected internal async void Do(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error($"{AppName} command failed", ex);
            if (!IsDisposed)
            {
                Fail(ex.Message);
            }
        }
    }

    // ---- the window's place and the settings (AX-5, AX-7.6) ----

    /// <summary>
    /// Places the window's controls once its constructor has made them: the notice panel first, so that it covers the
    /// rest when it shows; <paramref name="body"/> in the window's own docking order (WinForms docks the last added
    /// first, so the status bar comes last in it); the menu bar last; then the text font and the window's saved place.
    /// A hidden window (HotkeyForm) places itself and adds its controls on its own; the console window adds its own,
    /// one by one, for the order of the name line and the field.
    /// </summary>
    protected void PlaceControls(MenuStrip menu, params Control[] body)
    {
        Controls.AddRange([Overlay, .. body, menu]);
        MainMenuStrip = menu;
        ApplyTextFont();
        RestoreWindow();
    }

    /// <summary>The window where it was last, when that place is still on a screen; <see cref="PlaceControls"/> calls it at the end of a window's constructor.</summary>
    protected void RestoreWindow()
    {
        if (Settings.Window is not { } placement)
        {
            return;
        }

        // R-58: fitted into the screen it shares the most with, so a damaged or hand-edited place cannot put the
        // window where it is not found; on no screen at all, Windows places the window.
        var stored = new Rectangle(placement.X, placement.Y, placement.Width, placement.Height);
        if (WindowPlace.Fit(stored, [.. Screen.AllScreens.Select(s => s.WorkingArea)]) is not { } bounds)
        {
            return;
        }

        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        if (placement.Maximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    /// <summary>
    /// Whether the window's place and state are the user's to remember as it closes: true, but for a window whose
    /// place the user never saw (axClaude's hidden start, shown at a size of its own until it is first brought up,
    /// FR-9.6: saved, it wrote "not maximized" into the settings every axClaude window reads).
    /// </summary>
    protected virtual bool SavesWindowPlace => true;

    private void SaveWindow()
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        Settings.Window = new WindowPlacement
        {
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            Maximized = WindowState == FormWindowState.Maximized,
        };
    }

    /// <summary>
    /// The settings into their file. A file that cannot be written (read-only, a full disk) is logged every time and
    /// said once in a run (AX-5.9; repairs.md R-63), the way a file that could not be read is said
    /// (<see cref="SettingsNotSaved"/>).
    /// </summary>
    protected void SaveSettings() => SaveSettings(say: true);

    /// <param name="say">False for the save of a closing window, which has nothing left to say it in.</param>
    private void SaveSettings(bool say)
    {
        try
        {
            Settings.Save(SettingsPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("Settings could not be saved", ex);
            if (say && Visible && SettingsFile.NotWrittenOnce(ex.Message) is { } sentence)
            {
                SettingsNotSaved(sentence);
            }
        }
    }

    /// <summary>The settings file could not be written (AX-5.9): a notice, unless the window says it another way (the console windows write a system line).</summary>
    protected virtual void SettingsNotSaved(string sentence) =>
        ShowNotice(Notice.Plain("Settings", sentence) with { Unprompted = true });

    // ---- the ways out to the system (AX-7.2: the only separate windows) ----

    /// <summary>
    /// Whether one of the Windows dialogs (the folder, file and font pickers) is up. Its closing activates the window
    /// before the command that opened it has gone on, so a window that acts on activation (the editors' reload of a
    /// file changed on disk, axFile's read of the folder) waits while this is set (2026-10-23: "Reloaded" was said in
    /// the middle of Save as).
    /// </summary>
    protected bool DialogShowing { get; private set; }

    /// <summary>
    /// Shows one of the Windows dialogs over this window, the only separate windows an app opens (AX-7.2), with
    /// <see cref="DialogShowing"/> set meanwhile, and puts the focus back where it was when the dialog closes, OK or
    /// cancel alike, one rule for every window (2026-11-09; each caller did its own before, the editor's Open only after
    /// OK and its Save as only after cancel). A dialog opened from a notice's button leaves the focus to the notice.
    /// </summary>
    protected DialogResult ShowSystemDialog(CommonDialog dialog)
    {
        var before = FocusedControl();
        DialogShowing = true;
        try
        {
            return dialog.ShowDialog(this);
        }
        finally
        {
            DialogShowing = false;
            if (before is { IsDisposed: false, Visible: true, Enabled: true })
            {
                before.Select();
            }
        }
    }

    /// <summary>The Windows folder picker, titled with <paramref name="description"/> and opened on <paramref name="start"/>: the folder chosen, or null.</summary>
    protected internal string? ChooseFolder(string description, string? start = null)
    {
        using var dialog = new FolderBrowserDialog { Description = description, UseDescriptionForTitle = true, ShowNewFolderButton = true };
        if (start is not null)
        {
            dialog.InitialDirectory = start;
        }

        return ShowSystemDialog(dialog) == DialogResult.OK && dialog.SelectedPath.Length > 0 ? dialog.SelectedPath : null;
    }

    /// <summary>The Windows file picker for a file that is there, titled with <paramref name="title"/>, with <paramref name="filter"/> and opened in <paramref name="start"/>: the file chosen, or null.</summary>
    protected internal string? ChooseFile(string title, string filter, string? start = null)
    {
        using var dialog = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
        if (start is not null && Directory.Exists(start))
        {
            dialog.InitialDirectory = start;
        }

        return ShowSystemDialog(dialog) == DialogResult.OK && dialog.FileName.Length > 0 ? dialog.FileName : null;
    }

    /// <summary>A page in the default browser; a failure is a notice. Help, About, axKit on GitHub calls it for the repository page.</summary>
    protected internal void OpenUrl(string url)
    {
        try
        {
            // Outside call, program, Windows: opens a web page in the user's default browser through the Windows shell,
            // for the Help menu's links, the release page and links the user presses Enter on; axKit itself fetches
            // nothing.
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            Fail("The page could not be opened: " + ex.Message);
        }
    }

    /// <summary>
    /// The path in File Explorer: a folder opened, a file selected in its folder; with <paramref name="select"/> a folder
    /// is selected in its parent too (axFile's Show in File Explorer on an entry). A path that is gone is said as
    /// <paramref name="missing"/> and File Explorer is not started (AX-9.10).
    /// </summary>
    protected void OpenInExplorer(string path, bool select = false, string missing = FolderMissing)
    {
        if (SaidMissing(path, missing))
        {
            return;
        }

        var arguments = !select && Directory.Exists(path) ? $"\"{path}\"" : $"/select,\"{path}\"";
        try
        {
            // Outside call, program, Windows: opens File Explorer on a folder, or with a file selected, for Show in
            // File Explorer; Windows' own program by its full path.
            Process.Start(new ProcessStartInfo(SystemPrograms.Explorer, arguments) { UseShellExecute = false })?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Fail("File Explorer could not be started: " + ex.Message);
        }
    }

    /// <summary>Options → Developer → Open settings file: the app's settings, written first, in a new axJSON window, as any JSON file.</summary>
    protected void OpenSettingsFile()
    {
        SaveSettings();
        WindowMenu.Start(Dispatch.AxJSON, SettingsPath);
    }

    /// <summary>Help → Diagnostics → Copy diagnostics: the versions, the window's own lines and the end of the log, to the clipboard.</summary>
    private void CopyDiagnostics()
    {
        var text = string.Join("\r\n",
        [
            $"{AppName}, axKit {BundleInfo.Version} on .NET {Environment.Version}, {Environment.OSVersion}",
            .. DiagnosticsLines,
            $"Settings: {SettingsPath}",
            $"Log: {Log.FilePath}",
            string.Empty,
            Log.Tail(200),
        ]);
        // S-20: meant for a public bug report, so the account's name and paths, e-mail addresses and ids are replaced.
        CopyText(Redaction.Redact(text), "Copied diagnostics");
    }

    /// <summary>
    /// The one way a window puts text on the clipboard: <paramref name="said"/> announced when it is there (null says
    /// nothing), and a clipboard another program holds for a moment reported through <see cref="Fail"/> rather than as a
    /// crash; false then.
    /// </summary>
    protected internal bool CopyText(string text, string? said)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Fail("The clipboard could not be written: " + ex.Message);
            return false;
        }

        if (said is not null)
        {
            Announce(said, true);
        }

        return true;
    }
}

using AxHelp.Core;
using AxKit.Core;
using AxKit.Core.Ai;
using AxKit.Forms;
using static AxKit.Forms.User32;

namespace AxHelp;

/// <summary>
/// axHelp's window (SPEC.md HE-2, HE-3; 5.7.0, the user's ask of 2026-11-10: "just like axClaude ... a seamless
/// conversation", with axDescribe merged in): a background process on <see cref="HotkeyForm"/> with two hotkeys. The
/// conversation's hotkey brings up the conversation, an axClaude window on Claude's working folder titled "axHelp",
/// started when none is open (<see cref="OpenConversation"/>); the screenshot's hotkey takes a picture of the window in
/// front into that folder and puts its path into the conversation's message field. The window itself, shown by a start
/// without the hotkey, is one text that says this, with the menus. Nothing of the conversation is here: it is axClaude's
/// and Claude Code's.
/// </summary>
public sealed class HelpForm : HotkeyForm
{
    /// <summary>The conversation window's title, axClaude's <c>--title</c> (the user's ask: "just say axHelp").</summary>
    public const string ConversationTitle = "axHelp";

    private readonly HelpSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly AxKitTextBox _text = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        AccessibleName = "axHelp",
        TabIndex = 0,
    };

    private readonly string? _picture;

    /// <summary>axHelp started with --hidden, as at sign-in (HE-2.7): the one start that may start the conversation hidden too.</summary>
    private readonly bool _startedHidden;

    /// <summary>
    /// The process axHelp last started for the conversation, kept so that a press while its window is still coming up
    /// waits for that window, however long, and never starts a second one, and so that Quit can end a process whose
    /// window never came (<see cref="Conversation"/>); null before any start and once it has ended.
    /// </summary>
    private System.Diagnostics.Process? _conversation;

    /// <summary>Whether the process axHelp started for the conversation still runs.</summary>
    private bool ConversationProcessRuns
    {
        get
        {
            if (_conversation is not { } process)
            {
                return false;
            }

            try
            {
                if (!process.HasExited)
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Not ours to ask any more: as ended.
            }

            process.Dispose();
            _conversation = null;
            return false;
        }
    }

    /// <summary>The conversation's window started, its process kept in place of the one before.</summary>
    private void StartConversation(IReadOnlyList<string> arguments)
    {
        _conversation?.Dispose();
        _conversation = WindowMenu.StartKept(Dispatch.AxClaude, arguments);
    }

    /// <param name="picture">A picture file the start named (HE-3.3): put into the conversation once the window is made.</param>
    public HelpForm(bool hidden, HelpSettings settings, string? settingsError, string? picture)
        : base(AxHelpApp.Name, settings, HelpSettings.DefaultPath, settingsError, hidden || picture is not null)
    {
        _settings = settings;
        _picture = picture;
        _startedHidden = hidden && picture is null;
        // The window's own title differs from the conversation's, so that a second start never takes one for the other (HE-1.2).
        Text = AxHelpApp.WindowTitle;

        // ---- menus and keys (HE-6): one command list (docs/axKit/app-kit.md AK-2) ----
        // The bundle's eight (AX-9.1): Session holds Hide and Quit; Actions what axHelp does.
        var first = new ToolStripMenuItem();
        AddHideAndQuit(first, AxHelpKeys.Hide);
        var actions = new ToolStripMenuItem();
        Add(actions, new Command("&Open the conversation", AxHelpKeys.OpenConversation, () => OpenConversation(null)) { Mark = CommandMark.Act });
        // Ctrl+Enter, the bundle's main action (AX-8.10), is a second key of Open the conversation (the maintainer,
        // 2026-11-26): it sends nothing until you type and press Enter there.
        AddKey(new Command("Open the conversation", BundleKeys.MainAction, () => OpenConversation(null)) { Mark = CommandMark.Act });
        Add(actions, new Command("Put a picture &file into the conversation...", AxHelpKeys.PictureFile, ChoosePicture) { Mark = CommandMark.Act });
        Add(actions, new Command("Open the last &screenshot in axPreview", AxHelpKeys.OpenScreenshot, OpenLastScreenshot) { Mark = CommandMark.Open });
        actions.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Claude's &working folder", null, (_, _) => OpenAiWorkingFolder()), CommandMark.Open));

        var options = new ToolStripMenuItem();
        options.DropDownItems.Add(HotkeyItem());
        // HE-2.3: the conversation's own model, given at its start, so that Claude Code's saved default never changes it.
        var model = new ToolStripMenuItem("Conversation &model");
        _ = new ChoiceItems<string>(model, HelpSettings.Models.Select(choice => (choice.Model, choice.Label)), _settings.Model, chosen =>
        {
            _settings.Model = chosen;
            SaveSettings();
            Announce(FindConversation() != 0 ? $"{chosen}, from the conversation's next start: in its window, Session, End Claude and close this window, then the hotkey" : chosen, true);
        });
        options.DropDownItems.Add(model);
        // HE-2.7, HE-D9: off at first. On, Claude Code is loaded from sign-in, for a first press as quick as the later
        // ones; the item is all there is of it: no prompt, no reminder.
        options.DropDownItems.Add(Toggle("&Keep the conversation ready in the background", _settings.KeepConversationReady, v =>
        {
            _settings.KeepConversationReady = v;
            Announce(v
                ? "On: from axHelp's next start in the background, as at sign-in, Claude Code is started with it, may contact Anthropic then, and stays in memory"
                : "Off: Claude Code starts at the first press of the hotkey; closing the conversation still hides it", true);
        }, CommandMark.Act));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(StartWithWindowsItem()));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Claude &resources",
            [("&Claude Code", "https://code.claude.com/docs/en/overview")]);

        // The bundle's menus (AX-9.1), Window, axKit and Help, end the bar in every window.
        PlaceMenuBar(_menu, first, actions, new ToolStripMenuItem(), new ToolStripMenuItem(), options, help);
        MainMenuStrip = _menu;

        StatusLayout.Pair(_status);
        Controls.AddRange([Overlay, _text, _status, _menu]);
        ApplyTextFont();
        FillText();
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice() => _text.Visible = false;

    protected override void ShowAfterNotice() => _text.Visible = true;

    protected override Control FocusTarget => _text;

    protected override void ApplyFont(Font font) => _text.Font = font;

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Hotkey: {_settings.Key}, {(HotkeyRegistered ? "registered" : "not registered")}",
        $"Screenshot hotkey: {_settings.ScreenshotKey}, {(SecondHotkeyRegistered ? "registered" : "not registered")}",
        $"Conversation open: {(FindConversation() != 0 ? "yes" : "no")}",
    ];

    // ---- HotkeyForm: no list of matches here; the window is one text ----

    protected override int MatchCount => 0;

    protected override string SpokenMatch(int index) => string.Empty;

    protected override string NothingSaid => string.Empty;

    protected override void Refill()
    {
    }

    protected override void PrepareShowing(Rectangle area, bool keepText)
    {
        FillText();
        var width = Math.Max(MinimumSize.Width, area.Width / 3);
        var height = Math.Max(MinimumSize.Height, area.Height / 2);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    protected override void AfterShowing()
    {
        if (!NoticeOpen)
        {
            _text.Select();
            _text.SelectionStart = 0;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // axDescribe's sign-in start, from before the merge (5.7.0), removed; its verb still starts axHelp (Dispatch).
        SignInStart.Apply("axDescribe", Dispatch.FormerDescribe, false);
        if (_picture is not null)
        {
            BeginInvoke(() => PutPicture(_picture));
        }
        else if (_startedHidden)
        {
            BeginInvoke(StartConversationHidden);
        }
    }

    /// <summary>
    /// axHelp ends (Quit axHelp, an update's close; never a hide): the conversation is asked to end with it, hidden
    /// or on view (HE-1.3), since with axHelp gone nothing would bring a hidden one back and its Claude Code would run
    /// on unseen. Asked, not made to: a conversation with something to lose (Claude at work, a message typed, a
    /// question waiting) comes up and asks its own question (axClaude FR-9.6), and axHelp ends all the same, since
    /// its own process holds nothing. A process started a moment ago whose window does not exist yet is ended itself.
    /// Not at Windows' own end, which closes the conversation's window itself.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || e.CloseReason == CloseReason.WindowsShutDown)
        {
            return;
        }

        var open = FindConversation();
        switch (Conversation.AtQuit(open != 0, ConversationProcessRuns))
        {
            case ConversationEnd.AskToEnd:
                Log.Info("The conversation asked to end with axHelp");
                AppMessage.SendTo(open, ConversationTitle, AppMessage.End, []);
                break;
            case ConversationEnd.EndProcess:
                Log.Info("The conversation's process, started and without a window yet, ended with axHelp");
                try
                {
                    _conversation?.Kill(entireProcessTree: true);
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    Log.Error("The conversation's process could not be ended: " + ex.Message);
                }

                break;
        }
    }

    /// <summary>
    /// The conversation started hidden as axHelp starts hidden (HE-2.7), so that Claude Code is loaded at the first
    /// press: only with the setting on, axClaude's agreement for Claude Code already given (read from the bundle's
    /// consent file, nothing shown) and Claude Code installed; and never a second conversation.
    /// </summary>
    private void StartConversationHidden()
    {
        var agreed = Consent.Given(AppPaths.ConsentFile, Dispatch.Of(Dispatch.AxClaude).Name, ConsentTarget.ClaudeCode.Program);
        var installed = ClaudeCodeProgram.Find(null) is not null;
        if (!HelpSettings.StartsConversationHidden(_startedHidden, _settings.KeepConversationReady, agreed, installed)
            || Conversation.Next(FindConversation() != 0, ConversationProcessRuns) != ConversationStep.Start)
        {
            Log.Info($"The conversation not started ahead: the setting {(_settings.KeepConversationReady ? "on" : "off")}, agreed {agreed}, Claude Code {(installed ? "found" : "not found")}");
            return;
        }

        if (!AiWorkingFolder(out var folder))
        {
            return;
        }

        PrepareWorkingFolder(folder);
        // HE-2.9: never a silent start in a folder with files Claude Code would obey and the user has not accepted;
        // the start waits for the user's next ask, which shows the notice.
        if (AiFolderCheck.NewOrChanged(folder, AppPaths.AiFolderFilesFile, WorkingFolderFiles.Obeyed) is { Count: > 0 } files)
        {
            Log.Info($"The conversation not started ahead: {files.Count} files in Claude's working folder wait for the user's word");
            return;
        }

        Log.Info("The conversation started hidden, to be ready for the hotkey");
        // A moment at which Claude Code runs and may contact Anthropic without the user's key: axHelp's own start in
        // the background (sign-in), only with the setting the user turned on (HE-2.7, HE-D9; PRIVACY.md names it).
        // The start itself is WindowMenu.StartKept's outside call; Claude Code's is the console window's.
        StartConversation(HelpSettings.ConversationArguments(folder, _settings.Model, null, hidden: true));
    }

    /// <summary>The conversation's hotkey (HE-2.1): the conversation, never this window.</summary>
    protected override bool OnHotkey()
    {
        OpenConversation(null);
        return true;
    }

    protected override Hotkey? SecondHotkey => _settings.ScreenshotKey;

    /// <summary>The screenshot's hotkey (HE-3.1): the window in front, at this moment, into the conversation.</summary>
    protected override void OnSecondHotkey() => TakeScreenshot(GetForegroundWindow());

    /// <summary>A second start (HE-1.2): "show" shows this window; "open" with a picture puts it into the conversation.</summary>
    protected override Action? TakeHandOver(HandOver message) =>
        message.Verb == AppMessage.Open && message.Paths.Count == 1 ? () => PutPicture(message.Paths[0]) : base.TakeHandOver(message);

    private void FillText() =>
        _text.Text = HelpText.Window(_settings.Key.ToString(), _settings.ScreenshotKey.ToString(), SecondHotkeyRegistered || !IsHandleCreated,
            AiSettings.Load(AiSettings.DefaultPath, out _).ResolvedWorkingFolder()).ReplaceLineEndings("\r\n");

    // ---- the conversation (HE-2) ----

    /// <summary>
    /// The conversation's window, on view or hidden by its close (HE-1.3): the window titled "axHelp" of another
    /// process of this very program file, found where a second start finds a hidden hotkey window
    /// (<see cref="HotkeyForm.FindRunning"/>; the Window menu's list holds visible windows only); 0 when none.
    /// </summary>
    private static nint FindConversation() => FindRunning(ConversationTitle);

    /// <summary>
    /// The conversation brought up (HE-2.1): the open one to the front, with <paramref name="message"/> put into its
    /// message field (axClaude's "message" hand-over); or a new axClaude window on Claude's working folder with
    /// <c>--title axHelp</c> and <c>--message</c>, which continues the folder's last conversation (axClaude FR-9.1).
    /// This window hides first, so that the conversation comes up in front. Before a start, the files in the folder
    /// that Claude Code would obey and the user has not accepted are asked about (HE-2.9, <see cref="AskAboutFiles"/>);
    /// <paramref name="accepted"/> is the user's Start anyway.
    /// </summary>
    private void OpenConversation(string? message, bool accepted = false)
    {
        var shown = Visible;
        if (Visible)
        {
            HideWindow(giveBack: false);
        }

        var open = FindConversation();
        var step = Conversation.Next(open != 0, ConversationProcessRuns);
        if (step == ConversationStep.HandOver)
        {
            // The window brings itself up, hidden or behind others, and puts the focus in its message field (axClaude
            // FR-9.6): "show", or the "message" with its text. The hand-over passes on this process's right to the front.
            Log.Info("The conversation brought up");
            var handed = message is null
                ? AppMessage.SendTo(open, ConversationTitle, AppMessage.Show, [])
                : AppMessage.SendTo(open, ConversationTitle, AppMessage.Message, [message]);
            if (!handed)
            {
                WindowMenu.Activate(open);
            }

            return;
        }

        if (step == ConversationStep.Wait)
        {
            // Started (the start in the background, or a first press) and its window not made yet: wait for that one
            // while its process lives, never a second conversation. Nothing is read or written while waiting.
            After(300, () => OpenConversation(message));
            return;
        }

        if (!AiWorkingFolder(out var folder))
        {
            return;
        }

        PrepareWorkingFolder(folder);
        if (!accepted && AiFolderCheck.NewOrChanged(folder, AppPaths.AiFolderFilesFile, WorkingFolderFiles.Obeyed) is { Count: > 0 } files)
        {
            AskAboutFiles(folder, files, message, shown);
            return;
        }

        Log.Info("The conversation started");
        StartConversation(HelpSettings.ConversationArguments(folder, _settings.Model, message));
        Do(GiveForegroundBackLaterAsync);
    }

    /// <summary>
    /// HE-2.9 (`ai-help.md` AH-7.8): before Claude Code starts in a folder with files it would obey that axKit did
    /// not write and the user has not accepted, this window comes up with the notice that names them. Open the
    /// folder leaves the notice open; Start anyway remembers the files as they were named and starts; Cancel, the
    /// answer to Enter and Escape, starts nothing, and the window goes back out of sight when it was.
    /// </summary>
    private void AskAboutFiles(string folder, IReadOnlyList<ObeyedFile> files, string? message, bool shown)
    {
        Log.Info($"The conversation waits: {files.Count} files in Claude's working folder that axKit did not write");
        ShowWindowInFront();
        ShowNotice(new Notice(AiFolderCheck.NoticeTitle, AiFolderCheck.NoticeText(files),
        [
            new OverlayChoice("&Open the folder", OpenAiWorkingFolder, StaysOpen: true),
            new OverlayChoice("&Start anyway and remember these files", () =>
            {
                AiFolderCheck.Accept(folder, AppPaths.AiFolderFilesFile, files);
                OpenConversation(message, accepted: true);
            }),
            new OverlayChoice("&Cancel", () =>
            {
                Announce("Not started", true);
                if (!shown)
                {
                    HideWindow();
                }
            }, IsDefault: true, IsCancel: true),
        ]));
    }

    /// <summary>The working folder's two files, written where missing before a start (HE-2.4); a failure is logged, the conversation starts all the same.</summary>
    private static void PrepareWorkingFolder(string folder)
    {
        try
        {
            foreach (var name in WorkingFolderFiles.WriteMissing(folder))
            {
                Log.Info($"Wrote {name} into Claude's working folder");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The working folder's files could not be written: " + ex.Message);
        }
    }

    // ---- screenshots and pictures (HE-3) ----

    /// <summary>
    /// The picture of <paramref name="window"/> (HE-3.1, HE-3.2): into the working folder's screenshots folder under the
    /// time, the oldest beyond <see cref="HelpSettings.ScreenshotsKept"/> deleted, the path on the clipboard, and the
    /// path into the conversation with the window named.
    /// </summary>
    private void TakeScreenshot(nint window)
    {
        if (!AiWorkingFolder(out var folder))
        {
            return;
        }

        var screenshots = Path.Combine(folder, "screenshots");
        var path = Path.Combine(screenshots, $"{DateTime.Now:yyyy-MM-dd HH.mm.ss}.png");
        if (ScreenPicture.Capture(window, path) is { } problem)
        {
            Log.Info("The screenshot could not be taken: " + problem);
            Announce("The screenshot could not be taken: " + problem, true);
            return;
        }

        Log.Info("Screenshot taken of the window in front");
        DeleteOldScreenshots(screenshots);
        CopyText(path, null);
        var (program, title) = WindowOwner.Describe(window);
        var named = string.IsNullOrWhiteSpace(title) ? program ?? "the window in front" : $"\"{title}\"";
        OpenConversation($"Screenshot of {named}: {path} ");
    }

    private static void DeleteOldScreenshots(string folder)
    {
        try
        {
            foreach (var old in new DirectoryInfo(folder).GetFiles("*.png").OrderByDescending(file => file.Name, StringComparer.Ordinal).Skip(HelpSettings.ScreenshotsKept))
            {
                old.Delete();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("An old screenshot could not be deleted: " + ex.Message);
        }
    }

    /// <summary>A picture file of the user's into the conversation (HE-3.3): refused, said, when it is not one of the kinds Claude reads.</summary>
    private void PutPicture(string path)
    {
        if (PictureFile.Refusal(path) is { } refusal)
        {
            Announce(refusal, true);
            return;
        }

        OpenConversation($"Picture {Path.GetFileName(path)}: {path} ");
    }

    /// <summary>Ctrl+D: the Windows file picker on the Pictures folder, then the chosen file into the conversation.</summary>
    private void ChoosePicture()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a picture to put into the conversation",
            Filter = PictureFile.Filter,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            CheckFileExists = true,
        };
        if (ShowSystemDialog(dialog) == DialogResult.OK)
        {
            PutPicture(dialog.FileName);
        }
    }

    /// <summary>Ctrl+O: the newest screenshot in axPreview, whose reader gives the text in it; the window hidden first.</summary>
    private void OpenLastScreenshot()
    {
        var folder = Path.Combine(AiSettings.Load(AiSettings.DefaultPath, out _).ResolvedWorkingFolder(), "screenshots");
        var last = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.png").OrderByDescending(file => file, StringComparer.Ordinal).FirstOrDefault() : null;
        if (last is null)
        {
            Refuse("No screenshot yet: the screenshot's hotkey takes one");
            return;
        }

        HideWindow(giveBack: false);
        WindowMenu.Start(Dispatch.AxPreview, last);
        Do(GiveForegroundBackLaterAsync);
    }
}

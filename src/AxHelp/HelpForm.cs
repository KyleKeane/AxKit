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
    private readonly ToolStripStatusLabel _stateLabel;
    private readonly AxKitTextBox _text = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        AccessibleName = "axHelp",
        TabIndex = 0,
    };

    private readonly string? _picture;

    /// <param name="picture">A picture file the start named (HE-3.3): put into the conversation once the window is made.</param>
    public HelpForm(bool hidden, HelpSettings settings, string? settingsError, string? picture)
        : base(AxHelpApp.Name, settings, HelpSettings.DefaultPath, settingsError, hidden || picture is not null)
    {
        _settings = settings;
        _picture = picture;
        // The window's own title differs from the conversation's, so that a second start never takes one for the other (HE-1.2).
        Text = AxHelpApp.WindowTitle;

        // ---- menus and keys (HE-6): one command list (docs/axKit/app-kit.md AK-2) ----
        var first = new ToolStripMenuItem("&Session");
        Add(first, new Command("&Open the conversation", AxHelpKeys.OpenConversation, () => OpenConversation(null)) { Mark = CommandMark.Open });
        Add(first, new Command("Put a picture &file into the conversation...", AxHelpKeys.PictureFile, ChoosePicture) { Mark = CommandMark.Open });
        Add(first, new Command("Open the last &screenshot in axPreview", AxHelpKeys.OpenScreenshot, OpenLastScreenshot) { Mark = CommandMark.Open });
        first.DropDownItems.Add(new ToolStripMenuItem("Claude's &working folder", null, (_, _) => OpenAiWorkingFolder()));
        AddHideAndQuit(first, AxHelpKeys.Hide);

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(HotkeyItem());
        // HE-2.3: the conversation's own model, given at its start, so that Claude Code's saved default never changes it.
        var model = new ToolStripMenuItem("Conversation &model");
        _ = new ChoiceItems<string>(model, HelpSettings.Models.Select(choice => (choice.Model, choice.Label)), _settings.Model, chosen =>
        {
            _settings.Model = chosen;
            SaveSettings();
            Announce(FindConversation() != 0 ? $"{chosen}, from the conversation's next start: close it and press the hotkey" : chosen, true);
        });
        options.DropDownItems.Add(model);
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(StartWithWindowsItem()));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Claude &resources",
            [("&Claude Code", "https://code.claude.com/docs/en/overview")]);

        // The bundle's menus (AX-9.1), Window, axKit and Help, end the bar in every window.
        _menu.Items.AddRange([first, options, CreateWindowMenu(), CreateAxKitMenu(), help]);
        MainMenuStrip = _menu;

        (_stateLabel, _) = StatusLayout.Pair(_status);
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

    /// <summary>The open conversation window: an axKit window titled "axHelp" that is not this one; 0 when none.</summary>
    private nint FindConversation()
    {
        var self = IsHandleCreated ? Handle : 0;
        return WindowMenu.OpenWindows().FirstOrDefault(window => window.Handle != self && window.Title == ConversationTitle)?.Handle ?? 0;
    }

    /// <summary>
    /// The conversation brought up (HE-2.1): the open one to the front, with <paramref name="message"/> put into its
    /// message field (axClaude's "message" hand-over); or a new axClaude window on Claude's working folder with
    /// <c>--title axHelp</c> and <c>--message</c>, which continues the folder's last conversation (axClaude FR-9.1).
    /// This window hides first, so that the conversation comes up in front.
    /// </summary>
    private void OpenConversation(string? message)
    {
        if (!AiWorkingFolder(out var folder))
        {
            return;
        }

        if (Visible)
        {
            HideWindow(giveBack: false);
        }

        if (FindConversation() is var open and not 0)
        {
            Log.Info("The conversation brought to the front");
            if (message is null || !AppMessage.SendTo(open, ConversationTitle, AppMessage.Message, [message]))
            {
                WindowMenu.Activate(open);
            }

            return;
        }

        Log.Info("The conversation started");
        PrepareWorkingFolder(folder);
        WindowMenu.StartWith(Dispatch.AxClaude, HelpSettings.ConversationArguments(folder, _settings.Model, message));
        Do(GiveForegroundBackLaterAsync);
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
            Announce("No screenshot yet: the screenshot's hotkey takes one", true);
            return;
        }

        HideWindow(giveBack: false);
        WindowMenu.Start(Dispatch.AxPreview, last);
        Do(GiveForegroundBackLaterAsync);
    }
}

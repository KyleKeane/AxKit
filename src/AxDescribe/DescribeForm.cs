using AxDescribe.Core;
using AxKit.Core;
using AxKit.Core.Ai;
using AxKit.Forms;

namespace AxDescribe;

/// <summary>
/// axDescribe's window (SPEC.md DE-2, DE-3): the bundle's background window (<see cref="HotkeyForm"/>, as axStart and
/// axClip). The hotkey takes a picture of the window in front before this one shows (<see cref="ScreenPicture"/>), or
/// a picture file of the user's is taken as it is (DE-2.7: a start with its path, Ctrl+D); either is asked about
/// through the window base's AI help (<c>AxKitForm.Ai.cs</c>, docs/axKit/ai-help.md), and the description shows as a
/// notice with a field for more. Under the field, the earlier descriptions, which never take the focus. Nothing of AI
/// help itself is here.
/// </summary>
public sealed class DescribeForm : HotkeyForm
{
    /// <summary>The one picture of a window, under the local AxKit folder beside the logs; written over by the next one, deleted when axDescribe ends (DE-2.2).</summary>
    public static readonly string PicturePath = Path.Combine(Path.GetDirectoryName(AppPaths.LogsFolder)!, "axDescribe", "last.png");

    /// <summary>
    /// What a showing asks Claude about: the picture's file, and the window's program and title (DE-2.1), or for a
    /// picture file of the user's (DE-2.7) its name under <see cref="PictureFile.Program"/>.
    /// </summary>
    private sealed record Picture(string File, string? Program, string? Title)
    {
        public static Picture OfFile(string path) => new(path, PictureFile.Program, Path.GetFileName(path));

        /// <summary>A file of the user's, kept by its path, against the one picture axDescribe takes.</summary>
        public bool IsFile => !string.Equals(File, PicturePath, StringComparison.OrdinalIgnoreCase);
    }

    private readonly DescribeSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private IReadOnlyList<Description> _matches = [];
    private Picture? _pending;
    private Picture? _file;

    /// <param name="picture">A picture file the start named (DE-2.7): described at the first showing in place of the window in front.</param>
    public DescribeForm(bool hidden, DescribeSettings settings, string? settingsError, string? picture)
        : base(AxDescribeApp.Name, settings, DescribeSettings.DefaultPath, settingsError, hidden)
    {
        _settings = settings;
        _file = picture is null ? null : Picture.OfFile(picture);

        // ---- menus and keys (DE-6): one command list (docs/axKit/app-kit.md AK-2), the match keys first ----
        AddMatchKeys(AxDescribeKeys.NextMatch, AxDescribeKeys.PreviousMatch, AxDescribeKeys.FirstMatch, AxDescribeKeys.LastMatch);
        var first = new ToolStripMenuItem("ax&Describe");
        Add(first, new Command("&Ask about the picture", AxDescribeKeys.Ask, Ask) { Mark = CommandMark.Act });
        Add(first, new Command("Describe a picture &file...", AxDescribeKeys.DescribeFile, ChooseFile) { Mark = CommandMark.Open });
        Add(first, new Command("&Open the picture in axPreview", AxDescribeKeys.OpenPicture, OpenPicture) { Mark = CommandMark.Open });
        first.DropDownItems.Add(new ToolStripMenuItem("Forget the &descriptions", null, (_, _) => ForgetDescriptions()));
        AddHideAndQuit(first, AxDescribeKeys.Hide);

        // DE-3.3: no Say the first match item; nothing is said while a question is typed (the file's key turns it on).
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(HotkeyItem());
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(StartWithWindowsItem()));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Claude &resources",
            [("&Claude Code", "https://code.claude.com/docs/en/overview")]);

        // The bundle's axKit menu (AX-9) sits before Help in every window.
        _menu.Items.AddRange([first, options, CreateAxKitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the field and the earlier descriptions (DE-2.3, DE-3): HotkeyForm's, named here ----
        SearchField.AccessibleName = "Ask about the picture";
        Matches.AccessibleName = "Earlier descriptions, 0";
        Matches.DoubleClick += (_, _) => Ask();
        // DE-2.3: Tab reaches the list, where NVDA reads each description itself; Enter there shows it again.
        MatchesTakeFocus = true;

        // ---- the status bar (DE-2.4) ----
        (_stateLabel, _) = StatusLayout.Pair(_status);

        Controls.AddRange([Overlay, Matches, SearchField, _status, _menu]);
        ApplyTextFont();
        Fill();
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        SearchField.Visible = false;
        Matches.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        SearchField.Visible = true;
        Matches.Visible = true;
    }

    protected override Control FocusTarget => SearchField;

    protected override void ApplyFont(Font font)
    {
        SearchField.Font = font;
        Matches.Font = font;
    }

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Descriptions kept: {_settings.Descriptions.Count}",
        $"Picture: {(File.Exists(PicturePath) ? PicturePath : "none")}",
        $"Hotkey: {_settings.Key}, {(HotkeyRegistered ? "registered" : "not registered")}",
    ];

    /// <summary>
    /// Before the window shows (DE-2.1): the picture of the window in front, which is still in front at this moment,
    /// taken into the one file, unless a picture file of the user's waits (DE-2.7); then the field emptied and the
    /// window centred on that screen (axStart AS-3.4).
    /// </summary>
    protected override void PrepareShowing(Rectangle area, bool keepText)
    {
        if (!NoticeOpen && !keepText)
        {
            SearchField.Clear();
            _pending = _file ?? TakePicture();
            _file = null;
        }

        Fill();
        var width = Math.Max(MinimumSize.Width, area.Width / 3);
        var height = Math.Max(MinimumSize.Height, Height - ClientSize.Height + _menu.Height + SearchField.Height + Matches.ItemHeight * 12 + _status.Height + 4);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    /// <summary>The window in front: the picture taken, Claude asked (DE-2.1); the caret in the field.</summary>
    protected override void AfterShowing()
    {
        if (!NoticeOpen)
        {
            SearchField.Select();
        }

        if (_pending is { } picture)
        {
            _pending = null;
            Describe(picture);
        }
    }

    /// <summary>A second start's "open" with a picture file (DE-2.7), through the base; "show" is the base's.</summary>
    protected override Action? TakeHandOver(HandOver message) =>
        message.Verb == AppMessage.Open && message.Paths.Count == 1 ? () => DescribeFile(message.Paths[0]) : base.TakeHandOver(message);

    private Picture? TakePicture()
    {
        var window = ForegroundBefore;
        if (ScreenPicture.Capture(window, PicturePath) is { } problem)
        {
            // Said, and logged as information: a desktop without a screen to copy (the automation's) is not a fault of the window's.
            Log.Info("The picture could not be taken: " + problem);
            Announce("The picture could not be taken: " + problem, true);
            return null;
        }

        Log.Info("Picture taken of the window in front");
        var (program, title) = WindowOwner.Describe(window);
        return new Picture(PicturePath, program, title);
    }

    /// <summary>Claude asked about the picture (DE-2.1, DE-2.7), its folder the one folder of the call (AH-2.3); the answer remembered and shown.</summary>
    private void Describe(Picture picture) =>
        AskClaude(picture.IsFile ? AiPrompts.Picture(picture.File) : AiPrompts.Screen(picture.File, picture.Program, picture.Title),
            [FolderOf(picture.File)], null, answer => Described(picture, answer));

    private void Described(Picture picture, AiAnswer answer)
    {
        if (!answer.IsError)
        {
            _settings.Remember(picture.Title ?? string.Empty, picture.Program, answer.Text, answer.SessionId, DateTime.Now, picture.IsFile ? picture.File : null);
            SaveSettings();
            Fill();
        }

        ShowAnswer(answer, [FolderOf(picture.File)]);
    }

    private static string FolderOf(string file) => Path.GetDirectoryName(file) ?? file;

    // ---- a picture file of the user's (DE-2.7) ----

    /// <summary>Ctrl+D: the Windows file picker on the Pictures folder, then the chosen file described.</summary>
    private void ChooseFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a picture for Claude to describe",
            Filter = PictureFile.Filter,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            CheckFileExists = true,
        };
        if (ShowSystemDialog(dialog) == DialogResult.OK)
        {
            DescribeFile(dialog.FileName);
        }
    }

    /// <summary>
    /// A picture file described: from the picker, or handed over by a start with its path (which refused what is not a
    /// picture before sending). The window in front asks at once; a hidden one shows first, the file in place of the
    /// picture it would take.
    /// </summary>
    private void DescribeFile(string path)
    {
        if (PictureFile.Refusal(path) is { } refusal)
        {
            Announce(refusal, true);
            return;
        }

        var picture = Picture.OfFile(path);
        if (Visible && ContainsFocus)
        {
            Describe(picture);
            return;
        }

        _file = picture;
        ShowWindowInFront();
    }

    // ---- the earlier descriptions (DE-3) ----

    protected override int MatchCount => _matches.Count;

    protected override string SpokenMatch(int index) => DescribeLines.Spoken(_matches[index], index, _matches.Count);

    protected override string NothingSaid => Matches.Items.Count > 0 ? Matches.Items[0].ToString() ?? string.Empty : DescribeLines.None;

    protected override void Refill() => Fill();

    private void Fill()
    {
        _matches = DescribeLines.Matches(_settings.Descriptions, SearchField.Text);
        ShowMatches(_matches.Select(DescribeLines.Label).ToList(),
            _settings.Descriptions.Count == 0 ? DescribeLines.None : DescribeLines.NoMatch(SearchField.Text),
            $"Earlier descriptions, {_matches.Count}", 0);
        _stateLabel.Text = AiSettings.Load(AiSettings.DefaultPath, out _).Enabled
            ? $"{_settings.Descriptions.Count} {(_settings.Descriptions.Count == 1 ? "description" : "descriptions")} kept"
            : "AI help is off: axKit menu, AI help";
    }

    private Description? Selected => Matches.SelectedIndex >= 0 && Matches.SelectedIndex < _matches.Count ? _matches[Matches.SelectedIndex] : null;

    /// <summary>
    /// Enter (DE-2.3): the typed question about the selected description's picture, in its session, or about the
    /// newest when the typed words match no earlier description (the field is the filter too); with nothing typed,
    /// the selected description again. The call's folder is the picture's: a described file's (DE-2.7) or the one picture's.
    /// </summary>
    private void Ask()
    {
        var question = SearchField.Text.Trim();
        var description = Selected ?? (question.Length > 0 ? _settings.Descriptions.FirstOrDefault() : null);
        if (description is null)
        {
            Announce(NothingSaid, true);
            return;
        }

        var folder = FolderOf(description.Picture ?? PicturePath);
        if (question.Length == 0)
        {
            ShowAnswer(new AiAnswer(description.Answer, false, description.SessionId), [folder]);
            return;
        }

        AskClaude(AiPrompts.FollowUp(question), [folder], description.SessionId, answer =>
        {
            SearchField.Clear();
            Fill();
            ShowAnswer(answer, [folder]);
        });
    }

    /// <summary>Ctrl+O (DE-2.5): the last picture in axPreview, whose reader gives the text in it; the window hidden first so that axPreview comes up in front.</summary>
    private void OpenPicture()
    {
        if (!File.Exists(PicturePath))
        {
            Announce("No picture yet: the hotkey takes one", true);
            return;
        }

        Log.Info("Hidden: the picture opened in axPreview");
        HideWindow(giveBack: false);
        AxKitMenu.Start(Dispatch.AxPreview, PicturePath);
        Do(GiveForegroundBackLaterAsync);
    }

    private void ForgetDescriptions()
    {
        if (_settings.Descriptions.Count == 0)
        {
            Announce("No descriptions kept", true);
            return;
        }

        ShowNotice(Notice.Ask("Forget the descriptions?", $"The {_settings.Descriptions.Count} descriptions axDescribe keeps are deleted from this computer, and the last picture with them; your own picture files stay; nothing changes at Anthropic.", "&Forget", () =>
        {
            _settings.Descriptions.Clear();
            SaveSettings();
            DeletePicture();
            Fill();
            Announce("Descriptions forgotten", true);
        }, "&Keep them"));
    }

    /// <summary>The picture goes with the process (DE-2.2): a hide keeps it for Ctrl+O and the follow-ups; Quit, the update and Windows' end delete it.</summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DeletePicture();
        base.OnFormClosed(e);
    }

    private static void DeletePicture()
    {
        try
        {
            if (File.Exists(PicturePath))
            {
                File.Delete(PicturePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The picture could not be deleted: " + ex.Message);
        }
    }
}

using AxDescribe.Core;
using AxKit.Core;
using AxKit.Core.Ai;
using AxKit.Forms;

namespace AxDescribe;

/// <summary>
/// axDescribe's window (SPEC.md DE-2, DE-3): the bundle's background window (<see cref="HotkeyForm"/>, as axStart and
/// axClip). The hotkey takes a picture of the window in front before this one shows (<see cref="ScreenPicture"/>),
/// asks Claude to describe it through the window base's AI help (<c>AxKitForm.Ai.cs</c>, docs/axkit/ai-help.md), and
/// shows the description as a notice with a field for more. Under the field, the earlier descriptions, which never
/// take the focus. Nothing of AI help itself is here.
/// </summary>
public sealed class DescribeForm : HotkeyForm
{
    /// <summary>The one picture, under the local AxKit folder beside the logs; written over by the next one, deleted when axDescribe ends (DE-2.2).</summary>
    public static readonly string PicturePath = Path.Combine(Path.GetDirectoryName(AppPaths.LogsFolder)!, "axdescribe", "last.png");

    private readonly DescribeSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private IReadOnlyList<Description> _matches = [];
    private (string? Program, string? Title)? _pending;

    public DescribeForm(bool hidden, DescribeSettings settings, string? settingsError)
        : base(AxDescribeApp.Name, settings, DescribeSettings.DefaultPath, settingsError, hidden)
    {
        _settings = settings;

        // ---- menus and keys (DE-6): one command list (docs/axkit/app-kit.md AK-2), the match keys first ----
        AddMatchKeys(AxDescribeKeys.NextMatch, AxDescribeKeys.PreviousMatch, AxDescribeKeys.FirstMatch, AxDescribeKeys.LastMatch);
        var first = new ToolStripMenuItem("ax&Describe");
        Add(first, new Command("&Ask about the picture", AxDescribeKeys.Ask, Ask) { Mark = CommandMark.Act });
        Add(first, new Command("&Open the picture in axPreview", AxDescribeKeys.OpenPicture, OpenPicture) { Mark = CommandMark.Open });
        first.DropDownItems.Add(new ToolStripMenuItem("Forget the &descriptions", null, (_, _) => ForgetDescriptions()));
        AddHideAndQuit(first, AxDescribeKeys.Hide);

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(HotkeyItem());
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(StartWithWindowsItem(), SayFirstMatchItem()));
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
    /// taken into the one file; then the field emptied and the window centred on that screen (axStart AS-3.4).
    /// </summary>
    protected override void PrepareShowing(Rectangle area, bool keepText)
    {
        if (!NoticeOpen && !keepText)
        {
            SearchField.Clear();
            _pending = TakePicture();
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
            AskClaude(AiPrompts.Screen(PicturePath, picture.Program, picture.Title), [Path.GetDirectoryName(PicturePath)!], null, answer => Described(picture, answer));
        }
    }

    private (string? Program, string? Title)? TakePicture()
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
        return WindowOwner.Describe(window);
    }

    private void Described((string? Program, string? Title) picture, AiAnswer answer)
    {
        if (!answer.IsError)
        {
            _settings.Remember(picture.Title ?? string.Empty, picture.Program, answer.Text, answer.SessionId, DateTime.Now);
            SaveSettings();
            Fill();
        }

        ShowAnswer(answer, [Path.GetDirectoryName(PicturePath)!]);
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
    /// the selected description again.
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

        if (question.Length == 0)
        {
            ShowAnswer(new AiAnswer(description.Answer, false, description.SessionId), [Path.GetDirectoryName(PicturePath)!]);
            return;
        }

        AskClaude(AiPrompts.FollowUp(question), [Path.GetDirectoryName(PicturePath)!], description.SessionId, answer =>
        {
            SearchField.Clear();
            Fill();
            ShowAnswer(answer, [Path.GetDirectoryName(PicturePath)!]);
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

        ShowNotice(Notice.Ask("Forget the descriptions?", $"The {_settings.Descriptions.Count} descriptions axDescribe keeps are deleted from this computer, and the last picture with them; nothing changes at Anthropic.", "&Forget", () =>
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

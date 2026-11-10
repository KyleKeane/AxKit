using AxHelp.Core;
using AxKit.Core;
using AxKit.Core.Ai;
using AxKit.Forms;

namespace AxHelp;

/// <summary>
/// axHelp's window (SPEC.md HE-2, HE-3): the bundle's background window (<see cref="HotkeyForm"/>, as axStart and
/// axClip), one field with the caret always in it and the list of earlier questions under it, which never takes the
/// focus. Enter asks Claude what was typed, through the window base's AI help (<c>AxKitForm.Ai.cs</c>,
/// docs/axKit/ai-help.md): the waiting notice, then the answer as a notice with a field for more. The program that
/// was in front when the hotkey was pressed is named in the prompt (HE-2.4). Nothing of AI help itself is here.
/// </summary>
public sealed class HelpForm : HotkeyForm
{
    private readonly HelpSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private IReadOnlyList<HelpExchange> _matches = [];

    public HelpForm(bool hidden, HelpSettings settings, string? settingsError)
        : base(AxHelpApp.Name, settings, HelpSettings.DefaultPath, settingsError, hidden)
    {
        _settings = settings;

        // ---- menus and keys (HE-6): one command list (docs/axKit/app-kit.md AK-2), the match keys first ----
        AddMatchKeys(AxHelpKeys.NextMatch, AxHelpKeys.PreviousMatch, AxHelpKeys.FirstMatch, AxHelpKeys.LastMatch);
        var first = new ToolStripMenuItem("ax&Help");
        Add(first, new Command("&Ask", AxHelpKeys.Ask, Ask) { Mark = CommandMark.Act });
        first.DropDownItems.Add(new ToolStripMenuItem("&Folders Claude may read...", null, (_, _) => ShowAiFolders()));
        first.DropDownItems.Add(new ToolStripMenuItem("Claude's &working folder", null, (_, _) => OpenWorkingFolder()));
        first.DropDownItems.Add(new ToolStripMenuItem("Forget the &questions", null, (_, _) => ForgetQuestions()));
        AddHideAndQuit(first, AxHelpKeys.Hide);

        // HE-3.3: no Say the first match item; nothing is said while a question is typed (the file's key turns it on).
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

        // ---- the field and the earlier questions (HE-2.1, HE-3): HotkeyForm's, named here ----
        SearchField.AccessibleName = "Ask Claude";
        Matches.AccessibleName = "Earlier questions, 0";
        Matches.DoubleClick += (_, _) => Ask();
        // HE-2.1: Tab reaches the list, where NVDA reads each question itself; Enter there shows its answer.
        MatchesTakeFocus = true;

        // ---- the status bar (HE-2.2): whether AI help is on, and how many questions are kept ----
        (_stateLabel, _) = StatusLayout.Pair(_status);

        // Docked from the last control to the first: the menu at the top, the status bar at the bottom, the field
        // under the menu, and the list (or the notice over both) what is left.
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
        $"Questions kept: {_settings.Exchanges.Count}",
        $"Hotkey: {_settings.Key}, {(HotkeyRegistered ? "registered" : "not registered")}",
    ];

    /// <summary>The field emptied (unless a start failed) and the window centred on the screen of the program that had the focus (axStart AS-3.4).</summary>
    protected override void PrepareShowing(Rectangle area, bool keepText)
    {
        if (!NoticeOpen && !keepText)
        {
            SearchField.Clear();
        }

        Fill();
        var width = Math.Max(MinimumSize.Width, area.Width / 3);
        var height = Math.Max(MinimumSize.Height, Height - ClientSize.Height + _menu.Height + SearchField.Height + Matches.ItemHeight * 12 + _status.Height + 4);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    protected override void AfterShowing()
    {
        if (!NoticeOpen)
        {
            SearchField.Select();
        }
    }

    // ---- the earlier questions (HE-3) ----

    protected override int MatchCount => _matches.Count;

    protected override string SpokenMatch(int index) => HelpLines.Spoken(_matches[index], index, _matches.Count);

    protected override string NothingSaid => Matches.Items.Count > 0 ? Matches.Items[0].ToString() ?? string.Empty : HelpLines.None;

    protected override void Refill() => Fill();

    private void Fill()
    {
        _matches = HelpLines.Matches(_settings.Exchanges, SearchField.Text);
        ShowMatches(_matches.Select(exchange => exchange.Question).ToList(),
            _settings.Exchanges.Count == 0 ? HelpLines.None : HelpLines.NoMatch(SearchField.Text),
            $"Earlier questions, {_matches.Count}", 0);
        _stateLabel.Text = AiSettings.Load(AiSettings.DefaultPath, out _).Enabled
            ? $"{_settings.Exchanges.Count} {(_settings.Exchanges.Count == 1 ? "question" : "questions")} kept"
            : "AI help is off: axKit menu, AI help";
    }

    /// <summary>Enter (HE-2.3): the typed question to Claude, with the program that was in front; with nothing typed, the selected earlier answer again.</summary>
    private void Ask()
    {
        var question = SearchField.Text.Trim();
        if (question.Length == 0)
        {
            if (Matches.SelectedIndex >= 0 && Matches.SelectedIndex < _matches.Count)
            {
                var earlier = _matches[Matches.SelectedIndex];
                ShowAnswer(new AiAnswer(earlier.Answer, false, earlier.SessionId), []);
                return;
            }

            Announce(_settings.Exchanges.Count == 0 ? "Type a question first" : NothingSaid, true);
            return;
        }

        var (program, title) = WindowOwner.Describe(ForegroundBefore);
        AskClaude(AiPrompts.Anywhere(program, title, question), [], null, answer => Answered(question, answer));
    }

    private void Answered(string question, AiAnswer answer)
    {
        if (!answer.IsError)
        {
            _settings.Remember(question, answer.Text, answer.SessionId, DateTime.Now);
            SaveSettings();
            SearchField.Clear();
            Fill();
        }

        ShowAnswer(answer, []);
    }

    /// <summary>axHelp, Claude's working folder (ai-help.md AH-2.1): the folder Claude Code starts in, opened in axFile; the window hidden first so that axFile comes up in front.</summary>
    private void OpenWorkingFolder()
    {
        if (!AiWorkingFolder(out var folder))
        {
            return;
        }

        Log.Info("Hidden: Claude's working folder opened in axFile");
        HideWindow(giveBack: false);
        AxKitMenu.Start(Dispatch.AxFile, folder);
        Do(GiveForegroundBackLaterAsync);
    }

    private void ForgetQuestions()
    {
        if (_settings.Exchanges.Count == 0)
        {
            Announce("No questions kept", true);
            return;
        }

        ShowNotice(Notice.Ask("Forget the questions?", $"The {_settings.Exchanges.Count} questions and answers axHelp keeps are deleted from this computer; nothing changes at Anthropic.", "&Forget", () =>
        {
            _settings.Exchanges.Clear();
            SaveSettings();
            Fill();
            Announce("Questions forgotten", true);
        }, "&Keep them"));
    }
}

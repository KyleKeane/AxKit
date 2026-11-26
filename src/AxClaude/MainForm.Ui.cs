using AxClaude.Core;
using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using AxKit.Core;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the menus (FR-10): the controls are the console window's ----

    private void BuildMenus()
    {
        // The menus (FR-10): short, grouped by what the items are about, every mnemonic unique within its menu; each
        // item with a key is a command of the window's one list (docs/axKit/app-kit.md AK-2), its key from the table.
        // Session is the thing worked on, as axDown's File is: the session, where Claude runs, what the window holds.
        var session = new ToolStripMenuItem("&Session");
        Add(session, new Command("&New session...", AxClaudeKeys.NewSession, NewSession) { Mark = CommandMark.Act });
        session.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Open folder...", null, (_, _) => OpenFolder()), CommandMark.Act));
        // FR-8.3, FR-8.5: Open replaces this window's session; in new window leaves it running.
        session.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Open folder in new &window...", null, (_, _) => OpenFolderInNewWindow()), CommandMark.Act));
        session.DropDownItems.Add(RecentFoldersItem);
        session.DropDownItems.Add(RecentFoldersInNewWindowItem);
        // The standard order of a File menu (bundle AX-6): the editors have Save and Save as here; axClaude has none.
        session.DropDownItems.Add(new ToolStripSeparator());
        // FR-8.2: Show current folder in ▸ File Explorer and the other apps' windows on the project folder.
        var explorer = new Command("File &Explorer", Keys.None, ShowInExplorer) { Mark = CommandMark.Open };
        session.DropDownItems.Add(CreateShowInMenu("Show current folder &in", Add(explorer), FolderOrSay, FolderMissing));
        session.DropDownItems.Add(new ToolStripSeparator());
        // Not a save: the conversation is Claude's own record, and this writes a text copy of it. At the bottom, where
        // the editors have Save and Save as, so that the first menu of every window keeps the same order (bundle AX-6).
        session.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Export conversation...", null, (_, _) => ExportConversation()) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Save) }, CommandMark.Change));
        session.DropDownItems.Add(new ToolStripSeparator());
        if (_options.HideOnClose)
        {
            // FR-9.6: Alt+F4 puts this window away and Claude runs on; ending it is a command of its own.
            session.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Hide this window", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" }, CommandMark.Change));
            session.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("En&d Claude and close this window", null, (_, _) => EndForGood()), CommandMark.Act));
        }
        else
        {
            session.DropDownItems.Add(CreateExitItem());
        }

        // What goes to Claude itself (FR-11); the raw sequences are fixed here and listed in Appendix B.
        // "Claude Code" since 5.7.0: the bundle's axKit menu holds a Claude submenu (AI help, AX-9.1); this menu is the program.
        var claude = new ToolStripMenuItem();
        // The process first (FR-1.8), then the turn.
        Add(claude, new Command("Force Claude to res&tart", AxClaudeKeys.RestartClaude, RestartProgram) { Mark = CommandMark.Act });
        claude.DropDownItems.Add(new ToolStripSeparator());
        claude.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Send &message", null, (_, _) => SendInput()) { ShortcutKeyDisplayString = "Enter in the message field" }, CommandMark.Act));
        // Ctrl+Enter, the bundle's main action (AX-8.10 rule 3), sends the message from the field as Enter does: the
        // console window's command, named by SendFieldName.
        Add(claude, new Command("Send &queued messages now", AxClaudeKeys.SendNow, SendNow) { Mark = CommandMark.Act });
        // The interrupt key (FR-2.3). Ctrl+Escape opens the Start menu and never reaches the app.
        Add(claude, new Command("&Interrupt", AxClaudeKeys.Interrupt, Interrupt) { Mark = CommandMark.Act });
        Add(claude, new Command("Next &permission mode", AxClaudeKeys.NextPermissionMode, NextPermissionMode) { Mark = CommandMark.Act });
        // FR-14.3: Choose the model, Choose the effort: Claude Code's own pickers, from ClaudeCodeSettings.
        AddClaudeCodeCommands(claude);
        // FR-7.10: on demand, the number of things running in the background and each agent by type and description.
        Add(claude, new Command("What runs in the &background", AxClaudeKeys.BackgroundWork, SayBackgroundWork) { Mark = CommandMark.Read });
        claude.DropDownItems.Add(new ToolStripSeparator());
        // The keys Claude's own menus take. Up, Down, Shift+Up and Shift+Down are menu items only (FR-2.4): Ctrl+Up and
        // Ctrl+Down move by paragraph in every edit control of the bundle, and Shift with an arrow selects text in the
        // message field. The sequences are ClaudeCodeKeys' (rule 12).
        var sendKey = new ToolStripMenuItem("Send &key");
        // Without a key since 2026-11-26 (Ctrl+Shift+C until then): Interrupt is Ctrl+I, and this stays in the palette.
        Add(sendKey, new Command("Ctrl+&C", Keys.None, () => SendKey(ClaudeCodeKeys.CtrlC)) { Mark = CommandMark.Act });
        sendKey.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Ctrl+&D", null, (_, _) => SendKey(ClaudeCodeKeys.CtrlD)), CommandMark.Act));
        sendKey.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Tab", null, (_, _) => SendKey(ClaudeCodeKeys.Tab)), CommandMark.Act));
        sendKey.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Up", null, (_, _) => SendKey(ClaudeCodeKeys.Up)), CommandMark.Act));
        sendKey.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Do&wn", null, (_, _) => SendKey(ClaudeCodeKeys.Down)), CommandMark.Act));
        sendKey.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Shift+Up", null, (_, _) => SendKey(ClaudeCodeKeys.ShiftUp)), CommandMark.Act));
        sendKey.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("S&hift+Down", null, (_, _) => SendKey(ClaudeCodeKeys.ShiftDown)), CommandMark.Act));
        claude.DropDownItems.Add(sendKey);

        var navigate = new ToolStripMenuItem();
        // The places on Alt+1, 2 and 3, each with its older Ctrl key as a further key of the same command (bundle
        // AX-8.14): the console window's, under axClaude's names for them.
        AddPlaceItems(navigate, "&Message field", "&Conversation", "&Latest response");
        Add(navigate, new Command("Claude's &question again", AxClaudeKeys.ReadQuestion, ReadQuestionAgain) { Mark = CommandMark.Read });
        navigate.DropDownItems.Add(new ToolStripSeparator());
        AddFindItems(navigate);
        navigate.DropDownItems.Add(new ToolStripSeparator());
        AddBookmarkItems(navigate);

        // The settings: a submenu for each kind, nothing outside one. Each check item is saved as it is toggled.
        var options = new ToolStripMenuItem();
        // Sounds, the console window's menu under axClaude's words. FR-7.9: the music starts or stops at once when
        // toggled during a turn; Ctrl+M anywhere flips the same item.
        options.DropDownItems.Add(CreateSoundsMenu("&Chimes: ready, sent, replying, done, question", "&Ticks for new lines and tool calls", "&Background music while Claude works", AxClaudeKeys.ToggleMusic));
        // FR-7.10: the sounds and the words of work in the background, and the pause between the recurring chords.
        options.DropDownItems.Add(CreateBackgroundMenu());

        // FR-7.4: the reply choices are one of three, shown checked like a radio group; the choice is announced, since
        // the menu closes on it. The tool: row of each call is spoken whatever the reply mode; it starts with what arrives next.
        var speech = new ToolStripMenuItem("S&peech");
        _replySpeechItems = new ChoiceItems<ReplySpeechMode>(speech, ReplySpeechChoices, _settings.ReplySpeech, SetReplySpeech);

        speech.DropDownItems.Add(Toggle("Speak each &tool call", _settings.SpeakToolCalls, v =>
        {
            _settings.SpeakToolCalls = v;
            _arrivals.Skip(_model.Lines);
        }));
        // FR-7.4a: the user's words that keep a reply line silent, one field, semicolons between them.
        speech.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Words that keep a reply line &silent...", null, (_, _) => EditSilentWords()), CommandMark.Change));
        options.DropDownItems.Add(speech);

        // FR-7.3a: each of the window's own status words, and Claude's bracketed notices, on or off.
        var announcements = new ToolStripMenuItem("&Announcements");
        announcements.DropDownItems.Add(Toggle("\"&Ready\" when Claude starts", _settings.AnnounceReady, v => _settings.AnnounceReady = v));
        // FR-7.3b: a turn Claude Code begins by itself, when work in the background ends, says so.
        announcements.DropDownItems.Add(Toggle("\"Claude goes on by &itself\" when a turn starts that you did not send", _settings.AnnounceSelfStarted, v => _settings.AnnounceSelfStarted = v));
        announcements.DropDownItems.Add(Toggle("\"&Sent\" and \"Queued\" after a message", _settings.AnnounceSent, v => _settings.AnnounceSent = v));
        announcements.DropDownItems.Add(Toggle("\"Res&ponding\" when a reply starts", _settings.AnnounceResponding, v => _settings.AnnounceResponding = v));
        announcements.DropDownItems.Add(Toggle("\"Question &closed\" when Claude closes one", _settings.AnnounceQuestionClosed, v => _settings.AnnounceQuestionClosed = v));
        announcements.DropDownItems.Add(Toggle("Claude's own &notices, such as \"accept edits on\"", _settings.AnnounceClaudeNotices, v => _settings.AnnounceClaudeNotices = v));
        options.DropDownItems.Add(announcements);

        // When Claude is done: the console window's menu; the word's switch is axClaude's own setting.
        options.DropDownItems.Add(CreateDoneMenu("When Claude is &done", _settings.AnnounceBell, v => _settings.AnnounceBell = v));

        // D33: with the list off, what Claude waits on is only announced, as before 1.4.0: no control area, no guard, no waiting screens.
        var questions = new ToolStripMenuItem("&Questions");
        questions.DropDownItems.Add(Toggle("&Answer from a list", _settings.QuestionNotices, v =>
        {
            _settings.QuestionNotices = v;
            _model.DetectScreens = v;
            if (!v)
            {
                CloseArea();
            }
        }));
        questions.DropDownItems.Add(Toggle("&Go to them at once, interrupting speech", _settings.InterruptForQuestions, v => _settings.InterruptForQuestions = v));
        // Act for the AI tools: the review is the user's own check of what goes to Claude (docs/axKit/repairs.json R-04).
        questions.DropDownItems.Add(Toggle("&Review answers before they go to Claude", _settings.ReviewAnswers, v => _settings.ReviewAnswers = v, CommandMark.Act));
        // D33, 2026-11-12: what Claude asks for itself, about itself, can be left unheard.
        questions.DropDownItems.Add(Toggle("&Dismiss Claude's feedback surveys unheard", _settings.DismissSurveys, v => _settings.DismissSurveys = v));
        questions.DropDownItems.Add(Toggle("&Open Claude's feedback drafts by themselves", _settings.OpenFeedbackDrafts, v => _settings.OpenFeedbackDrafts = v));
        options.DropDownItems.Add(questions);
        // FR-14: Claude Code's own settings, written into its user settings file at the user's choice; a submenu of
        // Options, since they are settings, and one level deep (bundle AX-9.1).
        options.DropDownItems.Add(CreateClaudeCodeSettingsMenu());

        options.DropDownItems.Add(CreateTextMenu(TranscriptTextItems()));
        options.DropDownItems.Add(CreateGeneralMenu(Toggle("&Keep recordings and exported conversations out of git", _settings.KeepFilesOutOfGit, v =>
        {
            _settings.KeepFilesOutOfGit = v;
            KeepFilesOutOfGit();
        }, CommandMark.Act))); // Writes into the project repository's exclude file: act for the AI tools (R-04).
        // What only a developer or a bug report needs (FR-12.3, FR-13.3, FR-9.2), in one place. Since 2026-11-15 also
        // where axClaude's events come from (docs/axClaude/events-and-grounding.md EG-9.1 to EG-9.3): the two sources
        // that can be switched off, and their numbers on demand.
        options.DropDownItems.Add(CreateDeveloperMenu(
            RecordItem,
            new ToolStripSeparator(),
            Toggle("Events from Claude Code's &hooks", _settings.ClaudeHooks, SetClaudeHooks, CommandMark.Act), // Changes what Claude Code is started with: on, it starts "AxKit.exe hook" for the session's events.
            Toggle("&Ground the conversation in Claude Code's session file", _settings.GroundInSessionFile, SetGroundInSessionFile, CommandMark.Act), // Decides whether a file of Claude Code's, the user's conversation, is read.
            Toggle("Feed the next window from Claude Code's &stream", AppSettings.IsStream(_settings.ClaudeSource), SetClaudeSource, CommandMark.Act), // Changes how Claude Code is started in the next axClaude window: as one process over pipes, which starts "AxKit.exe ask" for its questions.
            CommandPalette.Marked(new ToolStripMenuItem("Say the &event sources", null, (_, _) => SayEventSources()), CommandMark.Read)));

        var help = CreateHelpMenu(HelpText.UserGuide, "Claude Code &resources",
        [
            ("&Documentation", "https://code.claude.com/docs/en/overview"),
            ("Slash &commands", "https://code.claude.com/docs/en/commands"),
            ("&Keys", "https://code.claude.com/docs/en/interactive-mode"),
            ("Command &line", "https://code.claude.com/docs/en/cli-reference"),
            ("&Install or update Claude Code", ClaudeLauncher.InstallUrl),
        ]);

        // The bundle's menus (AX-9.1), Window, axKit and Help, end the bar in every window; Help ends with the update
        // items and About (AX-3.1, FR-1.10): Check for updates names the version in use and asks GitHub every time; Update
        // to axKit n stands below it while a newer release is known; About is the bundle's sections, as this window's notices.
        // The bundle's eight (AX-9.1): Context is the line of the conversation and the selection (AX-9.11).
        PlaceMenuBar(MenuBar, session, claude, navigate, CreateTranscriptContext("&Quote this line in the message"), options, help);
    }

    /// <summary>Options → Speech → Words that keep a reply line silent (FR-7.4a): one field, the words between semicolons; empty speaks every line again.</summary>
    private void EditSilentWords() =>
        AskText("Words that keep a reply line silent",
            "A reply line holding one of these words is shown in the conversation and not spoken. Case does not matter. Put a semicolon between them; leave the field empty to speak every line.",
            "&Words, separated by semicolons", "Words that keep a reply line silent", SpeechFilter.Text(_settings.SilentWords), "&Save", text =>
            {
                _settings.SilentWords = SpeechFilter.Words(text);
                SaveSettings();
                Announce(_settings.SilentWords.Count switch
                {
                    0 => "Every reply line is spoken",
                    1 => "1 word keeps a line silent",
                    var count => $"{count} words keep a line silent",
                }, false);
            }, required: false);

    /// <summary>Options → Speech → Speak replies: a mode (FR-7.4): speech starts with what arrives next.</summary>
    private void SetReplySpeech(ReplySpeechMode mode)
    {
        _settings.ReplySpeech = mode;
        _replySpeechItems?.Check(mode);
        _arrivals.Skip(_model.Lines);
        SaveSettings();
        Announce(MenuText.Plain(ReplySpeechChoices.First(c => c.Mode == mode).Text), true);
    }
}

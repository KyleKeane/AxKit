using AxClaude.Core;
using AxClaude.Core.Flow;
using AxClaude.Core.Pty;
using AxClaude.Core.Streaming;
using AxClaude.Core.Transcript;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Core.Transcript;
using AxKit.Forms;

namespace AxClaude;

/// <summary>
/// axClaude's window (SPEC.md §6): the bundle's console window (<see cref="ConsoleForm"/>: the conversation, the
/// message field, the control area, the status bar, the pty bridge, the paced writes, the recording, the export,
/// find, the signals and the music) around Claude Code in screen reader mode. What is Claude's stays here: the menus,
/// the attention and the question flow in the control area (D33), the project folder, New session, the restart with
/// --continue. One class in partial files, one per section (see src/AxClaude/CLAUDE.md).
/// </summary>
internal sealed partial class MainForm : ConsoleForm
{
    /// <summary>Spoken for Ctrl+O, which the app does not pass on (D14).</summary>
    private const string CtrlOGuardText = "Ctrl+O is off. For tool output, start with --verbose";

    /// <summary>The command line's options; its Claude path is cleared when it no longer exists and an install put Claude in its own place.</summary>
    private StartupOptions _options;
    private readonly AppSettings _settings;

    /// <summary>What the window reads of its source, whichever source feeds it (docs/axClaude/stream-mode.md SM-3.1): the conversation's lines and the facts, by one class.</summary>
    private readonly ClaudeModel _model;

    /// <summary>
    /// The same object as the screen's own model while the screen feeds the window, null while the stream does. For
    /// the machinery beside the screen alone (SM-3.6): the turn marks, the frame and the turn the event core is handed
    /// and hands back. Never asked by the window's own logic, which reads <see cref="_model"/>, so that it does not
    /// know which source feeds it.
    /// </summary>
    private readonly SessionModel? _screenModel;

    /// <summary>
    /// The same object as the stream's own model while Claude Code's stream feeds the window, null while the screen
    /// does (SM-9.2: a window is made for its source and keeps it). For the stream source alone
    /// (<c>MainForm.Stream.cs</c>): the lines it hands the model and what it asks of it beyond the base class. Never
    /// asked by the window's own logic.
    /// </summary>
    private readonly StreamModel? _streamModel;

    /// <summary>The settle after which what Claude waits on is acted on (FR-7.2, D33): started by the bell and by a change of what waits; <see cref="OnFrameSettled"/> on its tick.</summary>
    private readonly System.Windows.Forms.Timer _attention = new() { Interval = SettleMs };

    /// <summary>Reply speech waits this long for Claude's prompt row, which comes after a question's rows (D33); <see cref="SpeakHeld"/> on its tick.</summary>
    private readonly System.Windows.Forms.Timer _speechHold = new() { Interval = SpeechHoldMs };

    /// <summary>The reply lines waiting for <see cref="_speechHold"/>; a question's rows among them are dropped once Claude turns out to be waiting, the rest is spoken.</summary>
    private readonly List<Line> _heldLines = [];

    /// <summary>The per-frame pass behind the tick and the spoken replies (FR-7.8, FR-7.4).</summary>
    private readonly Arrivals _arrivals = new();
    private ChoiceItems<ReplySpeechMode>? _replySpeechItems;

    /// <summary>The Speak replies choices of Options → Speech in menu order (FR-7.4); the text without its mnemonic is what the choice announces.</summary>
    private static readonly (ReplySpeechMode Mode, string Text)[] ReplySpeechChoices =
    [
        (ReplySpeechMode.None, "Speak replies: &none"),
        (ReplySpeechMode.FirstLines, "Speak replies: &first lines"),
        (ReplySpeechMode.All, "Speak replies: &all lines"),
    ];

    private string? _folder;

    /// <summary>The bell rang (FR-7.2): "Done" or "Claude asks" once the screen has settled. While the core decides Done, only a bell the core found to be no turn's end (<see cref="FlowBell"/>).</summary>
    private bool _bellPending;

    /// <summary>
    /// When the bell last rang, in the system's milliseconds, while the Done it may bring has not been said; 0
    /// otherwise. FR-7.2a (the maintainer's choice of 2026-11-22: "Done" is heard first, then the reply): the bell
    /// comes just before the turn's last frame, which brings the last reply's rows, and those rows went to NVDA a
    /// quarter of a second later, before Done was decided, so that Done was heard only after the whole reply.
    /// </summary>
    private long _doneAwaitedSince;

    /// <summary>Whether reply lines still held wait for a Done (FR-7.2a): the bell rang less than <see cref="DoneFirstMs"/> ago and its Done has not been said.</summary>
    private bool DoneAwaited => _doneAwaitedSince != 0 && Environment.TickCount64 - _doneAwaitedSince < DoneFirstMs;

    /// <summary>The bell that is pending came in a run that has shown no mark (EG-6.5): the bell and the settle are all there is of a turn's end, and the settle says Done as before the core (<see cref="DoneBySettle"/>).</summary>
    private bool _bellNoMarks;

    /// <summary>Whether a feedback draft Claude queued in the turn that just ended waits (D33): the mode line's count against the count before the turn.</summary>
    private readonly FeedbackDraftWatch _drafts = new();

    /// <summary>The /config setting last chosen, where the list of settings starts when Claude's list comes back.</summary>
    private string? _settingKey;

    /// <summary>The question Claude asked last (shown or not), which the text prompt after Other names and whose step says what comes next.</summary>
    private Question? _askedQuestion;

    /// <summary>The slash command sent last, which the question that follows belongs to (QuestionRouter); null after a message.</summary>
    private SlashCommand? _lastCommand;

    /// <summary>Bundle AX-11.6: the line said when the user signs in to Anthropic through Claude Code (/login, or Claude's error asking for it).</summary>
    private const string SignInReminder = "You are signing in to Anthropic through Claude Code; the account and its terms are between you and Anthropic";

    /// <summary>A message kept out of a waiting question by the guard (FR-2.10, D33), sent after all when Enter is pressed again with the same text.</summary>
    private string? _keptMessage;
    private bool _readyAnnounced;
    private bool _messageSent;
    /// <summary>Claude's arguments after --ax-screen-reader, from the command line or the New session notice (FR-8.6); null is the default, --continue (FR-9.1). Restart keeps them.</summary>
    private List<string>? _claudeArgs;
    /// <summary>Claude found no conversation to continue in this folder: the default is no arguments until the folder changes (FR-9.1).</summary>
    private bool _nothingToContinue;
    /// <summary>The folder Claude was last started in: a restart there with --continue replays what the window shows (FR-4.8).</summary>
    private string? _launchedFolder;

    public MainForm(StartupOptions options, AppSettings settings, string? settingsError)
        : this(options, settings, settingsError, NewModel(options, settings))
    {
    }

    /// <summary>The model of the source this start names (SM-9.1): the stream's when the command line or the setting says so, else the screen's, sized as the hidden console.</summary>
    private static ClaudeModel NewModel(StartupOptions options, AppSettings settings) =>
        options.Streams(settings) ? new StreamModel() : new SessionModel(options.Columns ?? settings.PtyColumns, options.Rows ?? settings.PtyRows);

    /// <summary>The model is made first so that the base and this class both hold it: the base as a LineStore, this class as Claude's, and the source's own code as that source's own.</summary>
    private MainForm(StartupOptions options, AppSettings settings, string? settingsError, ClaudeModel model)
        : base("axClaude", settings, AppSettings.DefaultPath, settingsError, model, AxClaudeKeys.Transcript, "Conversation", "Message to Claude")
    {
        _options = options;
        _claudeArgs = options.ClaudeArgs is { } given ? [.. given] : null;
        _settings = settings;
        _model = model;
        _screenModel = model as SessionModel;
        _streamModel = model as StreamModel;
        _model.DetectScreens = settings.QuestionNotices;
        _model.ShowProgressRows = settings.ShowProgressRows;
        // The core (docs/axClaude/events-and-grounding.md EG-4): everything that happens reaches it from here on (MainForm.Flow.cs).
        _flow = new SessionFlow(_health);
        // FR-8.1: a remembered folder that is never a project (Windows' own, from a start before 2026-11-09) is not taken.
        // Bundle AX-9.5a: the remembered folder is looked at only on a drive of this computer.
        string? notOpened = null;
        _folder = options.Folder ?? (LocalPaths.AtStart(settings.LastProjectFolder, Directory.Exists, out notOpened) is { } last && !StartupOptions.IsNeverAProject(last) ? last : null);
        SayAtStart(notOpened);
        _model.ResponseStarted += () =>
        {
            // FR-7.3a: each of the window's own status words can be switched off in Options, Announcements.
            if (_settings.AnnounceResponding)
            {
                Announce("Responding", false);
            }

            if (_settings.SoundOnBell)
            {
                EventDoor.Raise(EventKind.Responding);
            }
        };
        _model.Announcement += text =>
        {
            if (_settings.AnnounceClaudeNotices)
            {
                Announce(text, false);
            }
        };
        _model.Bell += () =>
        {
            // FR-7.2a: a Done may follow this bell, and the reply lines not yet spoken wait for it (DoneAwaited).
            _doneAwaitedSince = Environment.TickCount64;
            if (!FlowDecides(FlowCharge.Done))
            {
                // The old rule (the setting eventCore set to shadow, a core that failed): every bell starts the
                // settle, which says "Done" or shows what Claude asks.
                _bellPending = true;
                _attention.Stop();
                _attention.Start();
            }

            // While the core decides Done the bell is only queued for it (EG-6.5): the core tells a turn's end, whose
            // Done it says itself, from a bell that is none, which it hands back to the settle (MainForm.Flow.cs).
            // The bell and the marks are raised inside the model's Feed, as a frame's end is: in the stream's order (EG-1.5).
            // The marks are the screen's own and are wired with the core (WireFlow).
            EnqueueFlow(new BellSeen());
        };
        _attention.Tick += (_, _) =>
        {
            _attention.Stop();
            if (_putAway)
            {
                // FR-9.6: a question that arrives while the window is put away waits, unspoken, until it is shown.
                SettledWhilePutAway();
                return;
            }

            OnFrameSettled();
        };
        _speechHold.Tick += (_, _) =>
        {
            // FR-7.2a: while a Done is awaited the held lines stay held, and the timer asks again on its next tick;
            // Done speaks them right after itself (FlowDone, SayDoneBySettle).
            if (!DoneAwaited)
            {
                SpeakHeld();
            }
        };
        WireBackground();
        // The source's own code, of which one part wires itself and the other does nothing (SM-3.6, SM-9.2): the
        // machinery beside the screen, or the stream source.
        WireFlow();
        // Claude Code's hooks (MainForm.Hooks.cs): its version is asked here already, so that the start does not wait for it.
        WireHooks();
        WireStream();
        Input.KeyDown += OnInputKeyDown;
        Transcript.LineChosen += QuoteLineInMessage;
        BuildMenus();
        // The raw recording is of the pseudo console's bytes (SM-6.5): in a window the stream feeds its item is there,
        // so that the menus are the same, and not available, since it would say "Recording started" and write nothing.
        RecordItem.Enabled = !Streams;
        SetStatus("Claude: starting", string.Empty);
        UpdateFolderUi();
        ApplyTextFont();
        RestoreWindow();
        if (_options.StartsHidden)
        {
            // FR-9.6: put away from the start, silent, and shown once without activation and without a pixel
            // (ShowWithoutActivation; a maximized window would be activated as it shows, so it starts at its normal
            // size and takes its own state when it is first brought up).
            _putAway = true;
            EventDoor.Muted = true;
            _stateWhenBroughtUp = WindowState;
            WindowState = FormWindowState.Normal;
            Opacity = 0;
        }
    }

    /// <summary>The saved window state of a hidden start, taken when the window is first brought up; null otherwise.</summary>
    private FormWindowState? _stateWhenBroughtUp;

    protected override void OnShown(EventArgs e)
    {
        if (_options.StartsHidden && _putAway)
        {
            // The hidden start (FR-9.6): hidden again before anything else, Claude started below as in any start.
            Hide();
            _front.GiveBack(Handle);
            Log.Info("Started hidden; Claude is loaded for the first showing");
        }

        // The base puts the focus in the field first: a notice shown by what follows (Claude not found) takes it from there and gives it back.
        base.OnShown(e);
        if (_options.RecordPath is { } record)
        {
            if (Streams)
            {
                // The recording is of the pseudo console's bytes, which a window the stream feeds has none of
                // (docs/axClaude/stream-mode.md SM-6.5): said, and no empty file made.
                _model.AddSystemLine("Nothing is recorded: the raw stream is the screen's, and this window is fed by Claude Code's stream.");
            }
            else
            {
                StartRecording(record);
            }
        }

        // --message (FR-9.2): the text in the message field, for the user to read and send; axHelp's screenshot (axTask's Open in axClaude on an issue until 2026-11-14).
        if (_options.Message is { Length: > 0 } message)
        {
            Input.Text = message.ReplaceLineEndings("\r\n");
            // The caret at the end, so that typing goes after the text (axHelp's screenshot path, then the question).
            Input.SelectionStart = Input.Text.Length;
        }

        if (_folder is null && PickFolder(null) is { } chosen)
        {
            SetFolder(chosen);
        }

        if (_folder is null)
        {
            _model.AddSystemLine("No project folder. Press Ctrl+N to choose one.");
        }
        else
        {
            StartClaude();
        }
    }

    /// <summary>FR-1.7: closing now would cut Claude off in the middle of a turn, or stop the work it runs in the background after the turn: the question first, inside the window (D23).</summary>
    /// <summary>
    /// A start's "message" (FR-9.2; axHelp's screenshot into its conversation, docs/axHelp/SPEC.md HE-3): the text
    /// added to the message field on a line of its own, not sent, the caret at its end and the window in front, so
    /// that the user types the question after it and presses Enter.
    /// </summary>
    /// <remarks>
    /// "show" (FR-9.6) brings the window up as it is, the focus in the message field; "end" asks a window that hides
    /// on closing to close, which it does at once only with nothing to lose (<see cref="EndRequested"/>): axHelp's
    /// hotkey and axHelp's Quit. The window base has checked that the sender is this very program file.
    /// </remarks>
    protected override Action? TakeHandOver(HandOver message) => message.Verb switch
    {
        AppMessage.Message when message.Paths.Count == 1 => () =>
        {
            _keeperLeft = false;
            Input.Text = Input.Text.Length == 0 ? message.Paths[0] : Input.Text.TrimEnd() + "\r\n" + message.Paths[0];
            Input.SelectionStart = Input.Text.Length;
            BringUp();
            // Said, since any program of the user's can send it (docs/axKit/repairs.json R-07): text that appears in
            // the field without a word could be sent with the next Enter unread.
            Announce("Text added to your message from another window, not sent", true);
        },
        AppMessage.Show => () =>
        {
            _keeperLeft = false;
            BringUp();
            SayUnsentMessage();
        },
        AppMessage.End when _options.HideOnClose => EndRequested,
        _ => null,
    };

    protected override bool AskBeforeClosing()
    {
        if (!ClaudeBusy && ClaudeBackground is null)
        {
            return false;
        }

        ConfirmClose();
        return true;
    }

    /// <summary>The Close axClaude? notice (FR-1.7): what Claude is doing, what closing does to it. Enter closes, Escape keeps the window.</summary>
    private void ConfirmClose()
    {
        var background = ClaudeBackground;
        string state;
        if (ClaudeBusy)
        {
            state = _model.PromptPending ? "Claude is waiting for your answer" : "Claude is still working";
            if (_model.QueuedMessages > 0)
            {
                state += _model.QueuedMessages == 1
                    ? " and a message you sent is still waiting for it"
                    : $" and {_model.QueuedMessages} messages you sent are still waiting for it";
            }

            if (background is not null)
            {
                state += $", and it has work running in the background ({background})";
            }
        }
        else
        {
            state = $"Claude has work running in the background ({background})";
        }

        var stops = ClaudeBusy ? "Claude stops in the middle of its work" : "the background work stops";
        var consequence = !RestartPending
            ? $"If you close now, {stops}."
            : $"If you close now, {stops} and the updated axClaude starts again.";
        ShowNotice(Notice.Ask("Close axClaude?",
            $"{state}. {consequence}\n" +
            "What is done so far is saved. Start axClaude again to carry on.",
            "&Close anyway", CloseConfirmed, "&Keep working", KeepWorkingAfterUpdate));
    }

    /// <summary>Claude is in the middle of a turn: working, waiting for an answer, or holding a message sent while it worked.</summary>
    private bool ClaudeBusy => ProcessRunning && (ClaudeWorks || _model.PromptPending || _model.QueuedMessages > 0);

    /// <summary>The work Claude runs in the background, which outlives the turn ("1 shell"); null when none runs or Claude is stopped.</summary>
    private string? ClaudeBackground => ProcessRunning ? _model.Background : null;

    /// <summary>
    /// The one key of the window's own that is no command: the Ctrl+O guard, a key sent to Claude only in one state of
    /// its screen. Every other key is a command of the menus (<c>MainForm.Ui.cs</c>, docs/axKit/app-kit.md AK-2), run by
    /// the base after this; the console window's read to the end runs before it.
    /// </summary>
    protected override bool ProcessConsoleKey(Keys keyData)
    {
        if (keyData != AxClaudeKeys.CtrlOGuard)
        {
            return false;
        }

        // D14: Claude's detailed view redraws the conversation in screen reader mode, which doubles the transcript and
        // speaks old replies again. The key is sent only to close the view should it be open.
        if (_model.TranscriptViewOpen)
        {
            Write(ClaudeCodeKeys.CtrlO);
        }
        else if (Streams)
        {
            // The stream has no detailed view (stream-mode.md SM-5.7).
            SayNotWithStream();
        }
        else
        {
            Announce(CtrlOGuardText, true);
        }

        return true;
    }

    // ---- what the window base and the console window ask for (bundle AX-6.5) ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>The music plays while Claude works and is not asking (FR-7.9).</summary>
    protected override bool Working => ClaudeWorks && !_model.PromptPending;

    /// <summary>Ctrl+S, the bundle's save key: the conversation exported as text (FR-3.8), since the window holds no document of its own.</summary>
    protected override void SaveCommand() => ExportConversation();

    /// <summary>How the new version starts after Update and restart: on this folder, and on the conversation when a message was sent (FR-9.1).</summary>
    protected override IReadOnlyList<string> RestartArguments()
    {
        var arguments = new List<string> { "claude" };
        if (_folder is not null)
        {
            arguments.Add(_folder);
            // A window another program keeps (axHelp's conversation, FR-9.2, FR-9.6) comes back as that window.
            if (_options.Title is { Length: > 0 } title)
            {
                arguments.Add("--title");
                arguments.Add(title);
            }

            if (_options.Model is { Length: > 0 } model)
            {
                arguments.Add("--model");
                arguments.Add(model);
            }

            if (_options.HideOnClose)
            {
                arguments.Add("--hide-on-close");
            }

            // A source the command line named (SM-9.1) is this window's, whatever the setting says by then.
            if (_options.Source is { } source)
            {
                arguments.Add("--source");
                arguments.Add(source);
            }

            if (_messageSent)
            {
                arguments.Add("--");
                arguments.Add("--continue");
            }
        }

        return arguments;
    }

    protected override string UpdateRestartText =>
        "closes axClaude, installs it and starts it again on the same folder; the conversation is picked up again if one is open";

    /// <summary>FR-13.3: the program, Claude, the folder, the console size and the state, after the base's version line and before its paths.</summary>
    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Program: {Environment.ProcessPath ?? "(unknown)"} (process {Environment.ProcessId})",
        $"Claude: {ClaudeCodeProgram.Find(_options.ClaudePath ?? _settings.ClaudePath) ?? "(not found)"}",
        $"Claude Code version: {_claudeVersion ?? "(not asked)"}",
        $"Project folder: {_folder ?? "(none)"}",
        $"Console size: {_model.Columns}x{_model.Rows}",
        $"Claude state: {StateLabel.Text}",
        // Where the events come from (events-and-grounding.md EG-9.4): numbers and fixed words only.
        .. FlowDiagnostics(),
    ];
}

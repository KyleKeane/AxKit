using AxClaude.Core;
using AxClaude.Core.Flow;
using AxClaude.Core.Hooks;
using AxClaude.Core.Pty;
using AxClaude.Core.Record;
using AxClaude.Core.Transcript;
using AxKit.Core;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the core beside the window (docs/axClaude/events-and-grounding.md EG-4.8, EG-4.10, EG-4.11, EG-9) ----
    //
    // Everything that happens in the session is one input at the end of one queue: the model's marks, bells and
    // frames, what the user does, and what the hooks' pipe and the record's reader post. One loop takes an input,
    // gives it to the core and carries out each decision in order; an input raised while the loop runs is only queued,
    // so the core never runs nested and its decisions come in arrival order. The core has no timer: when something is
    // due later it asks to be woken, and the one timer here sends a tick.
    //
    // In charge, or in shadow: for each family of decisions (the state, Done, reply speech, the background) the window
    // either carries the core's decisions out, or lets its own code act and tells the shadow what that code did, which
    // writes one line of numbers to the log for each turn. Built names the families the core is in charge of.

    /// <summary>The families of the core's decisions (EG-4.11).</summary>
    [Flags]
    private enum FlowCharge
    {
        None = 0,
        State = 1,
        Done = 2,
        Speech = 4,
        Background = 8,
    }

    /// <summary>The families the core is in charge of in this build: the state (EG-6.1 to EG-6.4) and Done (EG-5.6, EG-6.5); reply speech and the background are in shadow. Each package that moves a family to the core adds it here (events-and-grounding.md section 10).</summary>
    private const FlowCharge Built = FlowCharge.State | FlowCharge.Done;

    /// <summary>The numbers of where the events come from (EG-9.3, EG-9.4), counted on this thread alone: the core counts in it, and what a background thread counted is added here when its post arrives.</summary>
    private readonly SourceHealth _health = new();

    /// <summary>The core; made in the constructor, over <see cref="_health"/>.</summary>
    private readonly SessionFlow _flow;

    private readonly FlowShadow _shadow = new();

    private readonly Queue<FlowInput> _flowInputs = new();

    /// <summary>The loop of <see cref="EnqueueFlow"/> runs: an input raised now is queued and taken by that loop.</summary>
    private bool _flowDraining;

    /// <summary>The time of the input the loop is carrying out, for the decisions handed to the shadow.</summary>
    private long _flowNow;

    /// <summary>The core threw: it gets no more inputs and decides nothing until Claude is started again.</summary>
    private bool _flowFailed;

    /// <summary>The one timer of the core's wake time (EG-4.10) and of the shadow's line.</summary>
    private readonly System.Windows.Forms.Timer _flowTimer = new();

    /// <summary>The time the core asked to be woken at; <see cref="long.MaxValue"/> for none.</summary>
    private long _flowWakeAt = long.MaxValue;

    /// <summary>The time <see cref="_flowTimer"/> is set for; <see cref="long.MaxValue"/> while it is stopped.</summary>
    private long _flowTimerAt = long.MaxValue;

    /// <summary>What <see cref="RecordTurn"/> was last told: the core has a turn open or ending.</summary>
    private bool _flowTurn;

    /// <summary>What the shadow was last told of the window's own working state.</summary>
    private bool _flowWindowWorking;

    // What the record's reader fills in (MainForm.Record.cs, events-and-grounding.md EG-3); called here, and nothing until then.
    partial void RecordProcessStarted();

    partial void RecordProcessEnded();

    partial void RecordFollow(string path);

    partial void RecordForget();

    partial void RecordSwitch(bool on);

    partial void RecordSent(string key);

    /// <summary>The core has a turn open or ending, or no longer: the reader polls the file faster while it has (EG-3.3).</summary>
    partial void RecordTurn(bool open);

    /// <summary>Called once from the constructor.</summary>
    private void WireFlow()
    {
        _health.TableVersion = ClaudeCodeHooks.TableVersion;
        _health.Hooks = _settings.ClaudeHooks ? HookUse.NotPassed : HookUse.Off;
        _health.Record = _settings.GroundInSessionFile ? RecordUse.NotFound : RecordUse.Off;
        _flowTimer.Tick += (_, _) =>
        {
            _flowTimer.Stop();
            _flowTimerAt = long.MaxValue;
            // The core gives its wake time again after a tick while something is due; one that came early costs one more.
            _flowWakeAt = long.MaxValue;
            EnqueueFlow(new Tick());
        };
    }

    /// <summary>Whether the core is in charge of a family of decisions (EG-4.11): the build says which, the file-only setting <c>eventCore</c> set to <c>shadow</c> puts all of them back in shadow, and so does a core that failed.</summary>
    private bool FlowDecides(FlowCharge family) =>
        (Built & family) != 0 && !_flowFailed && _settings.EventCore != AppSettings.EventCoreShadow;

    /// <summary>
    /// Claude works: a turn is open. By the core while it decides the state (EG-6.1 to EG-6.3: the turn's marks, with
    /// the record and the screen standing in where a run has none), else by the screen's spinner and hint, as before
    /// the core. Everything in the window that asks whether Claude is busy or idle asks here. One place still reads
    /// the screen's own word, the pass that says which reply line is final (<see cref="OnTranscriptChanged"/>, where
    /// the reason stands).
    /// </summary>
    private bool ClaudeWorks => FlowDecides(FlowCharge.State) ? _flow.Working : _model.Working;

    /// <summary>
    /// A turn is open, or has ended and is not yet over, its last rows still to come: a message sent now is "Queued"
    /// and goes after that turn's reply (EG-6.4). The ending is the core's alone; the screen knows none.
    /// </summary>
    private bool ClaudeInTurn => ClaudeWorks || (FlowDecides(FlowCharge.State) && _flow.Ending);

    /// <summary>
    /// The model is told whether a turn is open or ending, which is what holds a sent message's block back
    /// (<see cref="SessionModel.TurnOpen"/>, EG-1.4): the core's word while the core decides the state, null while it
    /// does not, and the model then reads the screen as before.
    /// </summary>
    private void FlowTurnToModel()
    {
        bool? open = FlowDecides(FlowCharge.State) ? _flow.Working || _flow.Ending : null;
        var over = _model.TurnOpen == true && open != true;
        _model.TurnOpen = open;
        if (over)
        {
            // The model reads the value only as a frame ends, and a turn is over with no frame behind it (at Done, by
            // time): a held block whose echo never came would then wait for Claude's next output, where the screen's
            // own word placed it with the turn's last frame (SPEC.md FR-4.7). So one more frame end, of the screen as
            // it stands, as the quiet timer gives one; after the pass that runs now, never inside it.
            After(1, () =>
            {
                if (ProcessRunning && _model.TurnOpen != true)
                {
                    _model.EndFrame();
                }
            });
        }
    }

    /// <summary>
    /// Puts an input at the end of the queue and, unless the loop already runs, takes the queue's inputs one by one:
    /// the core's decisions for one are carried out in order before the next is taken (EG-4.8). Null is no input, so
    /// that a source's mapping can be handed over as it is. Nothing the core does may take the window down while the
    /// window's own code still acts beside it: an exception is logged, with the input's kind and never its content,
    /// and the core is left alone until the next start of Claude.
    /// </summary>
    private void EnqueueFlow(FlowInput? input)
    {
        if (input is null || (_flowFailed && input is not ProcessStarted))
        {
            return;
        }

        _flowInputs.Enqueue(input);
        if (_flowDraining)
        {
            return;
        }

        _flowDraining = true;
        try
        {
            while (_flowInputs.Count > 0)
            {
                var next = _flowInputs.Dequeue();
                try
                {
                    if (next is ProcessStarted)
                    {
                        // A new run: the core starts afresh, and a core that failed in the run before gets its inputs again.
                        _flowFailed = false;
                    }

                    _flowNow = Environment.TickCount64;
                    // The list is the core's own and its next call reuses it: read here, before the next input.
                    foreach (var decision in _flow.Handle(next, _flowNow))
                    {
                        Carry(decision);
                    }

                    FlowAfter(next);
                }
                catch (Exception ex)
                {
                    // Every kind, on purpose: the core is new code beside code that works. An input prints as its
                    // type's name, and the exception's message is left out, since it could quote a key.
                    _flowFailed = true;
                    // A bell handed to the core and not carried out: its handler only queued it (MainForm.cs).
                    var bell = next is BellSeen || _flowInputs.Any(input => input is BellSeen);
                    _flowInputs.Clear();
                    _flowTimer.Stop();
                    _flowTimerAt = long.MaxValue;
                    Log.Error($"Events: the core failed on {next} with {ex.GetType().Name} and is off until Claude starts again\n{ex.StackTrace}");
                    // What the core decided is the screen's again from here: the model's rule, the status bar and the
                    // music, and Done by the bell and the settle, that bell's too.
                    FlowTurnToModel();
                    UpdateStatus();
                    UpdateMusic();
                    if (bell)
                    {
                        FlowBell(noMarks: true);
                    }
                }
            }
        }
        finally
        {
            _flowDraining = false;
        }
    }

    /// <summary>
    /// Carries one decision out. Whatever the core is in charge of: the wake time (the timer), a note (the log), and
    /// the record to follow or to forget (the reader). The state, while the core decides it: the model's rule for a
    /// sent message, the status bar and the music, at once, since a mark, an event or a tick changes it with no frame
    /// behind it. Done, while the core decides it: the word and its chime (<see cref="FlowDone"/>), and a bell that
    /// is no turn's end starts the window's settle (<see cref="FlowBell"/>). Every other decision belongs to a family
    /// that is in shadow in this build: the window's own code acts, and the decision only goes to the shadow's count.
    /// </summary>
    private void Carry(FlowDecision decision)
    {
        switch (decision)
        {
            case StateChanged when FlowDecides(FlowCharge.State):
                // The shadow still needs the state, of both sides: a turn of its line lasts until both are idle, and
                // Done and reply speech are compared inside it. The window has no state of its own to compare any
                // more, so both sides are told the one state at the one time, and the line says that they agree.
                _shadow.Core(decision, _flowNow);
                FlowWindowState(_flowNow);
                FlowTurnToModel();
                UpdateStatus();
                UpdateMusic();
                break;
            case SayDone done when FlowDecides(FlowCharge.Done):
                FlowDone(done);
                break;
            case AttentionBell bell when FlowDecides(FlowCharge.Done):
                FlowBell(bell.NoMarks);
                break;
            case WakeAt wake:
                _flowWakeAt = wake.AtMs;
                break;
            case Note note:
                // Fixed words and numbers (EG-9.5).
                Log.Info("Events: " + note.ToLine());
                break;
            case FollowRecord follow:
                RecordFollow(follow.Path);
                break;
            case ForgetRecord:
                RecordForget();
                break;
            default:
                _shadow.Core(decision, _flowNow);
                break;
        }
    }

    /// <summary>
    /// The core's Done carried out (EG-5.6, EG-6.5): the turn has ended with its bell and its last reply is in the
    /// conversation. While the window is put away it says and plays nothing (FR-9.6): the Done is owed, one for
    /// several turns, and the settle that <see cref="BringUp"/> restarts says it (<see cref="OnFrameSettled"/>).
    /// Else "Done" by its switch, the chime and the taskbar signal, as the settle gave them before the core.
    ///
    /// The shadow, as with the state: the window has no Done of its own beside this one, so both sides of its line
    /// are told the one Done at the one time, and the line names no difference in it. That holds for a Done that is
    /// owed too: it is told here, when it was decided, and not again when the settle says it.
    /// </summary>
    private void FlowDone(SayDone done)
    {
        if (_putAway)
        {
            _shadow.Core(done, _flowNow);
            _shadow.Window(ShadowFact.SaidDone, _flowNow);
            _doneOwed = true;
            SettledWhilePutAway();
            return;
        }

        // Reply speech is still the window's own (WP-A18), behind a hold of its own that knows nothing of this
        // moment: with a record the core's Done comes some 40 ms after the reply's frame, the hold runs for 250, and
        // the reply would be heard after its Done. Lines the core releases with a Done are spoken before it
        // (EG-5.6), so what the window still holds goes first; a question that waits has emptied the hold already
        // (OnTranscriptChanged). The shadow is told in the order heard: the lines, then Done.
        SpeakHeld();
        _shadow.Core(done, _flowNow);
        _shadow.Window(ShadowFact.SaidDone, _flowNow);
        if (_settings.AnnounceBell)
        {
            Announce("Done", false);
        }

        Signal(question: false);
    }

    /// <summary>
    /// A bell that the core found to be no turn's end (EG-6.5, section 4.2), carried out as the bell's handler did
    /// it before the core: the settle starts, which shows what Claude waits on. <paramref name="noMarks"/>: no mark
    /// was seen in this run, so the bell is all there is of a turn's end, and the settle says Done by the old rule.
    /// </summary>
    private void FlowBell(bool noMarks)
    {
        _bellPending = true;
        _bellNoMarks = noMarks;
        _attention.Stop();
        _attention.Start();
    }

    /// <summary>After an input's decisions: the reader is told when the turn opened or ended, the shadow's line for a turn both have ended goes to the log, and the timer is set for what is due next.</summary>
    private void FlowAfter(FlowInput handled)
    {
        // Ending changes without a decision (at an end mark, at Done, by time), so both are read after every input.
        var turn = _flow.Working || _flow.Ending;
        if (turn != _flowTurn)
        {
            _flowTurn = turn;
            RecordTurn(turn);
        }

        // For the same reason the model is told here, and not only with a change of the state (EG-1.4).
        FlowTurnToModel();
        var ended = handled is ProcessEnded;
        if (ended)
        {
            // Claude is gone: the window's own state is idle, and a turn the shadow holds is over for both.
            FlowWindowState(Environment.TickCount64);
        }

        if (_shadow.TurnLine(ended ? long.MaxValue : _flowNow) is { } line)
        {
            Log.Info("Events: shadow " + line);
        }

        var due = Math.Min(_flowWakeAt, _shadow.DueAt);
        if (due == _flowTimerAt)
        {
            return;
        }

        _flowTimer.Stop();
        _flowTimerAt = due;
        if (due != long.MaxValue)
        {
            _flowTimer.Interval = (int)Math.Clamp(due - Environment.TickCount64, 1, int.MaxValue);
            _flowTimer.Start();
        }
    }

    // ---- what the window's own code tells the core and the shadow ----

    /// <summary>
    /// Every pass over the conversation (<see cref="OnTranscriptChanged"/>), right after <see cref="Arrivals.Update"/>:
    /// the frame as the core's input, and the window's own working state for the shadow. Not while Claude is stopped:
    /// the window's own lines change the conversation then, and the screen still shows what the last frame left.
    /// </summary>
    private void FlowFrame(Arrival arrival)
    {
        if (!ProcessRunning)
        {
            return;
        }

        FlowWindowState(Environment.TickCount64);
        if (_model.PromptPending)
        {
            // For the shadow's line of a turn that differs: Claude waited on a question, a permission prompt or a screen.
            _shadow.QuestionOpen();
        }

        EnqueueFlow(FlowFeed.FromFrame(_model, arrival, _model.PromptPending, _readyAnnounced && _settings.ReplySpeech != ReplySpeechMode.None));
    }

    /// <summary>
    /// The window's working state, as the status bar has it (FR-10.2), told to the shadow when it changed. While the
    /// core is in shadow that is the screen's word, and the shadow's line says how long the two stood apart; while the
    /// core decides the state it is the core's own, told with the decision (<see cref="Carry"/>).
    /// </summary>
    private void FlowWindowState(long now)
    {
        var working = ProcessRunning && ClaudeWorks;
        if (working != _flowWindowWorking)
        {
            _flowWindowWorking = working;
            _shadow.Window(working ? ShadowFact.Working : ShadowFact.Idle, now);
        }
    }

    /// <summary>
    /// The window's own code said Done: a turn's end by the old rule, the bell and the settle (FR-7.2). That is every
    /// Done while the core is in shadow, and, while the core decides Done, the Done of a run without marks, which the
    /// core leaves to the settle (EG-6.5): the shadow's line for such a turn names Done as a difference, truly, since
    /// the core said none.
    /// </summary>
    private void FlowSaidDone() => _shadow.Window(ShadowFact.SaidDone, Environment.TickCount64);

    /// <summary>The window's own code spoke these reply lines after its hold (FR-7.4): the shadow counts them and keeps their kinds, never their text.</summary>
    private void FlowSpoke(IReadOnlyList<AxKit.Core.Transcript.Line> lines) => _shadow.WindowSpoke(lines, Environment.TickCount64);

    /// <summary>The user acted, or the app for him: he has moved on from what was held for him (EG-4.7).</summary>
    private void FlowActed(UserAction action) => EnqueueFlow(new UserActed(action));

    /// <summary>A message or a command went to Claude: the user's action with the message's key, which the record's reader looks for in the session files (EG-3.2).</summary>
    private void FlowSent(string text)
    {
        var key = FlowFeed.SentKey(text);
        EnqueueFlow(new UserActed(UserAction.Sent, key));
        RecordSent(key);
    }

    /// <summary>A question or a screen was shown in the message field's place, or as a notice (EG-4.7).</summary>
    private void FlowQuestionShown() => EnqueueFlow(new QuestionShown());

    /// <summary>
    /// Claude was started: the numbers begin afresh for this run, the switches say how the two sources start (what
    /// passes the hooks' file and what follows the session's file say more after this), and the core and the reader
    /// are told. One line in the log for the start (EG-9.5).
    /// </summary>
    private void FlowProcessStarted()
    {
        _health.Reset();
        if (!_settings.ClaudeHooks)
        {
            _health.Hooks = HookUse.Off;
            _health.HooksReason = string.Empty;
        }
        else if (_health.Hooks == HookUse.Off)
        {
            _health.Hooks = HookUse.NotPassed;
        }

        _health.Record = _settings.GroundInSessionFile ? RecordUse.NotFound : RecordUse.Off;
        _health.RecordReason = string.Empty;
        EnqueueFlow(new ProcessStarted());
        RecordProcessStarted();
        Log.Info($"Events: Claude started; core {FlowCoreWords(spoken: false)}; hooks {_health.HooksWords}; session file {_health.RecordWords}");
    }

    /// <summary>Claude ended or was stopped: the core and the reader are told. Called from <see cref="StopProcess"/> alone, which every end passes.</summary>
    private void FlowProcessEnded()
    {
        EnqueueFlow(new ProcessEnded());
        RecordProcessEnded();
    }

    // ---- on demand: Say the event sources, the diagnostics, the two switches (EG-9) ----

    /// <summary>The words for what the core is in charge of: "in shadow", or "in charge of state, Done and speech" as spoken and "in charge of state, Done, speech" in the diagnostics; "off after an error" for a core that failed.</summary>
    private string FlowCoreWords(bool spoken)
    {
        if (_flowFailed)
        {
            return "off after an error";
        }

        var families = new List<string>();
        foreach (var (family, word) in new[] { (FlowCharge.State, "state"), (FlowCharge.Done, "Done"), (FlowCharge.Speech, "speech"), (FlowCharge.Background, "background") })
        {
            if (FlowDecides(family))
            {
                families.Add(word);
            }
        }

        if (families.Count == 0)
        {
            return "in shadow";
        }

        return "in charge of " + (spoken && families.Count > 1
            ? string.Join(", ", families.Take(families.Count - 1)) + " and " + families[^1]
            : string.Join(", ", families));
    }

    /// <summary>Options, Developer, Say the event sources (EG-9.3): where the events come from, in numbers and fixed words, on demand.</summary>
    private void SayEventSources() => Announce(_health.Said(FlowCoreWords(spoken: true)), true);

    /// <summary>The lines Copy diagnostics gains (EG-9.4): numbers and fixed words only.</summary>
    private IReadOnlyList<string> FlowDiagnostics() => _health.Lines(FlowCoreWords(spoken: false));

    /// <summary>
    /// Options, Developer, Events from Claude Code's hooks (EG-9.1): the setting, for the next start of Claude Code;
    /// the choice is said, since the menu closes on it and nothing else shows it before that start.
    /// </summary>
    private void SetClaudeHooks(bool on)
    {
        _settings.ClaudeHooks = on;
        Announce($"Events from Claude Code's hooks {(on ? "on" : "off")}, from the next start of Claude", true);
    }

    /// <summary>
    /// Options, Developer, Ground the conversation in Claude Code's session file (EG-9.2): at once. Off, there is no
    /// record from here on; on again, the reader starts with what the file gains from then on. What reads the file
    /// says more through <see cref="RecordSwitch"/>, called last so that its word stands.
    /// </summary>
    private void SetGroundInSessionFile(bool on)
    {
        _settings.GroundInSessionFile = on;
        _health.Record = on ? RecordUse.NotFound : RecordUse.Off;
        _health.RecordReason = string.Empty;
        RecordSwitch(on);
        Announce($"Grounding in Claude Code's session file {(on ? "on" : "off")}", true);
    }

    /// <summary>
    /// The projects folders a session file of the Claude Code this window starts may lie in (EG-3.1): Claude Code's
    /// configuration folder as the child's environment names it, else under the user's profile, and in a test run the
    /// run's own folder too. What checks a path an event names (EG-2.9) and what looks for the session's file (EG-3.2)
    /// both ask here.
    /// </summary>
    private IReadOnlyList<string> SessionProjectsFolders() =>
        ClaudeCodeTranscript.ProjectsFolders(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ClaudeLauncher.BuildEnvironment(), AutomationEnvironment.Home);
}

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

    /// <summary>The families the core is in charge of in this build: none, the core runs in shadow. Each package that moves a family to the core adds it here (events-and-grounding.md section 10).</summary>
    private const FlowCharge Built = FlowCharge.None;

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
                    _flowInputs.Clear();
                    _flowTimer.Stop();
                    _flowTimerAt = long.MaxValue;
                    Log.Error($"Events: the core failed on {next} with {ex.GetType().Name} and is off until Claude starts again\n{ex.StackTrace}");
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
    /// the record to follow or to forget (the reader). Every other decision belongs to a family that is in shadow in
    /// this build: the window's own code acts, and the decision only goes to the shadow's count.
    /// </summary>
    private void Carry(FlowDecision decision)
    {
        switch (decision)
        {
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

        var ended = handled is ProcessEnded;
        if (ended)
        {
            // Claude is gone: the window's own state is idle, and a turn the shadow holds is over for both.
            FlowWindowState();
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

        FlowWindowState();
        EnqueueFlow(FlowFeed.FromFrame(_model, arrival, _model.PromptPending, _readyAnnounced && _settings.ReplySpeech != ReplySpeechMode.None));
    }

    /// <summary>The window's own working state, as the status bar has it (FR-10.2), told to the shadow when it changed.</summary>
    private void FlowWindowState()
    {
        var working = ProcessRunning && _model.Working;
        if (working != _flowWindowWorking)
        {
            _flowWindowWorking = working;
            _shadow.Window(working ? ShadowFact.Working : ShadowFact.Idle, 0, Environment.TickCount64);
        }
    }

    /// <summary>The window's own code said Done: a turn's end by the old rule, the bell and the settle (FR-7.2).</summary>
    private void FlowSaidDone() => _shadow.Window(ShadowFact.SaidDone, 0, Environment.TickCount64);

    /// <summary>The window's own code spoke this many reply lines after its hold (FR-7.4).</summary>
    private void FlowSpoke(int lines) => _shadow.Window(ShadowFact.SpokeLines, lines, Environment.TickCount64);

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

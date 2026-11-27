using AxClaude.Core.Transcript;
using AxKit.Core.Events;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- work in the background (FR-7.10): the number, its sounds, its optional words, and the answer on demand ----
    //
    // The rules are AxClaude.Core's BackgroundWork; this file wires them to the model, the sounds and the settings.
    // What the model shows is compared with what was last signalled only after it has stood still for a moment
    // (BackgroundSettleMs): Claude Code redraws its panel often, and a frame cut in the middle of a redraw can show
    // the mode line without the rows. So a redraw is never a sound, and a start or an end is one sound.

    /// <summary>How long the background work must look the same before a change of it is signalled.</summary>
    private const int BackgroundSettleMs = 700;

    private readonly System.Windows.Forms.Timer _backgroundSettle = new() { Interval = BackgroundSettleMs };

    /// <summary>How often the recurring chords' timer asks whether a chord is due: fine enough that a pause of 2 s is 2 s, and that a chord held for a reply comes soon after it.</summary>
    private const int BackgroundChordCheckMs = 250;

    /// <summary>The recurring chords while background work runs: the timer asks <see cref="_backgroundChords"/> whether one is due.</summary>
    private readonly System.Windows.Forms.Timer _backgroundChordTimer = new() { Interval = BackgroundChordCheckMs };

    /// <summary>When the chords play and which (FR-7.10): Core's schedule, with the shared random numbers.</summary>
    private readonly BackgroundChords _backgroundChords = new(AxKit.Core.Audio.AmbientSounds.ChordNames.Count, Random.Shared.Next);

    /// <summary>What the model showed at the last frame, by which a frame that changes nothing costs a few comparisons.</summary>
    private string? _backgroundSeenText;
    private IReadOnlyList<BackgroundAgent> _backgroundSeenAgents = [];
    private int _backgroundSeenAwaited;
    private IReadOnlyList<BackgroundWorkflow> _backgroundSeenWorkflows = [];
    private int _backgroundSeenWorkflowsAwaited;
    private int _backgroundSeenUnknown;

    /// <summary>What was last signalled: the sounds and the words are for the change from this.</summary>
    private BackgroundCount _backgroundHeard = BackgroundCount.None;
    private IReadOnlyList<BackgroundAgent> _backgroundAgentsHeard = [];
    private IReadOnlyList<BackgroundWorkflow> _backgroundWorkflowsHeard = [];

    private ChoiceItems<int>? _pauseItems;

    /// <summary>Called once from the constructor.</summary>
    private void WireBackground()
    {
        _backgroundSettle.Tick += (_, _) =>
        {
            _backgroundSettle.Stop();
            BackgroundSettled();
        };
        _backgroundChordTimer.Tick += (_, _) => PlayBackgroundChord();
    }

    /// <summary>The number now (FR-7.10): the agents and workflows shown or awaited, the rows of kinds not known and what the mode line counts; nothing while Claude is stopped.</summary>
    private BackgroundCount BackgroundNow =>
        ProcessRunning
            ? BackgroundWork.Count(_model.Background, _model.BackgroundAgents.Count, _model.BackgroundAgentsAwaited,
                workflowsShown: _model.BackgroundWorkflows.Count, workflowsAwaited: _model.BackgroundWorkflowsAwaited, unknownShown: _model.BackgroundUnknown)
            : BackgroundCount.None;

    /// <summary>Every frame: when what the model shows of the background work differs from the frame before, the settle starts again.</summary>
    private void WatchBackground()
    {
        if (ReferenceEquals(_backgroundSeenAgents, _model.BackgroundAgents) && _backgroundSeenAwaited == _model.BackgroundAgentsAwaited
            && ReferenceEquals(_backgroundSeenWorkflows, _model.BackgroundWorkflows) && _backgroundSeenWorkflowsAwaited == _model.BackgroundWorkflowsAwaited
            && _backgroundSeenUnknown == _model.BackgroundUnknown && string.Equals(_backgroundSeenText, _model.Background, StringComparison.Ordinal))
        {
            return;
        }

        _backgroundSeenText = _model.Background;
        _backgroundSeenAgents = _model.BackgroundAgents;
        _backgroundSeenAwaited = _model.BackgroundAgentsAwaited;
        _backgroundSeenWorkflows = _model.BackgroundWorkflows;
        _backgroundSeenWorkflowsAwaited = _model.BackgroundWorkflowsAwaited;
        _backgroundSeenUnknown = _model.BackgroundUnknown;
        _backgroundSettle.Stop();
        _backgroundSettle.Start();
    }

    /// <summary>The background work has stood still: a change from what was last signalled gets its one sound, and its words when the user asked for them.</summary>
    private void BackgroundSettled()
    {
        var now = BackgroundNow;
        var agents = ProcessRunning ? _model.BackgroundAgents : [];
        var workflows = ProcessRunning ? _model.BackgroundWorkflows : [];
        var changes = AgentPanel.Changes(_backgroundAgentsHeard, agents);
        var workflowChanges = AgentPanel.WorkflowChanges(_backgroundWorkflowsHeard, workflows);
        var sound = BackgroundWork.Sound(_backgroundHeard, now, changes, workflowChanges);
        var before = _backgroundHeard;
        _backgroundHeard = now;
        _backgroundAgentsHeard = agents;
        _backgroundWorkflowsHeard = workflows;
        if (sound != BackgroundSound.None)
        {
            if (_settings.SoundOnBackgroundWork)
            {
                if (sound == BackgroundSound.Start)
                {
                    EventDoor.Raise(EventKind.BackgroundStarted);
                }
                else
                {
                    EventDoor.Raise(EventKind.BackgroundFinished);
                }
            }

            if (_settings.SayBackgroundWork && BackgroundWork.ChangeLines(before, now, changes, workflowChanges) is { Count: > 0 } lines)
            {
                // Never interrupting: behind a reply being read, joined to what the window says at the same moment (AX-7.3).
                Announce(string.Join(". ", lines), false);
            }
        }

        FollowBackgroundChords();
    }

    /// <summary>Claude stopped or starts again: nothing runs, and its going is no end to signal.</summary>
    private void ForgetBackground()
    {
        _backgroundSettle.Stop();
        _backgroundChordTimer.Stop();
        _backgroundHeard = BackgroundCount.None;
        _backgroundAgentsHeard = [];
        _backgroundWorkflowsHeard = [];
    }

    /// <summary>
    /// The chords' timer runs while something runs and the setting is on; at zero it stops, so no new chord starts,
    /// and a chord that sounds rings out. The first chord comes at the first tick.
    /// </summary>
    private void FollowBackgroundChords()
    {
        if (_settings.BackgroundPulse && _backgroundHeard.Total > 0)
        {
            _backgroundChordTimer.Start();
        }
        else
        {
            _backgroundChordTimer.Stop();
        }
    }

    /// <summary>
    /// A tick of the chords' timer: a chord when one is due (<see cref="BackgroundChords.Next"/>: the last one and its
    /// pause over), with a tone for each thing running now. Chords play under speech too: held back while a reply was
    /// estimated to be read, they were never heard (the user, 2026-11-15), since Claude speaks most of the time work
    /// runs. A window put away is silent through <see cref="EventDoor.Muted"/>.
    /// </summary>
    private void PlayBackgroundChord()
    {
        if (_backgroundHeard.Total == 0 || !ProcessRunning)
        {
            _backgroundChordTimer.Stop();
            return;
        }

        var now = Environment.TickCount64;
        if (_backgroundChords.Next(now, _backgroundHeard.Total, _settings.BackgroundChordPauseSeconds, held: false) is { } chord)
        {
            EventDoor.BackgroundRunning(chord.Chord, chord.Things);
        }
    }

    /// <summary>Actions, What runs in the background (<see cref="AxClaudeKeys.BackgroundWork"/>, F8): the number, each agent's type and description, each workflow's progress, on demand.</summary>
    private void SayBackgroundWork() =>
        Announce(BackgroundWork.Said(BackgroundNow, ProcessRunning ? _model.BackgroundAgents : [], ProcessRunning ? _model.BackgroundWorkflows : []), true);

    /// <summary>
    /// Options, Background work (FR-7.10): the two sounds, the words, Claude Code's live progress rows in the
    /// conversation, and the pause between the recurring chords.
    /// </summary>
    private ToolStripMenuItem CreateBackgroundMenu()
    {
        var menu = new ToolStripMenuItem("&Background work");
        menu.DropDownItems.Add(Toggle("Play a &sound when background work starts and ends", _settings.SoundOnBackgroundWork, v => _settings.SoundOnBackgroundWork = v));
        menu.DropDownItems.Add(Toggle("Play a sound &while background work runs", _settings.BackgroundPulse, v =>
        {
            _settings.BackgroundPulse = v;
            FollowBackgroundChords();
        }));
        menu.DropDownItems.Add(Toggle("Sa&y background work as it starts and ends", _settings.SayBackgroundWork, v => _settings.SayBackgroundWork = v));
        // The user's decision of 2026-11-15, "an option, hidden at first": the rows shown, never spoken. Applied to the
        // screen as it stands at once (a frame end now, as the quiet timer gives one), not at Claude Code's next redraw.
        menu.DropDownItems.Add(Toggle("Show Claude Code's live &progress rows in the conversation", _settings.ShowProgressRows, v =>
        {
            _settings.ShowProgressRows = v;
            _model.ShowProgressRows = v;
            _model.EndFrame();
        }));
        menu.DropDownItems.Add(new ToolStripSeparator());
        _pauseItems = new ChoiceItems<int>(menu, BackgroundChords.PauseSeconds.Select(seconds => (seconds, PauseChoice(seconds))), _settings.BackgroundChordPauseSeconds, SetPauseSeconds);
        return menu;
    }

    /// <summary>"A pause of &amp;2 seconds between chords": the choice's text, its number the mnemonic (the first digit; for 20, whose 2 is taken by 2, its 0).</summary>
    private static string PauseChoice(int seconds) =>
        seconds >= 10 && BackgroundChords.PauseSeconds.Contains(seconds / 10)
            ? $"A pause of {seconds / 10}&{seconds % 10} seconds between chords"
            : $"A pause of &{seconds} seconds between chords";

    private void SetPauseSeconds(int seconds)
    {
        _settings.BackgroundChordPauseSeconds = seconds;
        _pauseItems?.Check(seconds);
        SaveSettings();
        // The menu closes on a choice, so the choice is said (AX-6.4).
        Announce(AxKit.Core.MenuText.Plain(PauseChoice(seconds)), true);
    }
}

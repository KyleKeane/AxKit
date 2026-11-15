using AxClaude.Core.Transcript;
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

    /// <summary>The recurring sound while background work runs; its interval is the setting's.</summary>
    private readonly System.Windows.Forms.Timer _backgroundPulse = new();

    /// <summary>What the model showed at the last frame, by which a frame that changes nothing costs three comparisons.</summary>
    private string? _backgroundSeenText;
    private IReadOnlyList<BackgroundAgent> _backgroundSeenAgents = [];
    private int _backgroundSeenAwaited;

    /// <summary>What was last signalled: the sounds and the words are for the change from this.</summary>
    private BackgroundCount _backgroundHeard = BackgroundCount.None;
    private IReadOnlyList<BackgroundAgent> _backgroundAgentsHeard = [];

    private ChoiceItems<int>? _pulseItems;

    /// <summary>Called once from the constructor.</summary>
    private void WireBackground()
    {
        _backgroundSettle.Tick += (_, _) =>
        {
            _backgroundSettle.Stop();
            BackgroundSettled();
        };
        _backgroundPulse.Tick += (_, _) => PlayBackgroundPulse();
    }

    /// <summary>The number now (FR-7.10): the agents shown or awaited and what the mode line counts; nothing while Claude is stopped.</summary>
    private BackgroundCount BackgroundNow =>
        ProcessRunning ? BackgroundWork.Count(_model.Background, _model.BackgroundAgents.Count, _model.BackgroundAgentsAwaited) : BackgroundCount.None;

    /// <summary>Every frame: when what the model shows of the background work differs from the frame before, the settle starts again.</summary>
    private void WatchBackground()
    {
        if (ReferenceEquals(_backgroundSeenAgents, _model.BackgroundAgents) && _backgroundSeenAwaited == _model.BackgroundAgentsAwaited
            && string.Equals(_backgroundSeenText, _model.Background, StringComparison.Ordinal))
        {
            return;
        }

        _backgroundSeenText = _model.Background;
        _backgroundSeenAgents = _model.BackgroundAgents;
        _backgroundSeenAwaited = _model.BackgroundAgentsAwaited;
        _backgroundSettle.Stop();
        _backgroundSettle.Start();
    }

    /// <summary>The background work has stood still: a change from what was last signalled gets its one sound, and its words when the user asked for them.</summary>
    private void BackgroundSettled()
    {
        var now = BackgroundNow;
        var agents = ProcessRunning ? _model.BackgroundAgents : [];
        var changes = AgentPanel.Changes(_backgroundAgentsHeard, agents);
        var sound = BackgroundWork.Sound(_backgroundHeard, now, changes);
        var before = _backgroundHeard;
        _backgroundHeard = now;
        _backgroundAgentsHeard = agents;
        if (sound != BackgroundSound.None)
        {
            if (_settings.SoundOnBackgroundWork)
            {
                if (sound == BackgroundSound.Start)
                {
                    Sounds.BackgroundStart();
                }
                else
                {
                    Sounds.BackgroundFinish();
                }
            }

            if (_settings.SayBackgroundWork && BackgroundWork.ChangeLines(before, now, changes) is { Count: > 0 } lines)
            {
                // Never interrupting: behind a reply being read, joined to what the window says at the same moment (AX-7.3).
                Announce(string.Join(". ", lines), false);
            }
        }

        FollowBackgroundPulse();
    }

    /// <summary>Claude stopped or starts again: nothing runs, and its going is no end to signal.</summary>
    private void ForgetBackground()
    {
        _backgroundSettle.Stop();
        _backgroundPulse.Stop();
        _backgroundHeard = BackgroundCount.None;
        _backgroundAgentsHeard = [];
    }

    /// <summary>The recurring sound's timer runs while something runs and the setting is on, at the setting's interval; it stops at once at zero.</summary>
    private void FollowBackgroundPulse()
    {
        var wanted = _settings.BackgroundPulse && _backgroundHeard.Total > 0;
        var interval = _settings.BackgroundPulseSeconds * 1000;
        if (!wanted)
        {
            _backgroundPulse.Stop();
        }
        else if (!_backgroundPulse.Enabled || _backgroundPulse.Interval != interval)
        {
            _backgroundPulse.Stop();
            _backgroundPulse.Interval = interval;
            _backgroundPulse.Start();
        }
    }

    /// <summary>
    /// One beat of the recurring sound: as many notes as things run. A beat is skipped while NVDA is counted as still
    /// reading a reply this window spoke (<see cref="LeftoverSpeech"/>, the estimate FR-7.4b uses); a window put away
    /// is silent through <see cref="Sounds.Muted"/>.
    /// </summary>
    private void PlayBackgroundPulse()
    {
        if (_backgroundHeard.Total == 0 || !ProcessRunning)
        {
            _backgroundPulse.Stop();
            return;
        }

        if (!LeftoverSpeech.Running(Environment.TickCount64, _replySpeechEnds))
        {
            Sounds.BackgroundPulse(BackgroundWork.Notes(_backgroundHeard.Total));
        }
    }

    /// <summary>Actions, What runs in the background (Ctrl+B): the number, each agent's type and description, on demand.</summary>
    private void SayBackgroundWork() =>
        Announce(BackgroundWork.Said(BackgroundNow, ProcessRunning ? _model.BackgroundAgents : []), true);

    /// <summary>Options, Background work (FR-7.10): the two sounds, the words, and how often the recurring sound plays.</summary>
    private ToolStripMenuItem CreateBackgroundMenu()
    {
        var menu = new ToolStripMenuItem("&Background work");
        menu.DropDownItems.Add(Toggle("Play a &sound when background work starts and ends", _settings.SoundOnBackgroundWork, v => _settings.SoundOnBackgroundWork = v));
        menu.DropDownItems.Add(Toggle("Play a sound &while background work runs", _settings.BackgroundPulse, v =>
        {
            _settings.BackgroundPulse = v;
            FollowBackgroundPulse();
        }));
        menu.DropDownItems.Add(Toggle("Sa&y background work as it starts and ends", _settings.SayBackgroundWork, v => _settings.SayBackgroundWork = v));
        menu.DropDownItems.Add(new ToolStripSeparator());
        _pulseItems = new ChoiceItems<int>(menu, BackgroundWork.PulseSeconds.Select(seconds => (seconds, PulseChoice(seconds))), _settings.BackgroundPulseSeconds, SetPulseSeconds);
        return menu;
    }

    /// <summary>"While it runs, a sound every &amp;20 seconds": the choice's text, its number the mnemonic.</summary>
    private static string PulseChoice(int seconds) => $"While it runs, a sound every &{seconds} seconds";

    private void SetPulseSeconds(int seconds)
    {
        _settings.BackgroundPulseSeconds = seconds;
        _pulseItems?.Check(seconds);
        FollowBackgroundPulse();
        SaveSettings();
        // The menu closes on a choice, so the choice is said (AX-6.4).
        Announce(AxKit.Core.MenuText.Plain(PulseChoice(seconds)), true);
    }
}

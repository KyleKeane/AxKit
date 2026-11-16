using AxClaude.Core;
using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The rules of the signals for work in the background (SPEC.md FR-7.10): the one number from the mode line's counts
/// and the panel's agents, the status bar's words, the sound of a change, the lines said when asked for, and the
/// answer on demand.
/// </summary>
public class BackgroundWorkTests
{
    private static readonly BackgroundAgent Callers = new("general-purpose", "Checking callers");
    private static readonly BackgroundAgent Spec = new("Explore", "Reading the spec");
    private static readonly BackgroundAgent Bare = new("Plan", string.Empty);

    [Theory]
    // Nothing anywhere.
    [InlineData(null, 0, 0, 0, 0, 0, 0)]
    // Shells are counted on the mode line alone.
    [InlineData("1 shell", 0, 0, 1, 0, 0, 0)]
    [InlineData("3 shells", 0, 0, 3, 0, 0, 0)]
    // Agents shown by the panel, the mode line only saying that there is something to manage.
    [InlineData("see /tasks", 2, 0, 0, 2, 0, 0)]
    [InlineData(null, 2, 0, 0, 2, 0, 0)]
    // The waiting row knows of more agents than the rows show, or of fewer: the larger number.
    [InlineData("see /tasks", 1, 2, 0, 2, 0, 0)]
    [InlineData("see /tasks", 2, 1, 0, 2, 0, 0)]
    [InlineData(null, 0, 2, 0, 2, 0, 0)]
    // A shell and agents: added.
    [InlineData("1 shell", 2, 2, 1, 2, 0, 0)]
    // The mode line counts the agents itself: the same agents as the panel's, never added to them.
    [InlineData("1 shell, 2 agents", 2, 2, 1, 2, 0, 0)]
    [InlineData("1 shell, 2 agents", 0, 0, 1, 2, 0, 0)]
    [InlineData("1 shell, 2 agents", 3, 0, 1, 3, 0, 0)]
    [InlineData("2 local agents", 1, 0, 0, 2, 0, 0)]
    // A kind under another word is its own.
    [InlineData("1 shell, 1 monitor", 1, 0, 1, 1, 1, 0)]
    // Background work shown and nothing counted: one thing, uncounted.
    [InlineData("see /tasks", 0, 0, 0, 0, 0, 1)]
    public void The_one_number_adds_shells_to_agents_and_never_counts_an_agent_twice(string? modeLine, int shown, int awaited, int shells, int agents, int other, int uncounted)
    {
        var count = BackgroundWork.Count(modeLine, shown, awaited);
        Assert.Equal(new BackgroundCount(shells, agents, other, uncounted), count);
        Assert.Equal(shells + agents + other + uncounted, count.Total);
    }

    [Theory]
    // A workflow's live row: one workflow.
    [InlineData(null, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0)]
    // Its row and the waiting row count the same workflow: never added; the waiting row alone counts it too.
    [InlineData(null, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0)]
    [InlineData(null, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0)]
    [InlineData(null, 0, 0, 1, 2, 0, 0, 0, 2, 0, 0)]
    // A mode line that only says there is something to manage: the workflow is counted, nothing is uncounted.
    [InlineData("see /tasks", 0, 0, 1, 1, 0, 0, 0, 1, 0, 0)]
    // A workflow and an agent and a shell: each kind once.
    [InlineData("1 shell", 1, 1, 1, 1, 0, 1, 1, 1, 0, 0)]
    // A mode line that counts workflows itself (assumed, not seen): the same workflows as the row's.
    [InlineData("1 workflow", 0, 0, 1, 0, 0, 0, 0, 1, 0, 0)]
    // A progress row of a kind not known: one thing; with a kind the mode line counts under another word, the larger.
    [InlineData(null, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0)]
    [InlineData("1 monitor", 0, 0, 0, 0, 1, 0, 0, 0, 1, 0)]
    [InlineData("2 monitors", 0, 0, 0, 0, 1, 0, 0, 0, 2, 0)]
    [InlineData("see /tasks", 0, 0, 0, 0, 1, 0, 0, 0, 1, 0)]
    [InlineData(null, 0, 0, 1, 0, 1, 0, 0, 1, 1, 0)]
    public void Workflows_and_rows_of_kinds_not_known_are_counted_once(string? modeLine, int agentsShown, int agentsAwaited, int workflowsShown,
        int workflowsAwaited, int unknown, int shells, int agents, int workflows, int other, int uncounted)
    {
        var count = BackgroundWork.Count(modeLine, agentsShown, agentsAwaited, workflowsShown, workflowsAwaited, unknown);
        Assert.Equal(new BackgroundCount(shells, agents, other, uncounted, workflows), count);
        Assert.Equal(shells + agents + workflows + other + uncounted, count.Total);
    }

    [Fact]
    public void Nothing_running_is_the_same_object_so_a_frame_costs_nothing()
    {
        Assert.Same(BackgroundCount.None, BackgroundWork.Count(null, 0, 0));
    }

    [Fact]
    public void The_status_bar_says_the_number_in_plain_words()
    {
        Assert.Null(BackgroundWork.Status(BackgroundCount.None));
        Assert.Equal("1 shell in the background", BackgroundWork.Status(new BackgroundCount(1, 0)));
        Assert.Equal("2 shells in the background", BackgroundWork.Status(new BackgroundCount(2, 0)));
        Assert.Equal("1 agent in the background", BackgroundWork.Status(new BackgroundCount(0, 1)));
        Assert.Equal("2 in the background: 1 shell, 1 agent", BackgroundWork.Status(new BackgroundCount(1, 1)));
        Assert.Equal("4 in the background: 1 shell, 2 agents, 1 other task", BackgroundWork.Status(new BackgroundCount(1, 2, 1)));
        Assert.Equal("2 other tasks in the background", BackgroundWork.Status(new BackgroundCount(0, 0, 2)));
        // Only when nothing can be counted.
        Assert.Equal("see /tasks in the background", BackgroundWork.Status(new BackgroundCount(0, 0, 0, 1)));

        // Dynamic workflows, after the agents and before the other kinds.
        Assert.Equal("1 workflow in the background", BackgroundWork.Status(new BackgroundCount(0, 0, Workflows: 1)));
        Assert.Equal("2 workflows in the background", BackgroundWork.Status(new BackgroundCount(0, 0, Workflows: 2)));
        Assert.Equal("2 in the background: 1 agent, 1 workflow", BackgroundWork.Status(new BackgroundCount(0, 1, Workflows: 1)));
        Assert.Equal("4 in the background: 1 shell, 1 agent, 1 workflow, 1 other task", BackgroundWork.Status(new BackgroundCount(1, 1, 1, Workflows: 1)));
        // A progress row of a kind not known, alone.
        Assert.Equal("1 other task in the background", BackgroundWork.Status(new BackgroundCount(0, 0, 1)));
    }

    private static readonly BackgroundWorkflow Design = new("axkit-design-map-and-verify", 88, 8, 9, "20m18s");
    private static readonly BackgroundWorkflow Nightly = new("nightly", 5, 1, 20, "2m");

    [Fact]
    public void A_workflow_is_said_with_how_far_it_is_and_named_as_it_starts_and_ends()
    {
        var one = new BackgroundCount(0, 0, Workflows: 1);
        Assert.Equal("1 workflow in the background. Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s",
            BackgroundWork.Said(one, [], [Design]));
        Assert.Equal("2 in the background: 1 agent, 1 workflow. general-purpose: Checking callers. Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s",
            BackgroundWork.Said(new BackgroundCount(0, 1, Workflows: 1), [Callers], [Design]));
        Assert.Equal("2 workflows in the background. Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s. 1 more workflow, not named",
            BackgroundWork.Said(new BackgroundCount(0, 0, Workflows: 2), [], [Design]));
        // The waiting row alone, the row gone: the number.
        Assert.Equal("1 workflow in the background", BackgroundWork.Said(one, [], []));
        // A progress row of a kind not known: counted, not named.
        Assert.Equal("1 other task in the background", BackgroundWork.Said(new BackgroundCount(0, 0, 1), [], []));

        // The words of a change, when asked for.
        var started = new BackgroundWorkflowChanges([Design], []);
        var finished = new BackgroundWorkflowChanges([], [Design]);
        Assert.Equal(["Workflow axkit-design-map-and-verify started"], BackgroundWork.ChangeLines(BackgroundCount.None, one, BackgroundAgentChanges.None, started));
        Assert.Equal(["Workflow axkit-design-map-and-verify finished"], BackgroundWork.ChangeLines(one, BackgroundCount.None, BackgroundAgentChanges.None, finished));
        Assert.Equal(["general-purpose started: Checking callers", "Workflow axkit-design-map-and-verify started"],
            BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(0, 1, Workflows: 1), new BackgroundAgentChanges([Callers], []), started));
        // A redraw at another percent is no change; a workflow counted and not named is counted.
        Assert.Empty(BackgroundWork.ChangeLines(one, one, BackgroundAgentChanges.None, BackgroundWorkflowChanges.None));
        Assert.Equal(["1 workflow started"], BackgroundWork.ChangeLines(BackgroundCount.None, one, BackgroundAgentChanges.None));
        Assert.Equal(["1 other task started"], BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(0, 0, 1), BackgroundAgentChanges.None));

        // The sounds: a rise, a fall, and one workflow taking another's place.
        Assert.Equal(BackgroundSound.Start, BackgroundWork.Sound(BackgroundCount.None, one, BackgroundAgentChanges.None, started));
        Assert.Equal(BackgroundSound.Finish, BackgroundWork.Sound(one, BackgroundCount.None, BackgroundAgentChanges.None, finished));
        Assert.Equal(BackgroundSound.None, BackgroundWork.Sound(one, one, BackgroundAgentChanges.None, BackgroundWorkflowChanges.None));
        Assert.Equal(BackgroundSound.Start, BackgroundWork.Sound(one, one, BackgroundAgentChanges.None, new BackgroundWorkflowChanges([Nightly], [Design])));
    }

    /// <summary>The model's own texts feed the count: a workflow's row, an agent's row and a shell on the mode line, the rows as Claude Code draws them.</summary>
    [Fact]
    public void A_frame_with_a_workflow_an_agent_and_a_shell_counts_three()
    {
        var model = new SessionModel(160, 20);
        const string esc = "\x1b";
        TestHelpers.Feed(model, $"{esc}[?25l{esc}[2J{esc}[Hclaude: started\r\nWaiting for 1 background agent to finish\r\nauto mode on  ·  1 shell  ·  ↓ to manage\r\n"
            + "  ◯  general-purpose Checking callers running\r\n   ◯  axkit-design-map-and-verify 88%  8/9 · 20m18s · ↓ 2.5m tokens · ⚠ Large workflow · /workflows to stop\r\n"
            + $"${esc}[?25h");
        var count = BackgroundWork.Count(model.Background, model.BackgroundAgents.Count, model.BackgroundAgentsAwaited,
            model.BackgroundWorkflows.Count, model.BackgroundWorkflowsAwaited, model.BackgroundUnknown);
        Assert.Equal(new BackgroundCount(1, 1, Workflows: 1), count);
        Assert.Equal("3 in the background: 1 shell, 1 agent, 1 workflow", BackgroundWork.Status(count));
        Assert.Equal("3 in the background: 1 shell, 1 agent, 1 workflow. general-purpose: Checking callers. Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s",
            BackgroundWork.Said(count, model.BackgroundAgents, model.BackgroundWorkflows));
    }

    [Fact]
    public void A_rise_is_the_start_sound_a_fall_the_finish_sound_and_a_redraw_none()
    {
        var none = BackgroundAgentChanges.None;
        Assert.Equal(BackgroundSound.None, BackgroundWork.Sound(BackgroundCount.None, BackgroundCount.None, none));
        Assert.Equal(BackgroundSound.None, BackgroundWork.Sound(new BackgroundCount(1, 2), new BackgroundCount(1, 2), none));
        Assert.Equal(BackgroundSound.Start, BackgroundWork.Sound(BackgroundCount.None, new BackgroundCount(1, 0), none));
        Assert.Equal(BackgroundSound.Finish, BackgroundWork.Sound(new BackgroundCount(1, 0), BackgroundCount.None, none));
        // One sound even when the number changes by two.
        Assert.Equal(BackgroundSound.Start, BackgroundWork.Sound(new BackgroundCount(1, 0), new BackgroundCount(1, 2), new BackgroundAgentChanges([Callers, Spec], [])));
        Assert.Equal(BackgroundSound.Finish, BackgroundWork.Sound(new BackgroundCount(1, 2), new BackgroundCount(1, 0), new BackgroundAgentChanges([], [Callers, Spec])));
        // The number stays and one agent took another's place: something arrived.
        Assert.Equal(BackgroundSound.Start, BackgroundWork.Sound(new BackgroundCount(0, 1), new BackgroundCount(0, 1), new BackgroundAgentChanges([Spec], [Callers])));
        // A shell ended as an agent started: the number stays, an agent arrived.
        Assert.Equal(BackgroundSound.Start, BackgroundWork.Sound(new BackgroundCount(1, 0), new BackgroundCount(0, 1), new BackgroundAgentChanges([Spec], [])));
    }

    [Fact]
    public void The_lines_of_a_change_name_each_agent_and_count_the_rest()
    {
        Assert.Empty(BackgroundWork.ChangeLines(new BackgroundCount(1, 1), new BackgroundCount(1, 1), BackgroundAgentChanges.None));
        Assert.Equal(["general-purpose started: Checking callers"],
            BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(0, 1), new BackgroundAgentChanges([Callers], [])));
        Assert.Equal(["Checking callers finished"],
            BackgroundWork.ChangeLines(new BackgroundCount(0, 1), BackgroundCount.None, new BackgroundAgentChanges([], [Callers])));
        // An agent without a description is named by its type.
        Assert.Equal(["Plan started"], BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(0, 1), new BackgroundAgentChanges([Bare], [])));
        Assert.Equal(["Plan finished"], BackgroundWork.ChangeLines(new BackgroundCount(0, 1), BackgroundCount.None, new BackgroundAgentChanges([], [Bare])));
        Assert.Equal(["1 shell started"], BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(1, 0), BackgroundAgentChanges.None));
        Assert.Equal(["2 shells finished"], BackgroundWork.ChangeLines(new BackgroundCount(3, 0), new BackgroundCount(1, 0), BackgroundAgentChanges.None));
        Assert.Equal(["Explore started: Reading the spec", "Checking callers finished", "1 shell started"],
            BackgroundWork.ChangeLines(new BackgroundCount(0, 1), new BackgroundCount(1, 1), new BackgroundAgentChanges([Spec], [Callers])));
        // The waiting row counted two agents and the panel named one: the other is counted.
        Assert.Equal(["general-purpose started: Checking callers", "1 agent started"],
            BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(0, 2), new BackgroundAgentChanges([Callers], [])));
        Assert.Equal(["1 other task started"], BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(0, 0, 1), BackgroundAgentChanges.None));
        Assert.Equal(["Background work started"], BackgroundWork.ChangeLines(BackgroundCount.None, new BackgroundCount(0, 0, 0, 1), BackgroundAgentChanges.None));
        Assert.Equal(["Background work finished"], BackgroundWork.ChangeLines(new BackgroundCount(0, 0, 0, 1), BackgroundCount.None, BackgroundAgentChanges.None));
    }

    [Fact]
    public void On_demand_the_number_then_each_agent()
    {
        Assert.Equal("Nothing runs in the background", BackgroundWork.Said(BackgroundCount.None, []));
        Assert.Equal("1 shell in the background", BackgroundWork.Said(new BackgroundCount(1, 0), []));
        Assert.Equal("3 in the background: 1 shell, 2 agents. general-purpose: Checking callers. Explore: Reading the spec",
            BackgroundWork.Said(new BackgroundCount(1, 2), [Callers, Spec]));
        Assert.Equal("1 agent in the background. Plan", BackgroundWork.Said(new BackgroundCount(0, 1), [Bare]));
        Assert.Equal("2 agents in the background. general-purpose: Checking callers. 1 more agent, not named", BackgroundWork.Said(new BackgroundCount(0, 2), [Callers]));
        Assert.Equal("2 agents in the background", BackgroundWork.Said(new BackgroundCount(0, 2), []));
        Assert.Equal("Claude Code shows work in the background and does not count it. The command /tasks lists it", BackgroundWork.Said(new BackgroundCount(0, 0, 0, 1), []));
    }

    /// <summary>Random numbers from a list, in turn, as the schedule asks for them; the bound each was asked under is kept.</summary>
    private sealed class Dice(params int[] rolls)
    {
        private int _next;

        public List<int> Bounds { get; } = [];

        public int Roll(int bound)
        {
            Bounds.Add(bound);
            return rolls[_next++ % rolls.Length];
        }
    }

    [Fact]
    public void A_chord_then_its_length_and_the_pause_then_the_next()
    {
        var chords = new BackgroundChords(4, new Dice(2, 0, 0).Roll);

        // The first chord at once, for the things running then.
        Assert.Equal(new BackgroundChord(2, 1), chords.Next(1000, 1, 2, false));

        // Nothing while it sounds (4.6 s) and while the pause lasts (2 s).
        Assert.Null(chords.Next(1001, 1, 2, false));
        Assert.Null(chords.Next(1000 + 4600, 1, 2, false));
        Assert.Null(chords.Next(1000 + 4600 + 1999, 1, 2, false));

        // Then the next, another chord: 0 among the others than 2 is 3.
        Assert.Equal(new BackgroundChord(3, 1), chords.Next(1000 + 4600 + 2000, 1, 2, false));

        // With a pause of 20 s the third waits 20 s after the second's end; 0 among the others than 3 is 0.
        Assert.Null(chords.Next(7600 + 4600 + 19_999, 1, 20, false));
        Assert.Equal(new BackgroundChord(0, 1), chords.Next(7600 + 4600 + 20_000, 1, 20, false));
        Assert.Equal(4600, BackgroundChords.ChordMilliseconds);
        Assert.Equal(AxKit.Core.Audio.AmbientSounds.ChordSeconds * 1000, BackgroundChords.ChordMilliseconds);
    }

    [Fact]
    public void The_same_chord_never_comes_twice_running_and_every_other_can_come()
    {
        var dice = new Dice(0, 0, 1, 2, 2, 1, 0, 2, 1, 0);
        var chords = new BackgroundChords(4, dice.Roll);
        var played = new List<int>();
        for (var n = 0; n < 10; n++)
        {
            played.Add(chords.Next(n * 10_000L, 1, 2, false)!.Chord);
        }

        // The first is chosen among all four, each later one among the three others.
        Assert.Equal(4, dice.Bounds[0]);
        Assert.All(dice.Bounds.Skip(1), bound => Assert.Equal(3, bound));
        for (var n = 1; n < played.Count; n++)
        {
            Assert.NotEqual(played[n - 1], played[n]);
        }

        Assert.Equal([0, 1, 3, 2, 1, 3, 0, 3, 1, 2], played);

        // With the real random numbers, a long run never repeats a chord and plays all four.
        var real = new BackgroundChords(4, new Random(7).Next);
        var seen = new List<int>();
        for (var n = 0; n < 400; n++)
        {
            seen.Add(real.Next(n * 10_000L, 1, 2, false)!.Chord);
        }

        Assert.All(seen.Zip(seen.Skip(1)), pair => Assert.NotEqual(pair.First, pair.Second));
        Assert.Equal([0, 1, 2, 3], seen.Distinct().Order());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(9, 5)]
    public void A_chord_has_a_tone_more_for_each_thing_running_five_at_most(int total, int things)
    {
        Assert.Equal(things, BackgroundChords.Things(total));
        Assert.Equal(things, new BackgroundChords(4, _ => 0).Next(0, total, 2, false)!.Things);
    }

    [Fact]
    public void A_thing_that_starts_during_a_chord_is_heard_in_the_next_and_none_starts_at_zero()
    {
        var chords = new BackgroundChords(4, _ => 0);
        Assert.Equal(1, chords.Next(0, 1, 2, false)!.Things);

        // Two more start while the chord sounds and in the pause: no chord is cut or added; the next has three.
        Assert.Null(chords.Next(2000, 3, 2, false));
        Assert.Null(chords.Next(5000, 3, 2, false));
        Assert.Equal(3, chords.Next(6600, 3, 2, false)!.Things);

        // Nothing runs any more: no chord starts, however long it is.
        Assert.Null(chords.Next(60_000, 0, 2, false));
        Assert.Null(chords.Next(600_000, 0, 2, false));

        // Work starts again long after: a chord at once.
        Assert.Equal(2, chords.Next(700_000, 2, 2, false)!.Things);
    }

    [Fact]
    public void A_chord_waits_while_a_reply_is_read_and_comes_as_soon_as_it_is_not()
    {
        var chords = new BackgroundChords(4, _ => 1);
        Assert.Null(chords.Next(0, 1, 2, true));
        Assert.Null(chords.Next(3000, 1, 2, true));
        Assert.Equal(new BackgroundChord(1, 1), chords.Next(3250, 1, 2, false));
        Assert.Equal(1, chords.Last);
    }

    [Fact]
    public void The_settings_start_with_both_sounds_on_the_words_off_and_a_pause_of_two_seconds()
    {
        var settings = new AppSettings();
        Assert.True(settings.SoundOnBackgroundWork);
        Assert.True(settings.BackgroundPulse);
        Assert.False(settings.SayBackgroundWork);
        // The live progress rows: hidden at first (the user's decision of 2026-11-15).
        Assert.False(settings.ShowProgressRows);
        Assert.Equal(2, settings.BackgroundChordPauseSeconds);
        Assert.Equal([2, 5, 10, 20], BackgroundChords.PauseSeconds);
        Assert.Equal(5, BackgroundChords.SanitizePauseSeconds(5));
        Assert.Equal(20, BackgroundChords.SanitizePauseSeconds(20));
        Assert.Equal(2, BackgroundChords.SanitizePauseSeconds(0));
        Assert.Equal(2, BackgroundChords.SanitizePauseSeconds(30));
    }

    /// <summary>A file that holds a pause keeps it; one with an invalid pause gets 2.</summary>
    [Theory]
    [InlineData("{ \"backgroundChordPauseSeconds\": 10 }", 10)]
    [InlineData("{ \"backgroundChordPauseSeconds\": 7 }", 2)]
    public void A_settings_file_that_holds_a_pause_keeps_a_valid_one(string json, int pause)
    {
        var path = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
            var settings = AppSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal(pause, settings.BackgroundChordPauseSeconds);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    /// <summary>
    /// A file of the first recurring sound, the breath, holds how often it came (<c>backgroundPulseSeconds</c>): read
    /// without an error, not carried over, so the pause is 2; the switch, <c>backgroundPulse</c>, keeps its name and its value.
    /// </summary>
    [Fact]
    public void A_settings_file_of_the_breath_is_read_without_error_and_gets_a_pause_of_two_seconds()
    {
        var path = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ \"backgroundPulse\": false, \"backgroundPulseSeconds\": 20 }");
            var settings = AppSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.False(settings.BackgroundPulse);
            Assert.Equal(2, settings.BackgroundChordPauseSeconds);

            // Saved again, the old key is gone and the new one is written.
            settings.Save(path);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("backgroundPulseSeconds", text, StringComparison.Ordinal);
            Assert.Contains("\"backgroundChordPauseSeconds\": 2", text, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    /// <summary>A settings file written before these settings existed: both sounds are on, the words off.</summary>
    [Fact]
    public void A_settings_file_from_before_the_background_sounds_has_both_on()
    {
        var path = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ \"ptyColumns\": 200, \"soundOnBell\": false, \"workingMusic\": true }");
            var settings = AppSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal(200, settings.PtyColumns);
            Assert.True(settings.SoundOnBackgroundWork);
            Assert.True(settings.BackgroundPulse);
            Assert.False(settings.SayBackgroundWork);
            Assert.Equal(2, settings.BackgroundChordPauseSeconds);
            Assert.False(settings.ShowProgressRows);

            // The option turned on is kept in the file under its name.
            settings.ShowProgressRows = true;
            settings.Save(path);
            Assert.Contains("\"showProgressRows\": true", File.ReadAllText(path), StringComparison.Ordinal);
            Assert.True(AppSettings.Load(path, out _).ShowProgressRows);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    /// <summary>The model's own texts feed the count: the mode line as Claude Code draws it, with a shell and the panel's rows.</summary>
    [Fact]
    public void A_frame_with_a_shell_on_the_mode_line_and_two_agents_in_the_panel_counts_three()
    {
        var model = new SessionModel(120, 20);
        const string esc = "\x1b";
        TestHelpers.Feed(model, $"{esc}[?25l{esc}[2J{esc}[Hclaude: started\r\nWaiting for 2 background agents to finish\r\nauto mode on  ·  1 shell  ·  ↓ to manage\r\n  ◯  general-purpose Checking callers running\r\n  ◯  Explore Reading the spec running\r\n${esc}[?25h");
        var count = BackgroundWork.Count(model.Background, model.BackgroundAgents.Count, model.BackgroundAgentsAwaited);
        Assert.Equal(new BackgroundCount(1, 2), count);
        Assert.Equal("3 in the background: 1 shell, 2 agents", BackgroundWork.Status(count));
        Assert.Equal("3 in the background: 1 shell, 2 agents. general-purpose: Checking callers. Explore: Reading the spec", BackgroundWork.Said(count, model.BackgroundAgents));
    }
}

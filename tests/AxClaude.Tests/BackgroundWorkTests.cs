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

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(9, 5)]
    public void The_recurring_sound_has_one_note_for_each_thing_and_five_at_most(int total, int notes)
    {
        Assert.Equal(notes, BackgroundWork.Notes(total));
    }

    [Fact]
    public void The_settings_start_with_both_sounds_on_the_words_off_and_twenty_seconds()
    {
        var settings = new AppSettings();
        Assert.True(settings.SoundOnBackgroundWork);
        Assert.True(settings.BackgroundPulse);
        Assert.False(settings.SayBackgroundWork);
        Assert.Equal(20, settings.BackgroundPulseSeconds);
        Assert.Equal([10, 20, 30, 60], BackgroundWork.PulseSeconds);
        Assert.Equal(30, BackgroundWork.SanitizePulseSeconds(30));
        Assert.Equal(20, BackgroundWork.SanitizePulseSeconds(0));
        Assert.Equal(20, BackgroundWork.SanitizePulseSeconds(45));
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
            Assert.Equal(20, settings.BackgroundPulseSeconds);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void The_recurring_sound_has_a_layer_for_every_note_the_rules_ask_for()
    {
        Assert.Equal(BackgroundWork.MostNotes, AxKit.Core.Audio.AmbientSounds.MostLayers);
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

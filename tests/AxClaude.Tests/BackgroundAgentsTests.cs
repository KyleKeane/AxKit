using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's panel of running agents, read as well as hidden: the agents the latest frame shows
/// (<see cref="SessionModel.BackgroundAgents"/>), the count of the waiting row, and what changed between two
/// readings (<see cref="AgentPanel.Changes"/>), so that a window can say so on a change and never on a redraw.
/// The rows are those heard on 2026-11-15, with neutral descriptions; no recording yet.
/// </summary>
public class BackgroundAgentsTests
{
    private const string Ready = "auto mode on (shift+tab to cycle)";
    private const string First = "  ◯  general-purpose Flipping a default in a settings file running";
    private const string Second = "  ◯  general-purpose Inspect the transcript for garbling running";

    private static readonly BackgroundAgent Flipping = new("general-purpose", "Flipping a default in a settings file");
    private static readonly BackgroundAgent Inspect = new("general-purpose", "Inspect the transcript for garbling");

    [Fact]
    public void A_row_gives_the_agents_type_and_description_in_either_form()
    {
        Assert.Equal(Inspect, LineClassifier.AgentOf(Second));
        Assert.Equal(new BackgroundAgent("Explore", "Reading the spec"), LineClassifier.AgentOf("◯  Explore Reading the spec running"));
        Assert.Equal(new BackgroundAgent("Explore", string.Empty), LineClassifier.AgentOf("◯  Explore running"));

        // The form with the time and the tokens, while the Agent tool runs (2.1.300).
        Assert.Equal(new BackgroundAgent("general-purpose", "Checking callers"), LineClassifier.AgentOf("   ◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens"));
        Assert.Equal(new BackgroundAgent("Explore", "Reading the spec"), LineClassifier.AgentOf("◯  Explore Reading the spec 3s"));
        Assert.Equal(new BackgroundAgent("Plan", "Wait 5s then go"), LineClassifier.AgentOf("◯  Plan Wait 5s then go 1h 2m"));

        Assert.Null(LineClassifier.AgentOf("- ◯ the second agent is still running"));
        Assert.Null(LineClassifier.AgentOf("◯ running"));
        Assert.Null(LineClassifier.AgentOf("claude: two agents are running"));

        Assert.Equal(2, LineClassifier.BackgroundWaitCount("Waiting for 2 background agents to finish"));
        Assert.Equal(1, LineClassifier.BackgroundWaitCount("  Waiting for 1 background agent to finish"));
        Assert.Equal(0, LineClassifier.BackgroundWaitCount("Waiting for 2 background agents to finish."));
        Assert.Equal(0, LineClassifier.BackgroundWaitCount(Ready));
    }

    [Fact]
    public void The_model_shows_the_agents_of_the_latest_frame()
    {
        var model = new SessionModel(120, 20);
        Feed(model, $"{Esc}[?25l{Ready}\r\n${Esc}[?25h");
        Assert.Empty(model.BackgroundAgents);
        Assert.Equal(0, model.BackgroundAgentsAwaited);

        // The rows captured: two agents, top to bottom.
        Feed(model, Panel("Waiting for 2 background agents to finish", Ready + "  ·  ↓ to manage", "  ● main", First, Second));
        Assert.Equal([Flipping, Inspect], model.BackgroundAgents);
        Assert.Equal(2, model.BackgroundAgentsAwaited);

        // A redraw of the same panel: the same list, no change.
        var shown = model.BackgroundAgents;
        Feed(model, Panel("Waiting for 2 background agents to finish", Ready + "  ·  ↓ to manage", "  ● main", First, Second));
        Assert.Same(shown, model.BackgroundAgents);
        Assert.False(AgentPanel.Changes(shown, model.BackgroundAgents).Any);

        // /tasks over the panel hides the mode line: the agents are kept, as the background count is.
        Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[HBackground\r\n↑/↓ to select · Enter to view · Esc to close{Esc}[?25h");
        Assert.Same(shown, model.BackgroundAgents);
        Assert.Equal(2, model.BackgroundAgentsAwaited);

        // One agent has left.
        Feed(model, Panel("Waiting for 1 background agent to finish", Ready + "  ·  ↓ to manage", "  ● main", Second));
        var changes = AgentPanel.Changes(shown, model.BackgroundAgents);
        Assert.Empty(changes.Started);
        Assert.Equal([Flipping], changes.Finished);
        Assert.Equal(1, model.BackgroundAgentsAwaited);

        // The panel is gone.
        var one = model.BackgroundAgents;
        Feed(model, Panel(Ready));
        Assert.Empty(model.BackgroundAgents);
        Assert.Equal(0, model.BackgroundAgentsAwaited);
        Assert.Equal([Inspect], AgentPanel.Changes(one, model.BackgroundAgents).Finished);

        // A restart of Claude starts with none.
        Feed(model, Panel(Ready + "  ·  ↓ to manage", First));
        Assert.Single(model.BackgroundAgents);
        model.ResetScreen();
        Assert.Empty(model.BackgroundAgents);
    }

    [Fact]
    public void The_rows_above_the_spinner_while_the_agent_tool_runs_are_read_too_and_their_times_are_no_change()
    {
        var model = new SessionModel(120, 20);
        Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: on it\r\n◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens\r\n◯  Explore Reading the spec 3s\r\nCerebrating…\r\n{Ready}  ·  esc to interrupt\r\n${Esc}[?25h");
        var before = model.BackgroundAgents;
        Assert.Equal([new BackgroundAgent("general-purpose", "Checking callers"), new BackgroundAgent("Explore", "Reading the spec")], before);

        Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: on it\r\n◯  general-purpose Checking callers 2m 16s · ↓ 1.3k tokens\r\n◯  Explore Reading the spec 4s\r\nCerebrating…\r\n{Ready}  ·  esc to interrupt\r\n${Esc}[?25h");
        Assert.Equal(before, model.BackgroundAgents);
        Assert.Same(BackgroundAgentChanges.None, AgentPanel.Changes(before, model.BackgroundAgents));
    }

    [Fact]
    public void Changes_are_the_agents_newly_shown_and_those_no_longer_shown()
    {
        BackgroundAgent[] none = [];
        Assert.Same(BackgroundAgentChanges.None, AgentPanel.Changes(none, none));
        Assert.Same(BackgroundAgentChanges.None, AgentPanel.Changes([Flipping, Inspect], [Inspect, Flipping]));

        var started = AgentPanel.Changes(none, [Flipping, Inspect]);
        Assert.True(started.Any);
        Assert.Equal([Flipping, Inspect], started.Started);
        Assert.Empty(started.Finished);

        var finished = AgentPanel.Changes([Flipping, Inspect], none);
        Assert.Empty(finished.Started);
        Assert.Equal([Flipping, Inspect], finished.Finished);

        var both = AgentPanel.Changes([Flipping], [Inspect]);
        Assert.Equal([Inspect], both.Started);
        Assert.Equal([Flipping], both.Finished);

        // Two agents with the same words are two: one of them leaving is one finished.
        Assert.Equal([Flipping], AgentPanel.Changes([Flipping, Flipping], [Flipping]).Finished);
        Assert.Equal([Flipping], AgentPanel.Changes([Flipping], [Flipping, Flipping]).Started);
    }

    /// <summary>A frame that clears the screen and draws a reply row, the given rows under it and the prompt.</summary>
    private static string Panel(params string[] rows) =>
        $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: started\r\n{string.Join("\r\n", rows)}\r\n${Esc}[?25h";
}

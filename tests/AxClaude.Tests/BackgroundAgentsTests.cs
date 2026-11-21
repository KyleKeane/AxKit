using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's panel of running agents, read as well as hidden: the agents the latest frame shows
/// (<see cref="SessionModel.BackgroundAgents"/>), the count of the waiting row, and what changed between two
/// readings (<see cref="AgentPanel.Changes"/>), so that a window can say so on a change and never on a redraw.
/// The rows are those heard on 2026-11-15, with neutral descriptions; the last test reads a recording.
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

    /// <summary>
    /// The window's own parser read off a recording of Claude Code with agents in the background
    /// (tests/fixtures/timelines/background-agents, 2.1.325, made through the whitelist of tools/ClaudeProbe): what
    /// <see cref="SessionModel.BackgroundAgents"/> and the waiting row give, frame by frame as the app ends frames
    /// (a chunk 100 ms or more after the one before), each change once. The core's replay reads the same recording's
    /// background list from the session file (TimelineTests); this is the screen's side of it.
    /// </summary>
    [Fact]
    public void The_recording_with_background_agents_gives_the_window_its_agents()
    {
        const string fixture = "timelines/background-agents/pty.vt";
        var model = new SessionModel(240, 50);
        var bytes = Fixture(fixture);
        var seen = new List<string>();
        var previousMs = 0;

        void Look()
        {
            model.EndFrame();
            var now = $"{model.BackgroundAgents.Count} shown, {model.BackgroundAgentsAwaited} awaited, mode line: {model.Background ?? "none"}"
                + string.Concat(model.BackgroundAgents.Select(a => $" [{a.Type}: {a.Description}]"));
            if (seen.Count == 0 || seen[^1] != now)
            {
                seen.Add(now);
            }
        }

        foreach (var chunk in Chunks(fixture))
        {
            if (chunk.Ms - previousMs >= 100)
            {
                Look();
            }

            model.Feed(bytes.AsSpan(chunk.Offset, chunk.Length));
            previousMs = chunk.Ms;
        }

        Look();
        // Two agents, one after the other: each is shown while the Agent tool runs, then awaited (the waiting row),
        // then gone; the mode line says only that there is something to manage. The core's replay of the session
        // file gives the same four changes (tasks 1, 0, 1, 0).
        Assert.Equal(
            [
                "0 shown, 0 awaited, mode line: none",
                "1 shown, 0 awaited, mode line: see /tasks [general-purpose: Run echo sub command]",
                "1 shown, 1 awaited, mode line: see /tasks [general-purpose: Run echo sub command]",
                "0 shown, 0 awaited, mode line: none",
                "1 shown, 0 awaited, mode line: see /tasks [general-purpose: Run ping delay command]",
                "1 shown, 1 awaited, mode line: see /tasks [general-purpose: Run ping delay command]",
                "0 shown, 0 awaited, mode line: none",
            ],
            seen);
    }

    /// <summary>
    /// Shells left running when the turn ends, with Claude Code 2.1.330 (tests/fixtures/timelines/background-shells-330,
    /// recorded 2026-11-21 for the maintainer's report of 2026-11-18 that nothing told him a background command still
    /// ran): a PowerShell command, then a Bash command beside it. The mode line names them while Claude works and
    /// after the turn has ended, which is what the status bar and the closing question read. And the recording's own
    /// find: a remark of Claude Code's under the mode line ("Removed 1 invisible character from your prompt"), which,
    /// unknown, made the mode line a line of the reply and the turn idle while it ran.
    /// </summary>
    [Fact]
    public void The_recording_with_shells_left_running_keeps_their_count_after_the_turn_and_no_interface_row_in_the_reply()
    {
        const string fixture = "timelines/background-shells-330/pty.vt";
        var model = new SessionModel(240, 50);
        var bytes = Fixture(fixture);
        var seen = new List<string>();
        var leaked = new List<string>();
        var previousMs = 0;

        void Look()
        {
            model.EndFrame();
            var now = model.Background ?? "none";
            if (seen.Count == 0 || seen[^1] != now)
            {
                seen.Add(now);
            }

            leaked.AddRange(model.Lines.Where(line => !line.Hidden
                && (ClaudeCodeTexts.ModeLine().IsMatch(line.Text) || ClaudeCodeTexts.RemovedFromPrompt().IsMatch(line.Text))).Select(line => line.Text));
        }

        foreach (var chunk in Chunks(fixture))
        {
            if (chunk.Ms - previousMs >= 100)
            {
                Look();
            }

            model.Feed(bytes.AsSpan(chunk.Offset, chunk.Length));
            previousMs = chunk.Ms;
        }

        Look();
        // None, then one shell from the moment the first command is in the background, through the end of its turn
        // and the idle prompt after it, then two.
        Assert.Equal(["none", "1 shell", "2 shells"], seen.Take(3));
        // The count still stands at the recording's end, the turn over and both commands running: what the closing
        // question asks about.
        Assert.Equal("2 shells", model.Background);
        Assert.Empty(leaked.Distinct());
    }

    [Theory]
    [InlineData("auto mode on  ·  1 shell  ·  esc to interrupt · ↓ to manage")]
    [InlineData("auto mode on  ·  1 shell  ·  ↓ to manage")]
    [InlineData("auto mode on  ·  1 shell")]
    [InlineData("auto mode on (shift+tab to cycle)  ·  esc to interrupt")]
    [InlineData("Removed 1 invisible character from your prompt")]
    [InlineData("Removed 12 invisible characters from your prompt")]
    public void The_rows_of_2_1_330_around_a_background_shell_are_interface(string row)
    {
        Assert.True(LineClassifier.IsChrome(row));
    }
}
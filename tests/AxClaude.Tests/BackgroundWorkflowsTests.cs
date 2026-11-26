using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's live row of a dynamic workflow and its waiting row (SPEC.md FR-7.10, 2026-11-15), and any other live
/// progress row of background work: read as well as hidden, so that the rows above them (the spinner, the mode line)
/// stay out of the conversation too, and shown, never spoken, with the option. The rows are those of the user's window
/// on 2026-11-15 (Claude Code 2.1.325, screen reader mode), exactly as it held them; no recording yet.
/// </summary>
public class BackgroundWorkflowsTests
{
    private const string Fourteen = "   ◯  axkit-design-map-and-verify 14%  1/7 · 9m27s · ↓ 1.7m tokens · ⚠ Large workflow · /workflows to stop";
    private const string EightyEight = "   ◯  axkit-design-map-and-verify 88%  8/9 · 20m18s · ↓ 2.5m tokens · ⚠ Large workflow · /workflows to stop";
    private const string Waiting = "Waiting for 1 dynamic workflow to finish";
    private const string Working = "auto mode on (shift+tab to cycle)  ·  esc to interrupt";
    private const string Ready = "auto mode on (shift+tab to cycle)";

    /// <summary>A row of the same class whose details match no known form: the next kind Claude Code invents.</summary>
    private const string Unknown = "   ◯  nightly-index rebuilding · 3 of 5 parts";

    private static readonly BackgroundWorkflow AtFourteen = new("axkit-design-map-and-verify", 14, 1, 7, "9m27s");
    private static readonly BackgroundWorkflow AtEightyEight = new("axkit-design-map-and-verify", 88, 8, 9, "20m18s");

    [Fact]
    public void The_workflows_row_gives_its_name_percent_steps_and_time()
    {
        Assert.Equal(AtFourteen, LineClassifier.WorkflowOf(Fourteen));
        Assert.Equal(AtEightyEight, LineClassifier.WorkflowOf(EightyEight));
        var m = ClaudeCodeTexts.WorkflowRow().Match(Fourteen);
        Assert.Equal(["axkit-design-map-and-verify", "14", "1", "7", "9m27s"], new[] { "name", "percent", "done", "total", "elapsed" }.Select(g => m.Groups[g].Value));

        // The tails after the time are optional and may change: the row without the warning, without anything after the time.
        Assert.Equal(AtFourteen, LineClassifier.WorkflowOf("   ◯  axkit-design-map-and-verify 14%  1/7 · 9m27s · ↓ 1.7m tokens"));
        Assert.Equal(AtFourteen, LineClassifier.WorkflowOf("◯  axkit-design-map-and-verify 14%  1/7 · 9m27s"));
        Assert.Equal(new BackgroundWorkflow("nightly", 0, 0, 3, "45s"), LineClassifier.WorkflowOf("   ◯  nightly 0%  0/3 · 45s · /workflows to stop"));

        // Its time alone ("· 45s") would read as an agent's time: a workflow's row is never an agent.
        Assert.Null(LineClassifier.AgentOf("   ◯  nightly 0%  0/3 · 45s"));
        Assert.Null(LineClassifier.AgentOf(Fourteen));
        Assert.Null(LineClassifier.WorkflowOf("  ◯  general-purpose Inspect the transcript for garbling running"));
        Assert.Null(LineClassifier.WorkflowOf("claude: the workflow is 14%  1/7 · 9m27s along"));
    }

    [Fact]
    public void The_waiting_row_gives_its_number_and_what_it_waits_for()
    {
        Assert.Equal(BackgroundKind.Workflow, LineClassifier.BackgroundWaitOf(Waiting, out var workflows));
        Assert.Equal(1, workflows);
        Assert.Equal(BackgroundKind.Workflow, LineClassifier.BackgroundWaitOf("Waiting for 2 dynamic workflows to finish", out workflows));
        Assert.Equal(2, workflows);
        Assert.Equal(BackgroundKind.Agent, LineClassifier.BackgroundWaitOf("Waiting for 2 background agents to finish", out var agents));
        Assert.Equal(2, agents);
        // A kind of neither is the row still, counted as work of a kind not known.
        Assert.Equal(BackgroundKind.Other, LineClassifier.BackgroundWaitOf("Waiting for 3 background shells to finish", out var other));
        Assert.Equal(3, other);

        Assert.Equal(BackgroundKind.Agent, LineClassifier.BackgroundWaitOf("Waiting for 1 background agent to finish", out agents));
        Assert.Equal(1, agents);

        // A reply's own sentence is not the row.
        Assert.Equal(BackgroundKind.None, LineClassifier.BackgroundWaitOf(Waiting + ".", out var none));
        Assert.Equal(0, none);
        Assert.Equal(BackgroundKind.None, LineClassifier.BackgroundWaitOf("Waiting for 2 people to finish", out _));
        Assert.Equal(BackgroundKind.None, LineClassifier.BackgroundWaitOf("- " + Waiting, out _));
    }

    [Fact]
    public void Every_live_progress_row_is_an_interface_row_and_a_reply_row_is_not()
    {
        Assert.True(LineClassifier.IsProgressRow(Fourteen));
        Assert.True(LineClassifier.IsProgressRow(EightyEight));
        Assert.True(LineClassifier.IsProgressRow(Waiting));
        Assert.True(LineClassifier.IsChrome(Fourteen));
        Assert.True(LineClassifier.IsChrome(Waiting));

        // The agents' rows match what they matched before.
        Assert.Matches(ClaudeCodeTexts.AgentRow(), "◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens");
        Assert.Matches(ClaudeCodeTexts.IdleAgentRow(), "  ◯  general-purpose Inspect the transcript for garbling running");
        Assert.Matches(ClaudeCodeTexts.BackgroundWait(), "Waiting for 2 background agents to finish");
        Assert.True(LineClassifier.IsProgressRow("◯  Explore Reading the spec 3s"));
        Assert.True(LineClassifier.IsProgressRow("  ◯  general-purpose Inspect the transcript for garbling running"));
        // And an agent with millions of tokens, written as the workflow's row writes them.
        Assert.Equal(new BackgroundAgent("general-purpose", "Checking callers"), LineClassifier.AgentOf("◯  general-purpose Checking callers 12m 5s · ↓ 1.7m tokens"));

        // The general class: the mark, two spaces, text.
        Assert.Matches(ClaudeCodeTexts.ProgressRow(), Unknown);
        Assert.True(LineClassifier.IsProgressRow(Unknown));
        Assert.Null(LineClassifier.AgentOf(Unknown));
        Assert.Null(LineClassifier.WorkflowOf(Unknown));

        // A reply's own rows with the mark are not.
        Assert.False(LineClassifier.IsProgressRow("- ◯ the second agent is still running"));
        Assert.False(LineClassifier.IsProgressRow("◯ running"));
        Assert.False(LineClassifier.IsProgressRow("1. ◯ Talk to your codebase"));
        Assert.False(LineClassifier.IsProgressRow("claude: the workflow axkit-design-map-and-verify is at 14%"));
    }

    [Fact]
    public void A_frame_that_ends_with_the_spinner_the_mode_line_and_the_live_row_shows_none_of_them()
    {
        // The fault of 2026-11-15: unknown and lowest, the live row kept the spinner and the mode line in the conversation.
        var model = new SessionModel(160, 20);
        Feed(model, Frame("claude: The workflow runs in the background.", "Jitterbugging…", Working, Fourteen));
        Assert.Equal(["claude: The workflow runs in the background."], Visible(model).Where(t => t.Length > 0));
        Assert.True(model.Working);
        Assert.Equal("auto mode on", model.Mode);
        Assert.Equal([AtFourteen], model.BackgroundWorkflows);
        Assert.Equal(0, model.BackgroundWorkflowsAwaited);
        Assert.Equal(0, model.BackgroundUnknown);
        Assert.Empty(model.BackgroundAgents);

        // A redraw of the same row: the same list. The row moving on: a new reading.
        var shown = model.BackgroundWorkflows;
        Feed(model, Frame("claude: The workflow runs in the background.", "Jitterbugging…", Working, Fourteen));
        Assert.Same(shown, model.BackgroundWorkflows);
        Feed(model, Frame("claude: The workflow runs in the background.", "Jitterbugging…", Working, EightyEight));
        Assert.Equal([AtEightyEight], model.BackgroundWorkflows);
        Assert.Same(BackgroundWorkflowChanges.None, AgentPanel.WorkflowChanges(shown, model.BackgroundWorkflows));

        // After the turn: the waiting row over the mode line, the live row under it; still nothing of them shown.
        Feed(model, Frame("claude: The workflow runs in the background.", Waiting, Ready, EightyEight));
        Assert.Equal(["claude: The workflow runs in the background."], Visible(model).Where(t => t.Length > 0));
        Assert.False(model.Working);
        Assert.Equal([AtEightyEight], model.BackgroundWorkflows);
        Assert.Equal(1, model.BackgroundWorkflowsAwaited);

        // /workflows or /tasks over it hides the mode line: what was read is kept.
        var kept = model.BackgroundWorkflows;
        Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[HBackground\r\n↑/↓ to select · Enter to view · Esc to close{Esc}[?25h");
        Assert.Same(kept, model.BackgroundWorkflows);
        Assert.Equal(1, model.BackgroundWorkflowsAwaited);

        // The row gone: none.
        Feed(model, Frame("claude: The workflow runs in the background.", Ready));
        Assert.Empty(model.BackgroundWorkflows);
        Assert.Equal(0, model.BackgroundWorkflowsAwaited);
        Assert.Equal([AtEightyEight], AgentPanel.WorkflowChanges(kept, model.BackgroundWorkflows).Finished);

        // A restart of Claude starts with none.
        Feed(model, Frame("claude: again", Ready, Fourteen));
        Assert.Single(model.BackgroundWorkflows);
        model.ResetScreen();
        Assert.Empty(model.BackgroundWorkflows);
    }

    [Fact]
    public void A_progress_row_of_a_kind_not_known_is_hidden_and_counted_as_one_thing()
    {
        var model = new SessionModel(160, 20);
        Feed(model, Frame("claude: on it", "Jitterbugging…", Working, Unknown));
        Assert.Equal(["claude: on it"], Visible(model).Where(t => t.Length > 0));
        Assert.Equal(1, model.BackgroundUnknown);
        Assert.Empty(model.BackgroundWorkflows);
        Assert.Empty(model.BackgroundAgents);
        Assert.Equal(new BackgroundCount(0, 0, 1), BackgroundWork.Count(model.Background, model.BackgroundAgents.Count, model.BackgroundAgentsAwaited,
            model.BackgroundWorkflows.Count, model.BackgroundWorkflowsAwaited, model.BackgroundUnknown));

        // A waiting row of a kind not known, over the same row: the larger of the two, the same things.
        Feed(model, Frame("claude: on it", "Waiting for 1 background index to finish", Ready, Unknown));
        Assert.Equal(["claude: on it"], Visible(model).Where(t => t.Length > 0));
        Assert.Equal(1, model.BackgroundUnknown);

        Feed(model, Frame("claude: on it", Ready));
        Assert.Equal(0, model.BackgroundUnknown);
    }

    [Fact]
    public void With_the_option_the_rows_stand_in_the_conversation_as_progress_lines_and_the_rows_above_them_stay_hidden()
    {
        var model = new SessionModel(160, 20) { ShowProgressRows = true };
        Feed(model, Frame("claude: The workflow runs in the background.", "Jitterbugging…", Working, Fourteen));
        Assert.Equal(["claude: The workflow runs in the background.", Fourteen], Visible(model).Where(t => t.Length > 0));
        Assert.Equal(LineKind.Progress, model.Lines.Single(l => l.Text == Fourteen).Kind);
        Assert.Equal([AtFourteen], model.BackgroundWorkflows);

        Feed(model, Frame("claude: The workflow runs in the background.", Waiting, Ready, EightyEight));
        Assert.Equal(["claude: The workflow runs in the background.", Waiting, EightyEight], Visible(model).Where(t => t.Length > 0));
        Assert.All(model.Lines.Where(l => !l.Hidden && (l.Text == Waiting || l.Text == EightyEight)), l => Assert.Equal(LineKind.Progress, l.Kind));
        Assert.Equal(1, model.BackgroundWorkflowsAwaited);

        // Turned off: hidden again at the next frame end; gone: neither shown nor a progress line any more.
        model.ShowProgressRows = false;
        model.EndFrame();
        Assert.Equal(["claude: The workflow runs in the background."], Visible(model).Where(t => t.Length > 0));
        model.ShowProgressRows = true;
        Feed(model, Frame("claude: The workflow runs in the background.", Ready));
        Assert.Equal(["claude: The workflow runs in the background."], Visible(model).Where(t => t.Length > 0));
        Assert.DoesNotContain(model.Lines, l => !l.Hidden && l.Kind == LineKind.Progress);
    }

    [Fact]
    public void The_agents_rows_are_shown_with_the_option_too()
    {
        var model = new SessionModel(120, 20) { ShowProgressRows = true };
        const string agent = "  ◯  general-purpose Inspect the transcript for garbling running";
        Feed(model, Frame("claude: Both agents are started.", "Waiting for 1 background agent to finish", Ready + "  ·  ↓ to manage", "  ● main", agent));
        Assert.Equal(["claude: Both agents are started.", "Waiting for 1 background agent to finish", agent], Visible(model).Where(t => t.Length > 0));
        Assert.Single(model.BackgroundAgents);
        Assert.Equal(1, model.BackgroundAgentsAwaited);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_rows_unchanged_cost_a_frame_end_nothing(bool shown)
    {
        // The live rows stand under every frame while work runs: reading them again adds nothing to what a frame end
        // costs without them (D24; the mode line's own reading allocates, measured 2026-11-15, and is not this test's).
        var without = Warmed(shown, "claude: The workflow runs in the background.", Ready);
        var with = Warmed(shown, "claude: The workflow runs in the background.", Waiting, Ready, EightyEight, "  ◯  general-purpose Checking callers running", Unknown);

        // The two are measured again and again until they agree, for five seconds at most. What a frame end allocates
        // depends on how far the runtime has compiled the code it runs: a method not yet compiled for speed puts small
        // objects on the heap that the compiled one keeps off it, and on a busy computer that compiling comes late.
        // One measurement, and then the least of eight, failed in whole runs of the project while other builds ran
        // (2026-11-15). Rows that really are read again at every frame never agree, however long this waits.
        var clock = System.Diagnostics.Stopwatch.StartNew();
        long plain;
        long rows;
        while (true)
        {
            plain = LeastFrameEnd(without);
            rows = LeastFrameEnd(with);
            if (plain == rows || clock.ElapsedMilliseconds > 5000)
            {
                break;
            }

            Thread.Sleep(50);
        }

        Assert.Equal(plain, rows);
    }

    /// <summary>A model over the given screen, with five frame ends behind it.</summary>
    private static SessionModel Warmed(bool shown, params string[] rows)
    {
        var model = new SessionModel(160, 20) { ShowProgressRows = shown };
        Feed(model, Frame(rows));
        for (var warm = 0; warm < 5; warm++)
        {
            model.EndFrame();
        }

        return model;
    }

    /// <summary>The least that one of fifty frame ends over the same screen allocates.</summary>
    private static long LeastFrameEnd(SessionModel model)
    {
        var least = long.MaxValue;
        for (var measured = 0; measured < 50; measured++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            model.EndFrame();
            least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return least;
    }

    [Fact]
    public void Workflow_changes_are_by_name_and_a_redraw_is_none()
    {
        BackgroundWorkflow[] none = [];
        var other = new BackgroundWorkflow("nightly", 5, 1, 20, "2m");
        Assert.Same(BackgroundWorkflowChanges.None, AgentPanel.WorkflowChanges(none, none));
        Assert.Same(BackgroundWorkflowChanges.None, AgentPanel.WorkflowChanges([AtFourteen], [AtEightyEight]));
        Assert.Equal([AtFourteen], AgentPanel.WorkflowChanges(none, [AtFourteen]).Started);
        Assert.Equal([AtEightyEight], AgentPanel.WorkflowChanges([AtEightyEight], none).Finished);
        var both = AgentPanel.WorkflowChanges([AtFourteen], [other]);
        Assert.Equal([other], both.Started);
        Assert.Equal([AtFourteen], both.Finished);
        Assert.True(both.Any);
        // Two with the same name are two.
        Assert.Equal([AtFourteen], AgentPanel.WorkflowChanges([AtFourteen, AtFourteen], [AtEightyEight]).Finished);
    }

    /// <summary>A frame that clears the screen and draws the given rows, the prompt under them with the cursor on it.</summary>
    private static string Frame(params string[] rows) =>
        $"{Esc}[?25l{Esc}[2J{Esc}[H{string.Join("\r\n", rows)}\r\n${Esc}[?25h";
}

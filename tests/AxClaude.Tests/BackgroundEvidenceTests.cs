using AxClaude.Core.Flow;

namespace AxClaude.Tests;

/// <summary>
/// The tasks the core keeps from the events (docs/axClaude/events-and-grounding.md EG-7.1, EG-D-12): a stop event's
/// list of what runs in the background, a worker's stop that takes its task off it, a worker's start that adds
/// nothing, and the list emptied when the conversation or the run starts afresh. The tasks and the times are those
/// measured on 2026-11-15 (M13 sections 6 and 7; the timeline fixtures <c>background-shell</c> and
/// <c>background-agents</c>), in milliseconds. Only the background list's decisions are looked at here; the turn's
/// are <see cref="SessionFlowTests"/>'.
/// </summary>
public class BackgroundEvidenceTests
{
    private static readonly FlowTask Ping = new("b0jdigiy3", SessionFlow.BackgroundShellType, "Run ping in background for about 7 seconds", null);
    private static readonly FlowTask EchoAgent = new("a98ad9d19db77d218", SessionFlow.BackgroundAgentType, "Run echo sub command", "general-purpose");
    private static readonly FlowTask PingAgent = new("ab5b9a61663d76c97", SessionFlow.BackgroundAgentType, "Run ping delay command", "general-purpose");

    /// <summary>One core. <see cref="At"/> feeds an input and gives the lines of its background decisions alone.</summary>
    private sealed class Core
    {
        public readonly SessionFlow Flow = new(new SourceHealth());

        /// <summary>The background decisions of the last input.</summary>
        public List<BackgroundListed> Last = [];

        public string[] At(long time, FlowInput input)
        {
            Last = [.. Flow.Handle(input, time).OfType<BackgroundListed>()];
            return [.. Last.Select(decision => decision.ToLine())];
        }
    }

    private static TurnStopped Stopped(params FlowTask[] tasks) => new("ok", tasks);

    private static FrameSeen Frame(int epoch) => new(epoch, WorkSeen.Idle, false, false, true, 0, []);

    [Fact]
    public void A_stop_replaces_the_list_with_its_running_tasks()
    {
        // M13 6, the background shell: the turn's stop event lists the shell while it runs.
        var c = new Core();
        Assert.Empty(c.Flow.Tasks);
        Assert.Equal(["background tasks=1"], c.At(17916, Stopped(Ping)));
        Assert.Equal([Ping], c.Flow.Tasks);
        Assert.Equal([Ping], c.Last[0].Tasks);
        var told = c.Last[0].Tasks;

        // A stop that lists what is listed already decides nothing.
        Assert.Empty(c.At(18000, Stopped(Ping)));

        // The turn Claude Code started by itself when the shell ended: its stop lists none.
        Assert.Equal(["background tasks=0"], c.At(25395, Stopped()));
        Assert.Empty(c.Flow.Tasks);
        Assert.Empty(c.Last[0].Tasks);
        Assert.Empty(c.At(44247, Stopped()));

        // A later list takes the place of the one before; nothing of the earlier one is kept.
        c.At(50000, Stopped(Ping));
        Assert.Equal(["background tasks=1"], c.At(60000, Stopped(EchoAgent)));
        Assert.Equal([EchoAgent], c.Flow.Tasks);
        Assert.Equal(["background tasks=2"], c.At(70000, Stopped(EchoAgent, Ping)));
        Assert.Equal([EchoAgent, Ping], c.Flow.Tasks);

        // What a decision carried stays as it was told, whatever came after it.
        Assert.Equal([Ping], told);
    }

    [Fact]
    public void A_workers_stop_takes_it_off_the_list()
    {
        // M13 7, the background agents: the worker's start, the turn's stop listing the agent, the worker's stop,
        // then the stop of the turn its hand-back started, which lists none.
        var c = new Core();
        Assert.Empty(c.At(8006, new WorkerStarted(EchoAgent.Id, "general-purpose")));
        Assert.Equal(["background tasks=1"], c.At(9951, Stopped(EchoAgent)));
        Assert.Equal(["background tasks=0"], c.At(12833, new WorkerStopped(EchoAgent.Id, "general-purpose")));
        Assert.Empty(c.Flow.Tasks);
        Assert.Empty(c.At(14129, Stopped()));

        // With a shell beside the agent, only the agent goes.
        c.At(20185, new WorkerStarted(PingAgent.Id, "general-purpose"));
        Assert.Equal(["background tasks=2"], c.At(21381, Stopped(Ping, PingAgent)));
        Assert.Equal(["background tasks=1"], c.At(29382, new WorkerStopped(PingAgent.Id, "general-purpose")));
        Assert.Equal([Ping], c.Flow.Tasks);

        // A worker's stop of an id not listed changes nothing.
        Assert.Empty(c.At(29400, new WorkerStopped("a0000000000000000", "Explore")));
        Assert.Equal([Ping], c.Flow.Tasks);

        // Each event comes through a process of its own, so a stop event made before the worker's stop can arrive
        // after it, and still list the worker: a worker that stopped is not taken back onto the list.
        Assert.Empty(c.At(29500, Stopped(Ping, PingAgent)));
        Assert.Equal([Ping], c.Flow.Tasks);
        Assert.Equal(["background tasks=0"], c.At(31558, Stopped(PingAgent)));
        Assert.Empty(c.Flow.Tasks);
    }

    [Fact]
    public void A_workers_start_adds_nothing()
    {
        // EG-D-12: a worker's start is counted for the diagnostics and is no place of the number by itself.
        var c = new Core();
        Assert.Empty(c.At(8006, new WorkerStarted(EchoAgent.Id, "general-purpose")));
        Assert.Empty(c.Flow.Tasks);
        Assert.Equal(1, c.Flow.BackgroundWorkersStarted);

        // With a shell listed, a start leaves the list as it is.
        c.At(17916, Stopped(Ping));
        Assert.Empty(c.At(20185, new WorkerStarted(PingAgent.Id, "general-purpose")));
        Assert.Equal([Ping], c.Flow.Tasks);

        // A dynamic workflow of fifty agents, should each fire a start: fifty counted, nothing decided.
        for (var i = 0; i < 50; i++)
        {
            Assert.Empty(c.At(30000 + i, new WorkerStarted($"w{i}", "general-purpose")));
        }

        Assert.Equal(52, c.Flow.BackgroundWorkersStarted);
        Assert.Equal([Ping], c.Flow.Tasks);
    }

    [Fact]
    public void A_new_epoch_or_the_process_end_empties_the_list()
    {
        // EG-4.9: the conversation started afresh under the window, a new epoch of the model.
        var c = new Core();
        c.At(100, Frame(1));
        c.At(17916, Stopped(Ping));
        Assert.Equal(["background tasks=0"], c.At(18000, Frame(2)));
        Assert.Empty(c.Flow.Tasks);

        // A session named as cleared or forked: the same.
        c.At(19000, Stopped(Ping));
        Assert.Equal(["background tasks=0"], c.At(19100, new SessionNamed("22222222", null, SessionSource.Clear)));
        Assert.Empty(c.Flow.Tasks);
        c.At(19200, Stopped(EchoAgent));
        Assert.Equal(["background tasks=0"], c.At(19300, new SessionNamed("33333333", null, SessionSource.Fork)));

        // A session taken up again or compacted is the same conversation: the list stays.
        c.At(19400, Stopped(Ping));
        Assert.Empty(c.At(19500, new SessionNamed("33333333", null, SessionSource.Compact)));
        Assert.Equal([Ping], c.Flow.Tasks);

        // The process's end: everything of the run is over.
        Assert.Equal(["background tasks=0"], c.At(20000, new ProcessEnded()));
        Assert.Empty(c.Flow.Tasks);

        // Nothing was listed: starting afresh decides nothing of the background.
        Assert.Empty(c.At(21000, new ProcessStarted()));
        Assert.Empty(c.At(21100, Frame(1)));
        Assert.Empty(c.At(21200, Frame(2)));
        Assert.Empty(c.At(21300, new ProcessEnded()));
    }
}

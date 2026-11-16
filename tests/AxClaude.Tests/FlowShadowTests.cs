using System.Text.RegularExpressions;
using AxClaude.Core.Flow;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The shadow (docs/axClaude/events-and-grounding.md EG-4.11; <see cref="FlowShadow"/>): what the window's own code
/// did against what the core decided, one line of numbers for each turn that both have ended. The times are the
/// measured ones of a plain turn (section 4.1) where they matter.
/// </summary>
public partial class FlowShadowTests
{
    private static readonly Line Head = new(412, LineKind.ClaudeReply, "claude: The quick brown fox");
    private static readonly Line Row = new(413, LineKind.Plain, "jumps over the lazy dog.");

    [Fact]
    public void A_turn_where_both_say_Done_gives_the_two_times()
    {
        var shadow = new FlowShadow();

        // The core opens the turn at its start mark; the window's code sees the spinner a frame later.
        shadow.Core(new StateChanged(1, FlowState.Working), 1000);
        shadow.Window(ShadowFact.Working, 0, 1040);
        Assert.Null(shadow.TurnLine(1040));
        Assert.Equal(long.MaxValue, shadow.DueAt);

        // The end: the core's state at the end mark, its Done 74 ms after the bell; the window's code idle with the
        // last frame and its Done at the settle, 400 ms after the bell.
        shadow.Core(new StateChanged(1, FlowState.Idle), 8000);
        shadow.Window(ShadowFact.Idle, 0, 8020);
        shadow.Core(new SayDone(1, 1, 74), 8090);
        Assert.Null(shadow.TurnLine(8100));
        shadow.Window(ShadowFact.SaidDone, 0, 8420);

        // Both have ended once neither has done anything more for the settle.
        Assert.Equal(8420 + FlowShadow.SettleMs, shadow.DueAt);
        Assert.Null(shadow.TurnLine(8420 + FlowShadow.SettleMs - 1));
        Assert.Equal("turn=1 differ=none done window=7420 core=7090 spoke window=0 core=0 state-differed=60", shadow.TurnLine(8420 + FlowShadow.SettleMs));

        // The line is given once, and the next turn is counted from its own start.
        Assert.Null(shadow.TurnLine(20000));
        Assert.Equal(long.MaxValue, shadow.DueAt);
        shadow.Window(ShadowFact.Working, 0, 30000);
        shadow.Core(new StateChanged(1, FlowState.Working), 30000);
        shadow.Window(ShadowFact.Idle, 0, 31000);
        shadow.Core(new StateChanged(1, FlowState.Idle), 31000);
        shadow.Core(new SayDone(1, 2, 80), 31080);
        shadow.Window(ShadowFact.SaidDone, 0, 31400);
        Assert.Equal("turn=2 differ=none done window=1400 core=1080 spoke window=0 core=0 state-differed=0", shadow.TurnLine(40000));
    }

    [Fact]
    public void A_Done_only_the_window_said_is_counted()
    {
        // A run without marks: the core says no Done of its own, and the window's settle after the bell says it.
        var shadow = new FlowShadow();
        shadow.Window(ShadowFact.Working, 0, 500);
        shadow.Core(new StateChanged(0, FlowState.Working), 500);
        shadow.Window(ShadowFact.Idle, 0, 2500);
        shadow.Core(new StateChanged(0, FlowState.Idle), 2500);
        shadow.Core(new AttentionBell(0, NoMarks: true), 2510);
        shadow.Window(ShadowFact.SaidDone, 0, 2910);
        Assert.Equal("turn=1 differ=done done window=2410 core=none spoke window=0 core=0 state-differed=0", shadow.TurnLine(2910 + FlowShadow.SettleMs));

        // And the other way round: a Done only the core said.
        shadow.Core(new StateChanged(0, FlowState.Working), 10000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 12000);
        shadow.Core(new SayDone(0, 1, 74), 12074);
        Assert.Equal("turn=2 differ=done,state done window=none core=2074 spoke window=0 core=0 state-differed=2000", shadow.TurnLine(long.MaxValue));

        // Two turns that follow at once, a waiting message's: one stretch with two Dones on each side, whichever
        // side says its second one last.
        shadow.Window(ShadowFact.Working, 0, 20000);
        shadow.Core(new StateChanged(0, FlowState.Working), 20000);
        shadow.Core(new SayDone(0, 2, 70), 21070);
        shadow.Window(ShadowFact.SaidDone, 0, 21400);
        shadow.Window(ShadowFact.Idle, 0, 23000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 23000);
        shadow.Window(ShadowFact.SaidDone, 0, 23400);
        Assert.Null(shadow.TurnLine(23400 + FlowShadow.SettleMs - 400));
        shadow.Core(new SayDone(0, 3, 600), 23600);
        Assert.Equal("turn=3 differ=none done window=1400,3400 core=1070,3600 spoke window=0 core=0 state-differed=0", shadow.TurnLine(23600 + FlowShadow.SettleMs));
    }

    [Fact]
    public void Lines_one_spoke_and_the_other_did_not_are_counted()
    {
        var shadow = new FlowShadow();
        shadow.Window(ShadowFact.Working, 0, 0);
        shadow.Core(new StateChanged(0, FlowState.Working), 0);

        // The window's code speaks five lines in two utterances; the core would speak three of them.
        shadow.Window(ShadowFact.SpokeLines, 2, 900);
        shadow.Core(new SpeakReply(0, [Head, Row]), 950);
        shadow.Window(ShadowFact.SpokeLines, 3, 4000);
        shadow.Core(new SpeakReply(0, [Row]), 4100);
        shadow.Window(ShadowFact.Idle, 0, 4200);

        // While either side works there is no line, however long.
        Assert.Null(shadow.TurnLine(60000));
        shadow.Core(new StateChanged(0, FlowState.Idle), 60000);
        Assert.Equal("turn=1 differ=spoke,state done window=none core=none spoke window=5 core=3 state-differed=55800", shadow.TurnLine(60000 + FlowShadow.SettleMs));

        // Lines spoken with no turn on either side (a repaint the old code read out) are a line of their own.
        shadow.Window(ShadowFact.SpokeLines, 4, 70000);
        Assert.Equal(70000 + FlowShadow.SettleMs, shadow.DueAt);
        Assert.Equal("turn=2 differ=spoke done window=none core=none spoke window=4 core=0 state-differed=0", shadow.TurnLine(70000 + FlowShadow.SettleMs));

        // Nothing spoken is nothing noted, and what the window carries out for the core whatever the charge says nothing of a turn.
        shadow.Window(ShadowFact.SpokeLines, 0, 80000);
        shadow.Window(ShadowFact.Idle, 0, 80000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 80000);
        shadow.Core(new WakeAt(0, 80150), 80000);
        shadow.Core(new Note(0, "stale-turn after=2004"), 80000);
        shadow.Core(new AttentionBell(0, NoMarks: false), 80000);
        Assert.Equal(long.MaxValue, shadow.DueAt);
        Assert.Null(shadow.TurnLine(long.MaxValue));
    }

    [Fact]
    public void The_line_holds_numbers_and_fixed_words_only()
    {
        var shadow = new FlowShadow();
        shadow.Window(ShadowFact.Working, 0, 100);
        shadow.Core(new StateChanged(3, FlowState.Working), 130);
        shadow.Core(new SpeakReply(3, [Head, Row]), 2000);
        shadow.Core(new EntryBound(3, "b8142ecf-0000-4000-8000-000000000000", 412), 2000);
        shadow.Window(ShadowFact.SpokeLines, 2, 2250);
        shadow.Window(ShadowFact.Idle, 0, 2300);
        shadow.Core(new StateChanged(3, FlowState.Idle), 2300);
        shadow.Core(new SayDone(3, 1, 74), 2374);
        shadow.Window(ShadowFact.SaidDone, 0, 2700);
        var line = shadow.TurnLine(long.MaxValue);

        Assert.Equal("turn=1 differ=none done window=2600 core=2274 spoke window=2 core=2 state-differed=30", line);
        Assert.Matches(Shape(), line);

        // Nothing of the lines it counted: not a word of the reply, not an entry's id.
        foreach (var word in new[] { "quick", "brown", "lazy", "claude", "b8142ecf" })
        {
            Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>The line's whole shape: the fixed words, and numbers or "none" after each.</summary>
    [GeneratedRegex(@"^turn=\d+ differ=(none|(done|spoke|state)(,(done|spoke|state))*) done window=(none|\d+(,\d+)*) core=(none|\d+(,\d+)*) spoke window=\d+ core=\d+ state-differed=\d+$")]
    private static partial Regex Shape();
}

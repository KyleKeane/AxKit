using System.Reflection;
using System.Text.RegularExpressions;
using AxClaude.Core.Flow;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The shadow (docs/axClaude/events-and-grounding.md EG-4.11; <see cref="FlowShadow"/>): what the window's own code
/// did against what the core decided, one line of numbers for each turn that both have ended, and for a turn that
/// differed the kinds of what each said, their states and whether a question was open. The times are the measured
/// ones of a plain turn (section 4.1) where they matter.
/// </summary>
public partial class FlowShadowTests
{
    private static readonly Line Head = new(412, LineKind.ClaudeReply, "claude: The quick brown fox");
    private static readonly Line Row = new(413, LineKind.Plain, "jumps over the lazy dog.");
    private static readonly Line ToolRow = new(414, LineKind.Tool, @"tool: Read(D:\work\secret.txt)");
    private static readonly Line SystemRow = new(415, LineKind.System, "[accept edits on]");

    [Fact]
    public void A_turn_where_both_say_Done_gives_the_two_times()
    {
        var shadow = new FlowShadow();

        // The core opens the turn at its start mark; the window's code sees the spinner a frame later.
        shadow.Core(new StateChanged(1, FlowState.Working), 1000);
        shadow.Window(ShadowFact.Working, 1040);
        Assert.Null(shadow.TurnLine(1040));
        Assert.Equal(long.MaxValue, shadow.DueAt);

        // The end: the core's state at the end mark, its Done 74 ms after the bell; the window's code idle with the
        // last frame and its Done at the settle, 400 ms after the bell.
        shadow.Core(new StateChanged(1, FlowState.Idle), 8000);
        shadow.Window(ShadowFact.Idle, 8020);
        shadow.Core(new SayDone(1, 1, 74), 8090);
        Assert.Null(shadow.TurnLine(8100));
        shadow.Window(ShadowFact.SaidDone, 8420);

        // Both have ended once neither has done anything more for the settle. A turn that does not differ has the
        // counts alone.
        Assert.Equal(8420 + FlowShadow.SettleMs, shadow.DueAt);
        Assert.Null(shadow.TurnLine(8420 + FlowShadow.SettleMs - 1));
        Assert.Equal("turn=1 differ=none done window=7420 core=7090 spoke window=0 core=0 state-differed=60", shadow.TurnLine(8420 + FlowShadow.SettleMs));

        // The line is given once, and the next turn is counted from its own start.
        Assert.Null(shadow.TurnLine(20000));
        Assert.Equal(long.MaxValue, shadow.DueAt);
        shadow.Window(ShadowFact.Working, 30000);
        shadow.Core(new StateChanged(1, FlowState.Working), 30000);
        shadow.Window(ShadowFact.Idle, 31000);
        shadow.Core(new StateChanged(1, FlowState.Idle), 31000);
        shadow.Core(new SayDone(1, 2, 80), 31080);
        shadow.Window(ShadowFact.SaidDone, 31400);
        Assert.Equal("turn=2 differ=none done window=1400 core=1080 spoke window=0 core=0 state-differed=0", shadow.TurnLine(40000));
    }

    [Fact]
    public void A_Done_only_the_window_said_is_counted()
    {
        // A run without marks: the core says no Done of its own, and the window's settle after the bell says it.
        var shadow = new FlowShadow();
        shadow.Window(ShadowFact.Working, 500);
        shadow.Core(new StateChanged(0, FlowState.Working), 500);
        shadow.Window(ShadowFact.Idle, 2500);
        shadow.Core(new StateChanged(0, FlowState.Idle), 2500);
        shadow.Core(new AttentionBell(0, NoMarks: true), 2510);
        shadow.Window(ShadowFact.SaidDone, 2910);
        Assert.Equal(
            "turn=1 differ=done done window=2410 core=none spoke window=0 core=0 state-differed=0"
            + " kinds window=[done] core=[] states window=[working:2000,idle:410] core=[working:2000,idle:410]",
            shadow.TurnLine(2910 + FlowShadow.SettleMs));

        // And the other way round: a Done only the core said.
        shadow.Core(new StateChanged(0, FlowState.Working), 10000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 12000);
        shadow.Core(new SayDone(0, 1, 74), 12074);
        Assert.Equal(
            "turn=2 differ=done,state done window=none core=2074 spoke window=0 core=0 state-differed=2000"
            + " kinds window=[] core=[done] states window=[idle:2074] core=[working:2000,idle:74]",
            shadow.TurnLine(long.MaxValue));

        // Two turns that follow at once, a waiting message's: one stretch with two Dones on each side, whichever
        // side says its second one last.
        shadow.Window(ShadowFact.Working, 20000);
        shadow.Core(new StateChanged(0, FlowState.Working), 20000);
        shadow.Core(new SayDone(0, 2, 70), 21070);
        shadow.Window(ShadowFact.SaidDone, 21400);
        shadow.Window(ShadowFact.Idle, 23000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 23000);
        shadow.Window(ShadowFact.SaidDone, 23400);
        Assert.Null(shadow.TurnLine(23400 + FlowShadow.SettleMs - 400));
        shadow.Core(new SayDone(0, 3, 600), 23600);
        Assert.Equal("turn=3 differ=none done window=1400,3400 core=1070,3600 spoke window=0 core=0 state-differed=0", shadow.TurnLine(23600 + FlowShadow.SettleMs));
    }

    [Fact]
    public void Lines_one_spoke_and_the_other_did_not_are_counted_and_named_by_their_kinds()
    {
        var shadow = new FlowShadow();
        shadow.Window(ShadowFact.Working, 0);
        shadow.Core(new StateChanged(0, FlowState.Working), 0);

        // The window's code speaks five lines in two utterances; the core would speak three of them.
        shadow.WindowSpoke([Head, Row], 900);
        shadow.Core(new SpeakReply(0, [Head, Row]), 950);
        shadow.WindowSpoke([Row, Row, Row], 4000);
        shadow.Core(new SpeakReply(0, [Row]), 4100);
        shadow.Window(ShadowFact.Idle, 4200);

        // While either side works there is no line, however long.
        Assert.Null(shadow.TurnLine(60000));
        shadow.Core(new StateChanged(0, FlowState.Idle), 60000);
        Assert.Equal(
            "turn=1 differ=spoke,state done window=none core=none spoke window=5 core=3 state-differed=55800"
            + " kinds window=[reply,row*4] core=[reply,row*2] states window=[working:4200,idle:55800] core=[working:60000]",
            shadow.TurnLine(60000 + FlowShadow.SettleMs));

        // Lines spoken with no turn on either side (a repaint the old code read out) are a line of their own, and
        // the kinds say what they were: here a tool's row, two plain rows and a system line.
        shadow.WindowSpoke([ToolRow, Row, Row, SystemRow], 70000);
        Assert.Equal(70000 + FlowShadow.SettleMs, shadow.DueAt);
        Assert.Equal(
            "turn=2 differ=spoke done window=none core=none spoke window=4 core=0 state-differed=0"
            + " kinds window=[tool,row*2,notice] core=[] states window=[] core=[]",
            shadow.TurnLine(70000 + FlowShadow.SettleMs));

        // Nothing spoken is nothing noted, and what the window carries out for the core whatever the charge says nothing of a turn.
        shadow.WindowSpoke([], 80000);
        shadow.Window(ShadowFact.Idle, 80000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 80000);
        shadow.Core(new WakeAt(0, 80150), 80000);
        shadow.Core(new Note(0, "stale-turn after=2004"), 80000);
        shadow.Core(new AttentionBell(0, NoMarks: false), 80000);
        Assert.Equal(long.MaxValue, shadow.DueAt);
        Assert.Null(shadow.TurnLine(long.MaxValue));
    }

    [Fact]
    public void A_question_open_in_a_turn_that_differs_is_named_and_one_before_the_turn_is_not()
    {
        var shadow = new FlowShadow();

        // A question open while no turn is noted opens none and marks none.
        shadow.QuestionOpen();
        Assert.Equal(long.MaxValue, shadow.DueAt);

        // The core ends its turn at the end mark; the window's code stays working while the question waits.
        shadow.Window(ShadowFact.Working, 0);
        shadow.Core(new StateChanged(0, FlowState.Working), 0);
        shadow.QuestionOpen();
        shadow.Core(new StateChanged(0, FlowState.Idle), 1000);
        shadow.QuestionOpen();
        shadow.Window(ShadowFact.Idle, 5000);
        Assert.Equal(
            "turn=1 differ=state done window=none core=none spoke window=0 core=0 state-differed=4000"
            + " kinds window=[] core=[] states window=[working:5000] core=[working:1000,idle:4000] question=open",
            shadow.TurnLine(long.MaxValue));

        // The next turn has no question of its own.
        shadow.Core(new StateChanged(0, FlowState.Working), 10000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 11000);
        Assert.Equal(
            "turn=2 differ=state done window=none core=none spoke window=0 core=0 state-differed=1000"
            + " kinds window=[] core=[] states window=[idle:1000] core=[working:1000]",
            shadow.TurnLine(long.MaxValue));

        // A turn that does not differ says nothing of its question.
        shadow.Window(ShadowFact.Working, 20000);
        shadow.Core(new StateChanged(0, FlowState.Working), 20000);
        shadow.QuestionOpen();
        shadow.Window(ShadowFact.Idle, 21000);
        shadow.Core(new StateChanged(0, FlowState.Idle), 21000);
        Assert.Equal("turn=3 differ=none done window=none core=none spoke window=0 core=0 state-differed=0", shadow.TurnLine(long.MaxValue));
    }

    [Fact]
    public void The_line_holds_numbers_and_fixed_words_only()
    {
        var shadow = new FlowShadow();
        shadow.Window(ShadowFact.Working, 100);
        shadow.Core(new StateChanged(3, FlowState.Working), 130);
        shadow.Core(new SpeakReply(3, [Head, Row]), 2000);
        shadow.Core(new EntryBound(3, "b8142ecf-0000-4000-8000-000000000000", 412), 2000);
        shadow.WindowSpoke([Head, Row], 2250);
        shadow.Window(ShadowFact.Idle, 2300);
        shadow.Core(new StateChanged(3, FlowState.Idle), 2300);
        shadow.Core(new SayDone(3, 1, 74), 2374);
        shadow.Window(ShadowFact.SaidDone, 2700);
        var line = shadow.TurnLine(long.MaxValue);

        Assert.Equal("turn=1 differ=none done window=2600 core=2274 spoke window=2 core=2 state-differed=30", line);
        Assert.Matches(Shape(), line);

        // Nothing of the lines it counted: not a word of the reply, not an entry's id.
        foreach (var word in new[] { "quick", "brown", "lazy", "claude", "b8142ecf" })
        {
            Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Whatever the spoken lines hold, the line of a turn that differs has the closed set of words and numbers and
    /// nothing else: lines of every kind, of kinds this version does not know, with texts made to look like the
    /// line's own parts.
    /// </summary>
    [Fact]
    public void No_text_of_a_line_reaches_the_line_of_a_turn_that_differs_whatever_the_lines_hold()
    {
        string[] hostile =
        [
            "] question=open kinds window=[done",
            @"tool: Bash(curl https://example.invalid/?key=sk-abc123) D:\work\private.txt",
            "claude: The password is hunter2\r\nEvents: shadow turn=9 differ=none",
            "row*999,more",
            string.Empty,
            new string('x', 5000),
        ];
        var kinds = Enum.GetValues<LineKind>().Append((LineKind)999).Append((LineKind)(-1)).ToList();
        var shadow = new FlowShadow();
        shadow.Window(ShadowFact.Working, 0);
        var id = 1;
        foreach (var kind in kinds)
        {
            foreach (var text in hostile)
            {
                shadow.WindowSpoke([new Line(id++, kind, text)], id);
                if (id % 3 == 0)
                {
                    shadow.Core(new SpeakReply(0, [new Line(id++, kind, text), new Line(id++, kind, text)]), id);
                }
            }
        }

        shadow.QuestionOpen();
        shadow.Window(ShadowFact.Idle, 100000);
        var line = shadow.TurnLine(long.MaxValue);

        Assert.NotNull(line);
        Assert.Matches(Shape(), line);
        Assert.Contains(" kinds window=[", line, StringComparison.Ordinal);
        Assert.EndsWith(" question=open", line, StringComparison.Ordinal);
        foreach (var word in new[] { "sk-abc", "hunter2", "password", "curl", "private", "example", "Bash", "xxx", "999", "turn=9" })
        {
            Assert.DoesNotContain(word, line, StringComparison.Ordinal);
        }
    }

    /// <summary>The formatter itself, handed values outside every set and numbers below zero: still the closed set.</summary>
    [Fact]
    public void The_formatter_writes_the_closed_set_whatever_values_it_is_handed()
    {
        var what = FlowShadow.What(
            [((ShadowKind)999, 3), (ShadowKind.Done, 1), ((ShadowKind)(-5), int.MinValue), (ShadowKind.Row, int.MaxValue)],
            [],
            [((FlowState)77, -5), (FlowState.Working, long.MaxValue), (FlowState.Idle, long.MinValue)],
            [(FlowState.Idle, 0)],
            question: true);
        Assert.Equal($" kinds window=[other*3,done,other,row*{int.MaxValue}] core=[] states window=[other:0,working:{long.MaxValue},idle:0] core=[idle:0] question=open", what);
        Assert.Matches(Shape(), "turn=1 differ=state done window=none core=none spoke window=0 core=0 state-differed=0" + what);

        // A list longer than the limit ends with the one word for the rest.
        var many = Enumerable.Range(0, 40).Select(i => (i % 2 == 0 ? ShadowKind.Reply : ShadowKind.Tool, 1)).ToList();
        var states = Enumerable.Range(0, 40).Select(i => (i % 2 == 0 ? FlowState.Working : FlowState.Idle, (long)i)).ToList();
        var capped = FlowShadow.What(many, many, states, states, question: false);
        Assert.Matches(Shape(), "turn=1 differ=state done window=none core=none spoke window=0 core=0 state-differed=0" + capped);
        Assert.Equal(4, Regex.Matches(capped, ",more]").Count);
        Assert.Equal(FlowShadow.MaxListed * 2, Regex.Matches(capped, "reply|tool").Count);
    }

    /// <summary>The words are one for each value, lower-case letters only, and every kind of line has its word.</summary>
    [Fact]
    public void Every_kind_and_state_has_one_word_of_letters()
    {
        var words = Enum.GetValues<ShadowKind>().Select(FlowShadow.Word).ToList();
        Assert.Equal(words.Count, words.Distinct().Count());
        Assert.All(words, word => Assert.Matches("^[a-z]+$", word));
        Assert.All(Enum.GetValues<FlowState>(), state => Assert.Matches("^[a-z]+$", FlowShadow.Word(state)));
        Assert.All(Enum.GetValues<LineKind>(), kind => Assert.True(Enum.IsDefined(FlowShadow.KindOf(kind))));
        Assert.Equal(ShadowKind.Reply, FlowShadow.KindOf(LineKind.ClaudeReply));
        Assert.Equal(ShadowKind.Row, FlowShadow.KindOf(LineKind.Plain));
        Assert.Equal(ShadowKind.Tool, FlowShadow.KindOf(LineKind.ToolError));
        Assert.Equal(ShadowKind.Other, FlowShadow.KindOf(LineKind.Bookmark));
    }

    /// <summary>
    /// The shadow keeps no text at all: none of its fields is a string, a line or a decision, or a list of them, so
    /// that there is nothing a later change to the line could print by mistake.
    /// </summary>
    [Fact]
    public void The_shadow_keeps_no_text_and_no_line()
    {
        Type[] allowed =
        [
            typeof(bool), typeof(int), typeof(long),
            typeof(List<long>), typeof(List<(ShadowKind Kind, int Count)>), typeof(List<(FlowState State, long Ms)>),
        ];
        var fields = typeof(FlowShadow).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotEmpty(fields);
        Assert.All(fields, field => Assert.Contains(field.FieldType, allowed));

        // And the formatter takes values only: no parameter of it is a text or a line.
        var formatter = typeof(FlowShadow).GetMethod(nameof(FlowShadow.What))!;
        Assert.All(formatter.GetParameters(), parameter =>
        {
            Assert.NotEqual(typeof(string), parameter.ParameterType);
            Assert.DoesNotContain("Line", parameter.ParameterType.ToString(), StringComparison.Ordinal);
        });
    }

    /// <summary>The line's whole shape: the fixed words, and numbers or "none" after each; for a turn that differed, the kinds and the states from their closed sets.</summary>
    [GeneratedRegex(@"^turn=\d+ differ=(none|(done|spoke|state)(,(done|spoke|state))*) done window=(none|\d+(,\d+)*) core=(none|\d+(,\d+)*) spoke window=\d+ core=\d+ state-differed=\d+"
        + @"( kinds window=" + KindList + " core=" + KindList + " states window=" + StateList + " core=" + StateList + @"( question=open)?)?$")]
    private static partial Regex Shape();

    private const string Kind = @"(done|reply|row|tool|thinking|notice|question|sent|summary|background|other)(\*\d+)?";
    private const string KindList = @"\[(" + Kind + "(," + Kind + ")*(,more)?)?\\]";
    private const string State = @"(idle|working|other):\d+";
    private const string StateList = @"\[(" + State + "(," + State + ")*(,more)?)?\\]";
}

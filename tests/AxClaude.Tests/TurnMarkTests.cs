using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// The model's part of the events (docs/axClaude/events-and-grounding.md EG-1): Claude Code's turn marks raised as
/// <see cref="SessionModel.Marked"/> in the order of the stream with the bell and the frame ends, what the last frame
/// showed of Claude's work (<see cref="SessionModel.Work"/>), the number that rises when the conversation starts afresh
/// (<see cref="SessionModel.Epoch"/>), and the window's word on an open turn (<see cref="SessionModel.TurnOpen"/>)
/// taking the screen's place where a sent message's block is held.
/// </summary>
public class TurnMarkTests
{
    private const string Idle = "auto mode on (shift+tab to cycle)";
    private const string Busy = "auto mode on (shift+tab to cycle)  ·  esc to interrupt";
    private const string Block = "# Input 1|hi||# Output 1 Reply from Claude|";

    /// <summary>The marks' letters and "bell", in the order the recording raises them, fed whole.</summary>
    private static List<string> MarksAndBells(string fixture)
    {
        var (columns, rows) = FixtureSize(fixture);
        var model = new SessionModel(columns, rows);
        var seen = new List<string>();
        model.Marked += letter => seen.Add(letter.ToString());
        model.Bell += () => seen.Add("bell");
        model.Feed(Fixture(fixture));
        model.EndFrame();
        return seen;
    }

    /// <summary>A model that has drawn Claude's idle mode line and prompt once, as after a start.</summary>
    private static SessionModel ReadyModel()
    {
        var model = new SessionModel(80, 10);
        Feed(model, $"{Esc}[?25l{Idle}\r\n${Esc}[?25h");
        return model;
    }

    private static string Shown(SessionModel model) => string.Join("|", Visible(model));

    [Fact]
    public void Marks_the_bell_and_frames_come_in_stream_order()
    {
        var model = ReadyModel();
        var seen = new List<string>();
        model.Marked += letter => seen.Add(letter.ToString());
        model.Bell += () => seen.Add("bell");
        model.Changed += () => seen.Add("frame");

        // One write, as Claude Code's end of a turn arrives: the marks and the bell are raised inside Feed, between the
        // frame ends around them. A B mark is no turn mark of Claude's and is not raised.
        Feed(model,
            $"{Esc}]133;A{Esc}\\{Esc}]133;B{Esc}\\"
            + $"{Esc}[?25l{Esc}[1;1HThinking…{Esc}[K\r\n{Busy}{Esc}[K\r\n${Esc}[K{Esc}[?25h"
            + $"{Esc}]133;C{Esc}\\{Esc}]133;D{Esc}\\\a"
            + $"{Esc}[?25l{Esc}[1;1Hclaude: hello{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[K{Esc}[?25h");

        Assert.Equal(["A", "frame", "C", "D", "bell", "frame"], seen);
    }

    [Fact]
    public void The_conversation_fixture_gives_A_then_C_D_and_the_bell_for_each_turn()
    {
        // Four messages and /exit, which Claude Code handles itself with the same four (section 4.1); the trust
        // question before them and the bare Enter on an empty prompt give none.
        var seen = MarksAndBells("conversation-claude2.1.277-240x50.vt");

        Assert.Equal(Enumerable.Repeat<string[]>(["A", "C", "D", "bell"], 5).SelectMany(turn => turn), seen);
    }

    [Fact]
    public void The_permission_fixture_gives_a_bell_alone_for_the_question()
    {
        // The tool's question rings the bell with no D since the A (section 4.2); Escape declines it and the turn
        // ends with C, D and the bell. Nothing after that in the recording gives a mark or a bell.
        var seen = MarksAndBells("permission-prompt-claude2.1.281-240x50.vt");

        Assert.Equal(["A", "bell", "C", "D", "bell"], seen);
    }

    [Fact]
    public void The_model_picker_fixture_gives_no_mark_and_no_bell()
    {
        // /model opens a picker, no turn: the recording ends with the picker still open.
        Assert.Empty(MarksAndBells("model-picker-claude2.1.281-240x50.vt"));
    }

    [Fact]
    public void Work_is_unknown_without_a_mode_line_idle_with_one_and_working_with_a_spinner()
    {
        var model = new SessionModel(80, 10);
        Assert.Equal(ScreenWork.Unknown, model.Work);

        // The first rows of a start: a banner and no mode line yet.
        Feed(model, $"{Esc}[?25lClaude Code v2.1.325\r\n{Esc}[?25h");
        Assert.Equal(ScreenWork.Unknown, model.Work);
        Assert.False(model.Working);

        Feed(model, $"{Esc}[?25l{Idle}\r\n${Esc}[?25h");
        Assert.Equal(ScreenWork.Idle, model.Work);

        Feed(model, $"{Esc}[?25l{Esc}[2;1HThinking…{Esc}[K\r\n{Busy}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(ScreenWork.Working, model.Work);
        Assert.True(model.Working);

        // A question drawn over the mode line: the screen says nothing of Claude's work, though the mode is kept.
        Feed(model, $"{Esc}[?25l{Esc}[2;1HDo you want to create notes.txt?{Esc}[K\r\n1. Yes{Esc}[K\r\n2. No{Esc}[K\r\nEnter selection: {Esc}[J{Esc}[?25h");
        Assert.Equal(ScreenWork.Unknown, model.Work);
        Assert.False(model.Working);
        Assert.Equal("auto mode on", model.Mode);

        Feed(model, $"{Esc}[?25l{Esc}[2;1Hclaude: done{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[J{Esc}[?25h");
        Assert.Equal(ScreenWork.Idle, model.Work);

        // A restart starts with nothing known.
        model.ResetScreen();
        Assert.Equal(ScreenWork.Unknown, model.Work);
    }

    [Fact]
    public void Epoch_rises_on_a_restart_a_new_session_and_clear()
    {
        var model = new SessionModel(80, 10);
        var epochs = new List<int> { model.Epoch };
        Feed(model, $"{Esc}[?25lClaude Code v2.1.325\r\n{Idle}\r\n${Esc}[?25h");
        Assert.True(model.Send("hi"));
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hyou: hi{Esc}[K\r\nclaude: hello{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        epochs.Add(model.Epoch);

        // Restart Claude (and a folder change): a fresh screen, the conversation kept.
        model.ResetScreen();
        epochs.Add(model.Epoch);

        // New session: the conversation emptied with the run.
        model.Clear();
        epochs.Add(model.Epoch);

        // Claude's own /clear, read off its echo above the live block (the clear fixture's shape, FR-8.8).
        Feed(model, $"{Esc}[?25lClaude Code v2.1.325\r\n{Idle}\r\n${Esc}[?25h");
        Assert.True(model.Send("again"));
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hyou: again{Esc}[K\r\nclaude: again{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        epochs.Add(model.Epoch);
        Assert.False(model.Send("/clear"));
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hyou: /clear{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[K\r\n{Esc}[K\r\n{Esc}[K{Esc}[4;2H{Esc}[?25h");
        Assert.Equal(0, model.ExchangeCount);
        epochs.Add(model.Epoch);

        var first = epochs[0];
        Assert.Equal([first, first, first + 1, first + 2, first + 2, first + 3], epochs);
    }

    [Fact]
    public void With_TurnOpen_true_a_sent_message_is_held_though_the_screen_shows_no_work()
    {
        // Claude has taken a prompt up and its screen shows no spinner yet: the window knows the turn is open.
        var now = new DateTime(2026, 11, 15, 21, 0, 0);
        var model = ReadyModel();
        model.Clock = () => now;
        model.TurnOpen = true;
        Assert.False(model.Working);

        Assert.True(model.Send("hi"));
        Assert.Equal(string.Empty, Shown(model));

        // Idle frames do not place it while the turn is open, nor forget it, however long the turn takes.
        now = now.AddMinutes(31);
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hclaude: a long answer{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal("claude: a long answer", Shown(model));

        // Claude takes the message up: the block goes in front of its echo, after the reply it followed.
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hyou: hi{Esc}[K\r\nclaude: hello{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal($"claude: a long answer|{Block}|claude: hello", Shown(model));
    }

    [Fact]
    public void With_TurnOpen_false_a_message_is_placed_though_the_screen_shows_a_spinner()
    {
        // A spinner left on the screen after the turn ended: the window knows no turn is open.
        var model = ReadyModel();
        Feed(model, $"{Esc}[?25l{Esc}[1;1HThinking…{Esc}[K\r\n{Busy}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.True(model.Working);
        model.TurnOpen = false;

        Assert.True(model.Send("hi"));
        Assert.Equal(Block, Shown(model));

        // With the window's word gone, the screen decides again: a second message is held behind the spinner.
        model.TurnOpen = null;
        Assert.True(model.Send("more"));
        Assert.Equal(Block, Shown(model));
    }
}

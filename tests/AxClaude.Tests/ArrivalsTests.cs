using System.Text;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// The per-frame pass behind the tick (FR-7.8) and Speak replies as they arrive (FR-7.4): a tick for every frame
/// that adds a line from Claude, and one utterance per frame made of the reply lines that became final, in the
/// chosen mode, and of the tool calls when asked for; nothing twice, nothing from tool output, thinking or a
/// replayed conversation.
/// </summary>
public class ArrivalsTests
{
    [Fact]
    public void A_block_printed_in_one_frame_is_one_utterance_and_important_takes_its_first_line()
    {
        var all = new SessionModel(80, 14);
        var important = new SessionModel(80, 14);
        var heard = Listen(all, ReplySpeechMode.All);
        var headlines = Listen(important, ReplySpeechMode.FirstLines);

        foreach (var model in new[] { all, important })
        {
            Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
            model.Send("blue?");
            Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: blue?{Esc}[K\r\nThinking…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
            Feed(model, $"{Esc}[?25l{Esc}[2;1Hclaude: What Blue Is{Esc}[K\r\n{Esc}[K\r\nBlue is a primary color.{Esc}[K\r\n{Esc}[K\r\nWhy People Like It{Esc}[K\r\n- It is calm.{Esc}[K\r\n- It is trusted{Esc}[K\r\nBaked for 1s · done 2:47 PM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        }

        Assert.Equal(["claude: What Blue Is. Blue is a primary color. Why People Like It. - It is calm. - It is trusted"], heard.Speech);
        Assert.Equal(["claude: What Blue Is"], headlines.Speech);

        // Nothing is spoken twice, and a spinner frame adds nothing.
        Feed(all, $"{Esc}[?25l{Esc}[9;1HPondering…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Single(heard.Speech);
    }

    [Fact]
    public void An_error_row_is_spoken_like_a_reply_in_both_modes_and_never_with_none()
    {
        // The login lapsed: Claude's whole answer is one error: row, and the turn said Sent, Responding and Done
        // without a word of it (2026-10-16).
        var all = new SessionModel(80, 14);
        var important = new SessionModel(80, 14);
        var silent = new SessionModel(80, 14);
        var heard = Listen(all, ReplySpeechMode.All);
        var headlines = Listen(important, ReplySpeechMode.FirstLines);
        var nothing = Listen(silent, ReplySpeechMode.None);
        const string error = "error: API Error: 401 Invalid API key · Please run /login";

        foreach (var model in new[] { all, important, silent })
        {
            Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
            model.Send("hello");
            Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: hello{Esc}[K\r\nThinking…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
            Feed(model, $"{Esc}[?25l{Esc}[2;1H{error}{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        }

        Assert.Equal([error], heard.Speech);
        Assert.Equal([error], headlines.Speech);
        Assert.Empty(nothing.Speech);
    }

    [Fact]
    public void The_lines_behind_the_speech_come_with_it_and_a_tool_row_loses_its_hint()
    {
        var arrivals = new Arrivals();
        var lines = new List<Line>
        {
            new(1, LineKind.OutputMarker, "claude"),
            new(2, LineKind.ClaudeReply, "claude: Hello."),
            new(3, LineKind.Tool, "tool: Bash (ls) (ctrl+o to expand)"),
        };
        var arrival = arrivals.Update(lines, working: false, ReplySpeechMode.All, toolCalls: true);
        Assert.NotNull(arrival.Speech);
        Assert.Equal([2, 3], arrival.SpokenLines.Select(line => line.Id));
        Assert.Equal("claude: Hello.", Arrivals.SpokenText(lines[1]));
        Assert.Equal("tool: Bash (ls)", Arrivals.SpokenText(lines[2]));
    }

    [Fact]
    public void What_was_remembered_about_trimmed_lines_is_forgotten()
    {
        var arrivals = new Arrivals();
        var lines = Enumerable.Range(1, 50).Select(i => new Line(i, i == 1 ? LineKind.OutputMarker : LineKind.ClaudeReply, $"claude: line {i}")).ToList();
        arrivals.Update(lines, working: false, ReplySpeechMode.All, toolCalls: false);
        var remembered = arrivals.Remembered;
        Assert.True(remembered >= 49);

        // The oldest tenth trimmed from the front (FR-3.6): the entries for those lines go, and the rest stay.
        arrivals.Update(lines.Skip(5).ToList(), working: false, ReplySpeechMode.All, toolCalls: false);
        Assert.Equal(remembered - 4, arrivals.Remembered);
    }

    [Fact]
    public void A_reply_claude_prints_again_later_in_the_same_turn_is_not_spoken_again()
    {
        var model = new SessionModel(80, 20);
        var heard = Listen(model, ReplySpeechMode.All);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.Send("check");
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: check{Esc}[K\r\nclaude: The messages never arrived.{Esc}[K\r\nNothing was interrupted.{Esc}[K\r\ntool: Bash (git status){Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(["claude: The messages never arrived. Nothing was interrupted."], heard.Speech);

        // Late in the turn Claude prints the first message again as new rows, then its last message.
        Feed(model, $"{Esc}[?25l{Esc}[5;1Hclaude: The messages never arrived.{Esc}[K\r\nNothing was interrupted.{Esc}[K\r\nclaude: All done.{Esc}[K\r\nBaked for 9s · done 1:05 AM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(["claude: The messages never arrived. Nothing was interrupted.", "claude: All done."], heard.Speech);

        // The next exchange starts afresh: the same words in a new reply are spoken.
        model.Send("again");
        Feed(model, $"{Esc}[?25l{Esc}[10;1Hyou: again{Esc}[K\r\nclaude: All done.{Esc}[K\r\nBaked for 1s · done 1:06 AM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal("claude: All done.", heard.Speech[^1]);
        Assert.Equal(3, heard.Speech.Count);
    }

    [Fact]
    public void The_tick_marks_every_frame_that_adds_a_line_from_claude_whatever_the_speech_mode()
    {
        var model = new SessionModel(80, 10);
        var heard = Listen(model, ReplySpeechMode.None);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        Assert.Equal([false], heard.Ticks);

        model.Send("go");
        Assert.Equal([false, false], heard.Ticks);

        // The echo is hidden, the reply and a tool row arrive, a spinner tick changes nothing, the summary arrives.
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: go{Esc}[K\r\nclaude: Looking.{Esc}[K\r\ntool: Read (a.txt){Esc}[K\r\nRunning…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Feed(model, $"{Esc}[?25l{Esc}[4;1HPondering…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Feed(model, $"{Esc}[?25l{Esc}[4;1HBaked for 1s · done 2:47 PM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal([false, false, true, false, true], heard.Ticks);
        // The tool tick for the frame that brought the tool row, whatever is spoken.
        Assert.Equal([false, false, true, false, false], heard.ToolCalls);
        Assert.Empty(heard.Speech);
    }

    [Fact]
    public void The_last_line_waits_until_another_line_follows_or_claude_is_idle()
    {
        var model = new SessionModel(80, 10);
        var heard = Listen(model, ReplySpeechMode.All);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.Send("go");

        // The first message of the turn, with the spinner still on: its last line may still grow.
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hclaude: Looking at the file.{Esc}[K\r\nRunning…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Empty(heard.Speech);

        // A tool row after it makes it final; the tool row itself is never spoken.
        Feed(model, $"{Esc}[?25l{Esc}[2;1Htool: Read (a.txt){Esc}[K\r\nRunning…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(["claude: Looking at the file."], heard.Speech);

        // The closing message becomes final when Claude is idle.
        Feed(model, $"{Esc}[?25l{Esc}[3;1Hclaude: The file is empty.{Esc}[K\r\nBaked for 1s · done 2:47 PM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(["claude: Looking at the file.", "claude: The file is empty."], heard.Speech);
    }

    [Fact]
    public void A_tool_call_is_spoken_as_it_arrives_when_asked_for_and_its_output_never()
    {
        var model = new SessionModel(80, 10);
        var heard = Listen(model, ReplySpeechMode.FirstLines, toolCalls: true);
        var calls = Listen(model, ReplySpeechMode.None, toolCalls: true);
        var replies = Listen(model, ReplySpeechMode.All);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.Send("go");

        // The opening line and the first call arrive together: one utterance, the tool row final at once although the tool still runs under it.
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hclaude: Looking at the file.{Esc}[K\r\ntool: Read (a.txt){Esc}[K\r\nRunning…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(["claude: Looking at the file. tool: Read (a.txt)"], heard.Speech);
        Assert.Equal(["tool: Read (a.txt)"], calls.Speech);
        Assert.True(replies.ToolCalls[^1]);

        // Its output rows are not spoken; the next call is, on its own.
        Feed(model, $"{Esc}[?25l{Esc}[3;1H  ⎿  Read 3 lines (ctrl+o to expand){Esc}[K\r\ntool: Bash (dir){Esc}[K\r\nRunning…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(["claude: Looking at the file. tool: Read (a.txt)", "tool: Bash (dir)"], heard.Speech);
        Assert.True(replies.ToolCalls[^1]);

        // The closing message when Claude is idle; without the option the tool rows were never spoken, but they ticked.
        Feed(model, $"{Esc}[?25l{Esc}[5;1H(No output){Esc}[K\r\nclaude: The file has 3 lines.{Esc}[K\r\nBaked for 1s · done 2:47 PM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Equal(["claude: Looking at the file. tool: Read (a.txt)", "tool: Bash (dir)", "claude: The file has 3 lines."], heard.Speech);
        Assert.Equal(["tool: Read (a.txt)", "tool: Bash (dir)"], calls.Speech);
        Assert.Equal(["claude: Looking at the file.", "claude: The file has 3 lines."], replies.Speech);
        Assert.False(replies.ToolCalls[^1]);
        Assert.Equal(2, replies.ToolCalls.Count(t => t));
    }

    [Fact]
    public void Skip_leaves_the_replayed_conversation_unspoken_but_not_the_rows_claude_redraws_afterwards()
    {
        var model = new SessionModel(80, 12);
        var arrivals = new Arrivals();
        Feed(model, $"{Esc}[?25lyou: earlier question\r\nclaude: earlier answer\r\nauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");

        // Ready: the window skips what is there, then marks the replay; none of it is spoken.
        arrivals.Skip(model.Lines);
        model.MarkReplayedExchanges();
        Assert.Null(arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, false).Speech);

        // The first live reply is drawn over the rows that held the mode line and the prompt: spoken all the same.
        model.Send("next");
        Feed(model, $"{Esc}[?25l{Esc}[3;1Hyou: next{Esc}[K\r\nclaude: live answer{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Assert.Null(arrivals.Update(model.Lines, model.Working, ReplySpeechMode.None, false).Speech);
        Assert.Equal("claude: live answer", arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, false).Speech);
        Assert.Null(arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, false).Speech);
    }

    [Theory]
    [InlineData("conversation-claude2.1.277-240x50.vt", 240, 50)]
    [InlineData("conversation-claude2.1.277-120x40.vt", 120, 40)]
    public void Every_reply_line_of_a_recorded_conversation_is_spoken_once(string fixture, int columns, int rows)
    {
        string[] sent =
        [
            "What is 2+2? Answer in one short line.",
            "Write a short markdown answer with two level-2 headings and a three item bullet list about the color blue.",
            "Create a file named hello.txt in this folder containing the single word hi.",
        ];
        var model = new SessionModel(columns, rows);
        var heard = Listen(model, ReplySpeechMode.All);
        var headlines = Listen(model, ReplySpeechMode.FirstLines);
        var calls = Listen(model, ReplySpeechMode.None, toolCalls: true);
        ReplayInFixedChunks(model, fixture, sent);

        // Every reply line after a live marker was heard, in one piece per frame, and nothing else was.
        var replyLines = new List<string>();
        var inReply = false;
        var marker = false;
        foreach (var line in model.Lines.Where(l => !l.Hidden))
        {
            marker |= line.Kind == LineKind.OutputMarker;
            if (line.IsMarker || !marker)
            {
                continue;
            }

            inReply = line.Kind == LineKind.ClaudeReply || (inReply && line.Kind == LineKind.Plain);
            if (inReply && line.Text.Length > 0 && !line.Text.StartsWith("Resume this session", StringComparison.Ordinal) && !line.Text.StartsWith("claude --resume", StringComparison.Ordinal))
            {
                replyLines.Add(line.Text);
            }
        }

        var spokenText = string.Join(" ", heard.Speech);
        Assert.All(replyLines, text => Assert.Contains(text.Trim(), spokenText));
        Assert.Equal(replyLines.Count(t => t.StartsWith("claude:", StringComparison.Ordinal)), headlines.Speech.Count);
        Assert.All(headlines.Speech, text => Assert.StartsWith("claude:", text));
        Assert.DoesNotContain(heard.Speech, text => text.Contains("ctrl+o to expand") || text.Contains("you:"));

        // The markdown answer, drawn in one frame, is one utterance with a pause after the heading.
        var markdown = heard.Speech.Single(text => text.StartsWith("claude: W", StringComparison.Ordinal) && text.Contains("- "));
        Assert.Matches(@"^claude: W[^.]+\. ", markdown);
        Assert.Equal(1, markdown.Split("claude:").Length - 1);

        // Lines from Claude ticked, and more frames ticked than spoke: tool rows tick too.
        Assert.True(heard.Ticks.Count(t => t) > heard.Speech.Count);

        // The file was written and read back under one collapsed tool row, "tool: Reading 1 file… (ctrl+o to expand)",
        // which Claude rewrote to "Read 1 file (ctrl+o to expand)" when done: spoken once, as it started, without the hint,
        // and one tool tick, in the frame that brought the row.
        Assert.Equal(["tool: Reading 1 file…"], calls.Speech);
        Assert.Equal(1, heard.ToolCalls.Count(t => t));
    }

    [Fact]
    public void A_recorded_tool_call_joins_the_opening_line_and_the_status_under_it_is_never_spoken()
    {
        var model = new SessionModel(240, 50);
        var heard = Listen(model, ReplySpeechMode.FirstLines, toolCalls: true);
        var calls = Listen(model, ReplySpeechMode.None, toolCalls: true);
        Replay(model, Steering, SteeringMessages);

        // The tool row arrives in the frame after the opening line, which it makes final: one utterance for both.
        Assert.Equal(["tool: Bash (sleep 20)"], calls.Speech);
        Assert.Equal(1, calls.ToolCalls.Count(t => t));
        Assert.Equal("claude: Running the sleep command now. tool: Bash (sleep 20)", heard.Speech[0]);
        Assert.DoesNotContain(heard.Speech, text => text.Contains("Running") && !text.Contains("claude:"));
    }

    // ---- Claude Code's record of an answered question (FR-7.4b): shown, a kind of its own, never reply speech ----

    [Fact]
    public void The_record_of_an_answered_question_is_shown_and_not_spoken_and_the_reply_around_it_is()
    {
        // Recorded with 2.1.319: two reply lines, the question, the answer 2, the record in the question's place, the reply.
        const string fixture = "one-question-claude2.1.319-240x50.vt";
        var model = new SessionModel(240, 50);
        var heard = Listen(model, ReplySpeechMode.All);
        Replay(model, fixture, ["First write two short sentences on two lines saying that you are about to ask me something."]);

        // As Claude Code prints it: a no-break space and a space after the bullet.
        const string label = "●  User answered Claude's questions:";
        const string row = "· Which colour do you prefer? → Green";
        Assert.Equal([label, row], model.Lines.Where(l => l.Kind == LineKind.AnswerRecord && !l.Hidden).Select(l => l.Text));
        var visible = Visible(model);
        Assert.Equal(visible.IndexOf(label) + 1, visible.IndexOf(row));
        Assert.Equal(visible.IndexOf(row) + 1, visible.IndexOf("claude: You prefer green."));

        var spoken = string.Join(" | ", heard.Speech);
        Assert.DoesNotContain("User answered", spoken);
        Assert.DoesNotContain("→ Green", spoken);
        Assert.Contains("claude: I am about to ask you a question.", spoken);
        Assert.Contains("This is a single colour preference question.", spoken);
        Assert.Contains("claude: You prefer green.", spoken);
    }

    [Theory]
    [InlineData("several-questions-claude2.1.281-240x50.vt", "Please use the AskUserQuestion tool once, with two questions in the same call.", "· Which colour do you prefer? → Green|· Which fruits do you like? → Apple, Cherry", "claude: You prefer green")]
    [InlineData("several-questions-claude2.1.316-240x50.vt", "Please use the AskUserQuestion tool once, with two questions in the same call.", "· Which colour do you prefer? → Green|· Which fruits do you like? → Apple, Cherry", "claude: You prefer green")]
    // The answer typed after Other stands where an answer's name does.
    [InlineData("other-answer-claude2.1.281-240x50.vt", "Please use the AskUserQuestion tool to ask me which flower I enjoy thinking about", "· Which flower do you enjoy thinking about? → tulip", "claude: You like thinking about tulips.")]
    public void The_record_has_a_row_for_every_question_answered_and_none_is_spoken(string fixture, string sent, string rows, string reply)
    {
        var model = new SessionModel(240, 50);
        var heard = Listen(model, ReplySpeechMode.All);
        Replay(model, fixture, [sent]);

        string[] record = ["●  User answered Claude's questions:", .. rows.Split('|')];
        Assert.Equal(record, model.Lines.Where(l => l.Kind == LineKind.AnswerRecord && !l.Hidden).Select(l => l.Text));
        Assert.All(record, text => Assert.Contains(text, Visible(model)));

        var spoken = string.Join(" | ", heard.Speech);
        Assert.DoesNotContain("User answered", spoken);
        Assert.DoesNotContain("→", spoken);
        Assert.Contains(reply, spoken);
    }

    [Fact]
    public void A_reply_goes_on_after_a_record_in_its_middle_and_a_wrapped_record_row_stays_whole()
    {
        var model = new SessionModel(80, 16);
        var heard = Listen(model, ReplySpeechMode.All);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.Send("ask me");
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: ask me{Esc}[K\r\nThinking…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        // The second row of the record is as wide as the console, so the row under it is the rest of it.
        var wide = "· Which of the colours that were named a moment ago do you like best of all the".PadRight(79, '?');
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hclaude: Before the record.{Esc}[K\r\n●  User answered Claude's questions:{Esc}[K\r\n· Which fruit? → Apple{Esc}[K\r\n{wide}{Esc}[K\r\ncolours? → Green{Esc}[K\r\nAfter the record, still the reply.{Esc}[K\r\nBaked for 1s · done 2:47 PM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");

        Assert.Equal(
            ["●  User answered Claude's questions:", "· Which fruit? → Apple", wide, "colours? → Green"],
            model.Lines.Where(l => l.Kind == LineKind.AnswerRecord).Select(l => l.Text));
        Assert.Equal(LineKind.Plain, model.Lines.Single(l => l.Text.StartsWith("After the record", StringComparison.Ordinal)).Kind);
        Assert.DoesNotContain(model.Lines, l => l.Kind == LineKind.AnswerRecord && l.JoinedToPrevious);
        Assert.Equal(["claude: Before the record. After the record, still the reply."], heard.Speech);
    }

    [Fact]
    public void A_reply_row_that_starts_like_a_record_row_is_a_reply_row_without_the_label_above_it()
    {
        var model = new SessionModel(80, 14);
        var heard = Listen(model, ReplySpeechMode.All);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.Send("list");
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: list{Esc}[K\r\nThinking…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hclaude: Two things.{Esc}[K\r\n· input → output{Esc}[K\r\nBaked for 1s · done 2:47 PM{Esc}[K\r\nauto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");

        Assert.DoesNotContain(model.Lines, l => l.Kind == LineKind.AnswerRecord);
        Assert.Equal(["claude: Two things. · input → output"], heard.Speech);
    }

    [Fact]
    public void A_reply_that_ends_while_background_agents_run_is_spoken_without_claude_codes_rows_under_it()
    {
        // Heard on 2026-11-15 over the current Claude Code (no recording yet): the turn ends while two background
        // agents run, and under the reply stand a waiting row, the mode line, the branch and a row per agent that ends
        // in "running" and names no time. That agent row was unknown and stood lowest, so it closed the chrome block
        // and every row above it was spoken as the reply's.
        var model = new SessionModel(120, 20);
        var heard = Listen(model, ReplySpeechMode.All);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.Send("go");
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: go{Esc}[K\r\nThinking…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hclaude: Both agents are started.{Esc}[K\r\nWaiting for 2 background agents to finish{Esc}[K\r\n"
            + $"auto mode on (shift+tab to cycle)  ·  ↓ to manage{Esc}[K\r\n  ● main{Esc}[K\r\n"
            + $"  ◯  general-purpose Flipping a default in a settings file running{Esc}[K\r\n"
            + $"  ◯  general-purpose Inspect the transcript for garbling running{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        model.EndFrame();

        Assert.Equal(["claude: Both agents are started."], heard.Speech);
        Assert.False(model.Working);
        Assert.Equal("auto mode on", model.Mode);
        Assert.Equal("see /tasks", model.Background);
        Assert.Equal("claude: Both agents are started.", model.Lines.Last(line => !line.Hidden && line.Text.Length > 0).Text);

        // The footer redrawn with a count, the agents still under it: nothing more is spoken.
        Feed(model, $"{Esc}[?25l{Esc}[3;1HWaiting for 1 background agent to finish{Esc}[K\r\nauto mode on  ·  1 shell  ·  ↓ to manage{Esc}[K\r\n  ● main{Esc}[K\r\n"
            + $"  ◯  Explore Inspect the transcript for garbling running{Esc}[K\r\n${Esc}[K\r\n{Esc}[K{Esc}[7;2H{Esc}[?25h");
        model.EndFrame();
        Assert.Equal(["claude: Both agents are started."], heard.Speech);
        Assert.Equal("1 shell", model.Background);
    }

    [Fact]
    public void A_replys_own_sentence_about_waiting_for_agents_is_spoken()
    {
        var model = new SessionModel(120, 20);
        var heard = Listen(model, ReplySpeechMode.All);
        Feed(model, $"{Esc}[?25lauto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.Send("go");
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: go{Esc}[K\r\nThinking…{Esc}[K\r\nauto mode on (shift+tab to cycle)  ·  esc to interrupt{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        Feed(model, $"{Esc}[?25l{Esc}[2;1Hclaude: Status.{Esc}[K\r\n- Waiting for 2 background agents to finish{Esc}[K\r\n"
            + $"I am waiting for 2 background agents to finish.{Esc}[K\r\n- ◯ the second agent is still running{Esc}[K\r\n"
            + $"auto mode on (shift+tab to cycle){Esc}[K\r\n${Esc}[K{Esc}[?25h");
        model.EndFrame();

        Assert.Equal(
            ["claude: Status. - Waiting for 2 background agents to finish. I am waiting for 2 background agents to finish. - ◯ the second agent is still running"],
            heard.Speech);
    }

    private sealed class Heard
    {
        public List<string> Speech { get; } = [];
        public List<bool> Ticks { get; } = [];
        public List<bool> ToolCalls { get; } = [];
    }

    private static Heard Listen(SessionModel model, ReplySpeechMode mode, bool toolCalls = false)
    {
        var arrivals = new Arrivals();
        var heard = new Heard();
        model.Changed += () =>
        {
            var arrival = arrivals.Update(model.Lines, model.Working, mode, toolCalls);
            heard.Ticks.Add(arrival.NewLine);
            heard.ToolCalls.Add(arrival.ToolCall);
            if (arrival.Speech is { } text)
            {
                heard.Speech.Add(text);
            }
        };
        return heard;
    }

    /// <summary>
    /// Feeds a recording without a chunk index (the conversation fixtures) in 64-byte chunks; each message is sent, as
    /// the app does, before its draft appears in the stream.
    /// </summary>
    private static void ReplayInFixedChunks(SessionModel model, string fixture, string[] sent)
    {
        var bytes = Fixture(fixture);
        var sendAt = new List<int>();
        var search = 0;
        foreach (var text in sent)
        {
            var probe = Encoding.UTF8.GetBytes(text[..Math.Min(30, text.Length)]);
            var at = bytes.AsSpan(search).IndexOf(probe);
            Assert.True(at >= 0, "The sent text was not found in the recording: " + text);
            sendAt.Add(search + at);
            search += at + probe.Length;
        }

        const int chunk = 64;
        var next = 0;
        for (var offset = 0; offset < bytes.Length; offset += chunk)
        {
            var data = bytes.AsSpan(offset, Math.Min(chunk, bytes.Length - offset));
            if (next < sent.Length && sendAt[next] < offset + data.Length)
            {
                var cut = sendAt[next] - offset;
                model.Feed(data[..cut]);
                model.Send(sent[next++]);
                model.Feed(data[cut..]);
            }
            else
            {
                model.Feed(data);
            }
        }

        model.EndFrame();
        Assert.Equal(sent.Length, next);
    }
}

using AxClaude.Core.Streaming;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The stream's model against the recordings of Claude Code's real stream (tests/fixtures/streams, whose page says
/// what a file holds and what the recordings showed; docs/axClaude/stream-mode.md SM-4, SM-10.1). Each case replayed
/// must give its expected file, line for line: what the model was handed, what it raised, how its facts changed, and
/// the conversation it holds. Set AXCLAUDE_UPDATE_EXPECTED=1 to write the files again after a deliberate change, and
/// read the difference through. The tests under it say in words what the recordings showed that the model must
/// follow, so that the rule is not only a line in a file.
/// </summary>
public sealed class StreamModelTests
{
    public static IEnumerable<object[]> Cases() => StreamReplay.Cases().Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void A_recorded_case_gives_its_expected_conversation_and_facts(string name)
    {
        var actual = string.Join("\n", StreamReplay.Run(name, out _)) + "\n";
        var path = Path.Combine(StreamReplay.FixturesFolder(), name + ".expected.txt");
        if (Environment.GetEnvironmentVariable("AXCLAUDE_UPDATE_EXPECTED") == "1")
        {
            Fixtures.WriteExpected(path, actual);
            return;
        }

        Assert.True(File.Exists(path), $"Missing {path}. Run this test once with AXCLAUDE_UPDATE_EXPECTED=1 and read the result through.");
        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), actual);
    }

    [Fact]
    public void Nineteen_cases_are_recorded()
    {
        // A fixture taken away by mistake would make the theory above pass with fewer cases.
        Assert.Equal(19, StreamReplay.Cases().Count());
    }

    [Fact]
    public void The_first_requests_answer_names_the_mode_before_any_message()
    {
        // What the window's rule for Ready waits for: with the screen it is the mode line, with the stream the answer
        // to the request the window sends first, some 650 ms after the start. It is the first mode known, and not said.
        var log = StreamReplay.Run("start", out var model);

        var answered = log.FindIndex(entry => entry.StartsWith("< Control, done", StringComparison.Ordinal));
        var firstMessage = log.FindIndex(entry => entry.StartsWith("> message", StringComparison.Ordinal));
        Assert.True(answered >= 0 && answered < firstMessage);
        Assert.StartsWith("    now: idle, mode ", log[answered + 1]);
        Assert.DoesNotContain("not known", log[answered + 1]);
        Assert.DoesNotContain(log, entry => entry.StartsWith("    announced:", StringComparison.Ordinal));
        Assert.NotNull(model.Mode);
        Assert.NotEqual(string.Empty, model.Version);
        Assert.Equal("claude: ok", model.Lines[^1].Text);
    }

    [Fact]
    public void A_turn_shows_the_message_in_its_block_and_the_reply_under_its_label()
    {
        var log = StreamReplay.Run("turn", out var model);

        Assert.Equal(
            ["# Input 1", "Reply with exactly: ok", "", "# Output 1 Reply from Claude", "", "claude: ok"],
            model.Lines.Select(line => line.Text));
        Assert.Equal(
            [LineKind.InputMarker, LineKind.UserMessage, LineKind.UserMessage, LineKind.OutputMarker, LineKind.UserMessage, LineKind.ClaudeReply],
            model.Lines.Select(line => line.Kind));
        Assert.All(model.Lines, line => Assert.True(line.Committed));
        Assert.False(model.Working);
        Assert.Equal("manual mode on", model.Mode);
        Assert.Equal(1, log.Count(entry => entry == "    the bell"));
        Assert.Equal(1, log.Count(entry => entry == "    the response started"));
    }

    [Fact]
    public void A_message_written_while_a_reply_is_written_keeps_Claude_working_past_the_first_end()
    {
        // The first turn's end line says that nothing waits, and the second message's turn starts 16 ms later: the
        // model knows from its own count that a message has not come back yet.
        var log = StreamReplay.Run("while-writing", out var model);

        var firstEnd = log.FindIndex(entry => entry.StartsWith("< Ended", StringComparison.Ordinal));
        var afterFirstEnd = log.Skip(firstEnd).TakeWhile((entry, index) => index == 0 || !entry.StartsWith("< ", StringComparison.Ordinal)).ToList();
        Assert.Contains("    the bell", afterFirstEnd);
        Assert.DoesNotContain(afterFirstEnd, entry => entry.Contains("now: idle", StringComparison.Ordinal));

        // Its block went in when it came back, after the first reply: the conversation is in the order it was held.
        var texts = model.Lines.Select(line => line.Text).ToList();
        Assert.True(texts.IndexOf("# Input 2") > texts.FindIndex(text => text.StartsWith("claude: ", StringComparison.Ordinal)));
        Assert.Equal("claude: second", texts[^1]);
        Assert.Equal(2, log.Count(entry => entry == "    the bell"));
        Assert.False(model.Working);
        Assert.Equal(0, model.QueuedMessages);
    }

    [Fact]
    public void A_message_written_while_a_tool_runs_gets_its_block_inside_the_turn_and_one_end_ends_both()
    {
        var log = StreamReplay.Run("mid-turn", out var model);

        var texts = model.Lines.Select(line => line.Text).ToList();
        var tool = texts.FindIndex(text => text.StartsWith("tool: Bash (ping -n 5 127.0.0.1)", StringComparison.Ordinal));
        Assert.True(tool > 0);
        Assert.True(texts.IndexOf("# Input 2") > tool);
        Assert.Equal("claude: second answer", texts[^1]);
        Assert.Equal(1, log.Count(entry => entry.StartsWith("< Ended", StringComparison.Ordinal)));
        Assert.Equal(1, log.Count(entry => entry == "    the bell"));
        // While it waited, it was counted.
        Assert.Contains(log, entry => entry.Contains("1 waiting", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("worker")]
    [InlineData("background")]
    public void A_turn_Claude_Code_starts_by_itself_is_told_once(string name)
    {
        var log = StreamReplay.Run(name, out var model);

        Assert.Equal(1, log.Count(entry => entry == "    a turn Claude Code started by itself"));
        // Two turns ended well: the one the message started, and the one Claude Code started.
        Assert.Equal(2, log.Count(entry => entry == "    the bell"));
        Assert.Null(model.Background);
        Assert.Contains(log, entry => entry.Contains("in the background 1 " + (name == "worker" ? "agent" : "shell"), StringComparison.Ordinal));
    }

    [Fact]
    public void A_subagents_own_lines_are_not_the_conversations()
    {
        StreamReplay.Run("worker", out var model);

        Assert.DoesNotContain(model.Lines, line => line.Text == "claude: from the worker");
        Assert.Contains(model.Lines, line => line.Kind == LineKind.Tool && line.Text.StartsWith("tool: Agent (", StringComparison.Ordinal));
    }

    [Fact]
    public void A_slash_commands_print_is_no_reply_and_its_end_no_turns_end()
    {
        var log = StreamReplay.Run("model", out var model);

        var texts = model.Lines.Select(line => line.Text).ToList();
        Assert.Equal("you: /model haiku", texts[0]);
        Assert.Equal(LineKind.UserEcho, model.Lines[0].Kind);
        Assert.Equal(LineKind.Plain, model.Lines[1].Kind);
        Assert.StartsWith("Set model to", texts[1]);
        // One bell, for the message after it; none for the command.
        Assert.Equal(1, log.Count(entry => entry == "    the bell"));
    }

    [Fact]
    public void A_cleared_conversation_is_emptied_and_says_so_as_the_screens_model_does()
    {
        var log = StreamReplay.Run("clear", out var model);

        var texts = model.Lines.Select(line => line.Text).ToList();
        Assert.Equal("System: Conversation cleared by /clear; the previous one stays on disk, and /resume brings it back", texts[0]);
        Assert.DoesNotContain("claude: before", texts);
        // The blocks count from 1 again.
        Assert.Equal("# Input 1", texts[1]);
        Assert.Equal("claude: after", texts[^1]);
        Assert.Contains("    announced: " + StreamModel.ClearedWords, log);
    }

    [Fact]
    public void A_compaction_is_announced_and_marked_and_its_summary_is_no_message()
    {
        var log = StreamReplay.Run("compact", out var model);

        Assert.Contains("    announced: " + StreamModel.CompactingWords, log);
        Assert.Contains("    announced: " + StreamModel.CompactedWords, log);
        Assert.Contains(model.Lines, line => line.Kind == LineKind.System && line.Text == "System: " + StreamModel.CompactedWords);
        Assert.DoesNotContain(model.Lines, line => line.Text.Contains("This session is being continued", StringComparison.Ordinal));
        Assert.Equal(2, model.ExchangeCount);
        Assert.Equal(2, log.Count(entry => entry == "    the bell"));
    }

    [Fact]
    public void A_refused_call_and_an_interrupted_one_become_error_lines_without_the_words_meant_for_the_model()
    {
        StreamReplay.Run("denied", out var denied);
        var call = Assert.Single(denied.Lines, line => line.Kind == LineKind.ToolError);
        Assert.Equal("tool error: Write (probe-denied.txt)", call.Text);
        Assert.DoesNotContain(denied.Lines, line => line.Text == ClaudeCodeStream.RefusedWords);

        var log = StreamReplay.Run("interrupt", out var interrupted);
        Assert.Contains(interrupted.Lines, line => line.Text == "tool error: Bash (ping -n 20 127.0.0.1)");
        Assert.DoesNotContain(interrupted.Lines, line => line.Text.Contains("doesn't want to proceed", StringComparison.Ordinal));
        // The interrupted turn says no Done; the message after it is answered and does.
        Assert.Equal(1, log.Count(entry => entry == "    the bell"));
        Assert.Equal("claude: still here", interrupted.Lines[^1].Text);
    }

    [Fact]
    public void A_question_of_Claudes_is_no_tool_line_and_is_recorded_once()
    {
        StreamReplay.Run("question", out var model);

        Assert.DoesNotContain(model.Lines, line => line.Kind is LineKind.Tool or LineKind.ToolError);
        Assert.Single(model.Lines, line => line.Kind == LineKind.Question);
        Assert.False(model.PromptPending);
    }

    [Fact]
    public void A_failure_of_the_service_is_an_error_line_and_says_no_Done()
    {
        var log = StreamReplay.Run("failure", out var model);

        Assert.Equal(LineKind.Error, model.Lines[^1].Kind);
        Assert.StartsWith("error: There's an issue with the selected model", model.Lines[^1].Text);
        Assert.DoesNotContain("    the bell", log);
        Assert.False(model.Working);
    }

    [Fact]
    public void A_reply_written_in_Markdown_keeps_its_headings_as_headings()
    {
        StreamReplay.Run("headings", out var model);

        // What the model wrote is its own; that it began with a heading and wrote a second one is what was asked of it.
        var reply = model.Lines.First(line => line.Kind == LineKind.ClaudeReply);
        // The first line is a heading: on the labelled row without its marks, a level under the exchange markers'.
        Assert.StartsWith("claude: ", reply.Text);
        Assert.DoesNotContain("#", reply.Text);
        Assert.InRange(reply.HeadingLevel, 2, 6);

        // A later heading keeps its marks, which the view hides when its option says so, and is one level deeper than its marks.
        var second = model.Lines.First(line => line.Kind == LineKind.Plain && line.HeadingLevel > 0);
        var marks = second.Text.TakeWhile(c => c == '#').Count();
        Assert.InRange(marks, 1, 5);
        Assert.Equal(marks + 1, second.HeadingLevel);
        Assert.Equal(second.Text[(marks + 1)..], second.BareText);

        // A line of the reply that is no heading is none, whatever it starts with.
        Assert.All(model.Lines.Where(line => line.Text.StartsWith("- ", StringComparison.Ordinal) || line.Text.StartsWith("```", StringComparison.Ordinal)), line => Assert.Equal(0, line.HeadingLevel));
    }

    [Theory]
    [InlineData("# One", 2)]
    [InlineData("## Two", 3)]
    [InlineData("###### Six", 6)]
    [InlineData("####### Seven marks are no heading", 0)]
    [InlineData("#NoSpace", 0)]
    [InlineData("#", 0)]
    [InlineData("Plain # text", 0)]
    public void A_line_is_a_heading_by_one_to_six_marks_and_a_space(string text, int level)
    {
        var model = new StreamModel();
        model.Take(ClaudeCodeStream.Read("{\"type\":\"assistant\",\"message\":{\"content\":[{\"type\":\"text\",\"text\":\"first\\n" + text + "\"}]}}"));
        Assert.Equal(level, model.Lines[^1].HeadingLevel);
    }

    [Fact]
    public void The_mode_is_said_in_the_screens_words_and_a_change_is_announced()
    {
        var log = StreamReplay.Run("mode", out var model);
        Assert.Equal("accept edits on", model.Mode);
        // Set before any start line: the first mode known is no change.
        Assert.DoesNotContain(log, entry => entry.StartsWith("    announced:", StringComparison.Ordinal));

        var announced = new List<string>();
        var later = new StreamModel();
        later.Announcement += announced.Add;
        later.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        later.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"status","permissionMode":"plan"}"""));
        Assert.Equal("plan mode on", later.Mode);
        Assert.Equal(["plan mode on"], announced);
    }

    [Fact]
    public void The_model_keeps_Claude_Codes_own_name_of_the_mode_and_every_mode_the_run_was_in()
    {
        // What the window's mode key counts on from (ClaudeCodeStream.NextMode): the name in force, and the modes seen.
        var model = new StreamModel();
        Assert.Equal(string.Empty, model.ModeName);
        Assert.Empty(model.ModesSeen);

        model.Take(ClaudeCodeStream.Read("""{"type":"control_response","response":{"subtype":"success","request_id":"r","response":{"current_permission_mode":"auto"}}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"status","permissionMode":"default"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"status","permissionMode":"auto"}"""));
        Assert.Equal("auto", model.ModeName);
        Assert.Equal("auto mode on", model.Mode);
        Assert.Equal(["auto", "default"], model.ModesSeen);

        // A new run knows none of it.
        model.ResetScreen();
        Assert.Equal(string.Empty, model.ModeName);
        Assert.Empty(model.ModesSeen);
    }

    [Fact]
    public void A_turn_the_user_interrupted_says_so_in_a_word_and_rings_no_bell()
    {
        var log = StreamReplay.Run("interrupt", out var model);
        Assert.Contains("    announced: " + StreamModel.InterruptedWords, log);
        Assert.Contains(model.Lines, line => line.Kind == LineKind.System && line.Text == "System: Interrupted");
        // One bell in the recording: the second turn's, which ended well.
        Assert.Single(log, entry => entry == "    the bell");
        // A failure of the service is no interrupt.
        Assert.DoesNotContain(StreamReplay.Run("failure", out _), entry => entry.Contains(StreamModel.InterruptedWords, StringComparison.Ordinal));
    }

    [Fact]
    public void A_new_run_puts_a_message_never_taken_on_record_and_forgets_the_old_run()
    {
        var model = new StreamModel();
        Assert.True(model.Send("first"));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        Assert.True(model.Send("second, while Claude works"));
        Assert.Equal(1, model.QueuedMessages);
        Assert.DoesNotContain(model.Lines, line => line.Text == "second, while Claude works");

        model.ResetScreen();

        Assert.Contains(model.Lines, line => line.Text == "second, while Claude works");
        Assert.False(model.Working);
        Assert.Null(model.Mode);
        Assert.Equal(0, model.QueuedMessages);
        Assert.Equal(string.Empty, model.Session);

        model.Clear();
        Assert.Empty(model.Lines);
        Assert.Equal(0, model.ExchangeCount);
    }

    [Fact]
    public void A_file_a_call_names_is_shortened_by_the_folder_the_model_was_last_told()
    {
        // A window keeps its model while its folder changes (SM-9.2): the folder is said with every start.
        static string Call(string id, string file) =>
            "{\"type\":\"assistant\",\"message\":{\"content\":[{\"type\":\"tool_use\",\"id\":\"" + id + "\",\"name\":\"Read\",\"input\":{\"file_path\":\"" + file.Replace("\\", "\\\\") + "\"}}]}}";

        var model = new StreamModel();
        model.Take(ClaudeCodeStream.Read(Call("1", @"C:\one\src\a.txt")));
        Assert.Equal(@"tool: Read (C:\one\src\a.txt)", model.Lines[^1].Text);

        model.WorksIn(@"C:\one");
        model.Take(ClaudeCodeStream.Read(Call("2", @"C:\one\src\a.txt")));
        Assert.Equal(@"tool: Read (src\a.txt)", model.Lines[^1].Text);

        // Another folder, given with its separator: the first is no longer cut off, the new one is.
        model.WorksIn(@"D:\two\");
        model.Take(ClaudeCodeStream.Read(Call("3", @"C:\one\src\a.txt")));
        Assert.Equal(@"tool: Read (C:\one\src\a.txt)", model.Lines[^1].Text);
        model.Take(ClaudeCodeStream.Read(Call("4", @"D:\two\b.txt")));
        Assert.Equal(@"tool: Read (b.txt)", model.Lines[^1].Text);

        model.WorksIn(null);
        model.Take(ClaudeCodeStream.Read(Call("5", @"D:\two\b.txt")));
        Assert.Equal(@"tool: Read (D:\two\b.txt)", model.Lines[^1].Text);
    }

    [Fact]
    public void A_reply_line_of_the_stream_is_final_when_it_is_there_and_is_spoken_while_Claude_works()
    {
        // The pass behind the spoken replies (Arrivals) holds the screen's last row back while Claude works, since a
        // row can still grow. A line of the stream cannot: it is spoken with the frame that brings it.
        var model = new StreamModel();
        var arrivals = new Arrivals();
        Assert.True(model.Send("hello"));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"hello"}}"""));
        arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false);

        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"First line.\nSecond line."}]}}"""));
        Assert.True(model.Working);
        var arrival = arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false);

        Assert.True(arrival.NewLine);
        Assert.Equal(["claude: First line.", "Second line."], arrival.SpokenLines.Select(line => line.Text));

        // And not again with the turn's end.
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":1}"""));
        Assert.Null(arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false).Speech);
    }

    [Fact]
    public void An_answer_to_a_waiting_question_gets_no_block_and_a_message_sent_on_purpose_does()
    {
        var model = new StreamModel();
        model.Ask(new AxClaude.Core.Transcript.Question("Which?", [], []));
        Assert.False(model.Send("an answer"));
        Assert.True(model.Send("a message all the same", evenWhilePrompting: true));
        Assert.False(model.Send("   "));
    }
}

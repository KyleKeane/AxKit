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
    public void Twenty_three_cases_are_recorded()
    {
        // A fixture taken away by mistake would make the theory above pass with fewer cases.
        Assert.Equal(23, StreamReplay.Cases().Count());
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
    public void A_turn_that_failed_is_an_error_line_and_is_heard_as_any_turn_is()
    {
        // The error is the turn's answer: the response starts with it and the turn's end rings the bell, as with
        // the screen, where an error row is a reply row. Until 2026-11-17 a failed turn said nothing at all.
        var log = StreamReplay.Run("failure", out var model);

        Assert.Equal(LineKind.Error, model.Lines[^1].Kind);
        Assert.StartsWith("error: There's an issue with the selected model", model.Lines[^1].Text);
        Assert.Contains("    the response started", log);
        Assert.Single(log, entry => entry == "    the bell");
        Assert.False(model.Working);
    }

    [Fact]
    public void A_slash_command_that_runs_the_model_ends_with_its_turn_and_leaves_Claude_idle()
    {
        // A skill or a custom command is a turn of the model's: its end line counts turns, where a command Claude
        // Code runs by itself counts none. The command is no longer awaited either way; the lines here are the
        // stream's shapes with a text of the test's own (the fixture custom-command records one such command).
        var model = new StreamModel();
        var bells = 0;
        model.Bell += () => bells++;

        Assert.False(model.Send("/review"));
        Assert.True(model.Working);
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"tool_use","id":"c1","name":"Read","input":{"file_path":"a.txt"}}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":3}"""));

        Assert.False(model.Working);
        Assert.Equal(1, bells);

        // And the next message is a turn like any other: placed at once, working until its end, one more bell.
        Assert.True(model.Send("hello"));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"hello"}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"hi"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":1}"""));
        Assert.False(model.Working);
        Assert.Equal(2, bells);
        Assert.Equal("claude: hi", model.Lines[^1].Text);
    }

    [Fact]
    public void A_custom_commands_own_line_is_its_echo_and_its_texts_are_replies()
    {
        // The fixture custom-command (Claude Code 2.1.332): the command's own line comes back as a replay of Claude
        // Code's tags just before the turn's texts. The tags are never shown; the command keeps the echo it got when it
        // was sent, as the screen shows one; and what Claude writes is a reply, labelled, spoken and found with c.
        var log = StreamReplay.Run("custom-command", out var model);

        Assert.DoesNotContain(model.Lines, line => line.Text.Contains("<command", StringComparison.Ordinal));
        Assert.Single(model.Lines, line => line.Kind == LineKind.UserEcho && line.Text == "you: /probe-greet");
        Assert.Equal(LineKind.ClaudeReply, model.Lines[1].Kind);
        Assert.Equal("claude: greeted", model.Lines[1].Text);
        // Its replies start a response, and its turn's end is Done; so does the message's after it.
        Assert.Equal(2, log.Count(entry => entry == "    the response started"));
        Assert.Equal(2, log.Count(entry => entry == "    the bell"));
        Assert.False(model.Working);
        Assert.Equal("claude: ok", model.Lines[^1].Text);
    }

    [Fact]
    public void A_command_the_window_never_sent_gets_the_echo_the_screen_shows()
    {
        var model = new StreamModel();
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"<command-message>review</command-message>\n<command-name>/review</command-name>\n<command-args>main</command-args>"}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"Looks fine"}]}}"""));

        Assert.Equal(["you: /review main", "claude: Looks fine"], model.Lines.Select(line => line.Text));
        Assert.Equal([LineKind.UserEcho, LineKind.ClaudeReply], model.Lines.Select(line => line.Kind));
    }

    [Fact]
    public void The_first_requests_answer_lists_the_models_and_a_value_is_set_by_its_command()
    {
        // The fixture choices: the models with their efforts, before any message; /model and /effort with a value are
        // taken mid-run as messages, each printing one line and ringing no bell.
        var log = StreamReplay.Run("choices", out var model);

        Assert.Equal(14, model.Models.Count);
        var haiku = Assert.Single(model.Models, choice => choice.Value == "haiku");
        Assert.Equal("Haiku 5.5", haiku.Name);
        Assert.Equal(["low", "medium", "high", "xhigh", "max"], haiku.Efforts);
        Assert.Empty(Assert.Single(model.Models, choice => choice.Value == "claude-haiku-4-5-20251001").Efforts);
        Assert.Equal("claude-haiku-5-5", model.ModelInUse);
        Assert.Contains(model.Lines, line => line.Kind == LineKind.Plain && line.Text.StartsWith("Set effort level to low", StringComparison.Ordinal));
        Assert.Equal(1, log.Count(entry => entry == "    the bell"));
    }

    [Fact]
    public void A_Claude_Code_that_does_not_answer_the_first_request_is_up_when_its_limit_has_passed()
    {
        // SM-4.10: Ready waits for the source to be up. Without an answer to the first request no mode is named until
        // the first turn; the window's limit then says it is up all the same.
        var model = new StreamModel();
        var changes = 0;
        model.Changed += () => changes++;
        Assert.False(model.Up);

        model.FirstRequestUnanswered();
        Assert.True(model.Up);
        Assert.Null(model.Mode);
        Assert.Equal(1, changes);
        model.FirstRequestUnanswered();
        Assert.Equal(1, changes);

        // A new run asks again.
        model.ResetScreen();
        Assert.False(model.Up);

        // A mode named is up by itself, and the limit then changes nothing.
        StreamReplay.Run("start", out var answered);
        Assert.True(answered.Up);
    }

    [Fact]
    public void The_record_of_an_answer_is_Claude_Codes_own_rows_and_no_reply()
    {
        var model = new StreamModel();
        model.AddAnswerRecord([ClaudeCodeTexts.AnsweredLabelText, "· Which colour? → Red"]);
        model.AddAnswerRecord([]);

        Assert.Equal([LineKind.AnswerRecord, LineKind.AnswerRecord], model.Lines.Select(line => line.Kind));
        Assert.All(model.Lines, line => Assert.True(line.Committed));
    }

    [Fact]
    public void A_command_sent_while_a_turn_runs_is_awaited_until_its_own_end()
    {
        // /model sent while Claude works: the running turn's end is the message's, with its bell, and the command's
        // own end, which counts no turn, comes after and rings none.
        var model = new StreamModel();
        var bells = 0;
        model.Bell += () => bells++;

        Assert.True(model.Send("hello"));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"hello"}}"""));
        Assert.False(model.Send("/model haiku"));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":1}"""));
        Assert.Equal(1, bells);
        Assert.True(model.Working);

        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"Set model to haiku"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":0}"""));
        Assert.Equal(1, bells);
        Assert.False(model.Working);
    }

    [Fact]
    public void A_line_with_a_value_of_another_kind_is_passed_over_and_never_thrown_on()
    {
        // A window reads the stream on a thread where an exception would end it with the conversation.
        foreach (var line in new[]
        {
            """{"type":"result","is_error":false,"num_turns":"three"}""",
            """{"type":"result","is_error":false,"queued_turn_count":null,"num_turns":null}""",
            """{"type":"stream_event","event":"text"}""",
            """{"type":"assistant","message":{"content":[{"type":"text","text":"cut \ud83d"}]}}""",
            """{"type":"assistant","message":"no object"}""",
            """{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":7}]}}""",
        })
        {
            var read = ClaudeCodeStream.Read(line);
            new StreamModel().Take(read);
        }
    }

    [Fact]
    public void A_turns_end_with_a_count_that_is_no_number_still_ends_the_turn()
    {
        // The reader's own TryGetInt32 throws on a null or a text: until 2026-11-27 such an end line was passed over
        // whole, and Claude stayed working for good. The count is 0; the end is an end.
        foreach (var end in new[]
        {
            """{"type":"result","subtype":"success","is_error":false,"queued_turn_count":null,"num_turns":null}""",
            """{"type":"result","subtype":"success","is_error":false,"num_turns":"three"}""",
        })
        {
            var read = ClaudeCodeStream.Read(end);
            Assert.True(read.Has(StreamKind.Ended));
            Assert.Equal(0, read.Turns);
            Assert.Equal(0, read.Queued);

            var model = new StreamModel();
            Assert.True(model.Send("hello"));
            model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
            model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"hello"}}"""));
            model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"hi"}]}}"""));
            Assert.True(model.Working);
            model.Take(read);
            Assert.False(model.Working);
        }
    }

    [Fact]
    public void A_message_written_after_a_command_that_runs_the_model_leaves_Claude_idle_when_both_have_ended()
    {
        // A skill, then a message before the skill's start line came: that start line sees a message waiting, so the
        // turn was no command's by its start, its end left the command awaited, and Claude worked for good (the review
        // of 2026-11-27). The command's own line, come back in the turn, makes the turn the command's.
        var model = new StreamModel();
        Assert.False(model.Send("/review"));
        Assert.True(model.Send("and then say hello"));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"<command-message>review</command-message>\n<command-name>/review</command-name>"}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"Looks fine"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":2}"""));
        // The message has not come back: its turn follows.
        Assert.True(model.Working);

        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"and then say hello"}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"hello"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":1}"""));
        Assert.False(model.Working);
        Assert.Equal(0, model.QueuedMessages);
    }

    [Fact]
    public void A_message_that_comes_back_with_another_text_is_the_oldest_one_awaited()
    {
        // Claude Code may give a message back changed (it takes characters that are not seen out of a prompt). Found by
        // its text alone, such a message stood twice in the conversation and stayed awaited, Claude working for good.
        var model = new StreamModel();
        Assert.True(model.Send("first​ message"));
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"first message"}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"ok"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":1}"""));

        Assert.False(model.Working);
        Assert.Single(model.Lines, line => line.Kind == LineKind.InputMarker);
        Assert.Equal("claude: ok", model.Lines[^1].Text);
    }

    [Fact]
    public void What_a_slash_command_printed_in_a_line_of_its_own_stands_under_its_echo()
    {
        // The fixture compact: /compact's own output comes back in the user's place, in Claude Code's tag. Read and
        // dropped until 2026-11-27; a plain row under the command's echo, as a command's print that comes as a text.
        StreamReplay.Run("compact", out var model);
        var lines = model.Lines.Select(line => line.Text).ToList();
        var echo = lines.IndexOf("you: /compact");
        Assert.True(echo >= 0);
        var printed = lines.IndexOf("Compacted");
        Assert.True(printed > echo);
        Assert.Equal(LineKind.Plain, model.Lines[printed].Kind);

        // Outside a command of the window's it is nothing of the conversation's.
        var idle = new StreamModel();
        idle.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"<local-command-stdout>stray</local-command-stdout>"}}"""));
        Assert.DoesNotContain(idle.Lines, line => line.Text == "stray");
    }

    [Fact]
    public void Each_thing_asked_has_its_own_number_and_only_the_one_asked_now_still_waits()
    {
        // What a control of the window was shown for must still wait when its answer comes (the review of
        // 2026-11-27): the next request is the model's at once, while the list of the one before stays in the window
        // for a moment, and two requests can read the same.
        var model = new StreamModel();
        var question = new Question("Claude asks to use Edit: notes.txt", [], [new QuestionOption("1", "No"), new QuestionOption("3", "Yes, once")]);
        Assert.False(model.StillAsks(model.Asked));

        model.Ask(question);
        var first = model.Asked;
        Assert.True(model.StillAsks(first));

        // Answered: nothing waits, and the number no longer answers anything.
        model.Answered();
        Assert.False(model.StillAsks(first));

        // The same question asked again is another request.
        model.Ask(question);
        var second = model.Asked;
        Assert.NotEqual(first, second);
        Assert.False(model.StillAsks(first));
        Assert.True(model.StillAsks(second));

        // The user's own words, and the way back to the list, are each their own.
        model.AskWords("Enter text for option 2 (Other), or Escape for the list:");
        var words = model.Asked;
        Assert.False(model.StillAsks(second));
        Assert.True(model.StillAsks(words));
        model.Ask(question);
        Assert.False(model.StillAsks(words));

        // Another request in place of a list of the window's own, with no answer between.
        var list = model.Asked;
        model.Ask(new Question("Choose the model", [], [new QuestionOption("1", "Default"), new QuestionOption("3", "Haiku")]));
        Assert.False(model.StillAsks(list));

        // A new run forgets what waited.
        var last = model.Asked;
        model.ResetScreen();
        Assert.False(model.StillAsks(last));

        // A screen's model numbers nothing, and a control shown for it always answers: its keys are typed to the screen.
        var screen = new SessionModel(80, 10);
        Assert.Equal(0, screen.Asked);
        Assert.True(screen.StillAsks(0));
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
    public void The_users_own_words_are_awaited_until_the_next_question_or_the_answer()
    {
        // After Other on a question of Claude's (stream-mode.md SM-7.5): no question is pending, Claude still waits,
        // and the row that asks for the words is the model's, which is what makes the window show its field.
        var model = new StreamModel();
        var changes = 0;
        model.Changed += () => changes++;
        var question = new Question("Which?", [], [new QuestionOption("1", "This"), new QuestionOption("2", "Other")]);

        model.Ask(question);
        Assert.False(model.AwaitsText);
        Assert.Null(model.TextPrompt);

        model.AskWords("Enter text for option 2 (Other), or Escape for the list:");
        Assert.True(model.AwaitsText);
        Assert.True(model.PromptPending);
        Assert.Null(model.PendingQuestion);
        Assert.Equal("Enter text for option 2 (Other), or Escape for the list:", model.TextPrompt);

        // Back to the list: the question is pending again, and no words are awaited.
        model.Ask(question);
        Assert.False(model.AwaitsText);
        Assert.Same(question, model.PendingQuestion);

        model.AskWords("Enter text for option 2 (Other), or Escape for the list:");
        model.Answered();
        Assert.False(model.AwaitsText);
        Assert.False(model.PromptPending);
        Assert.Null(model.TextPrompt);
        Assert.Equal(5, changes);

        // A new run forgets words that were awaited.
        model.AskWords("Enter text for option 2 (Other), or Escape for the list:");
        model.ResetScreen();
        Assert.False(model.AwaitsText);
        Assert.False(model.PromptPending);
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

    /// <summary>The answer to the first request, listing the commands the run takes (SM-5.8; measured 2026-11-26).</summary>
    private static StreamLine FirstAnswer(params string[] commands) =>
        ClaudeCodeStream.Read("{\"type\":\"control_response\",\"response\":{\"subtype\":\"success\",\"request_id\":\"r-1\",\"response\":{\"current_permission_mode\":\"default\",\"commands\":["
            + string.Join(",", commands.Select(name => "{\"name\":\"" + name + "\"}")) + "]}}}");

    /// <summary>
    /// A typed text, by the commands the run named (SM-5.8, SM-5.9; measured 2026-11-26 with the probe's cases
    /// command-lists and screen-commands): a command of Claude Code's own the run does not list is one of its screen
    /// alone, which the run would answer "isn't available in this environment"; a name it does not know at all goes
    /// to Claude as a message; a path is a message; a ! line is a shell line. Before the run has named its commands,
    /// every command counts as taken, as before.
    /// </summary>
    [Fact]
    public void A_typed_text_is_told_by_the_commands_the_run_named()
    {
        var model = new StreamModel();
        Assert.Equal(TypedText.Command, model.TypedTextOf("/help"));

        model.Take(FirstAnswer("compact", "clear", "model", "context", "config"));
        Assert.Equal(["compact", "clear", "model", "context", "config"], model.Commands);
        Assert.Equal(TypedText.Command, model.TypedTextOf("/compact"));
        Assert.Equal(TypedText.Command, model.TypedTextOf("/model haiku"));
        Assert.Equal(TypedText.Command, model.TypedTextOf("/MODEL haiku"));
        // An alias of a listed command is taken by its name: /settings is /config.
        Assert.Equal(TypedText.Command, model.TypedTextOf("/settings"));
        Assert.Equal(TypedText.ScreenCommand, model.TypedTextOf("/help"));
        Assert.Equal(TypedText.ScreenCommand, model.TypedTextOf("  /rewind now"));
        Assert.Equal(TypedText.ScreenCommand, model.TypedTextOf("/login"));
        Assert.Equal(TypedText.UnknownCommand, model.TypedTextOf("/probe-no-such-command"));
        Assert.Equal(TypedText.Message, model.TypedTextOf("/tmp/notes.txt is empty"));
        Assert.Equal(TypedText.Message, model.TypedTextOf("/"));
        Assert.Equal(TypedText.Message, model.TypedTextOf("Run /help for me"));
        Assert.Equal(TypedText.ShellLine, model.TypedTextOf("!git status"));
        Assert.Equal("/rewind", StreamModel.CommandWord("  /rewind now"));

        // A new run forgets them.
        model.ResetScreen();
        Assert.Empty(model.Commands);
    }

    /// <summary>A name the run does not take, sent all the same, goes to Claude as a message (measured): it gets a message's block, and a command the run takes gets its echo.</summary>
    [Fact]
    public void A_name_the_run_does_not_take_is_sent_as_a_message()
    {
        var model = new StreamModel();
        model.Take(FirstAnswer("compact"));
        Assert.True(model.Send("/probe-no-such-command"));
        Assert.Equal(LineKind.InputMarker, model.Lines[0].Kind);

        var other = new StreamModel();
        other.Take(FirstAnswer("compact"));
        Assert.False(other.Send("/compact"));
        Assert.Equal("you: /compact", Assert.Single(other.Lines).Text);
    }

    /// <summary>
    /// What a tool printed (SM-4.11; the fixture tool-output, Claude Code 2.1.332): under its call, the first three lines
    /// and how many more, as the screen's preview rows, plain lines; a command that printed nothing shows what Claude
    /// Code gave back for it. The whole output is not kept (since 2026-11-27): the lines shown and the count are all.
    /// </summary>
    [Fact]
    public void What_a_tool_printed_stands_under_its_call_with_the_count_of_the_rest()
    {
        StreamReplay.Run("tool-output", out var model);
        var lines = model.Lines.Select(line => line.Text).ToList();
        var seq = lines.IndexOf("tool: Bash (seq 1 12)");
        Assert.True(seq >= 0);
        Assert.Equal(["1", "2", "3", "… +9 lines", "tool: Bash (true)", "(Bash completed with no output)", "claude: listed"], lines.Skip(seq + 1));
        Assert.All(model.Lines.Skip(seq + 1).Take(4), line => Assert.Equal(LineKind.Plain, line.Kind));
        Assert.All(model.Lines.Skip(seq + 1).Take(4), line => Assert.Equal(0, line.HeadingLevel));
        // Nothing of the nine lines not shown is in the conversation.
        Assert.DoesNotContain("12", lines);
    }

    /// <summary>What a tool printed is never spoken, as the screen's preview rows are not: a reply ends at a tool's line, and the rows under it are no reply's (SPEC.md FR-7.4).</summary>
    [Fact]
    public void What_a_tool_printed_is_never_spoken_and_the_reply_after_it_is()
    {
        var model = new StreamModel();
        var arrivals = new Arrivals();
        model.Send("go");
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"go"}}"""));
        arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: true);

        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"Running it."},{"type":"tool_use","id":"c1","name":"Bash","input":{"command":"seq 1 5"}}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"c1","is_error":false,"content":"1\n2\n3\n4\n5"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"Five lines."}]}}"""));
        var arrival = arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: true);

        Assert.Equal(["claude: Running it.", "tool: Bash (seq 1 5)", "claude: Five lines."], arrival.SpokenLines.Select(line => line.Text));
        Assert.Contains(model.Lines, line => line.Text == "… +2 lines");
    }

    /// <summary>Calls made together get their results in any order: each output goes under its own call, ahead of the next call's line.</summary>
    [Fact]
    public void Each_output_goes_under_its_own_call_when_calls_were_made_together()
    {
        var model = new StreamModel();
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"tool_use","id":"a","name":"Bash","input":{"command":"echo a"}},{"type":"tool_use","id":"b","name":"Bash","input":{"command":"echo b"}}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"b","is_error":false,"content":[{"type":"text","text":"bee"}]}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"a","is_error":false,"content":"ay\r\n"}]}}"""));

        Assert.Equal(["tool: Bash (echo a)", "ay", "tool: Bash (echo b)", "bee"], model.Lines.Select(line => line.Text));
    }

    /// <summary>A failed call keeps its first line under it, as before; a refusal's words for the model alone are not shown.</summary>
    [Fact]
    public void A_failed_call_shows_its_first_line_and_a_refusals_words_are_not_shown()
    {
        var model = new StreamModel();
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"tool_use","id":"a","name":"Bash","input":{"command":"make"}},{"type":"tool_use","id":"b","name":"Write","input":{"file_path":"x.txt"}}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"a","is_error":true,"content":"Exit code 2\nmake: *** No rule\nstop"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"b","is_error":true,"content":"The user refused this tool call. The user says: no"}]}}"""));

        Assert.Equal(["tool error: Bash (make)", "Exit code 2", "tool error: Write (x.txt)"], model.Lines.Select(line => line.Text));
    }

    /// <summary>
    /// A result whose call's line the conversation's cap has let go changes nothing: until 2026-11-27 its rows went in
    /// at the top of the conversation (a line not found is place -1, and the rows went in after it).
    /// </summary>
    [Theory]
    [InlineData("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"a","is_error":false,"content":"late output"}]}}""")]
    [InlineData("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"a","is_error":true,"content":"late failure"}]}}""")]
    public void A_result_whose_calls_line_has_left_the_conversation_adds_nothing(string result)
    {
        var model = new StreamModel { MaxLines = 20 };
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"tool_use","id":"a","name":"Bash","input":{"command":"sleep 60"}}]}}"""));
        var call = model.Lines[0];
        for (var i = 0; i < 30; i++)
        {
            model.Take(ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"more"}]}}"""));
        }

        Assert.DoesNotContain(call, model.Lines);
        var before = model.Lines.Select(line => line.Text).ToList();
        model.Take(ClaudeCodeStream.Read(result));
        Assert.Equal(before, model.Lines.Select(line => line.Text));
    }

    /// <summary>An error of the service's that names the sign-in command asks for the sign-in once for a turn (SM-6.2); another error does not.</summary>
    [Fact]
    public void An_error_that_asks_for_the_sign_in_is_raised_once_for_a_turn()
    {
        var model = new StreamModel();
        var asked = 0;
        model.SignInAsked += () => asked++;
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","claude_code_version":"2.1.332","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","is_api_error_message":true,"error":"authentication_failed","message":{"content":[{"type":"text","text":"Invalid API key · Please run /login"}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":true,"num_turns":1,"terminal_reason":"api_error","result":"Invalid API key · Please run /login"}"""));
        Assert.Equal(1, asked);
        Assert.Contains(model.Lines, line => line.Kind == LineKind.Error);

        // A failed end alone names it too; an error that names no sign-in raises nothing.
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","claude_code_version":"2.1.332","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":true,"num_turns":1,"terminal_reason":"api_error","result":"Not logged in · Please run /login"}"""));
        Assert.Equal(2, asked);
        model.Take(ClaudeCodeStream.Read("""{"type":"system","subtype":"init","claude_code_version":"2.1.332","permissionMode":"default","session_id":"s"}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"assistant","is_api_error_message":true,"error":"model_not_found","message":{"content":[{"type":"text","text":"Run --model to pick a different model."}]}}"""));
        model.Take(ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":true,"num_turns":1,"terminal_reason":"api_error","result":"Run --model to pick a different model."}"""));
        Assert.Equal(2, asked);
    }
}

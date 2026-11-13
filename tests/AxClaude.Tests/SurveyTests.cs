using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's feedback survey above its own prompt, read as a question answered by its key alone
/// (docs/axClaude/claude-screens.md, "The feedback surveys"). The rows are the ones Claude Code 2.1.323 drew, the
/// survey forced with CLAUDE_FORCE_DISPLAY_SURVEY and answered 0 after a pause (survey.script).
/// </summary>
public class SurveyTests
{
    private const string Fixture = "survey-claude2.1.323-240x50.vt";
    private const string Esc = "\x1b";

    [Fact]
    public void The_survey_is_a_question_answered_by_its_key_alone_and_closes_when_answered()
    {
        var model = new SessionModel(240, 50);
        var questions = new List<Question>();
        var closedAfter = false;
        var bytes = TestHelpers.Fixture(Fixture);
        var previousMs = 0;
        foreach (var chunk in TestHelpers.Chunks(Fixture))
        {
            if (chunk.Ms - previousMs >= 100)
            {
                model.EndFrame();
                if (model.PendingQuestion is { } question)
                {
                    questions.Add(question);
                }
                else if (questions.Count > 0)
                {
                    closedAfter = true;
                }
            }

            model.Feed(bytes.AsSpan(chunk.Offset, chunk.Length).ToArray());
            previousMs = chunk.Ms;
        }

        model.EndFrame();
        var survey = Assert.Single(questions.DistinctBy(question => question.Signature));
        Assert.Equal("How is Claude doing this session? (optional)", survey.Title);
        Assert.Equal(["1", "2", "3", "0"], survey.Options.Select(option => option.Key));
        Assert.Equal(["Bad", "Fine", "Good", "Dismiss"], survey.Options.Select(option => option.Text));
        Assert.True(survey.KeyOnly);
        Assert.Equal("0", survey.EscapeKey);
        Assert.Empty(survey.Text);
        Assert.True(closedAfter, "the question closed once 0 was taken");
        Assert.Null(model.PendingQuestion);
    }

    [Fact]
    public void The_transcript_question_reads_its_link_row_and_says_no_for_escape()
    {
        var model = new SessionModel(240, 50);
        TestHelpers.Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: done\r\n"
            + "● Can Anthropic look at your session transcript to help us improve Claude Code?\r\n"
            + "https://code.claude.com/docs/en/data-usage#session-quality-surveys\r\n"
            + "y: Yes n: No d: Don't ask again\r\n"
            + $"auto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        model.EndFrame();

        var question = Assert.IsType<Question>(model.PendingQuestion);
        Assert.True(model.PromptPending);
        Assert.Equal("Can Anthropic look at your session transcript to help us improve Claude Code?", question.Title);
        Assert.Equal(["https://code.claude.com/docs/en/data-usage#session-quality-surveys"], question.Text);
        Assert.Equal(["Yes", "No", "Don't ask again"], question.Options.Select(option => option.Text));
        Assert.Equal("n", question.EscapeKey);
        Assert.True(question.KeyOnly);
    }

    [Fact]
    public void Answers_rows_in_a_reply_or_with_text_in_the_prompt_are_no_survey()
    {
        // The same rows printed by Claude in a reply, with its reply under them, or a draft in the prompt (a key then
        // goes into the draft, and Claude asks for Ctrl+X first): nothing waits.
        var reply = new SessionModel(240, 50);
        TestHelpers.Feed(reply, $"{Esc}[?25l{Esc}[2J{Esc}[H● How is Claude doing this session? (optional)\r\n"
            + "1: Bad 2: Fine 3: Good 0: Dismiss\r\nclaude: that is what the survey looks like.\r\n"
            + $"auto mode on (shift+tab to cycle)\r\n${Esc}[?25h");
        reply.EndFrame();
        Assert.Null(reply.PendingQuestion);
        Assert.False(reply.PromptPending);

        var draft = new SessionModel(240, 50);
        TestHelpers.Feed(draft, $"{Esc}[?25l{Esc}[2J{Esc}[H● How is Claude doing this session? (optional)\r\n"
            + "1: Bad 2: Fine 3: Good 0: Dismiss\r\n"
            + $"auto mode on (shift+tab to cycle)\r\n$  hello{Esc}[?25h");
        draft.EndFrame();
        Assert.Null(draft.PendingQuestion);
    }

    [Fact]
    public void The_memory_survey_offers_unsure()
    {
        Assert.Equal(["Bad", "Fine", "Good", "Unsure", "Dismiss"], SurveyReader.Options("1: Bad 2: Fine 3: Good 4: Unsure 0: Dismiss").Select(option => option.Text));
        Assert.False(SurveyReader.IsAnswersRow("1: Bad 2: Fine 3: Good"));
        Assert.False(SurveyReader.IsAnswersRow("Use 0: Dismiss"));
    }
}

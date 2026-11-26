using AxClaude.Core.Record;
using AxClaude.Core.Streaming;

namespace AxClaude.Tests;

/// <summary>
/// The choice of an earlier conversation over the stream (docs/axClaude/stream-mode.md SM-8.5): the
/// folder's session files, newest first, each named by when it was last written and the first words of its
/// first message, from the hand-written session files of tests/fixtures/records; a bare --resume asks for it, and the
/// choice becomes --resume with the conversation's id.
/// </summary>
public class ConversationChoiceTests
{
    private const string One = "11111111-1111-4111-8111-111111111111";
    private const string Two = "22222222-2222-4222-8222-222222222222";
    private const string Three = "33333333-3333-4333-8333-333333333333";

    private static string Record(string name) => Path.Combine(Fixtures.Folder(), "records", name);

    [Fact]
    public void A_bare_resume_asks_for_a_conversation_and_one_with_an_id_does_not()
    {
        Assert.True(SessionHandover.AsksToChoose([ClaudeCodeStream.ResumeOption]));
        Assert.True(SessionHandover.AsksToChoose(["--model", "haiku", ClaudeCodeStream.ResumeShortOption]));
        Assert.True(SessionHandover.AsksToChoose([ClaudeCodeStream.ResumeOption, "--model", "haiku"]));
        Assert.False(SessionHandover.AsksToChoose([ClaudeCodeStream.ResumeOption, One]));
        Assert.False(SessionHandover.AsksToChoose([ClaudeCodeStream.ContinueOption]));
        Assert.False(SessionHandover.AsksToChoose([]));
    }

    [Fact]
    public void The_choice_takes_the_place_of_the_bare_resume_and_none_leaves_it_out()
    {
        Assert.Equal([ClaudeCodeStream.ResumeOption, One], SessionHandover.WithChosen([ClaudeCodeStream.ResumeOption], One));
        Assert.Equal(["--model", "haiku", ClaudeCodeStream.ResumeOption, One], SessionHandover.WithChosen(["--model", "haiku", ClaudeCodeStream.ResumeShortOption], One));
        Assert.Equal([ClaudeCodeStream.ResumeOption, One, "--model", "haiku"], SessionHandover.WithChosen([ClaudeCodeStream.ResumeOption, "--model", "haiku"], One));
        // Nothing chosen: a new conversation. What is no session's id is never put on the line.
        Assert.Equal(["--model", "haiku"], SessionHandover.WithChosen([ClaudeCodeStream.ResumeOption, "--model", "haiku"], null));
        Assert.Empty(SessionHandover.WithChosen([ClaudeCodeStream.ResumeOption], "not-a-session"));
        // A session named already stays, and is not named twice.
        Assert.Equal([ClaudeCodeStream.ResumeOption, Two], SessionHandover.WithChosen([ClaudeCodeStream.ResumeOption, Two, ClaudeCodeStream.ResumeOption], One));
    }

    [Theory]
    [InlineData("stream-session.jsonl", "Remember this word for later: kiwi.")]
    [InlineData("plain-turn.jsonl", "Name three colours of a rainbow.")]
    [InlineData("queued.jsonl", "Count slowly to five.")]
    public void A_conversation_is_named_by_its_first_message(string record, string words)
    {
        Assert.Equal(words, ConversationChoice.FirstWordsOf(SessionHandover.Read(Record(record))!));
    }

    [Fact]
    public void Only_the_first_words_of_the_first_line_are_shown()
    {
        RecordEntry Prompt(string text, PromptOrigin origin = PromptOrigin.Typed) => new(RecordKind.Prompt, "p", Text: text, Origin: origin);

        Assert.Equal("one two three four five six seven eight…", ConversationChoice.FirstWordsOf([Prompt("one two three four five six seven eight nine ten")]));
        Assert.Equal("First line…", ConversationChoice.FirstWordsOf([Prompt("  First line\nsecond line holds more")]));
        Assert.Equal(new string('x', ConversationChoice.Characters) + "…", ConversationChoice.FirstWordsOf([Prompt(new string('x', 200))]));
        // A turn Claude Code began by itself, an agent's prompt and a note are no message of the user's.
        Assert.Equal("mine", ConversationChoice.FirstWordsOf(
        [
            Prompt("A background task finished.", PromptOrigin.SelfStarted),
            new RecordEntry(RecordKind.Prompt, "a", Text: "an agent's", Sidechain: true),
            new RecordEntry(RecordKind.Note, "n"),
            Prompt("mine"),
        ]));
        Assert.Null(ConversationChoice.FirstWordsOf([new RecordEntry(RecordKind.Note, "n"), Prompt("   ")]));
        // A hidden character is written out, never put into the list as it came.
        Assert.DoesNotContain('\u0007', ConversationChoice.FirstWordsOf([Prompt("bell\u0007 here")])!);
    }

    [Fact]
    public void A_conversation_is_named_by_its_date_and_time_and_its_first_words()
    {
        var now = new DateTime(2026, 11, 26, 18, 0, 0, DateTimeKind.Local);
        Assert.Equal("26 November, 14:05: Remember this", ConversationChoice.Name(new EarlierConversation(One, new DateTime(2026, 11, 26, 14, 5, 0, DateTimeKind.Local), "Remember this"), now));
        Assert.Equal("3 March 2025, 09:30: Old", ConversationChoice.Name(new EarlierConversation(One, new DateTime(2025, 3, 3, 9, 30, 0, DateTimeKind.Local), "Old"), now));
    }

    [Fact]
    public void The_folders_conversations_are_listed_newest_first_and_the_choice_gives_the_id()
    {
        var root = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"));
        var first = Path.Combine(root, "projects-a", "C--work");
        var second = Path.Combine(root, "projects-b", "C--work");
        try
        {
            Directory.CreateDirectory(first);
            Directory.CreateDirectory(second);
            Copy("plain-turn.jsonl", Path.Combine(first, One + ".jsonl"), new DateTime(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc));
            Copy("stream-session.jsonl", Path.Combine(second, Two + ".jsonl"), new DateTime(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc));
            // A file with no message of the user's is no conversation to take up; a file that is no session's is not looked at.
            File.WriteAllText(Path.Combine(first, Three + ".jsonl"), "{\"type\":\"summary\"}\n");
            Copy("queued.jsonl", Path.Combine(first, "notes.jsonl"), new DateTime(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc));

            var found = ConversationChoice.Find([first, second, Path.Combine(root, "missing")]);
            Assert.Equal([Two, One], found.Select(conversation => conversation.Session));
            Assert.Equal(["Remember this word for later: kiwi.", "Name three colours of a rainbow."], found.Select(conversation => conversation.FirstWords));

            var choice = ConversationChoice.Of(found, new DateTime(2026, 11, 26, 18, 0, 0))!;
            Assert.Equal(ConversationChoice.Title, choice.Question.Title);
            Assert.Equal(["1", "2"], choice.Question.Options.Select(option => option.Key));
            Assert.EndsWith(": Remember this word for later: kiwi.", choice.Question.Options[0].Text);
            Assert.DoesNotContain(choice.Question.Options, option => option.Current);
            Assert.Equal(Two, choice.SessionFor("1"));
            Assert.Equal(One, choice.SessionFor(" 2 "));
            Assert.Null(choice.SessionFor("3"));
            Assert.Null(choice.SessionFor("x"));
            Assert.Null(ConversationChoice.Of([], DateTime.Now));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        static void Copy(string record, string path, DateTime written)
        {
            File.Copy(Record(record), path);
            File.SetLastWriteTimeUtc(path, written);
        }
    }
}

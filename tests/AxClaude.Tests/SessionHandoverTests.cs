using AxClaude.Core;
using AxClaude.Core.Record;
using AxClaude.Core.Streaming;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// Stage 6 of docs/axClaude/stream-mode.md (SM-8.1 to SM-8.3): which conversation a start takes up, the older
/// conversation a window the stream feeds shows from the session's file, and the session kept for a folder so that
/// the screen finds a conversation the stream began. The session files are the hand-written ones of
/// tests/fixtures/records, read through Claude Code's own reader.
/// </summary>
public class SessionHandoverTests
{
    private const string One = "11111111-1111-4111-8111-111111111111";
    private const string Two = "22222222-2222-4222-8222-222222222222";

    private static string Record(string name) => Path.Combine(Fixtures.Folder(), "records", name);

    private static List<string> Lines(StreamModel model) => model.Lines.Select(line => line.Text).ToList();

    [Fact]
    public void Only_a_uuid_in_its_usual_form_is_a_session()
    {
        Assert.True(SessionHandover.IsSessionId(One));
        Assert.False(SessionHandover.IsSessionId(null));
        Assert.False(SessionHandover.IsSessionId(""));
        Assert.False(SessionHandover.IsSessionId("1"));
        Assert.False(SessionHandover.IsSessionId("..\\" + One[3..]));
        Assert.False(SessionHandover.IsSessionId("{" + One + "}"));
        Assert.False(SessionHandover.IsSessionId(One.Replace("-", "")));
    }

    [Fact]
    public void The_stream_names_the_newest_session_in_place_of_continue()
    {
        Assert.Equal([ClaudeCodeStream.ResumeOption, One], SessionHandover.ForStream([ClaudeCodeStream.ContinueOption], One));
        // No session in the folder: a new conversation, as the stream's own --continue begins one (measured).
        Assert.Empty(SessionHandover.ForStream([ClaudeCodeStream.ContinueOption], null));
        Assert.Empty(SessionHandover.ForStream([ClaudeCodeStream.ContinueOption], "not-a-session"));
        // The user's own arguments stay; only --continue is replaced, and a session named already is not named twice.
        Assert.Equal(["--model", "haiku", ClaudeCodeStream.ResumeOption, One], SessionHandover.ForStream(["--model", "haiku", ClaudeCodeStream.ContinueOption], One));
        Assert.Equal([ClaudeCodeStream.ResumeOption, Two], SessionHandover.ForStream([ClaudeCodeStream.ResumeOption, Two, ClaudeCodeStream.ContinueOption], One));
        Assert.Equal(["--model", "haiku"], SessionHandover.ForStream(["--model", "haiku"], One));
    }

    [Fact]
    public void The_screen_takes_up_the_streams_session_while_it_is_the_newest()
    {
        string[] continues = [ClaudeCodeStream.ContinueOption];
        Assert.Equal([ClaudeCodeStream.ResumeOption, One], SessionHandover.ForScreen(continues, One, One));
        Assert.Equal([ClaudeCodeStream.ResumeOption, One], SessionHandover.ForScreen(continues, One, One.ToUpperInvariant()));
        // The user's own --continue among other arguments, as the window scripts start Claude Code.
        Assert.Equal(["--model", "haiku", ClaudeCodeStream.ResumeOption, One], SessionHandover.ForScreen(["--model", "haiku", ClaudeCodeStream.ContinueOption], One, One));
        // A later conversation in the folder (the screen's own, the plain program's): --continue, as always.
        Assert.Equal(continues, SessionHandover.ForScreen(continues, One, Two));
        Assert.Equal(continues, SessionHandover.ForScreen(continues, null, Two));
        Assert.Equal(continues, SessionHandover.ForScreen(continues, "x", "x"));
        // Arguments that do not continue the last conversation are the user's choice and stay.
        Assert.Equal(["--model", "haiku"], SessionHandover.ForScreen(["--model", "haiku"], One, One));
        Assert.Equal([ClaudeCodeStream.ResumeOption, Two, ClaudeCodeStream.ContinueOption], SessionHandover.ForScreen([ClaudeCodeStream.ResumeOption, Two, ClaudeCodeStream.ContinueOption], One, One));
    }

    [Fact]
    public void A_session_is_named_by_resume_or_its_short_form_and_never_by_a_bare_resume()
    {
        Assert.Equal(One, SessionHandover.ResumedIn([ClaudeCodeStream.ResumeOption, One]));
        Assert.Equal(One, SessionHandover.ResumedIn(["--model", "haiku", ClaudeCodeStream.ResumeShortOption, One]));
        Assert.Null(SessionHandover.ResumedIn([ClaudeCodeStream.ResumeOption]));
        Assert.Null(SessionHandover.ResumedIn([ClaudeCodeStream.ResumeOption, "--model", "haiku"]));
        Assert.Null(SessionHandover.ResumedIn([ClaudeCodeStream.ContinueOption]));
    }

    /// <summary>The forms of Claude Code's own options that were not read until 2026-11-27: <c>-c</c> for <c>--continue</c>, <c>--resume=&lt;id&gt;</c> as one argument, and a search term after <c>--resume</c>.</summary>
    [Fact]
    public void The_short_continue_the_one_argument_resume_and_a_search_term_are_read()
    {
        // -c continues, for both sources.
        Assert.True(SessionHandover.Continues(["--model", "haiku", "-c"]));
        Assert.True(SessionHandover.Continues([ClaudeCodeStream.ContinueOption]));
        Assert.False(SessionHandover.Continues(["--model", "haiku"]));
        Assert.Equal(["--model", "haiku", ClaudeCodeStream.ResumeOption, One], SessionHandover.ForStream(["--model", "haiku", "-c"], One));
        Assert.Equal([ClaudeCodeStream.ResumeOption, One], SessionHandover.ForScreen(["-c"], One, One));
        Assert.Equal(["-c"], SessionHandover.ForScreen(["-c"], One, Two));

        // --resume=<id> names a session: nothing is to be chosen, and --continue beside it names none again.
        Assert.Equal(One, SessionHandover.ResumedIn(["--resume=" + One]));
        Assert.False(SessionHandover.AsksToChoose(["--resume=" + One]));
        Assert.Equal(["--resume=" + One], SessionHandover.ForStream(["--resume=" + One, "-c"], Two));
        Assert.Equal(["--resume=" + One, "--model", "haiku"], SessionHandover.WithChosen(["--resume=" + One, "--model", "haiku"], Two));

        // A word after --resume that is no id and no option is Claude Code's search term for its own list: a
        // conversation is to be chosen, and the term goes with the option when one was (it stayed, a stray argument).
        Assert.True(SessionHandover.AsksToChoose([ClaudeCodeStream.ResumeOption, "lighthouse"]));
        Assert.Equal([ClaudeCodeStream.ResumeOption, One, "--model", "haiku"], SessionHandover.WithChosen([ClaudeCodeStream.ResumeOption, "lighthouse", "--model", "haiku"], One));
        Assert.Equal(["--model", "haiku"], SessionHandover.WithChosen(["--model", "haiku", ClaudeCodeStream.ResumeShortOption, "lighthouse"], null));
        Assert.True(SessionHandover.AsksToChoose(["--resume=lighthouse"]));
        Assert.Equal([ClaudeCodeStream.ResumeOption, One], SessionHandover.WithChosen(["--resume=lighthouse"], One));
        // An option after a bare --resume is no search term and stays.
        Assert.Equal([ClaudeCodeStream.ResumeOption, One, "--model", "haiku"], SessionHandover.WithChosen([ClaudeCodeStream.ResumeOption, "--model", "haiku"], One));
    }

    [Fact]
    public void The_newest_is_the_session_file_written_last_directly_in_a_project_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"));
        var first = Path.Combine(root, "projects-a", "C--work");
        var second = Path.Combine(root, "projects-b", "C--work");
        try
        {
            Directory.CreateDirectory(first);
            Directory.CreateDirectory(second);
            Directory.CreateDirectory(Path.Combine(first, One));
            Write(Path.Combine(first, One + ".jsonl"), new DateTime(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc));
            Write(Path.Combine(second, Two + ".jsonl"), new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc));
            // Never a copy set aside, a name that is no session's or a file in a folder under the project folder.
            Write(Path.Combine(first, One + ".orphaned-1.jsonl"), new DateTime(2026, 11, 25, 11, 0, 0, DateTimeKind.Utc));
            Write(Path.Combine(first, "notes.jsonl"), new DateTime(2026, 11, 25, 11, 0, 0, DateTimeKind.Utc));
            Write(Path.Combine(first, One, "33333333-3333-4333-8333-333333333333.jsonl"), new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc));

            Assert.Equal(Two, SessionHandover.Newest([first, second]));
            Write(Path.Combine(first, One + ".jsonl"), new DateTime(2026, 11, 25, 10, 30, 0, DateTimeKind.Utc));
            Assert.Equal(One, SessionHandover.Newest([first, second, Path.Combine(root, "missing")]));
            Assert.Null(SessionHandover.Newest([Path.Combine(root, "missing")]));

            Assert.Equal(Path.Combine(second, Two + ".jsonl"), SessionHandover.FileOf([first, second], Two));
            Assert.Null(SessionHandover.FileOf([first, second], "33333333-3333-4333-8333-333333333333"));
            Assert.Null(SessionHandover.FileOf([first, second], "notes"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        static void Write(string path, DateTime written)
        {
            File.WriteAllText(path, "{}\n");
            File.SetLastWriteTimeUtc(path, written);
        }
    }

    [Fact]
    public void A_file_that_is_not_there_is_not_read()
    {
        Assert.Null(SessionHandover.Read(Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N") + ".jsonl")));
        Assert.NotNull(SessionHandover.Read(Record("stream-session.jsonl")));
    }

    [Fact]
    public void A_session_the_stream_began_is_shown_as_the_screen_shows_a_resumed_one()
    {
        var model = new StreamModel();
        var changed = 0;
        model.Changed += () => changed++;
        var added = model.ShowOlder(SessionHandover.Read(Record("stream-session.jsonl"))!);

        // As SessionModel.MarkReplayedExchanges marks the conversation the screen draws again (script 07's step 8).
        Assert.Equal(
        [
            "# Input 1 from previous session",
            "you: Remember this word for later: kiwi.",
            "",
            "# Output 1 from previous session Reply from Claude",
            "",
            "claude: Noted. I will remember it.",
            "# Input 2 from previous session",
            "you: List the files here.",
            "",
            "# Output 2 from previous session Reply from Claude",
            "",
            "tool: Bash",
            "claude: The folder holds two files.",
        ], Lines(model));
        Assert.Equal(added, model.Lines.Count);
        Assert.Equal(1, changed);
        Assert.All(model.Lines, line => Assert.True(line.Committed));
        Assert.Equal([LineKind.InputMarker, LineKind.UserEcho, LineKind.UserMessage, LineKind.OutputMarker, LineKind.UserMessage, LineKind.ClaudeReply], model.Lines.Take(6).Select(line => line.Kind));
        Assert.Equal(1, model.Lines[0].HeadingLevel);
        Assert.Equal(1, model.Lines[3].HeadingLevel);
        Assert.Equal(LineKind.Tool, model.Lines[11].Kind);
        // Nothing of the old run is Claude's work now: no turn, nothing waits, and the live exchanges count from 1.
        Assert.False(model.Working);
        Assert.False(model.PromptPending);
        Assert.Equal(0, model.ExchangeCount);
        Assert.True(model.HoldsConversation);
    }

    [Fact]
    public void A_failed_call_an_interrupt_and_what_must_not_be_read()
    {
        var model = new StreamModel();
        model.ShowOlder(SessionHandover.Read(Record("tool-turn.jsonl"))!);
        model.ShowOlder(SessionHandover.Read(Record("interrupt.jsonl"))!);
        model.ShowOlder(SessionHandover.Read(Record("odd-lines.jsonl"))!);
        var lines = Lines(model);

        Assert.Contains("tool: Bash", lines);
        Assert.Contains("tool error: Read", lines);
        Assert.Contains("System: " + StreamModel.InterruptedWords, lines);
        Assert.DoesNotContain(lines, line => line.Contains("MUST-NOT-BE-READ", StringComparison.Ordinal));
        // Claude Code's tags and notes are never shown as a message of the user's.
        Assert.DoesNotContain(lines, line => line.Contains("<command-", StringComparison.Ordinal) || line.Contains("<bash-", StringComparison.Ordinal));
    }

    [Fact]
    public void A_compaction_leaves_its_line_and_a_subagents_entries_none()
    {
        var model = new StreamModel();
        model.ShowOlder(
        [
            new RecordEntry(RecordKind.Prompt, "a", Text: "Before.", Origin: PromptOrigin.Typed),
            new RecordEntry(RecordKind.CompactBoundary, "b"),
            new RecordEntry(RecordKind.CompactSummary, "c"),
            new RecordEntry(RecordKind.ReplyText, "d", Text: "From the worker.", Sidechain: true),
            new RecordEntry(RecordKind.ToolCall, "e", ToolName: ClaudeCodeStream.QuestionTool, ToolUseId: "q"),
            new RecordEntry(RecordKind.ReplyText, "f", Text: "After."),
        ]);

        Assert.Equal(["# Input 1 from previous session", "you: Before.", "System: " + StreamModel.CompactedWords, "", "# Output 1 from previous session Reply from Claude", "", "claude: After."], Lines(model));
    }

    [Fact]
    public void A_queued_message_is_shown_once_and_a_turn_claude_code_began_gets_no_message()
    {
        var model = new StreamModel();
        model.ShowOlder(SessionHandover.Read(Record("queued.jsonl"))!);
        var lines = Lines(model);

        Assert.Equal(2, lines.Count(line => line.StartsWith("# Input ", StringComparison.Ordinal)));
        Assert.Single(lines, "you: Then say the date.");
        Assert.DoesNotContain(lines, line => line.Contains("Never mind.", StringComparison.Ordinal));
        Assert.Contains("claude: The background task finished.", lines);
    }

    [Fact]
    public void The_live_exchanges_count_from_one_after_the_older_conversation()
    {
        var model = new StreamModel();
        model.ShowOlder(SessionHandover.Read(Record("stream-session.jsonl"))!);
        model.Send("Which word was it?");

        Assert.Equal("# Input 1", model.Lines[^5].Text);
        Assert.Equal("Which word was it?", model.Lines[^4].Text);
    }

    [Fact]
    public void A_message_of_several_lines_keeps_them()
    {
        var model = new StreamModel();
        model.ShowOlder([new RecordEntry(RecordKind.Prompt, "a", Text: "First line\nSecond line", Origin: PromptOrigin.Typed)]);

        Assert.Equal(["# Input 1 from previous session", "you: First line", "Second line"], Lines(model));
    }

    [Fact]
    public void Nothing_to_show_changes_nothing()
    {
        var model = new StreamModel();
        var changed = 0;
        model.Changed += () => changed++;

        Assert.Equal(0, model.ShowOlder([]));
        Assert.Equal(0, model.ShowOlder([new RecordEntry(RecordKind.Thinking, "a"), new RecordEntry(RecordKind.Prompt, "b", Origin: PromptOrigin.SelfStarted)]));
        Assert.Empty(model.Lines);
        Assert.Equal(0, changed);
    }

    [Fact]
    public void A_folders_stream_session_is_kept_first_and_once()
    {
        var settings = new AppSettings();
        Assert.False(settings.KeepStreamSession(@"C:\work", "1"));
        Assert.True(settings.KeepStreamSession(@"C:\work", One));
        Assert.False(settings.KeepStreamSession(@"C:\work", One));
        Assert.True(settings.KeepStreamSession(@"C:\other", Two));
        Assert.Equal(One, settings.StreamSessionOf(@"c:\WORK\"));
        // /clear begins another session (SM-8.4): the folder's entry is replaced and comes first.
        Assert.True(settings.KeepStreamSession(@"c:\work", Two));
        Assert.Equal([new KeptSession(@"c:\work", Two), new KeptSession(@"C:\other", Two)], settings.StreamSessions);
        Assert.Null(settings.StreamSessionOf(@"C:\elsewhere"));
    }

    [Fact]
    public void What_another_window_kept_meanwhile_stays()
    {
        var settings = new AppSettings();
        settings.KeepStreamSession(@"C:\mine", One);
        // The file now holds what another window kept for its own folder since this one read it.
        List<KeptSession> saved = [new(@"C:\theirs", Two)];

        Assert.True(settings.KeepStreamSession(@"C:\mine", One, saved));
        Assert.Equal([new KeptSession(@"C:\mine", One), new KeptSession(@"C:\theirs", Two)], settings.StreamSessions);
    }

    [Fact]
    public void As_many_folders_are_kept_as_the_limit_and_a_file_keeps_only_sessions()
    {
        var settings = new AppSettings();
        for (var i = 0; i < AppSettings.KeptStreamSessions + 5; i++)
        {
            settings.KeepStreamSession($@"C:\work{i}", One);
        }

        Assert.Equal(AppSettings.KeptStreamSessions, settings.StreamSessions.Count);
        Assert.Equal($@"C:\work{AppSettings.KeptStreamSessions + 4}", settings.StreamSessions[0].Folder);

        var path = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $$"""{"streamSessions":[{"folder":"C:\\a","session":"{{One}}"},{"folder":"C:\\b","session":"--model x"},{"folder":"","session":"{{Two}}"},{"folder":"c:\\A\\","session":"{{Two}}"}]}""");
            var loaded = AppSettings.Load(path, out _);

            Assert.Equal([new KeptSession(@"C:\a", One)], loaded.StreamSessions);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}

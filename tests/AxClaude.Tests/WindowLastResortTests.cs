namespace AxClaude.Tests;

/// <summary>
/// What the review of 2026-11-27 repaired in the window's own files where no pure code decides and no stand-in can
/// play it: held here by reading the source, the last resort. Each test says what the lines are for; a change that
/// moves them changes the test with it. What a script plays is not here (tests/scripts/axClaude/61 to 64).
/// </summary>
public sealed class WindowLastResortTests
{
    /// <summary>The body of a method of one of the window's files, from its first line to the line that closes it.</summary>
    private static string Body(string file, string firstLine)
    {
        var lines = File.ReadAllLines(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxClaude", file));
        var start = Array.FindIndex(lines, line => line.Contains(firstLine, StringComparison.Ordinal));
        Assert.True(start >= 0, $"{file} has no line with: {firstLine}");
        var indent = lines[start].Length - lines[start].TrimStart().Length;
        var end = Array.FindIndex(lines, start + 1, line => line.Length == indent + 1 && line.TrimStart() == "}");
        Assert.True(end > start, $"{file}: the end of {firstLine} was not found");
        return string.Join("\n", lines[start..(end + 1)]);
    }

    /// <summary>
    /// The questions' pipe asks who connected before it reads anything (stream-mode.md SM-7.3): its name and secret
    /// are in the environment of everything Claude Code starts, and a program that is not this program file is dropped.
    /// </summary>
    [Fact]
    public void The_questions_pipe_takes_a_connection_of_this_program_file_alone()
    {
        var take = Body("MainForm.Stream.cs", "private void Take(NamedPipeServerStream pipe)");
        var asked = take.IndexOf("UserPipe.ClientOf(pipe)", StringComparison.Ordinal);
        var read = take.IndexOf("HookFrame.Read(", StringComparison.Ordinal);
        Assert.True(asked >= 0 && read > asked, "the client is asked for before the frame is read");
        Assert.Contains("ThisProgram.Runs(client)", take, StringComparison.Ordinal);
    }

    /// <summary>A new run of Claude Code starts with nothing owed and nothing the user's doing from the run before it.</summary>
    [Theory]
    [InlineData("_wait = null;")]
    [InlineData("_bellPending = false;")]
    [InlineData("_bellNoMarks = false;")]
    [InlineData("_doneAwaitedSince = 0;")]
    [InlineData("_userActedAt = 0;")]
    [InlineData("_readyAnnounced = false;")]
    public void A_new_run_forgets_what_the_run_before_left(string reset)
    {
        Assert.Contains(reset, Body("MainForm.Process.cs", "private void ClaudeStarted()"), StringComparison.Ordinal);
    }

    /// <summary>Every end of a run drops what an AI tool offered in it (FR-2.12): the offer shown and those that wait.</summary>
    [Fact]
    public void Every_end_of_a_run_drops_the_offers_of_that_run()
    {
        Assert.Contains("ForgetOffers();", Body("MainForm.Process.cs", "protected override void StopProcess(bool terminateAtOnce = false)"), StringComparison.Ordinal);
        var forget = Body("MainForm.Mcp.cs", "private void ForgetOffers()");
        Assert.Contains("_offers.Clear();", forget, StringComparison.Ordinal);
        Assert.Contains("_offerShown = null;", forget, StringComparison.Ordinal);
    }

    /// <summary>The way back to the screen starts the window again as the window it is (stream-mode.md SM-9.3): by the arguments a restart is made of, for the other source.</summary>
    [Fact]
    public void The_way_back_to_the_screen_starts_the_same_window_again()
    {
        var reopen = Body("MainForm.Stream.cs", "private void ReopenWithScreen()");
        Assert.Contains("RestartArguments(folder, messageSent: false)", reopen, StringComparison.Ordinal);
        Assert.Contains("Source = AppSettings.SourceScreen", reopen, StringComparison.Ordinal);
    }

    /// <summary>Every list and field of the control area that answers a question asks first whether what it was shown for still waits (stream-mode.md SM-7.4; the pure part is StreamModel's, the play script 62).</summary>
    [Theory]
    [InlineData("private void ShowQuestionList(", 2)]
    [InlineData("private void ShowOwnAnswer(string prompt)", 2)]
    public void An_answer_from_the_control_area_is_bound_to_what_it_was_shown_for(string method, int times)
    {
        var body = Body("MainForm.Area.cs", method);
        Assert.Equal(times, body.Split("StillAsked(").Length - 1);
    }
}

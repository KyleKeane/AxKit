namespace AxClaude.Tests;

/// <summary>
/// What the review of 2026-11-27 repaired in the window's own files where no pure code decides and no stand-in can
/// play it: held here by reading the source, the last resort. Each test says what the lines are for; a change that
/// moves them changes the test with it. What a script plays is not here (tests/scripts/axClaude/61 to 64).
///
/// What these tests are and are not (the second review of the same day asked that it be said): they look for text
/// in the window's source files, a line that must be there or a call that must be nowhere else. They run nothing of
/// the window, so they show that a line stands where it should and never that the window behaves. The behaviour
/// each of them guards is tested where it can be: the pure rules in <c>AnsweredWaitTests</c>, <c>StreamModelTests</c>
/// and the bundle's <c>KeyHoldTests</c> and <c>ControlAreaHoldTests</c>, the window on the stand-in by the scripts.
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

    /// <summary>The window's source files, each with its name and its text.</summary>
    private static IEnumerable<(string Name, string Text)> WindowFiles() =>
        Directory.GetFiles(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxClaude"), "*.cs").Select(file => (Path.GetFileName(file), File.ReadAllText(file)));

    /// <summary>
    /// Every list and field of the control area is held and bound to what it was shown for (stream-mode.md SM-7.4,
    /// SPEC.md D33; the pure parts are <c>StreamModel.StillAsks</c> and <c>AnsweredWait.Unheard</c>, each with its
    /// tests). The window has one method that puts a list there and one that puts a field (<c>AreaList</c>,
    /// <c>AreaField</c>), which hold it and wrap Enter, Escape and a key of the list's own in the caller's check; so
    /// what is held here is that no other line of the window calls the area's own methods. Until the second review of
    /// 2026-11-27 this test counted the checks in two methods, and the screens' lists had none.
    /// </summary>
    [Theory]
    [InlineData("Area.ShowList(", "private void AreaList(")]
    [InlineData("Area.ShowField(", "private void AreaField(")]
    public void A_control_reaches_the_area_through_the_one_method_that_holds_and_binds_it(string call, string method)
    {
        var calls = WindowFiles().SelectMany(file => file.Text.Split('\n').Select((line, index) => (file.Name, Line: line.Trim(), Number: index + 1)))
            .Where(line => line.Line.Contains(call, StringComparison.Ordinal) && !line.Line.StartsWith("//", StringComparison.Ordinal))
            .Select(line => $"{line.Name}:{line.Number}")
            .ToList();
        Assert.True(calls.Count == 1, $"{call} is called in {calls.Count} places of the window ({string.Join(", ", calls)}): only {method} may call it, so that every control is held and bound.");

        var body = Body("MainForm.Area.cs", method);
        Assert.Contains(call, body, StringComparison.Ordinal);
        Assert.Contains("hold: true", body, StringComparison.Ordinal);
        // Enter and Escape, and for a list a key of its own: each behind the caller's check.
        Assert.Equal(call.Contains("List", StringComparison.Ordinal) ? 3 : 2, body.Split("if (still())").Length - 1);
    }

    /// <summary>
    /// The keys that reach Claude Code's screen from outside the area ask first whether what waits there has been put
    /// to the user (SPEC.md D33, <c>AnsweredWait.Unheard</c>): the message field's Enter, and Claude Code's own mode
    /// key, which approves a plan.
    /// </summary>
    [Theory]
    [InlineData("MainForm.Input.cs", "private void SendInput()", "FieldWaitsForQuestion(")]
    [InlineData("MainForm.Input.cs", "private bool FieldWaitsForQuestion(bool typed)", "WaitsUnheard")]
    [InlineData("MainForm.Input.cs", "private void NextPermissionMode()", "WaitsUnheard")]
    [InlineData("MainForm.Area.cs", "private bool StillAsked(int asked)", "WaitsUnheard")]
    [InlineData("MainForm.Area.cs", "private bool ScreenStillShown(string signature)", "WaitsUnheard")]
    public void A_key_for_Claude_Codes_screen_asks_whether_what_waits_was_put_to_the_user(string file, string method, string check)
    {
        Assert.Contains(check, Body(file, method), StringComparison.Ordinal);
    }

    /// <summary>
    /// An interrupt ends the user's act at once (SPEC.md D33): the keys of an answer still being typed out were
    /// dropped, and the settle must not wait for them. And every message typed to Claude Code's prompt empties a
    /// message the interrupt put back there first, in the one place every message passes (FR-2.3a).
    /// </summary>
    [Fact]
    public void An_interrupt_ends_the_users_act_and_every_message_empties_a_restored_one_first()
    {
        var interrupt = Body("MainForm.Input.cs", "private void Interrupt()");
        var dropped = interrupt.IndexOf("ClearWrites();", StringComparison.Ordinal);
        var stamped = interrupt.IndexOf("UserActedNow();", StringComparison.Ordinal);
        Assert.True(dropped >= 0 && stamped > dropped, "Interrupt stamps the user's act as ending now, after the queued keys were dropped");
        Assert.Contains("Area.Hold(UserActionMs);", interrupt, StringComparison.Ordinal);

        var queue = Body("MainForm.Input.cs", "private void QueueWrites(string text)");
        var cleared = queue.IndexOf("ClearRestoredMessage();", StringComparison.Ordinal);
        var typed = queue.IndexOf("Enqueue(", StringComparison.Ordinal);
        Assert.True(cleared >= 0 && typed > cleared, "QueueWrites empties a restored message before it types the next one");
        var elsewhere = WindowFiles().Where(file => file.Text.Split("ClearRestoredMessage();").Length - 1 > (file.Name == "MainForm.Input.cs" ? 1 : 0)).Select(file => file.Name);
        Assert.Empty(elsewhere);
    }
}

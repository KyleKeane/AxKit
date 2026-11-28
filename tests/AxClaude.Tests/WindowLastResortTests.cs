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
public sealed partial class WindowLastResortTests
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
    [InlineData("MainForm.Input.cs", "private void NextPermissionMode()", "if (_model.PromptPending)")]
    [InlineData("MainForm.Input.cs", "private bool KeyMayGoToClaude()", "WaitsUnheard")]
    [InlineData("MainForm.Area.cs", "private bool StillAsked(int asked, string signature)", "AnsweredWait.StillWaits(signature, WaitSignature, _wait?.Signature, _wait?.Stale ?? false)")]
    [InlineData("MainForm.Area.cs", "private bool ScreenStillShown(string signature)", "AnsweredWait.StillWaits(signature, WaitSignature, _wait?.Signature, _wait?.Stale ?? false)")]
    public void A_key_for_Claude_Codes_screen_asks_whether_what_waits_was_put_to_the_user(string file, string method, string check)
    {
        Assert.Contains(check, Body(file, method), StringComparison.Ordinal);
    }

    /// <summary>
    /// Every line an AI tool offered that the window queues carries how it is written out when it is given back: as the
    /// question showed it, under the AI caution's words (SPEC.md D33; the seventh review of 2026-11-27, K5; root rule
    /// 11). Until then it was written raw, in axKit's voice. Every <c>QueueWrites</c> of the offers' file names its
    /// <c>shown</c>, and the words come from <c>AiCaution</c> and <c>McpOffer.Shown</c>.
    /// </summary>
    [Theory]
    [InlineData("private void SendOffered(Offer offer)", "AiCaution.OfferedNotSent(")]
    [InlineData("private void AskConcern(Offer offer)", "AiCaution.ConcernNotSent(")]
    public void An_offered_line_given_back_is_written_under_the_AI_caution(string method, string words)
    {
        var body = Body("MainForm.Mcp.cs", method);
        var queued = body.IndexOf("QueueWrites(", StringComparison.Ordinal);
        Assert.True(queued >= 0, $"{method} queues its line");
        var shown = body.IndexOf("shown: $\"{" + words, queued, StringComparison.Ordinal);
        Assert.True(shown > queued && body.IndexOf("McpOffer.Shown(line)", shown, StringComparison.Ordinal) > shown, $"{method}: the line given back is written as {words} and McpOffer.Shown make it");
        var mcp = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxClaude", "MainForm.Mcp.cs"));
        Assert.Equal(2, mcp.Split("QueueWrites(").Length - 1);
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

        var queue = Body("MainForm.Input.cs", "private void QueueWrites(string text, bool fromField = false, Action? sent = null, string? shown = null)");
        var cleared = queue.IndexOf("ClearRestoredMessage(message.Flight.Goes);", StringComparison.Ordinal);
        var typed = queue.IndexOf("Enqueue(", StringComparison.Ordinal);
        Assert.True(cleared >= 0 && typed > cleared, "QueueWrites empties a restored message before it types the next one");
        var elsewhere = WindowFiles().Where(file => file.Text.Split("ClearRestoredMessage(message.Flight.Goes);").Length - 1 > (file.Name == "MainForm.Input.cs" ? 1 : 0)).Select(file => file.Name);
        Assert.Empty(elsewhere);
    }

    /// <summary>
    /// Every place of the window that writes to Claude Code's console, by its method, with how many writes it makes
    /// and what stands between a key of the user's and a question that has not been heard, or is held (SPEC.md D33,
    /// "No key goes to a question but through its list"; the third review of 2026-11-27, which found the mode key
    /// answering a question inside its hold). A write is a call of <c>Write</c>, <c>Enqueue</c>,
    /// <c>QueueWrites</c> or <c>TypeAnswer</c>. The last column is a text the method's body must hold: the check it
    /// asks first, or the bound it gives its writes. A write in a method that is not listed here fails the test
    /// below: it is then added with its check, or with the reason it needs none.
    /// </summary>
    public static TheoryData<string, string, int, string> ConsoleWrites() => new()
    {
        // The message field's Enter: not while what waits was not put to the user; with the list on, the field is hidden while Claude asks.
        { "MainForm.Input.cs", "private void SendInput()", 1, "if (FieldWaitsForQuestion(text.Length > 0))" },
        // The empty field's Enter (the fifth review of 2026-11-27): bound to the question it was pressed for, or only
        // while the frame shows a draft on Claude Code's prompt row and nothing is asked, asked again as it goes.
        { "MainForm.Input.cs", "private void EmptyFieldEnter()", 1, "QueuedWrite.MessageGoes(true, waitedFor, _model.PromptPending, WaitIdentity()) : !_model.PromptPending && _model.PromptDraft is { Length: > 0 }" },
        // A message's writes, each bound: over when Claude Code has begun to ask since it was queued; its first only onto a bare row, its Enter only on a
        // positive check, the message on Claude Code's prompt row (QueuedWrite.MessageEnter), not yet while it waits; the Enter again and the wait until it is taken (the sixth review of 2026-11-27, F1). The row emptied only while it holds exactly the message given back.
        { "MainForm.Input.cs", "private void QueueWrites(string text, bool fromField = false, Action? sent = null, string? shown = null)", 1, "Enqueue(write, delay, MessageChecks(message, check, () =>" },
        { "MainForm.Input.cs", "private void ClearRestoredMessage(Func<bool> messageGoes)", 2, "Enqueue(ClaudeCodeKeys.CtrlU, 30, Clear);" },
        { "MainForm.Input.cs", "private void SendNow()", 2, "if (!KeyMayGoToClaude())" },
        // The stop key: the one command that sends a key whatever Claude Code asks; its Escape is a no, never a yes.
        { "MainForm.Input.cs", "private void Interrupt()", 1, "Write(ClaudeCodeKeys.Escape, still: null);" },
        { "MainForm.Input.cs", "private void NextPermissionMode()", 1, "if (_model.PromptPending)" },
        { "MainForm.Input.cs", "private void SendKey(string key)", 1, "if (!KeyMayGoToClaude())" },
        // The area's own: every list and field is held and bound (AreaList, AreaField; the test above), and what it sends is typed bound to what waits.
        { "MainForm.Area.cs", "private bool ShowClaudeQuestion(Question question)", 1, "TypeAnswer(dismiss, enter: false);" },
        { "MainForm.Area.cs", "private void ShowEditedRow(ClaudeScreen screen, string text)", 1, "() => ScreenStillShown(shownFor)" },
        { "MainForm.Area.cs", "private void ShowSearchField(ClaudeScreen screen, string text)", 1, "() => ScreenStillShown(shownFor)" },
        { "MainForm.Area.cs", "private void SendScreenKey(string key)", 1, "ScreenKeySent();" },
        { "MainForm.Area.cs", "private void SendEscape()", 1, "MarkWaitAnswered();" },
        { "MainForm.Area.cs", "private void SendAnswer(string key, bool sound = true, bool? keyOnly = null)", 1, "typing = TypeAnswer(" },
        { "MainForm.Area.cs", "private int TypeAnswer(string key, bool enter = true)", 2, "var still = AnswerStill();" },
        // The app's own /feedback for a draft, and its answers to the list of drafts: only while Claude is idle, or to the list it reads.
        { "MainForm.Area.cs", "private void OpenFeedbackDrafts()", 1, "if (!ClaudeIdle" },
        { "MainForm.Area.cs", "private bool AnswerDraftsList(Question question)", 2, "question.Title != FeedbackDraftWatch.ListTitle" },
        { "MainForm.ClaudeCodeSettings.cs", "private void SendClaudeCodeCommand(ClaudeCodeSetting setting)", 1, "if (!ClaudeIdle)" },
        // An offered command's line and a concern's line: their callers send only while Claude is idle (AnswerOffer, AskConcern).
        { "MainForm.Mcp.cs", "private void AskConcern(Offer offer)", 1, "if (!ClaudeIdle)" },
        { "MainForm.Mcp.cs", "private void SendOffered(Offer offer)", 1, "QueueWrites(line, sent: () =>" },
        // Over the stream a chosen value goes as its slash command; nothing is typed there (QueueWrites writes one line).
        { "MainForm.Stream.cs", "private void StreamAnswer(string given)", 1, "StreamChoiceOver();" },
        // The detailed view's guard: the key goes only to close the view when it is open, which shows no question's rows.
        { "MainForm.cs", "protected override bool ProcessConsoleKey(Keys keyData)", 1, "if (_model.TranscriptViewOpen)" },
    };

    /// <summary>
    /// A call of one of the four, bare or through <c>this.</c> or <c>base.</c> (the fourth review of 2026-11-27, L3:
    /// those two passed the pattern, and so did a call on a line that began with "private" or "public", an
    /// expression-bodied member). Since then the compiler holds the rest: <c>ConsoleForm.Write</c> and
    /// <c>Enqueue</c> take the check as a required argument, so a write with none says <c>still: null</c> where it
    /// stands; this table says why that is right for each method, and that nothing writes outside it.
    /// </summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"(?:(?<![\w.])|(?<=\b(?:this|base)\.))(?:Write|Enqueue|QueueWrites|TypeAnswer)\(")]
    private static partial System.Text.RegularExpressions.Regex ConsoleWrite();

    /// <summary>A definition of one of the four, which is no write.</summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"\b(?:void|int)\s+(?:Write|Enqueue|QueueWrites|TypeAnswer)\(")]
    private static partial System.Text.RegularExpressions.Regex WriteDefinition();

    /// <summary>The lines of a text that write to the console: a call of one of the four, outside a comment and outside the four's own definitions.</summary>
    private static int WritesIn(string text) => text.Split('\n').Select(line => line.Trim())
        .Count(line => !line.StartsWith("//", StringComparison.Ordinal) && !WriteDefinition().IsMatch(line) && ConsoleWrite().IsMatch(line));

    /// <summary>The pattern finds the ways around it that it missed until the fourth review of 2026-11-27.</summary>
    [Theory]
    [InlineData("Write(key, still: null);", 1)]
    [InlineData("this.Write(key, still: null);", 1)]
    [InlineData("base.Enqueue(key, 30, still: null);", 1)]
    [InlineData("private void Go() => Write(key, still: null);", 1)]
    [InlineData("public void X() => QueueWrites(line);", 1)]
    [InlineData("private void QueueWrites(string text, bool fromField = false)", 0)]
    [InlineData("private int TypeAnswer(string key, bool enter = true)", 0)]
    [InlineData("writer.Write(bytes);", 0)]
    [InlineData("// Write(key);", 0)]
    public void The_pattern_of_a_console_write_finds_every_call(string line, int writes)
    {
        Assert.Equal(writes, WritesIn(line));
    }

    [Theory]
    [MemberData(nameof(ConsoleWrites))]
    public void A_method_that_writes_to_the_console_asks_or_binds_first(string file, string method, int writes, string check)
    {
        var body = Body(file, method);
        Assert.Contains(check, body, StringComparison.Ordinal);
        Assert.Equal(writes, WritesIn(body));
    }

    /// <summary>No write to the console stands outside the methods of the table: each file's count is the sum of its methods'.</summary>
    [Fact]
    public void Every_write_to_the_console_is_in_a_method_of_the_table()
    {
        var listed = ConsoleWrites().GroupBy(row => (string)row[0]).ToDictionary(group => group.Key, group => group.Sum(row => (int)row[2]));
        foreach (var (name, text) in WindowFiles())
        {
            var writes = WritesIn(text);
            Assert.True(writes == listed.GetValueOrDefault(name), $"{name} writes to the console in {writes} places, and the table of ConsoleWrites lists {listed.GetValueOrDefault(name)}: a new write is listed with the check it asks first.");
        }
    }
}

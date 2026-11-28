using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AxKit.Core;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// Every notice holds its answering keys unless the user's own command opened it (docs/axKit/SPEC.md AX-6.4; the
/// fourth review of 2026-11-27, N1, which closed H2, M1 and L2 as one class): the default turned around, as the
/// question area's <c>hold:</c> was (<see cref="ControlAreaHoldTests"/>). Until that day a notice held only when its
/// maker remembered <see cref="Notice.ByItself"/>, and five did not: the confirmation a command run for an AI tool
/// opened (axCalc's Clear history, axClip's clear, axProcess's End process, axMCP's Remove, Restart now), the report
/// an AI tool drafted, a job's notices. A doubled Enter confirmed them unheard. Here, on the panel with the key
/// messages Windows sends, and on the sources: the table of every caller of <see cref="Notice.ByUser"/>.
/// </summary>
public sealed partial class NoticeHoldTests : IDisposable
{
    /// <summary>A clock the test moves.</summary>
    private long _now = 100_000;

    /// <summary>How often a held press was heard.</summary>
    private int _heard;

    /// <summary>The texts of the buttons that ran, in order.</summary>
    private readonly List<string> _chosen = [];

    private readonly Form _window = new() { TopLevel = false };
    private readonly OverlayPanel _panel;

    public NoticeHoldTests()
    {
        _panel = new OverlayPanel(() => _now, () => _heard++);
        _panel.Chosen += choice => _chosen.Add(choice.Text);
        _window.Controls.Add(_panel);
    }

    public void Dispose() => _window.Dispose();

    /// <summary>The notice in the panel as the window base shows it.</summary>
    private void Show(Notice notice)
    {
        _panel.Populate(notice);
        _panel.Visible = true;
        _window.Visible = true;
        _window.AcceptButton = _panel.DefaultButton;
        _window.CancelButton = _panel.CancelButton;
        Assert.NotEqual(0, _panel.Body.Handle);
    }

    /// <summary>axCalc's question as its command makes it: nothing said about who opened it.</summary>
    private static Notice ClearHistory() => Notice.Ask("Clear the history?", "The 3 entries cannot be brought back.", "C&lear", () => { });

    /// <summary>
    /// The fault (H2): a notice whose maker said nothing about who opened it held nothing, and Enter right after it
    /// came cleared the history. It holds now, whoever opened it, and is heard.
    /// </summary>
    [Fact]
    public void A_notice_holds_its_keys_unless_its_maker_says_the_user_opened_it()
    {
        Show(ClearHistory());
        Assert.True(KeyMessages.Press(_panel.Body, Keys.Enter));
        Assert.Empty(_chosen);
        Assert.Equal(1, _heard);
        _now += KeyHold.DefaultMs;
        KeyMessages.Press(_panel.Body, Keys.Enter);
        Assert.Equal(["C&lear"], _chosen);

        // ByItself holds as before, and the chime is all it adds.
        _chosen.Clear();
        Show(ClearHistory().ByItself());
        KeyMessages.Press(_panel.Body, Keys.Enter);
        Assert.Empty(_chosen);

        // The first result of the user's own command takes its keys at once.
        _now += 60_000;
        Show(Notice.Plain("Variables", "x = 1").ByUser());
        KeyMessages.Press(_panel.Body, Keys.Enter);
        Assert.Equal(["&Close"], _chosen);
        Assert.Equal(2, _heard);
    }

    /// <summary>
    /// The window base's rule (<see cref="Notice.AsShown"/>, which <c>AxKitForm.ShowNotice</c> asks first): a notice
    /// made <see cref="Notice.ByUser"/> holds after all where it cannot be the user's own command's first result:
    /// shown by a notice's button or after one closed, or inside a command clicked for an AI tool.
    /// </summary>
    [Fact]
    public void A_notice_made_by_the_user_holds_after_an_answer_and_inside_an_AI_tools_run()
    {
        var mine = Notice.Plain("Keyboard shortcuts", "F1: the palette.").ByUser();
        Assert.True(Notice.AsShown(mine, followsAnswer: false, forAiTool: false, userKey: Keys.F1).OpenedByUser);
        Assert.False(Notice.AsShown(mine, followsAnswer: true, forAiTool: false, userKey: Keys.F1).OpenedByUser);
        Assert.False(Notice.AsShown(mine, followsAnswer: false, forAiTool: true, userKey: Keys.F1).OpenedByUser);
        // Nothing makes a notice the user's that was not made so.
        Assert.False(Notice.AsShown(ClearHistory(), followsAnswer: false, forAiTool: false, userKey: Keys.F1).OpenedByUser);
        // The last word of its maker counts, and only ByUser makes it the user's.
        Assert.False(mine.ByItself().OpenedByUser);
        Assert.False(ClearHistory().ByUser().ByItself().OpenedByUser);

        var held = Notice.AsShown(mine, followsAnswer: false, forAiTool: true, userKey: Keys.F1);
        Show(held);
        KeyMessages.Press(_panel.Body, Keys.Enter);
        Assert.Empty(_chosen);
        Assert.Equal(1, _heard);

        var window = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AxKitForm.Notices.cs")).ReplaceLineEndings("\n");
        var show = window[window.IndexOf("protected internal void ShowNotice(Notice notice)", StringComparison.Ordinal)..];
        var first = show.IndexOf("notice = Notice.AsShown(notice, followsAnswer: _answeringNotice || _chaining, forAiTool: ClickedForAiTool, userKey: _userKey.Current);", StringComparison.Ordinal);
        var populate = show.IndexOf("Overlay.Populate(notice);", StringComparison.Ordinal);
        Assert.True(first > 0 && populate > first, "ShowNotice asks Notice.AsShown before anything else uses the notice");
        Assert.Contains("_answeringNotice = true;", window, StringComparison.Ordinal);
    }

    /// <summary>
    /// The fifth review of 2026-11-27 (E3, M3, L1): "opened by the user" is a fact of the moment. A notice made
    /// <see cref="Notice.ByUser"/> takes its keys at once only while the key dispatch handles a key of the user's that
    /// can answer no notice: one with Ctrl or Alt, or a function key. Opened by Enter (a menu item chosen with it, a
    /// list's line), Space, Ctrl+Enter, a letter or a button's Alt letter, or by no key at all (a click, a post, after
    /// an await), it holds, whatever its maker said: a doubled press would answer it unheard. Until then the maker's
    /// word alone counted, and axCalc's Clear the history?, reached through the Actions menu with Enter, took the
    /// Enter pressed twice.
    /// </summary>
    [Theory]
    [InlineData(Keys.Control | Keys.Shift | Keys.Delete, true)]
    [InlineData(Keys.F1, true)]
    [InlineData(Keys.Shift | Keys.F10, true)]
    [InlineData(Keys.Control | Keys.F, true)]
    [InlineData(Keys.Alt | Keys.F4, true)]
    [InlineData(Keys.Control | Keys.Alt | Keys.R, true)]
    [InlineData(Keys.Enter, false)]
    [InlineData(Keys.Shift | Keys.Enter, false)]
    [InlineData(Keys.Control | Keys.Enter, false)]
    [InlineData(Keys.Space, false)]
    [InlineData(Keys.Escape, false)]
    [InlineData(Keys.H, false)]
    [InlineData(Keys.D3, false)]
    [InlineData(Keys.Delete, false)]
    [InlineData(Keys.Alt | Keys.L, false)]
    [InlineData(Keys.Alt | Keys.Shift | Keys.L, false)]
    [InlineData(Keys.Alt | Keys.D2, false)]
    public void Only_a_key_that_answers_no_notice_opens_one_that_takes_its_keys_at_once(Keys key, bool atOnce)
    {
        var mine = ClearHistory().ByUser();
        Assert.Equal(atOnce, Notice.AsShown(mine, followsAnswer: false, forAiTool: false, userKey: key).OpenedByUser);
        // No key of the user's handled now: a click, a menu item chosen in a drop-down, a post, the code after an await.
        Assert.False(Notice.AsShown(mine, followsAnswer: false, forAiTool: false, userKey: null).OpenedByUser);

        // The panel as the window base shows it: Enter at once after a menu item's Enter is held and heard.
        Show(Notice.AsShown(mine, followsAnswer: false, forAiTool: false, userKey: key));
        KeyMessages.Press(_panel.Body, Keys.Enter);
        Assert.Equal(atOnce ? new List<string> { "C&lear" } : [], _chosen);
        Assert.Equal(atOnce ? 0 : 1, _heard);
    }

    /// <summary>
    /// The fifth review of 2026-11-27 (L7): a notice that comes by itself in place of a question the user has not
    /// answered runs that question's cancel first, as Escape would, and its text begins with the title it replaced.
    /// Not for a question the user's own key, a button or the action after one replaces, not for a notice the user
    /// opened, and not for one that only informs or shows progress, which its flow replaces with its result.
    /// </summary>
    [Fact]
    public void A_notice_that_comes_by_itself_over_an_unanswered_question_cancels_it_and_says_so()
    {
        var question = ClearHistory();
        Assert.True(Notice.ReplacesUnanswered(question, byItself: true));
        Assert.False(Notice.ReplacesUnanswered(question, byItself: false));
        Assert.False(Notice.ReplacesUnanswered(question.ByUser(), byItself: true));
        Assert.False(Notice.ReplacesUnanswered(Notice.Plain("Installing Git", "This takes a minute.") with { Progress = true }, byItself: true));
        Assert.False(Notice.ReplacesUnanswered(new Notice("Downloading", "40%", [new OverlayChoice("&Cancel", IsDefault: true, IsCancel: true, StaysOpen: true)]), byItself: true));
        Assert.False(Notice.ReplacesUnanswered(null, byItself: true));
        // L4 (the seventh review of 2026-11-27): a notice whose cancel sends a key to the program (axClaude's
        // information screen) is never cancelled by one that comes by itself; nothing goes outside the window unasked.
        var screen = new Notice("Status", "Version 2.1.332", [new OverlayChoice("&Close", IsDefault: true, IsCancel: true), new OverlayChoice("&Copy", StaysOpen: true)]) { CancelReachesProgram = true };
        Assert.False(Notice.ReplacesUnanswered(screen, byItself: true));
        Assert.True(Notice.ReplacesUnanswered(screen with { CancelReachesProgram = false }, byItself: true));
        var area = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxClaude", "MainForm.Area.cs")).ReplaceLineEndings("\n");
        var open = area[area.IndexOf("private void OpenScreenNotice(", StringComparison.Ordinal)..];
        Assert.True(open.IndexOf("CancelReachesProgram = true,", StringComparison.Ordinal) is var marked and > 0
            && marked < open.IndexOf("ShowNotice(notice);", StringComparison.Ordinal), "the information screen's notice says that its Close sends a key to Claude Code");

        var error = Notice.Plain("Failed", "The disk is full.").ByItself().Replacing(question);
        Assert.Equal("Replaces: Clear the history?\r\nThe disk is full.", error.Text);
        Assert.Equal("Replaces: Update.\r\nx", Notice.Plain("t", "x").Replacing(Notice.Plain("Update", string.Empty)).Text);

        var window = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AxKitForm.Notices.cs")).ReplaceLineEndings("\n");
        var show = window[window.IndexOf("protected internal void ShowNotice(Notice notice)", StringComparison.Ordinal)..];
        var asked = show.IndexOf("Notice.ReplacesUnanswered(shown, byItself: !_answeringNotice && !_chaining && _userKey.Current is null)", StringComparison.Ordinal);
        var cancelled = show.IndexOf("notice = Notice.InPlaceOf(shown, notice, byItself: true);", StringComparison.Ordinal);
        var populate = show.IndexOf("Overlay.Populate(notice);", StringComparison.Ordinal);
        Assert.True(asked > 0 && cancelled > asked && populate > cancelled, "ShowNotice cancels an unanswered question it replaces before the new notice is shown");
    }

    /// <summary>
    /// The sixth review of 2026-11-27 (L1, L7): every held question with a cancel and a second choice is cancelled
    /// when a notice comes by itself in its place, whatever its default: the AI tools' pairing request and run request,
    /// whose default is their No, included. Run, as the window base runs it (<see cref="Notice.InPlaceOf"/>): the
    /// pairing's call, which waits on its answer, hears its no at once instead of after its two minutes.
    /// </summary>
    [Fact]
    public async Task A_notice_by_itself_over_an_ai_tools_request_ends_its_wait_at_once()
    {
        var answer = new TaskCompletionSource<bool>();
        var pairing = new Notice("axAutomate asks to use axKit", "It can read what the windows show.",
        [
            new OverlayChoice("&Allow", () => answer.TrySetResult(true)),
            new OverlayChoice("&Deny", () => answer.TrySetResult(false), IsDefault: true, IsCancel: true),
        ]).ByItself();
        var failure = Notice.Plain("Failed", "The disk is full.").ByItself();
        var shown = Notice.InPlaceOf(pairing, failure, byItself: true);
        Assert.True(answer.Task.IsCompletedSuccessfully);
        Assert.False(await answer.Task);
        Assert.StartsWith("Replaces: axAutomate asks to use axKit.", shown.Text, StringComparison.Ordinal);

        var ran = false;
        var request = new Notice("axAutomate asks to run Next permission mode", "x",
            [new OverlayChoice("&Yes, run it", () => ran = true, StaysOpen: true), new OverlayChoice("&No", () => ran = false, IsDefault: true, IsCancel: true)]).ByItself();
        Assert.True(Notice.ReplacesUnanswered(request, byItself: true));
        Assert.False(ran);

        // Not a notice with one choice, not one the user's key replaces, and nothing is run then.
        var close = Notice.Plain("Keyboard shortcuts", "F1.");
        Assert.Same(failure, Notice.InPlaceOf(close, failure, byItself: true));
        var untouched = new TaskCompletionSource<bool>();
        var again = new Notice("Pair?", "x", [new OverlayChoice("&Allow"), new OverlayChoice("&Deny", () => untouched.TrySetResult(false), IsDefault: true, IsCancel: true)]);
        Assert.Same(failure, Notice.InPlaceOf(again, failure, byItself: false));
        Assert.False(untouched.Task.IsCompleted);
    }

    /// <summary>
    /// The sixth review of 2026-11-27 (L3): an AI tool's run types its text only into the notice its click showed,
    /// recorded as it was shown, never into one that came by itself after it, though that one has a field and stands
    /// when the run looks again after the messages already posted.
    /// </summary>
    [Fact]
    public void Only_the_notice_the_runs_click_showed_is_filled()
    {
        var clicked = new Notice("Go to line", "Line:", [OverlayChoice.Close]);
        var byItself = new Notice("Name the file", "Name:", [OverlayChoice.Close]).ByItself();
        Assert.Same(clicked, Notice.ShownForRun(clicked, clicked));
        Assert.Null(Notice.ShownForRun(byItself, clicked));
        Assert.Null(Notice.ShownForRun(byItself, null));
        Assert.Null(Notice.ShownForRun(null, clicked));
        // A notice equal to the click's but not the same one is not the click's.
        Assert.Null(Notice.ShownForRun(clicked with { }, clicked));

        var notices = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AxKitForm.Notices.cs")).ReplaceLineEndings("\n");
        var populate = notices.IndexOf("Overlay.Populate(notice);", StringComparison.Ordinal);
        var recorded = notices.IndexOf("_runNotice = Overlay.Shown;", StringComparison.Ordinal);
        Assert.True(populate > 0 && recorded > populate, "ShowNotice records the notice a run's click showed, as it is shown");
    }

    /// <summary>The key dispatch is where the key is known: the window base sets it around the whole of its key handling, and the notice is asked with it.</summary>
    [Fact]
    public void The_window_base_knows_the_users_key_for_the_length_of_its_handling()
    {
        var window = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AxKitForm.cs")).ReplaceLineEndings("\n");
        var dispatch = window[window.IndexOf("protected override bool ProcessCmdKey(ref Message msg, Keys keyData)", StringComparison.Ordinal)..];
        dispatch = dispatch[..dispatch.IndexOf("\n    }\n", StringComparison.Ordinal)];
        Assert.Contains("_userKey.Handle(keyData, () => ProcessUsersKey(ref message, keyData));", dispatch, StringComparison.Ordinal);
        // Handled nowhere else: no other place can say that a key of the user's is being handled (UserKeyMoment sets
        // it around the handling and gives back what it found; ModalLoopTests runs it).
        var forms = Directory.GetFiles(Path.Combine(SourceTexts.RepositoryRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        Assert.Equal(1, forms.Sum(file => File.ReadAllText(file).Split("_userKey.Handle(").Length - 1));
        // A key of the whole window pressed in menu mode, on the bar or in an open drop-down, runs once the menus have
        // closed: posted, after its handling, so the palette or the report it opens holds (a menu's keys cannot be
        // driven on the run's desktop, so this is read here, and AsShown's null above is the rule it meets). Read
        // over every partial file of the window base (the sixth review of 2026-11-27, L7: a call in another file
        // went unseen): posted twice, and run at once only inside the key's own handling.
        var partials = Directory.GetFiles(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms"), "AxKitForm*.cs");
        Assert.True(partials.Length > 5, "the window base's partial files are read");
        var calls = partials.SelectMany(file => File.ReadAllLines(file)).Select(line => line.Trim())
            .Where(line => line.Contains("RunWindowKey(", StringComparison.Ordinal) && !line.StartsWith("private bool RunWindowKey(", StringComparison.Ordinal) && !line.StartsWith("//", StringComparison.Ordinal) && !line.StartsWith("///", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, calls.Count(line => line == "BeginInvoke(() => RunWindowKey(keyData));"));
        Assert.Equal(3, calls.Count);
        var body = window[window.IndexOf("private bool ProcessUsersKey(ref Message msg, Keys keyData)", StringComparison.Ordinal)..];
        body = body[..body.IndexOf("\n    }\n", StringComparison.Ordinal)];
        Assert.Contains("if (RunWindowKey(keyData))", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The text an AI tool's run carries is typed into the notice that run opened and its default button run (MC-7.9):
    /// no key of the user's, so it passes the hold, which stays against the user's keys (the area's Fill does the same).
    /// The button's own checks still apply.
    /// </summary>
    [Fact]
    public void The_fill_of_an_AI_tools_run_passes_the_hold_and_the_users_keys_do_not()
    {
        var notice = new Notice("New folder", string.Empty,
        [
            new OverlayChoice("&Create", IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type something first"),
        };
        Show(notice);
        KeyMessages.Press(_panel.Body, Keys.Escape);
        Assert.Empty(_chosen);

        var fill = typeof(OverlayPanel).GetMethod("Fill", BindingFlags.Instance | BindingFlags.NonPublic)!;
        fill.Invoke(_panel, [string.Empty]);
        // An empty required field keeps the notice, as for the user's Enter.
        Assert.Empty(_chosen);
        fill.Invoke(_panel, ["Notes"]);
        Assert.Equal(["&Create"], _chosen);
        Assert.Equal("Notes", _panel.InputText);

        var mcp = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AxKitForm.Mcp.cs"));
        Assert.Contains("Overlay.Fill(text);", mcp, StringComparison.Ordinal);
        // Only the notice shown inside the click, recorded there, is filled, on the second pass too (the sixth review of
        // 2026-11-27, L3; the rule itself is run below, Only_the_notice_the_runs_click_showed_is_filled).
        Assert.Contains("Notice.ShownForRun(Overlay.Shown, _runNotice) is { Input: { } input }", mcp, StringComparison.Ordinal);
        Assert.DoesNotContain("Overlay.PressDefault();", mcp, StringComparison.Ordinal);
    }

    /// <summary>
    /// The callers allowed <see cref="Notice.ByUser"/> (and <c>AskText</c>'s and <c>ShowForm</c>'s <c>byUser: true</c>), by
    /// file, with how many and why: the first result of the user's own key or command, shown in the same breath (N1 of
    /// the fourth review of 2026-11-27): a help or a read-only text, the details of what is selected, an explanation
    /// chosen from a list, a field or a form the command opens, and the command's own confirmation. Never anything
    /// shown after an await, by a timer, a job or a flow, and never what a program or an AI tool asks: add a caller
    /// only when its notice cannot appear without the user's key for it. Whatever such a notice's button opens next
    /// holds all the same, and so does all of it when an AI tool's run opened it (<see cref="Notice.AsShown"/>).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (int Calls, string Why)> OpenedByTheUser = new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase)
    {
        [Path.Combine("src", "AxKit.Forms", "AxKitForm.cs")] = (8, "Help, User guide; the six About sections; Help, Keyboard shortcuts: read-only texts from the menus"),
        [Path.Combine("src", "AxKit.Forms", "AxKitForm.Find.cs")] = (1, "the Find notice (Ctrl+F), typed into at once; what comes back after a find follows its button, and holds"),
        [Path.Combine("src", "AxKit.Forms", "AxKitForm.Palette.cs")] = (1, "the command palette (F1), typed into at once"),
        [Path.Combine("src", "AxKit.Forms", "AxKitForm.Mcp.cs")] = (1, "Explain, chosen from the list of an AI's request: its text, read after the list was heard"),
        [Path.Combine("src", "AxKit.Forms", "AiHelpFlow.cs")] = (2, "Ask Claude about this window (its field) and Folders Claude may read, from their keys and menu"),
        [Path.Combine("src", "AxKit.Forms", "AiToolsSetup.cs")] = (1, "Turn on AI tools?, the menu item's own question"),
        [Path.Combine("src", "AxKit.Forms", "ConsoleForm.Session.cs")] = (1, "Restart now?, the restart command's own question (the closing question holds: a flow can ask it)"),
        [Path.Combine("src", "AxKit.Forms", "HotkeyForm.cs")] = (1, "Options, Hotkey: the field that takes the next key"),
        [Path.Combine("src", "AxKit.Forms", "ItemForm.Arrange.cs")] = (1, "Options, Filter: its field"),
        [Path.Combine("src", "AxKit.Forms", "ItemForm.Fields.cs")] = (4, "F2 on a field: the title's and a value's field, the list of a choice, a long text's box"),
        [Path.Combine("src", "AxKit.Forms", "ItemForm.Form.cs")] = (1, "Enter on an item: its form"),
        [Path.Combine("src", "AxKit.Forms", "ReportFlow.cs")] = (1, "F4's report form; an AI tool's draft of it holds (M1)"),
        [Path.Combine("src", "AxKit.Forms", "TextEditorForm.Files.cs")] = (6, "Ctrl+S's own questions: the file changed on disk, Save as UTF-8; Save changes? for New, Open and a recent file, and its one ByUser (the window's closing asks it held: Windows or an update can close it)"),
        [Path.Combine("src", "AxKit.Forms", "TextEditorForm.Viewer.cs")] = (1, "Open in its program refused for a program, a script or a shortcut: the command's answer"),
        [Path.Combine("src", "AxClaude", "MainForm.Mcp.cs")] = (2, "Explain, chosen from the list of an AI's request or of an offered command"),
        [Path.Combine("src", "AxClaude", "MainForm.Stream.cs")] = (1, "the answer that explains, chosen in the list or typed in the message field for a question of the stream"),
        [Path.Combine("src", "AxClaude", "MainForm.NewSession.cs")] = (1, "New session: its controls"),
        [Path.Combine("src", "AxClaude", "MainForm.Ui.cs")] = (1, "Options, Words that keep a reply line silent: its field"),
        [Path.Combine("src", "AxAutomate", "AutomateForm.Run.cs")] = (1, "the report of the last run (its command)"),
        [Path.Combine("src", "AxCalc", "CalcForm.cs")] = (3, "Variables as text; Clear the history? and Clear the variables?, each its command's own question"),
        [Path.Combine("src", "AxClip", "ClipForm.cs")] = (2, "Read in full; Clear the clipboard history?, its command's own question"),
        [Path.Combine("src", "AxFile", "FileForm.Completion.cs")] = (1, "Go to folder: its field"),
        [Path.Combine("src", "AxFile", "FileForm.Operations.cs")] = (3, "the question of an undo that deletes for good, New folder's field, Move to folder's field"),
        [Path.Combine("src", "AxFile", "FileForm.Programs.cs")] = (1, "Add a program: the name, after the Windows file picker the command opened"),
        [Path.Combine("src", "AxGit", "GitForm.Commands.cs")] = (4, "Discard's two questions, Commit's message, New branch's name"),
        [Path.Combine("src", "AxMCP", "MCPForm.cs")] = (4, "Remove's two questions, Add server's form, a resource template's address"),
        [Path.Combine("src", "AxPass", "PassForm.Commands.cs")] = (7, "Delete's four questions, New entry's and New group's fields, the new database's master password after the file picker"),
        [Path.Combine("src", "AxProcess", "ProcessForm.cs")] = (6, "End process's and End processes' questions, a service's and a startup program's details, Remove's two questions of the startup programs (a process's details are read first, and hold)"),
        [Path.Combine("src", "AxTask", "TaskForm.cs")] = (2, "the kept fields and the fields of a list's items, read from a line of the detail pane"),
        [Path.Combine("src", "AxTask", "TaskForm.Actions.cs")] = (4, "an action's form, Run it? and Put it into axTerminal? (Cancel the default), Read the last output"),
        [Path.Combine("src", "AxTask", "TaskForm.Commands.cs")] = (6, "New item, New list, New smart list, a link's title: their fields; Move a list and Move items: their forms"),
        [Path.Combine("src", "AxTask", "TaskForm.Conversion.cs")] = (1, "Enter on a list of the earlier format: Convert it?"),
        [Path.Combine("src", "AxTask", "TaskForm.Approvals.cs")] = (3, "Approve or Decline this report and Options, Approval passphrase: their passphrase fields (the passphrase typed again follows a button, and holds)"),
        [Path.Combine("src", "AxTask", "TaskForm.ManyFields.cs")] = (2, "Set fields and Clear fields: their forms"),
        [Path.Combine("src", "AxTask", "TaskForm.Projects.cs")] = (2, "Add a project folder and Find in every list: their fields"),
        [Path.Combine("src", "AxTemplate", "BrowserForm.cs")] = (1, "Delete: Archive it?, its command's own question"),
        [Path.Combine("src", "AxTemplate", "LauncherForm.cs")] = (2, "Read in full; Enter on a template of several lines for a terminal: Put it in how?"),
        [Path.Combine("src", "AxTerminal", "TerminalForm.NewShell.cs")] = (4, "New session's controls, Switch the shell?, Add a shell's form, Remove the shell?"),
        [Path.Combine("src", "AxMedia", "MediaForm.Player.cs")] = (1, "Go to time: its field, a few digits and Enter typed at once"),
    };

    [GeneratedRegex(@"\.ByUser\(\)|\bbyUser:\s*true\b")]
    private static partial Regex ByUserCall();

    /// <summary>What a notice made after the user's command has passed shows by: an await, a timer of the window base, a task, a post from another thread.</summary>
    [GeneratedRegex(@"\bawait\b|\bAfter\(|\bDo\(async|\bPost\(|\bTask\.Run\(|\bContinueWith\(")]
    private static partial Regex Later();

    /// <summary>The start of a statement that shows a notice: a line that begins with the call.</summary>
    [GeneratedRegex(@"(?m)^[ \t]*(?:_form\.)?(?:ShowNotice|AskText|ShowForm)\(")]
    private static partial Regex Shows();

    /// <summary>The first line of a member of a class, four spaces in: where the text that leads to a call begins.</summary>
    [GeneratedRegex(@"^    (?:(?:public|private|protected|internal|static|async|override|virtual|sealed|new|partial|readonly)\s+)+[^=;]*\(", RegexOptions.Multiline)]
    private static partial Regex MemberStart();

    /// <summary>
    /// Read off the sources: every <see cref="Notice.ByUser"/> and <c>byUser: true</c> stands only where the table
    /// above allows it, and in a member that shows it in the same breath: no await, timer, task or post before it in
    /// the member (a notice shown after the command's work is no first result, and holds).
    /// </summary>
    [Fact]
    public void Every_notice_holds_unless_its_caller_is_listed_as_the_users_own_command()
    {
        var root = SourceTexts.RepositoryRoot();
        var offenders = new List<string>();
        var found = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.EndsWith("OverlayPanel.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file).ReplaceLineEndings("\n");
            foreach (Match call in ByUserCall().Matches(text))
            {
                var lineStart = text.LastIndexOf('\n', call.Index) + 1;
                var lineEnd = text.IndexOf('\n', call.Index);
                var line = text[lineStart..(lineEnd < 0 ? text.Length : lineEnd)].TrimStart();
                if (line.StartsWith("//", StringComparison.Ordinal) || (relative.EndsWith("AxKitForm.Notices.cs", StringComparison.OrdinalIgnoreCase) && line.Contains("(byUser ? ", StringComparison.Ordinal)))
                {
                    // A comment may name it; AskText's and ShowForm's own use is their callers' byUser: true, counted where they stand.
                    continue;
                }

                found[relative] = found.GetValueOrDefault(relative) + 1;
                // What leads to the statement that shows the notice: from the member's first line to the line the
                // statement starts on (the notice's own button actions, which run later, come after it).
                var member = MemberStart().Matches(text[..call.Index]).LastOrDefault();
                var memberStart = member?.Index ?? 0;
                var statement = Shows().Matches(text[memberStart..call.Index]).LastOrDefault();
                var before = text[memberStart..(statement is null ? call.Index : memberStart + statement.Index)];
                if (Later().Match(before) is { Success: true } later)
                {
                    offenders.Add($"{relative}:{text[..call.Index].Count(c => c == '\n') + 1} makes a notice the user's after {later.Value}: it is no first result of the command, and holds");
                }
            }
        }

        foreach (var (file, calls) in found)
        {
            if (!OpenedByTheUser.TryGetValue(file, out var allowed) || allowed.Calls != calls)
            {
                offenders.Add($"{file} makes {calls} notices the user's, and the table allows {(OpenedByTheUser.TryGetValue(file, out var known) ? known.Calls : 0)}");
            }
        }

        foreach (var (file, allowed) in OpenedByTheUser)
        {
            if (!found.ContainsKey(file))
            {
                offenders.Add($"{file} is listed for {allowed.Calls} notices the user opened and has none: take it off the list");
            }
        }

        Assert.True(offenders.Count == 0,
            "A notice holds its answering keys for a moment unless the user's own command opened it at once (AxKit.Core/KeyHold, SPEC AX-6.4): " + string.Join("; ", offenders));
    }

    /// <summary>A notice's property says who opened it, and no caller can hand it a value worked out: it has no setter a caller can reach.</summary>
    [Fact]
    public void Who_opened_a_notice_is_never_a_value_worked_out()
    {
        var property = typeof(Notice).GetProperty(nameof(Notice.OpenedByUser))!;
        Assert.False(property.SetMethod?.IsPublic ?? false, "Notice.OpenedByUser can be set by a caller: the hold of a notice must not be a value worked out.");
        Assert.False(new Notice("t", "x", [OverlayChoice.Close]).OpenedByUser);
        Assert.False(Notice.Plain("t", "x").ByItself().OpenedByUser);
        Assert.True((Notice.Plain("t", "x").ByUser() with { Silent = true }).OpenedByUser);
    }
}

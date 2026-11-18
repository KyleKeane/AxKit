using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;

namespace AxClaude.Core.Streaming;

/// <summary>
/// The conversation and the facts of a session as Claude Code's non-interactive stream gives them
/// (docs/axClaude/stream-mode.md SM-4): the second model on <see cref="ClaudeModel"/>, beside the screen's
/// <see cref="SessionModel"/>. It is handed each line of the stream read down (<see cref="Take"/>), each message the
/// window sends (<see cref="Send"/>) and each question the relay passes on (<see cref="Ask"/>), and keeps the lines a
/// window shows, by the same labels and markers the screen's model gives them, so that every key of the conversation
/// lands where it lands today. No screen, no frames, no rows to tell apart: a line is final when it is added.
/// What it does with each kind of line is settled by the recordings in tests/fixtures/streams, which
/// <c>StreamModelTests</c> replay; their page says what the recordings showed. A window made for the stream feeds it
/// Single threaded: call everything from one thread.
/// </summary>
public sealed class StreamModel : ClaudeModel
{
    /// <summary>A message the window sent, with its block: placed at once when Claude was idle, else when its line comes back; gone from the list once it has come back.</summary>
    private sealed class Sent(string text, List<Line> block)
    {
        public string Text { get; } = text;

        public List<Line> Block { get; } = block;

        public bool Placed { get; set; }
    }

    /// <summary>The messages written and not yet come back, oldest first. While one is here a turn is still to come or runs.</summary>
    private readonly List<Sent> _sent = [];

    /// <summary>The lines of the tool calls that have no result yet, by the call's id: a result that failed turns its call's line into an error's.</summary>
    private readonly Dictionary<string, Line> _calls = new(StringComparer.Ordinal);

    /// <summary>The calls that are a question of Claude's and get no line: the question is the window's to show and to record.</summary>
    private readonly HashSet<string> _questions = new(StringComparer.Ordinal);

    /// <summary>The folder Claude works in, with its separator at the end, by which a file a call names is shortened; empty when not known.</summary>
    private string _folder = string.Empty;

    /// <summary>Between a turn's start (a message written while idle, a start line) and its end line.</summary>
    private bool _turn;

    /// <summary>The slash commands the window sent whose end line has not come: what the assistant writes meanwhile, with no message come back, is a command's print and no reply.</summary>
    private int _commands;

    /// <summary>A message has come back in the turn that runs: from there on its texts are Claude's replies.</summary>
    private bool _taken;

    /// <summary><see cref="ResponseStarted"/> was raised for the turn that runs.</summary>
    private bool _responded;

    /// <summary>
    /// The turn that runs began with a slash command of the window's awaited and no message: its end line is that
    /// command's, whatever turns it counts. A command Claude Code runs by itself counts none (/model); a skill or a
    /// custom command runs the model and counts them, and without this its end left the command awaited, and Claude
    /// working, for good (found by the review of 2026-11-17; not yet recorded).
    /// </summary>
    private bool _commandTurn;

    /// <summary>
    /// The folder Claude works in, for shortening the files its calls name; null when not known. Said with every
    /// start of Claude Code and not once with the model: a window keeps its model while its folder changes.
    /// </summary>
    public void WorksIn(string? folder) => _folder = string.IsNullOrEmpty(folder) ? string.Empty : folder.TrimEnd('\\', '/') + "\\";

    public override bool PromptPending { get; protected set; }

    public override Question? PendingQuestion { get; protected set; }

    /// <summary>
    /// Claude works: from a message written while it was idle, or a start line, until an end line that leaves no
    /// message of the window's still to come back. The end line's own count of waiting messages cannot say it: a
    /// message written while a reply was being written starts a turn of its own after an end line that counts none
    /// (the fixture while-writing).
    /// </summary>
    public override bool Working { get; protected set; }

    /// <summary>The messages written while Claude worked whose block is not yet in the conversation.</summary>
    public override int QueuedMessages { get; protected set; }

    public override string? Mode { get; protected set; }

    public override string? Background { get; protected set; }

    public override IReadOnlyList<BackgroundAgent> BackgroundAgents { get; protected set; } = [];

    /// <summary>Dynamic workflows are not told apart in the stream's list of background work yet: none.</summary>
    public override IReadOnlyList<BackgroundWorkflow> BackgroundWorkflows { get; protected set; } = [];

    /// <summary>The permission mode in force by Claude Code's own name (<see cref="Mode"/> has the screen's words for it); empty while not known. What the window's mode key counts on from (<see cref="ClaudeCodeStream.NextMode"/>).</summary>
    public string ModeName { get; private set; } = string.Empty;

    /// <summary>Every permission mode this run has been in, by Claude Code's own names, in the order it was.</summary>
    public IReadOnlyList<string> ModesSeen => _modesSeen;

    private readonly List<string> _modesSeen = [];

    /// <summary>The session's id as the last start line gave it: what a later run is given to take the conversation up. An id: never shown, said or logged.</summary>
    public string Session { get; private set; } = string.Empty;

    /// <summary>Claude Code's version as the first start line gave it; empty before.</summary>
    public string Version { get; private set; } = string.Empty;

    public override event Action? ResponseStarted;

    public override event Action<string>? Announcement;

    /// <summary>A turn began that no message of the window's started: Claude Code goes on by itself, after work in the background ended (SPEC.md FR-7.3b).</summary>
    public event Action? TurnSelfStarted;

    /// <summary>Said while Claude Code shortens the conversation, and where it cut it.</summary>
    public const string CompactingWords = "Compacting the conversation";
    public const string CompactedWords = "Conversation compacted";

    /// <summary>Said, and written as a line of the app's, when a turn ends by the user's interrupt.</summary>
    public const string InterruptedWords = "Interrupted";

    /// <summary>The words of the screen's model for a conversation emptied by /clear, so that both sources say the same.</summary>
    public const string ClearedWords = "Conversation cleared";
    private const string ClearedLine = "Conversation cleared by /clear; the previous one stays on disk, and /resume brings it back";

    /// <summary>One line of the stream, read down by <see cref="ClaudeCodeStream.Read"/>.</summary>
    public void Take(StreamLine line)
    {
        var changed = false;
        if (line.Tasks is { } tasks)
        {
            changed |= TakeTasks(tasks);
        }

        foreach (var part in line.Parts)
        {
            switch (part.Kind)
            {
                case StreamKind.Started:
                    Session = line.Session;
                    Version = Version.Length > 0 ? Version : line.Version;
                    changed |= SetMode(line.Mode, announce: false);
                    if (!_turn)
                    {
                        _turn = true;
                        _taken = false;
                        _responded = false;
                        // A turn that begins with a command awaited and no message is that command's own.
                        _commandTurn = _commands > 0 && _sent.Count == 0;
                        // No message of the window's waits to be taken and no command runs: Claude Code began this turn.
                        if (_sent.Count == 0 && _commands == 0)
                        {
                            TurnSelfStarted?.Invoke();
                        }
                    }

                    changed |= !Working;
                    Working = true;
                    break;

                case StreamKind.Message:
                    changed |= TakeMessage(part.Text);
                    break;

                case StreamKind.Text when !line.FromWorker:
                    changed = true;
                    TakeText(part);
                    break;

                case StreamKind.Thinking when !line.FromWorker:
                    Respond();
                    break;

                case StreamKind.ToolCall when !line.FromWorker:
                    if (part.Tool == ClaudeCodeStream.QuestionTool)
                    {
                        _questions.Add(part.Call);
                        break;
                    }

                    Respond();
                    var call = Final(LineKind.Tool, $"{ClaudeCodeTexts.ToolLabel} {part.Tool}{About(part.About)}");
                    _lines.Add(call);
                    if (part.Call.Length > 0)
                    {
                        _calls[part.Call] = call;
                    }

                    changed = true;
                    break;

                case StreamKind.ToolResult when !line.FromWorker:
                    changed |= TakeResult(part);
                    break;

                case StreamKind.Note:
                    changed |= TakeNote(part.Text, line);
                    break;

                case StreamKind.Control:
                    // The answer to the request a host sends first names the mode in force and the version, before
                    // any message: the first mode known is no change, and is not said.
                    Version = Version.Length > 0 ? Version : line.Version;
                    changed |= SetMode(line.Mode, announce: false);
                    break;

                case StreamKind.Ended:
                    changed = true;
                    TakeEnd(line);
                    break;
            }
        }

        if (changed)
        {
            Trim();
            RaiseChanged();
        }
    }

    /// <summary>
    /// Records that the user sent a message, as the screen's model does (SPEC.md FR-4.1, FR-4.7): while Claude is idle
    /// its exchange block goes into the conversation at once; while Claude works it is held and goes in when the
    /// message's line comes back, which is when Claude takes it up, so that the message and its reply arrive
    /// together. A slash command gets no block: its line is the echo Claude Code's screen prints, <c>you: /model</c>.
    /// False when no block applies (empty text, a slash command, an answer to a waiting question).
    /// </summary>
    public override bool Send(string text, bool evenWhilePrompting = false)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed.StartsWith(ClaudeCodeTexts.CommandStart))
        {
            _commands++;
            _lines.Add(Final(LineKind.UserEcho, $"{ClaudeCodeTexts.YouLabel} {trimmed}"));
            Working = true;
            RaiseChanged();
            return false;
        }

        if (PromptPending && !evenWhilePrompting)
        {
            return false;
        }

        var sent = new Sent(trimmed, ExchangeBlock(trimmed));
        _sent.Add(sent);
        if (!Working)
        {
            Place(sent);
        }

        Working = true;
        Count();
        RaiseChanged();
        return true;
    }

    /// <summary>Claude waits for the user's own words, after Other was chosen on one of its questions (<see cref="AskWords"/>): the one thing of a screen's this model also has.</summary>
    public override bool AwaitsText => TextPrompt is not null;

    /// <summary>The relay passed on a question: Claude waits on it until <see cref="Answered"/>.</summary>
    public void Ask(Question question)
    {
        PendingQuestion = question;
        TextPrompt = null;
        PromptPending = true;
        RaiseChanged();
    }

    /// <summary>
    /// The user chose Other on a question of Claude's: Claude waits for their own words, asked for with the row
    /// Claude Code's screen shows for them (<paramref name="prompt"/>), until the next <see cref="Ask"/> or
    /// <see cref="Answered"/>.
    /// </summary>
    public void AskWords(string prompt)
    {
        PendingQuestion = null;
        TextPrompt = prompt;
        PromptPending = true;
        RaiseChanged();
    }

    /// <summary>The question was answered, or taken back: nothing waits.</summary>
    public void Answered()
    {
        if (!PromptPending)
        {
            return;
        }

        PendingQuestion = null;
        TextPrompt = null;
        PromptPending = false;
        RaiseChanged();
    }

    /// <summary>A line of the app's own, at the end of the conversation: a stream has no live rows to keep it above.</summary>
    public override void AddSystemLine(string text)
    {
        _lines.Add(Final(LineKind.System, "System: " + text));
        RaiseChanged();
    }

    /// <summary>Records the question Claude waits on as the line <c>Question: title</c>, once, found again with q (SPEC.md D33).</summary>
    public override void MarkQuestion()
    {
        if (PendingQuestion is not { } question)
        {
            return;
        }

        var text = "Question: " + question.Title;
        for (var i = _lines.Count - 1; i >= 0; i--)
        {
            if (_lines[i].Kind == LineKind.Bookmark)
            {
                continue;
            }

            if (_lines[i].Kind == LineKind.Question && _lines[i].Text == text)
            {
                return;
            }

            break;
        }

        _lines.Add(Final(LineKind.Question, text));
        RaiseChanged();
    }

    /// <summary>Empties the conversation for a new session: every line goes, the messages held with them, and the blocks and bookmarks count from 1 again.</summary>
    public override void Clear()
    {
        _lines.Clear();
        _sent.Clear();
        ExchangeCount = 0;
        ResetBookmarks();
        ForgetRun();
    }

    /// <summary>
    /// A new run of Claude begins in the same window and the conversation stays. A message the old run never took up
    /// goes into the conversation here, at the end of that run, so that what was sent is on record with no reply
    /// after it; what was known of the old run is forgotten.
    /// </summary>
    public override void ResetScreen()
    {
        foreach (var sent in _sent)
        {
            Place(sent);
        }

        _sent.Clear();
        ForgetRun();
    }

    /// <summary>A stream has no frames: nothing to complete but to say that the lines may have changed.</summary>
    protected override void CompleteFrame() => RaiseChanged();

    private void ForgetRun()
    {
        _calls.Clear();
        _questions.Clear();
        _turn = false;
        _taken = false;
        _responded = false;
        _commandTurn = false;
        _commands = 0;
        Working = false;
        QueuedMessages = 0;
        Mode = null;
        ModeName = string.Empty;
        _modesSeen.Clear();
        PendingQuestion = null;
        TextPrompt = null;
        PromptPending = false;
        Background = null;
        BackgroundAgents = [];
        Session = string.Empty;
    }

    /// <summary>A message came back: its block goes in now unless it went in when it was sent, and it is no longer awaited.</summary>
    private bool TakeMessage(string text)
    {
        _taken = true;
        var sent = _sent.FirstOrDefault(candidate => candidate.Text == text.Trim());
        if (sent is null)
        {
            // A message this model was never told of: on record all the same, with its block.
            sent = new Sent(text.Trim(), ExchangeBlock(text.Trim()));
        }
        else
        {
            _sent.Remove(sent);
        }

        var placed = !sent.Placed;
        Place(sent);
        Count();
        return placed;
    }

    /// <summary>A text of the assistant's: the service's error, a slash command's print, or a reply of Claude's, by what the turn is.</summary>
    private void TakeText(StreamPart part)
    {
        var lines = part.Text.Replace("\r\n", "\n").Split('\n');
        if (part.Error)
        {
            // An error is the turn's answer: the response has started with it, as the screen's error row starts one.
            Respond();
            Add(LineKind.Error, ClaudeCodeTexts.ErrorLabel, lines);
            return;
        }

        if (_commands > 0 && !_taken)
        {
            // What a slash command prints, as the screen shows it: plain rows under the command's echo.
            foreach (var line in lines)
            {
                _lines.Add(Final(LineKind.Plain, line.TrimEnd()));
            }

            return;
        }

        Respond();
        Add(LineKind.ClaudeReply, ClaudeCodeTexts.ClaudeLabel, lines);
    }

    /// <summary>
    /// The lines of a labelled text: the label and the first line on one row, as Claude Code's screen prints them,
    /// the rest plain under it. A line written as a Markdown heading is one, a level under the exchange markers'
    /// (<c>#</c> is level 2), and keeps its marker, which the view hides when its option says so; on the labelled
    /// row the marker goes, since the label stands in front of it.
    /// </summary>
    private void Add(LineKind kind, string label, string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var text = lines[i].TrimEnd();
            var level = HeadingLevel(text);
            if (i == 0)
            {
                var first = level > 0 ? text.TrimStart('#').TrimStart(' ') : text;
                var labelled = Final(kind, first.Length > 0 ? $"{label} {first}" : label);
                labelled.HeadingLevel = level;
                _lines.Add(labelled);
            }
            else
            {
                var plain = Final(LineKind.Plain, text);
                plain.HeadingLevel = level;
                _lines.Add(plain);
            }
        }
    }

    /// <summary>The level of a line written as a Markdown heading, one to six marks and a space: one deeper than its marks, so that the exchange markers stay the only level 1; 0 for any other line.</summary>
    private static int HeadingLevel(string text)
    {
        var marks = 0;
        while (marks < text.Length && text[marks] == '#')
        {
            marks++;
        }

        return marks is >= 1 and <= 6 && marks < text.Length && text[marks] == ' ' ? Math.Min(marks + 1, 6) : 0;
    }

    /// <summary>A tool's result: a failure turns its call's line into an error's, as Claude Code's screen redraws it, with the first line of what came back under it.</summary>
    private bool TakeResult(StreamPart part)
    {
        if (_questions.Remove(part.Call) || !_calls.Remove(part.Call, out var call) || !part.Error)
        {
            return false;
        }

        call.Kind = LineKind.ToolError;
        call.Text = ClaudeCodeTexts.ToolErrorLabel + call.Text[ClaudeCodeTexts.ToolLabel.Length..];
        if (part.Text.Length > 0)
        {
            var at = _lines.LastIndexOf(call);
            _lines.Insert(at + 1, Final(LineKind.Plain, part.Text));
        }

        return true;
    }

    /// <summary>Something Claude Code says about itself: the permission mode, a compaction, the conversation emptied.</summary>
    private bool TakeNote(string note, StreamLine line)
    {
        switch (note)
        {
            case ClaudeCodeStream.StatusNote:
                if (line.Status == ClaudeCodeStream.CompactingStatus)
                {
                    Announcement?.Invoke(CompactingWords);
                }

                return SetMode(line.Mode, announce: true);

            case ClaudeCodeStream.CompactedNote:
                _lines.Add(Final(LineKind.System, "System: " + CompactedWords));
                Announcement?.Invoke(CompactedWords);
                return true;

            case ClaudeCodeStream.ClearedNote:
                // Claude Code emptied its conversation (/clear) and goes on: the window's is emptied with it, as the
                // screen's model does, and says so with the same line and the same words.
                _lines.Clear();
                _sent.Clear();
                _calls.Clear();
                _questions.Clear();
                ExchangeCount = 0;
                ResetBookmarks();
                QueuedMessages = 0;
                _lines.Add(Final(LineKind.System, "System: " + ClearedLine));
                Announcement?.Invoke(ClearedWords);
                return true;

            default:
                return false;
        }
    }

    /// <summary>The end line of a turn, or of a slash command that Claude Code ran by itself.</summary>
    private void TakeEnd(StreamLine line)
    {
        var command = _commands > 0 && (line.Turns == 0 || _commandTurn);
        if (command)
        {
            _commands--;
        }

        _turn = false;
        _taken = false;
        _responded = false;
        _commandTurn = false;
        _calls.Clear();
        _questions.Clear();
        // Another turn follows while a message of the window's has not come back, or a command's end is still due.
        Working = _sent.Count > 0 || _commands > 0;
        Count();
        if (ClaudeCodeStream.Interrupted(line))
        {
            // The user's interrupt ended the turn: said in a word, and on record where the turn broke off, since no
            // Done follows and the screen's own row for it does not exist here.
            _lines.Add(Final(LineKind.System, "System: " + InterruptedWords));
            Announcement?.Invoke(InterruptedWords);
        }
        else if (!(command && line.Turns == 0))
        {
            // A turn of the model's ended: the window's rule for a bell says Done (SM-4.6). Also after a turn that
            // failed, whose error line stands in the conversation, as with the screen: a turn that said nothing at
            // its end left the user without a word. A command Claude Code ran by itself, which counts no turn,
            // rings none.
            RaiseBell();
        }
    }

    /// <summary>Everything that runs in the background, whole: the words the screen's mode line has for it, and the agents with their types.</summary>
    private bool TakeTasks(IReadOnlyList<StreamTask> tasks)
    {
        var shells = tasks.Count(task => task.Kind == StreamTask.Shell);
        var agents = tasks.Where(task => task.Kind == StreamTask.AgentKind).Select(task => new BackgroundAgent(task.Agent, task.Description)).ToList();
        var others = tasks.Count - shells - agents.Count;
        var parts = new List<string>();
        Counted(parts, shells, ClaudeCodeTexts.CountedShell);
        Counted(parts, agents.Count, ClaudeCodeTexts.CountedAgent);
        Counted(parts, others, "task");
        var words = parts.Count == 0 ? null : string.Join(", ", parts);
        var same = words == Background && agents.SequenceEqual(BackgroundAgents);
        if (!same)
        {
            Background = words;
            BackgroundAgents = agents;
        }

        return !same;
    }

    private static void Counted(List<string> parts, int count, string word)
    {
        if (count > 0)
        {
            parts.Add($"{count} {word}{(count == 1 ? string.Empty : "s")}");
        }
    }

    /// <summary>The permission mode a line names, in the screen's words; a change after the first is said, as the screen says Claude Code's own row for it.</summary>
    private bool SetMode(string mode, bool announce)
    {
        if (ClaudeCodeStream.ModeWords(mode) is not { } words || words == Mode)
        {
            return false;
        }

        var first = Mode is null;
        Mode = words;
        ModeName = mode;
        if (!_modesSeen.Contains(mode))
        {
            _modesSeen.Add(mode);
        }

        if (announce && !first)
        {
            Announcement?.Invoke(words);
        }

        return true;
    }

    /// <summary>Raised once for a turn, when its first reply, thinking or tool line comes.</summary>
    private void Respond()
    {
        if (_responded)
        {
            return;
        }

        _responded = true;
        ResponseStarted?.Invoke();
    }

    private void Place(Sent sent)
    {
        if (sent.Placed)
        {
            return;
        }

        sent.Placed = true;
        foreach (var line in sent.Block)
        {
            line.Committed = true;
        }

        _lines.AddRange(sent.Block);
    }

    private void Count() => QueuedMessages = _sent.Count(sent => !sent.Placed);

    /// <summary>What a call is about, in parentheses after the tool's name as the screen prints it; a file in Claude's folder by its path from there.</summary>
    private string About(string about)
    {
        if (about.Length == 0)
        {
            return string.Empty;
        }

        var shown = _folder.Length > 0 && about.StartsWith(_folder, StringComparison.OrdinalIgnoreCase) ? about[_folder.Length..] : about;
        return $" ({shown})";
    }

    /// <summary>A line that is final as it is added: no row of a screen will rewrite it.</summary>
    private Line Final(LineKind kind, string text)
    {
        var line = NewLine(kind, text);
        line.Committed = true;
        return line;
    }
}

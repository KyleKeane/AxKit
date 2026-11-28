using AxKit.Core.Pty;

namespace AxClaude.Core.Transcript;

/// <summary>
/// One message in flight to Claude Code's screen (docs/axClaude/SPEC.md D33, "One message in flight"): the checks its
/// writes in the window's queue ask right before each goes, with what they remember between them. Pure, so that the
/// very checks the window wires (src/AxClaude/MainForm.Input.cs, <c>MessageChecks</c>) are run by the tests over the
/// recordings, a frame at a time and with the recorded clock (the seventh review of 2026-11-27, L9). The rules are
/// <see cref="QueuedWrite"/>'s: the text typed only onto a bare row (<see cref="First"/>), the rest while it still
/// goes where it was meant to (<see cref="Rest"/>), the Enter once the row shows it (<see cref="Enter"/>), the Enter
/// once more after <see cref="AgainMs"/> (<see cref="Again"/>), and the wait until Claude Code has taken it or the
/// limit has come (<see cref="Taken"/>). Once its Enter went the message is Claude Code's: no check says over any more
/// (the eighth review of 2026-11-28, M-A), and a draft still on the row at the limit is left there
/// (<see cref="LeftOnRow"/>) for the window to say. A check that says over, before the Enter, leaves its reason in
/// <see cref="Why"/>. What the window does with it (the words, the conversation, the field) stays the window's. The
/// model is asked as of the last frame's end (K3).
/// </summary>
public sealed class MessageFlight
{
    /// <summary>How long the text waits for a bare row, its Enter for the row to show it, and its flight for the row to be bare again.</summary>
    public const int WaitMs = 3000;

    /// <summary>How long after its Enter the row may still hold the message before the Enter is sent once more (FR-2.1: Claude Code drops an Enter in its first seconds).</summary>
    public const int AgainMs = 1500;

    private readonly ClaudeModel _model;
    private readonly Func<string?> _waitIdentity;
    private readonly Func<long> _clock;
    private readonly bool _askedThen;
    private readonly string? _waitedFor;
    private readonly bool _own;
    private long _firstAsked = -1;
    private long _enterAsked = -1;
    private long _enteredAt;

    /// <summary>
    /// A message queued now: what Claude Code waits on now is what it is bound to (<see cref="QueuedWrite.MessageGoes"/>).
    /// <paramref name="own"/> for the user's own message from the message field, which the row is compared with
    /// without its hidden characters (M1); any other text, an offered line among them, only with them.
    /// </summary>
    public MessageFlight(ClaudeModel model, string text, Func<string?> waitIdentity, Func<long> clock, bool own = false)
    {
        _model = model;
        Text = text;
        _waitIdentity = waitIdentity;
        _clock = clock;
        _own = own;
        _askedThen = model.PromptPending;
        _waitedFor = waitIdentity();
        Hides = AxKit.Core.DisplayText.HoldsHidden(text);
    }

    public string Text { get; }

    /// <summary>Whether the text holds a hidden character, a line end and a tab aside (<see cref="AxKit.Core.DisplayText.HoldsHidden"/>): it gets no second Enter (K4, M1).</summary>
    public bool Hides { get; }

    /// <summary>Whether its first write went, so that Claude Code's row may hold it.</summary>
    public bool Typed { get; private set; }

    /// <summary>Whether its Enter went: from then on the window counts it as sent, and it is never given back (M-A).</summary>
    public bool Entered { get; private set; }

    /// <summary>Whether its flight is over: Claude Code has taken it, or the limit after its Enter has come.</summary>
    public bool Done { get; private set; }

    /// <summary>Whether the limit after its Enter came while Claude Code's prompt row still showed a draft (any draft), which the window says once (M-A).</summary>
    public bool LeftOnRow { get; private set; }

    /// <summary>Claude Code's own line about that draft, as the frame showed it (<see cref="ClaudeModel.PromptRemark"/>); null when none stood.</summary>
    public string? Remark { get; private set; }

    /// <summary>Why the check that said over said it.</summary>
    public GivenBack Why { get; private set; } = GivenBack.Asked;

    /// <summary>Whether it still goes where it was meant to.</summary>
    public bool Goes() => QueuedWrite.MessageGoes(_askedThen, _waitedFor, _model.PromptPending, _waitIdentity());

    /// <summary>
    /// The message's writes for the window's queue, in order, each with the pause after it and its check: each line,
    /// Claude Code's Ctrl+J between two (a lone newline write: text and a line break in one write is a paste), the
    /// Enter, the Enter again, and an empty write where the queue waits until Claude Code has taken it
    /// (<see cref="AxKit.Core.Pty.WriteQueue"/> writes no empty text). The window queues these and nothing else for a
    /// message, and the tests queue the same.
    /// </summary>
    public IReadOnlyList<(string Text, int DelayAfter, Func<WriteCheck> Check)> Writes()
    {
        var writes = new List<(string, int, Func<WriteCheck>)>();
        var lines = Text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            writes.Add((lines[i], i == lines.Length - 1 ? 100 : 30, i == 0 ? First : Rest));
            if (i < lines.Length - 1)
            {
                writes.Add((ClaudeCodeKeys.NewLine, 30, Rest));
            }
        }

        writes.Add((ClaudeCodeKeys.Enter, 50, Enter));
        writes.Add((ClaudeCodeKeys.Enter, 50, Again));
        writes.Add((string.Empty, 50, Taken));
        return writes;
    }

    /// <summary>Whether the Enter was sent once more, the row still holding the message after <see cref="AgainMs"/>.</summary>
    public bool EnteredAgain { get; private set; }

    /// <summary>Its first write: only onto a bare prompt row (<see cref="QueuedWrite.MessageFirst"/>); a row not drawn by the limit is a reason of its own (M3).</summary>
    public WriteCheck First()
    {
        var now = _clock();
        _firstAsked = _firstAsked < 0 ? now : _firstAsked;
        var draft = _model.PromptDraft;
        var check = QueuedWrite.MessageFirst(_askedThen, _waitedFor, _model.PromptPending, _waitIdentity(), draft, now - _firstAsked, WaitMs);
        Typed |= check == WriteCheck.Goes;
        return check == WriteCheck.Over ? Over(!Goes() ? GivenBack.Asked : draft is null ? GivenBack.NotDrawn : GivenBack.NotBare) : check;
    }

    /// <summary>Its other lines and line breaks: while it still goes where it was meant to.</summary>
    public WriteCheck Rest() => Goes() ? WriteCheck.Goes : Over(GivenBack.Asked);

    /// <summary>Its Enter: once the frame shows it whole on the row (<see cref="QueuedWrite.MessageEnter"/>).</summary>
    public WriteCheck Enter()
    {
        var now = _clock();
        _enterAsked = _enterAsked < 0 ? now : _enterAsked;
        var check = QueuedWrite.MessageEnter(_askedThen, _waitedFor, _model.PromptPending, _waitIdentity(), _model.PromptHolds(Text, hiddenAside: _own), now - _enterAsked, WaitMs);
        if (check == WriteCheck.Goes)
        {
            _enteredAt = now;
            Entered = true;
        }

        return check == WriteCheck.Over ? Over(Goes() ? GivenBack.NotShown : GivenBack.Asked) : check;
    }

    /// <summary>The Enter once more while the row still holds it, never for a text with a hidden character (<see cref="QueuedWrite.EnterAgain"/>).</summary>
    public WriteCheck Again()
    {
        var check = QueuedWrite.EnterAgain(_askedThen, _model.PromptPending, _model.PromptHolds(Text, hiddenAside: _own), Hides, _clock() - _enteredAt, AgainMs);
        EnteredAgain |= check == WriteCheck.Goes;
        return check;
    }

    /// <summary>
    /// Whether Claude Code has taken it (<see cref="QueuedWrite.Taken"/>): not yet while the row is not bare, until the
    /// limit; then skipped, whatever the row shows, and the flight is over. A draft still on the row then is
    /// <see cref="LeftOnRow"/>, with Claude Code's line about it.
    /// </summary>
    public WriteCheck Taken()
    {
        var asks = _model.PromptPending;
        var draft = _model.PromptDraft;
        var check = QueuedWrite.Taken(_askedThen, asks, draft, _clock() - _enteredAt, WaitMs);
        Done = check == WriteCheck.Skip;
        LeftOnRow = Done && !_askedThen && !asks && draft is { Length: > 0 };
        Remark = LeftOnRow ? _model.PromptRemark : null;
        return check;
    }

    private WriteCheck Over(GivenBack why)
    {
        Why = why;
        return WriteCheck.Over;
    }
}

/// <summary>
/// Why messages whose Enter had not gone were given back (D33): Claude Code began to ask while one was typed; its row
/// did not show the message; its row held other text, so nothing was typed; its row was not drawn, so nothing was
/// typed (the eighth review of 2026-11-28, M3); the user interrupted; Claude stopped; a check failed (the seventh
/// review, L2). A message whose Enter went is never given back (the eighth review, M-A).
/// </summary>
public enum GivenBack { Asked, NotShown, NotBare, NotDrawn, Interrupted, Stopped, Failed }

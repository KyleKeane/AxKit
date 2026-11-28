using AxKit.Core.Pty;

namespace AxClaude.Core.Transcript;

/// <summary>
/// One message in flight to Claude Code's screen (docs/axClaude/SPEC.md D33, "One message in flight"): the checks its
/// writes in the window's queue ask right before each goes, with what they remember between them. Pure, so that the
/// very checks the window wires (src/AxClaude/MainForm.Input.cs, <c>MessageChecks</c>) are run by the tests over the
/// recordings, a frame at a time and with the recorded clock (the seventh review of 2026-11-27, L9: until then the
/// tests counted the window's wiring and ran a copy of the rule). The rules are <see cref="QueuedWrite"/>'s:
/// the text typed only onto a bare row (<see cref="First"/>), the rest while it still goes where it was meant to
/// (<see cref="Rest"/>), the Enter once the row shows it (<see cref="Enter"/>), the Enter once more after
/// <see cref="AgainMs"/> (<see cref="Again"/>), and the wait until Claude Code has taken it (<see cref="Taken"/>).
/// A check that says over leaves its reason in <see cref="Why"/>. What the window does with it (the words, the
/// conversation, the field) stays the window's. The model is asked as of the last frame's end (K3).
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
    private readonly bool _hides;
    private long _firstAsked = -1;
    private long _enterAsked = -1;
    private long _enteredAt;

    /// <summary>A message queued now: what Claude Code waits on now is what it is bound to (<see cref="QueuedWrite.MessageGoes"/>).</summary>
    public MessageFlight(ClaudeModel model, string text, Func<string?> waitIdentity, Func<long> clock)
    {
        _model = model;
        Text = text;
        _waitIdentity = waitIdentity;
        _clock = clock;
        _askedThen = model.PromptPending;
        _waitedFor = waitIdentity();
        _hides = AxKit.Core.DisplayText.WithoutHidden(text).Length != text.Length;
    }

    public string Text { get; }

    /// <summary>Whether its first write went, so that Claude Code's row may hold it.</summary>
    public bool Typed { get; private set; }

    /// <summary>Whether its Enter went: from then on the window counts it as sent.</summary>
    public bool Entered { get; private set; }

    /// <summary>Whether Claude Code has taken it: its flight is over.</summary>
    public bool Done { get; private set; }

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

    /// <summary>Its first write: only onto a bare prompt row (<see cref="QueuedWrite.MessageFirst"/>).</summary>
    public WriteCheck First()
    {
        var now = _clock();
        _firstAsked = _firstAsked < 0 ? now : _firstAsked;
        var check = QueuedWrite.MessageFirst(_askedThen, _waitedFor, _model.PromptPending, _waitIdentity(), _model.PromptDraft, now - _firstAsked, WaitMs);
        Typed |= check == WriteCheck.Goes;
        return check == WriteCheck.Over ? Over(Goes() ? GivenBack.NotBare : GivenBack.Asked) : check;
    }

    /// <summary>Its other lines and line breaks: while it still goes where it was meant to.</summary>
    public WriteCheck Rest() => Goes() ? WriteCheck.Goes : Over(GivenBack.Asked);

    /// <summary>Its Enter: once the frame shows it whole on the row (<see cref="QueuedWrite.MessageEnter"/>).</summary>
    public WriteCheck Enter()
    {
        var now = _clock();
        _enterAsked = _enterAsked < 0 ? now : _enterAsked;
        var check = QueuedWrite.MessageEnter(_askedThen, _waitedFor, _model.PromptPending, _waitIdentity(), _model.PromptHolds(Text), now - _enterAsked, WaitMs);
        if (check == WriteCheck.Goes)
        {
            _enteredAt = now;
            Entered = true;
            SeenDraft = _askedThen ? null : _model.PromptDraft;
        }

        return check == WriteCheck.Over ? Over(Goes() ? GivenBack.NotShown : GivenBack.Asked) : check;
    }

    /// <summary>
    /// The prompt row's draft as it was last seen holding the message, paste marks and their numbers as drawn; null
    /// when it was never seen so. The window empties the row of a message given back only while the row is exactly
    /// this (the seventh review of 2026-11-27, L3: a paste mark matches its own number when the window knows it).
    /// </summary>
    public string? SeenDraft { get; private set; }

    /// <summary>The Enter once more while the row still holds it, never for a text with a hidden character (<see cref="QueuedWrite.EnterAgain"/>).</summary>
    public WriteCheck Again()
    {
        var holds = _model.PromptHolds(Text);
        SeenDraft = holds && !_askedThen ? _model.PromptDraft : SeenDraft;
        var check = QueuedWrite.EnterAgain(_askedThen, _model.PromptPending, holds, _hides, _clock() - _enteredAt, AgainMs);
        EnteredAgain |= check == WriteCheck.Goes;
        return check;
    }

    /// <summary>Whether Claude Code has taken it (<see cref="QueuedWrite.Taken"/>): skipped once it has, over when it has not.</summary>
    public WriteCheck Taken()
    {
        var asks = _model.PromptPending;
        var holds = _model.PromptHolds(Text);
        SeenDraft = holds && !asks && !_askedThen ? _model.PromptDraft : SeenDraft;
        var check = QueuedWrite.Taken(_askedThen, asks, asks && _model.PromptHeldBeforeAsk(Text), _model.PromptDraft, holds, _clock() - _enteredAt, WaitMs);
        Done = check == WriteCheck.Skip;
        return check == WriteCheck.Over ? Over(asks ? GivenBack.AskedOverIt : GivenBack.StillShown) : check;
    }

    private WriteCheck Over(GivenBack why)
    {
        Why = why;
        return WriteCheck.Over;
    }
}

/// <summary>
/// Why messages Claude Code had not taken were given back (D33): it began to ask while one was typed; its row did not
/// show the message; its row held other text, so nothing was typed; its row still showed the message at the limit
/// after its Enter (the one time limit that gives back, the seventh review of 2026-11-27, K1); it began to ask while
/// its row still showed the message (K2); the user interrupted; Claude stopped; a check failed (L2).
/// </summary>
public enum GivenBack { Asked, NotShown, NotBare, StillShown, AskedOverIt, Interrupted, Stopped, Failed }

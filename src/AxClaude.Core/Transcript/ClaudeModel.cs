using AxKit.Core.Transcript;

namespace AxClaude.Core.Transcript;

/// <summary>
/// What axClaude's window reads of its source, whatever the source is (docs/axClaude/stream-mode.md SM-3): the
/// conversation's lines on the bundle's <see cref="LineStore"/>, and the facts and events below, by the names the
/// window has always read them by. A model fills it: <see cref="SessionModel"/>, which reads Claude Code's drawn
/// screen, and later the stream's model, which reads Claude Code's non-interactive stream. The window knows this
/// class alone, so that it does not know which source feeds it.
///
/// What every source has is abstract. What only a drawn screen has is virtual and answers with nothing here (null,
/// false, zero, no effect), so that a model without a screen says nothing of it and the window's code for it is
/// never entered; what the window sets of that kind is not kept here. A fact's meaning for the screen is written
/// where <see cref="SessionModel"/> fills it.
///
/// Not here, on purpose: what only the machinery beside the screen asks (the turn marks, the screen's work, the
/// epoch, the turn the event core hands back). That machinery is the screen source's own and asks
/// <see cref="SessionModel"/> itself (SM-3.6).
/// </summary>
public abstract class ClaudeModel : LineStore
{
    /// <param name="columns">The console's size for a model with a screen; 0 for one without.</param>
    protected ClaudeModel(int columns = 0, int rows = 0)
        : base(columns, rows)
    {
    }

    // ---- What Claude waits on ----

    /// <summary>Claude waits for an answer of the user's.</summary>
    public abstract bool PromptPending { get; protected set; }

    /// <summary>The question Claude waits on, with its answers; null while it waits on none that can be shown as one.</summary>
    public abstract Question? PendingQuestion { get; protected set; }

    /// <summary>Claude waits for the user's own words on a row of its screen; never without a screen.</summary>
    public virtual bool AwaitsText => false;

    /// <summary>The row of the screen that asks for the user's own words; null without one.</summary>
    public virtual string? TextPrompt { get; protected set; }

    /// <summary>A screen of Claude Code's own that waits for keys named on its hint row; null without one.</summary>
    public virtual ClaudeScreen? PendingScreen { get; protected set; }

    /// <summary>Whether Claude Code's prompt row still holds this text, sent a moment ago; never without a screen.</summary>
    public virtual bool PromptHolds(string text) => false;

    /// <summary>The feedback drafts Claude Code's mode line counts; zero without a screen.</summary>
    public virtual int FeedbackDrafts { get; protected set; }

    /// <summary>The title of the feedback draft whose card was drawn last; null without a screen.</summary>
    public virtual string? DraftTitle { get; protected set; }

    /// <summary>Look for Claude Code's screens that wait on a hint row, which the window sets; no meaning without a screen.</summary>
    public virtual bool DetectScreens
    {
        get => false;
        set { }
    }

    // ---- The turn ----

    /// <summary>Claude works on a turn, as the source itself shows it.</summary>
    public abstract bool Working { get; protected set; }

    /// <summary>The messages sent while Claude worked that it has not taken up yet.</summary>
    public abstract int QueuedMessages { get; protected set; }

    /// <summary>The permission mode in force, in Claude Code's own words; null until the source has named one.</summary>
    public abstract string? Mode { get; protected set; }

    /// <summary>What a spinner row says beside "working"; null without a screen.</summary>
    public virtual string? Spinner { get; protected set; }

    /// <summary>The session's name; null where the source gives none.</summary>
    public virtual string? SessionName { get; protected set; }

    /// <summary>The name Claude was started with, which the window sets; only a screen draws it.</summary>
    public virtual string? StartName
    {
        get => null;
        set { }
    }

    /// <summary>Claude Code's detailed view has taken the prompt's place; never without a screen.</summary>
    public virtual bool TranscriptViewOpen { get; protected set; }

    // ---- Work in the background ----

    /// <summary>The background work in words, "1 shell" or "1 shell, 2 agents"; null when none runs.</summary>
    public abstract string? Background { get; protected set; }

    /// <summary>The agents that run in the background, each with its type and description; empty when none runs.</summary>
    public abstract IReadOnlyList<BackgroundAgent> BackgroundAgents { get; protected set; }

    /// <summary>The dynamic workflows that run in the background; empty when none runs.</summary>
    public abstract IReadOnlyList<BackgroundWorkflow> BackgroundWorkflows { get; protected set; }

    /// <summary>The number of a screen's row that waits for agents; zero without a screen.</summary>
    public virtual int BackgroundAgentsAwaited { get; protected set; }

    /// <summary>The number of a screen's row that waits for workflows; zero without a screen.</summary>
    public virtual int BackgroundWorkflowsAwaited { get; protected set; }

    /// <summary>Background work of a kind not known, read off a screen's rows; zero without a screen.</summary>
    public virtual int BackgroundUnknown { get; protected set; }

    /// <summary>Show Claude Code's live progress rows in the conversation, which the window sets; only a screen has such rows.</summary>
    public virtual bool ShowProgressRows
    {
        get => false;
        set { }
    }

    // ---- The conversation ----

    /// <summary>
    /// The block of a message the user sent, put into the conversation now or held until Claude takes the message
    /// up; false for a text that gets no block.
    /// </summary>
    public abstract bool Send(string text, bool evenWhilePrompting = false);

    /// <summary>Marks the question Claude waits on in the conversation, so that it can be found again.</summary>
    public abstract void MarkQuestion();

    /// <summary>Empties the conversation and everything known of the run.</summary>
    public abstract void Clear();

    /// <summary>
    /// A new run of Claude begins in the same window and the conversation stays (Restart Claude): what was known of
    /// the old run is forgotten, and a message the old run never took up is put on record. The name is the screen's,
    /// kept so that the window calls what it always called.
    /// </summary>
    public abstract void ResetScreen();

    /// <summary>
    /// While set, what Claude Code draws is a conversation the window already shows and stays hidden. Only a screen
    /// draws a conversation again: without one this is never set, whatever the window asks.
    /// </summary>
    public virtual bool HideReplay
    {
        get => false;
        set { }
    }

    /// <summary>Shows what <see cref="HideReplay"/> hid after all; nothing without a screen.</summary>
    public virtual void ShowHiddenLines()
    {
    }

    /// <summary>Gives the conversation a screen drew again its markers; nothing without a screen.</summary>
    public virtual void MarkReplayedExchanges()
    {
    }

    /// <summary>Whether Claude Code just said on its screen that there is no conversation to continue; never without a screen.</summary>
    public virtual bool SaidNoConversationToContinue() => false;

    // ---- What happens ----

    /// <summary>Claude began its reply to the message sent last.</summary>
    public abstract event Action? ResponseStarted;

    /// <summary>Claude Code said something of its own, with its text: a change of mode, the conversation emptied.</summary>
    public abstract event Action<string>? Announcement;
}

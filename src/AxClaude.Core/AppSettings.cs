using AxClaude.Core.Transcript;
using AxKit.Core;

namespace AxClaude.Core;

/// <summary>
/// Everything the app remembers between runs. Stored as JSON in <c>%APPDATA%\AxKit\axClaude.json</c> (see SPEC.md
/// Appendix C) through the bundle's <see cref="SettingsFile"/>. A missing or unreadable file falls
/// back to the defaults. The console fields (the size, the recent folders, the sounds, the signals, the transcript
/// options) are <see cref="ConsoleSettings"/>', shared with axTerminal; the font, the update check and the window's
/// place are <see cref="WindowSettings"/>' (bundle AX-6.5).
/// </summary>
public sealed class AppSettings : ConsoleSettings
{
    /// <summary>
    /// The defaults of a new install follow the maintainer's own options (his word of 2026-11-21: "we must speak
    /// claude results on by default ... use my current options as defaults"): every line of a reply spoken
    /// (<see cref="ReplySpeech"/>), a question waiting after what is read (<see cref="InterruptForQuestions"/> off),
    /// and, of the console's shared settings, the working music and the louder alert in the background on, which
    /// axTerminal leaves off. A file that exists keeps what it holds.
    /// </summary>
    public AppSettings()
    {
        WorkingMusic = true;
        BackgroundAlert = true;
    }

    public string? LastProjectFolder { get; set; }
    public string? ClaudePath { get; set; }

    public bool AnnounceBell { get; set; } = true;

    /// <summary>How much of a reply is spoken as it arrives (FR-7.4): none, the first line of each message, or all.</summary>
    public ReplySpeechMode ReplySpeech { get; set; } = ReplySpeechMode.All;

    /// <summary>Speak the <c>tool:</c> row of each tool call as it arrives, whatever <see cref="ReplySpeech"/> says (FR-7.4).</summary>
    public bool SpeakToolCalls { get; set; }

    /// <summary>The words that keep a reply line silent (FR-7.4a): a line holding one is shown and not spoken.</summary>
    public List<string> SilentWords { get; set; } = [];

    /// <summary>Say "Ready" when Claude is ready after a start (FR-7.3a; the chime is the sounds' switch).</summary>
    public bool AnnounceReady { get; set; } = true;

    /// <summary>Say "Sent" or "Queued" after a message goes (FR-7.3a).</summary>
    public bool AnnounceSent { get; set; } = true;

    /// <summary>Say "Responding" when Claude's reply starts (FR-7.3a).</summary>
    public bool AnnounceResponding { get; set; } = true;

    /// <summary>Say "Claude goes on by itself" when a turn begins that the user did not send (FR-7.3b): work in the background ended and Claude Code took it up.</summary>
    public bool AnnounceSelfStarted { get; set; } = true;

    /// <summary>Say "Question closed" when Claude closes what it asked by itself (FR-7.3a).</summary>
    public bool AnnounceQuestionClosed { get; set; } = true;

    /// <summary>Speak Claude's own notices, the bracketed rows such as "[accept edits on]", and "Conversation cleared" (FR-7.3a).</summary>
    public bool AnnounceClaudeNotices { get; set; } = true;

    /// <summary>Claude's feedback surveys are dismissed by the app, unheard (D33, 2026-11-12): its Dismiss, or No to the transcript question.</summary>
    public bool DismissSurveys { get; set; }

    /// <summary>A feedback draft Claude queued opens its review by itself once Claude is idle (D33). Off: it waits for /feedback.</summary>
    public bool OpenFeedbackDrafts { get; set; } = true;

    /// <summary>A short sound when work in the background starts and when it ends (FR-7.10): rising for a start, falling for an end.</summary>
    public bool SoundOnBackgroundWork { get; set; } = true;

    /// <summary>
    /// The recurring sound while work runs in the background (FR-7.10): slow soft chords, a tone more for each thing
    /// running, with a pause of <see cref="BackgroundChordPauseSeconds"/> between them. The name is from the first
    /// recurring sound and stays, so that a file keeps the user's choice.
    /// </summary>
    public bool BackgroundPulse { get; set; } = true;

    /// <summary>
    /// The pause between two chords: 2, 5, 10 or 20 seconds (<see cref="BackgroundChords.PauseSeconds"/>). The
    /// setting before it, <c>backgroundPulseSeconds</c> (how often the first recurring sound came), is read past
    /// without error and not carried over: every file starts with 2.
    /// </summary>
    public int BackgroundChordPauseSeconds { get; set; } = BackgroundChords.DefaultPauseSeconds;

    /// <summary>Say background work as it starts and ends (FR-7.10): off, since the sounds say it.</summary>
    public bool SayBackgroundWork { get; set; }

    /// <summary>
    /// Show Claude Code's live progress rows in the conversation (FR-7.10; the user's decision of 2026-11-15, "an
    /// option, hidden at first"): the rows of running agents and workflows and the waiting row, never spoken either way.
    /// </summary>
    public bool ShowProgressRows { get; set; }

    /// <summary>
    /// Recognise what Claude waits on and show it in the control area in the message field's place, and keep
    /// messages out of a waiting question (D33, FR-2.10). Off: questions are only announced, as before 1.4.0.
    /// </summary>
    public bool QuestionNotices { get; set; } = true;

    /// <summary>
    /// A question Claude asks takes the focus wherever it is and cuts off what NVDA was reading, a reply included
    /// (D33). Off: the focus moves only from the bottom of the window, and the question waits after what is read.
    /// </summary>
    public bool InterruptForQuestions { get; set; }

    /// <summary>
    /// Show Claude's "Review your answers" after the last of its questions (D33). Off: the app submits the review for
    /// the user, unless it warns that a question has no answer.
    /// </summary>
    public bool ReviewAnswers { get; set; }

    /// <summary>
    /// axClaude's recordings and exported conversations (<c>axclaude-…</c>) are listed in the project repository's local
    /// exclude file, so git never offers them for a commit (D34). Off removes the lines.
    /// </summary>
    public bool KeepFilesOutOfGit { get; set; } = true;

    /// <summary>
    /// Events from Claude Code's hooks (docs/axClaude/events-and-grounding.md EG-9.1; Options, Developer): on, the
    /// settings file with the hooks is passed at a start of Claude Code and the window serves their pipe. Off: neither,
    /// from the next start, and the window works from the marks, the session file and the screen. Off at first (EG-D-9 as amended on 2026-11-15): passing hooks changes how the user's Claude Code runs, so it is his to turn on.
    /// </summary>
    public bool ClaudeHooks { get; set; } = false;

    /// <summary>
    /// Ground the conversation in Claude Code's session file (EG-9.2; Options, Developer): on, the window reads the
    /// file Claude Code keeps for the session, in memory only. Off: no record, and nothing is bound, pinned or moved.
    /// </summary>
    public bool GroundInSessionFile { get; set; } = true;

    /// <summary>The word of <see cref="EventCore"/> that puts every family of the core's decisions back in shadow.</summary>
    public const string EventCoreShadow = "shadow";

    /// <summary>The word of <see cref="EventCore"/> at first: the core is in charge of what the build puts it in charge of.</summary>
    public const string EventCoreOn = "on";

    /// <summary>
    /// The way back without a rebuild (EG-9.6, EG-4.11), set in the file alone: <c>on</c>, the core is in charge of
    /// the families of decisions the build gives it; <c>shadow</c>, the window's own code acts for all of them and the
    /// core only computes beside it. Any other word is read as <c>on</c>.
    /// </summary>
    public string EventCore { get; set; } = EventCoreOn;

    /// <summary>The words of <see cref="ClaudeSource"/>: the screen, which is the source at first, and the stream.</summary>
    public const string SourceScreen = "screen";
    public const string SourceStream = "stream";

    /// <summary>
    /// What feeds the window (docs/axClaude/stream-mode.md SM-9.1): <c>screen</c>, Claude Code in a pseudo console in
    /// screen reader mode, read row by row, as always; <c>stream</c>, Claude Code as one process over pipes, read line
    /// by line. Any other word is read as <c>screen</c>. A window is made for its source and keeps it (SM-9.2), so a
    /// change applies to the next window of axClaude. The command line's <c>--source</c> goes before it for one start.
    /// </summary>
    public string ClaudeSource { get; set; } = SourceScreen;

    /// <summary>Whether a word names the stream as the source: that word alone, in any case; everything else is the screen.</summary>
    public static bool IsStream(string? source) => string.Equals(source?.Trim(), SourceStream, StringComparison.OrdinalIgnoreCase);

    /// <summary>How many folders <see cref="StreamSessions"/> keeps, the last used first.</summary>
    public const int KeptStreamSessions = 30;

    /// <summary>
    /// For each folder, the session a window the stream feeds last ran there (docs/axClaude/stream-mode.md SM-8.3),
    /// the last used first: the interactive program's <c>--continue</c> passes over a session the stream began, so a
    /// window the screen feeds takes it up by this id (<c>Streaming/SessionHandover.ForScreen</c>). An id: never shown,
    /// said or logged.
    /// </summary>
    public List<KeptSession> StreamSessions { get; set; } = [];

    /// <summary>The session a window the stream feeds last ran in the folder; null when none is kept.</summary>
    public string? StreamSessionOf(string folder) =>
        StreamSessions.FirstOrDefault(kept => SameFolder(kept.Folder, folder))?.Session;

    /// <summary>
    /// Keeps the session a window the stream feeds runs in the folder, first in the list, over what
    /// <paramref name="saved"/> holds (the list as the settings file has it now, which another window may have
    /// changed since this one read it: the file's members are merged whole, R-62). False, and nothing changed, for
    /// an id that is no session's and for the session already kept first for the folder.
    /// </summary>
    public bool KeepStreamSession(string folder, string session, IReadOnlyList<KeptSession>? saved = null)
    {
        if (!Streaming.SessionHandover.IsSessionId(session) || string.IsNullOrWhiteSpace(folder))
        {
            return false;
        }

        List<KeptSession> kept = [new KeptSession(folder, session), .. (saved ?? StreamSessions).Where(other => other is not null && !SameFolder(other.Folder, folder))];
        kept = kept.Take(KeptStreamSessions).ToList();
        if (kept.SequenceEqual(StreamSessions))
        {
            return false;
        }

        StreamSessions = kept;
        return true;
    }

    private static bool SameFolder(string? one, string? other) =>
        string.Equals(one?.TrimEnd('\\', '/'), other?.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

    public static string DefaultPath => AppPaths.SettingsFile("axClaude");

    /// <summary>Loads the file, or returns defaults. <paramref name="error"/> describes an unreadable file.</summary>
    public static AppSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<AppSettings>(path, out error);
        if (!SettingsSeal.Checks(path, settings.Seal, settings.SealedText(), new AppSettings().SealedText()))
        {
            settings.ClaudePath = null;
            error = SettingsSeal.Refused(error, ClaudePathNotUsed);
        }

        settings.Sanitize();
        return settings;
    }

    /// <summary>The seal over <see cref="ClaudePath"/> (bundle AX-5.6, Appendix C): written at every save, checked at every load.</summary>
    public string Seal { get; set; } = string.Empty;

    /// <summary>What the load's error text says when the seal does not check (Appendix C).</summary>
    public const string ClaudePathNotUsed = "The place of Claude Code in axClaude's settings, claudePath, was changed outside axKit and is not used: axClaude looks for Claude Code in the usual places. If it is not found, choose it again with Locate claude.exe in the notice that says so.";

    /// <summary>The sealed field: the program started as Claude Code.</summary>
    private string SealedText() => SettingsSeal.Text([ClaudePath]);

    /// <summary>Writes the file atomically: a temporary file next to it is moved into place; with its seal.</summary>
    public override void Save(string path)
    {
        Seal = SettingsSeal.Of(path, SealedText());
        // R-62: what another window saved meanwhile is kept, from a file whose seal checks, and sealed anew.
        if (SettingsFile.Save(path, this,
            file => SettingsSeal.Checks(path, file.Seal, file.SealedText(), new AppSettings().SealedText()),
            merged => merged.Seal = SettingsSeal.Of(path, merged.SealedText())))
        {
            SettingsSeal.Remember(path);
        }
    }

    /// <summary>The folder Claude starts in: the last one, and the front of the recent list (FR-8.1).</summary>
    public void RememberFolder(string folder)
    {
        LastProjectFolder = folder;
        RememberRecentFolder(folder);
    }

    private void Sanitize()
    {
        SanitizeConsole();
        // A setting this app itself retired is not one a newer version wrote: it goes with the next save and is not
        // kept with the members this version does not know (bundle AX-5.8).
        OtherMembers?.Remove("backgroundPulseSeconds");
        BackgroundChordPauseSeconds = BackgroundChords.SanitizePauseSeconds(BackgroundChordPauseSeconds);
        SilentWords = SpeechFilter.Words(SpeechFilter.Text(SilentWords ?? []));
        EventCore = string.Equals(EventCore?.Trim(), EventCoreShadow, StringComparison.OrdinalIgnoreCase) ? EventCoreShadow : EventCoreOn;
        ClaudeSource = IsStream(ClaudeSource) ? SourceStream : SourceScreen;
        // A kept session is put on a command line: only a folder and a session's id, each once, as many as are kept.
        StreamSessions = (StreamSessions ?? [])
            .Where(kept => kept is not null && !string.IsNullOrWhiteSpace(kept.Folder) && Streaming.SessionHandover.IsSessionId(kept.Session))
            .DistinctBy(kept => kept.Folder.TrimEnd('\\', '/'), StringComparer.OrdinalIgnoreCase)
            .Take(KeptStreamSessions)
            .ToList();
    }
}

/// <summary>A folder and the session a window the stream feeds last ran there (<see cref="AppSettings.StreamSessions"/>).</summary>
public sealed record KeptSession(string Folder, string Session);

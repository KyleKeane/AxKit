using System.ComponentModel;
using System.IO.Pipes;
using System.Text;
using AxClaude.Core;
using AxClaude.Core.Hooks;
using AxClaude.Core.Pty;
using AxClaude.Core.Streaming;
using AxKit.Core;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the stream source (docs/axClaude/stream-mode.md SM-4, SM-9) ----
    //
    // A window made for Claude Code's stream (SM-9.2) runs Claude Code as one process over pipes: no pseudo console,
    // no screen, no frames. Each line Claude Code writes is read down in Claude Code's one place for the stream
    // (ClaudeCodeStream) and handed to the stream's model, which the window reads as it reads the screen's: through
    // _model. This file is the stream source's own, as the machinery beside the screen is the screen's: it alone asks
    // _streamModel; the window's own code asks Streams and calls what is here. In a window the screen feeds, nothing
    // here does anything.

    /// <summary>An end of the stream's process this soon after its start is a start that failed (SM-9.3): the notice offers the screen.</summary>
    private const int StreamEarlyEndMs = 5000;

    /// <summary>How long a Claude Code whose input was closed gets to end by itself before it is ended, as the pseudo console's two seconds.</summary>
    private const int StreamEndGraceMs = 2000;

    /// <summary>As many of the last lines Claude Code wrote as errors as are shown when it ends by itself.</summary>
    private const int StreamErrorLines = 5;

    /// <summary>The way back (SM-9.3), said under what went wrong.</summary>
    private const string ScreenOfferWords = "You can open this folder with the screen instead, as axClaude has always worked: axClaude starts again on the same folder, and this window closes.";

    /// <summary>The Claude Code that runs over its stream; null when none does, and always in a window the screen feeds.</summary>
    private StreamHost? _stream;

    /// <summary>When the stream's process was started (Environment.TickCount64).</summary>
    private long _streamStartedAt;

    /// <summary>The requests written to Claude Code in the life of the window (ClaudeCodeStream's control lines): the number names a request in its answer.</summary>
    private int _streamRequest;

    /// <summary>The lines read in this run, and those among them of no kind the reader knows: for Say the event sources and the diagnostics.</summary>
    private int _streamLines;
    private int _streamUnknown;

    /// <summary>The folders the user said yes to in this window (SM-7.1): Claude Code's stream is started in no other.</summary>
    private readonly HashSet<string> _streamTrusted = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The pipe on which the relay passes Claude Code's questions to this window, for the Claude Code that runs; null when none runs.</summary>
    private AskChannel? _askChannel;

    /// <summary>The question of Claude Code's that the window asks the user now, with the request on the pipe it answers; null when none waits.</summary>
    private StreamAsking? _asking;
    private AskRequest? _askedRequest;

    /// <summary>Questions that came while another was asked, in their order.</summary>
    private readonly Queue<AskRequest> _askedWaiting = new();

    /// <summary>How many questions Claude Code asked in this run: for Say the event sources and the diagnostics.</summary>
    private int _streamAsked;

    /// <summary>The window is closing for good (set around the console window's closing, MainForm.PutAway.cs): a stream's Claude Code is then ended at once.</summary>
    private bool _windowCloses;

    /// <summary>Claude Code's stream feeds this window (SM-9.2): the window was made for it and keeps it.</summary>
    private bool Streams => _streamModel is not null;

    /// <summary>Claude runs: in its pseudo console, or over its stream (SM-3.5). Everything that asks whether Claude runs asks here.</summary>
    protected override bool ProcessRunning => _stream is not null || base.ProcessRunning;

    /// <summary>Called once from the constructor: what the stream's model raises beyond the base class. Nothing in a window the screen feeds.</summary>
    private void WireStream()
    {
        if (_streamModel is not { } model)
        {
            return;
        }

        // FR-7.3b: a turn the user did not send, said as the core says it for the screen: one short line by its
        // switch, never interrupting, no sound.
        model.TurnSelfStarted += () =>
        {
            Log.Info("Stream: a turn Claude Code began by itself");
            if (_settings.AnnounceSelfStarted)
            {
                Announce(SelfStartedWords, false);
            }
        };
    }

    /// <summary>
    /// Starts Claude Code over its stream in the project folder (SM-4.1, SM-4.2): the file that names axKit's relay
    /// for Claude Code's questions (SM-7.3), the line from the stream's one place, the process over its pipes, and the
    /// first request, whose answer names the permission mode and so lets the window say Ready (SM-4.10). Where it
    /// cannot be started, the reason is a line of the conversation and a notice offers the screen (SM-9.3). The
    /// folder, the program and the user's consent were checked by <see cref="StartClaude"/>.
    /// </summary>
    private void StartStream(string claude)
    {
        if (_streamModel is not { } model || _folder is not { } folder)
        {
            return;
        }

        // The relay is this program file, named in a file Claude Code reads. In the Store's copy it is not known that
        // Claude Code can read a file under the package's folders (the hooks' file, EG-2.11), and without the relay
        // nothing could be asked (SM-7.2): no start.
        if (PackageIdentity.IsPackaged)
        {
            StreamNotStarted("Claude Code was not started over its stream: the copy of axKit from the Microsoft Store cannot do that yet.");
            return;
        }

        if (PackageIdentity.ProgramPath is not { } program)
        {
            StreamNotStarted("Claude Code was not started over its stream: axKit could not tell where its own program file is.");
            return;
        }

        // SM-7.1: Claude Code's own window asks before it works in a folder it has not seen; its stream does not. So
        // the window asks, with no as the default, once for a folder in the life of this window.
        if (!_streamTrusted.Contains(folder))
        {
            AskStreamTrust(folder);
            return;
        }

        try
        {
            if (AskRelay.WriteConfig(AppPaths.ClaudeAskFile, program))
            {
                Log.Info("Stream: wrote the file that names axKit as the program Claude Code asks its questions of");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StreamNotStarted("Claude Code was not started over its stream: the file that lets it ask you its questions could not be written. " + ex.Message);
            return;
        }

        var line = ClaudeLauncher.BuildCommandLine(claude, ClaudeCodeStream.StartArguments(AppPaths.ClaudeAskFile, AskRelay.ToolName, _options.Model, ClaudeArgs));
        if (line.Refusal is { } refusal)
        {
            // FR-1.2: npm's claude.cmd runs through cmd.exe, and an argument cmd.exe would act on is refused.
            StreamNotStarted("Claude Code was not started. " + refusal);
            return;
        }

        // SM-7.3: the pipe on which the relay passes Claude Code's questions to this window, new for each start; its
        // name and secret reach the relay through Claude Code's environment and are written nowhere. Without it
        // nothing could be asked, and nothing runs without the user's word (SM-7.2): no start.
        if (AskChannel.Start(this, StreamAsked, StreamQuestionsLost) is not { } questions)
        {
            StreamNotStarted("Claude Code was not started over its stream: the pipe for its questions could not be opened.");
            return;
        }

        // What the reader's thread and the end hand the window: one post for a burst, in the order they came, the
        // end last. A line is read down on the reader's thread, so that nothing of its text reaches this one.
        var inbox = new List<(StreamLine? Line, int ExitCode)>();
        var posted = false;
        StreamHost? host = null;
        try
        {
            host = StreamHost.Start(new StreamHostOptions(line, folder, ClaudeLauncher.BuildEnvironment(askPipe: questions.VariableValue)), text => Hand((ReadStreamLine(text), 0)), code => Hand((null, code)));
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or IOException)
        {
            questions.Stop();
            Log.Error("Claude could not be started over its stream", ex);
            StreamNotStarted("Claude could not be started: " + ex.Message);
            return;
        }

        Log.Info($"Started {line.CommandLine} (pid {host.ProcessId}) in {folder}, over its stream");
        _launchedFolder = folder;
        KeepFilesOutOfGit();
        model.WorksIn(folder);
        _stream = host;
        _askChannel = questions;
        _streamStartedAt = Environment.TickCount64;
        _streamLines = 0;
        _streamUnknown = 0;
        _streamAsked = 0;
        // FR-9.7: the AI tools are given to Claude Code in its pseudo console alone so far.
        if (_options.AiTools)
        {
            Log.Info("Stream: axKit's AI tools are not given to Claude Code over its stream");
        }

        host.Write(ClaudeCodeStream.InitializeLine(NextStreamRequest()));
        ClaudeStarted();
        return;

        void Hand((StreamLine? Line, int ExitCode) item)
        {
            lock (inbox)
            {
                inbox.Add(item);
                if (posted)
                {
                    return;
                }

                posted = true;
            }

            if (!Post(Deliver))
            {
                lock (inbox)
                {
                    posted = false;
                }
            }
        }

        void Deliver()
        {
            (StreamLine? Line, int ExitCode)[] batch;
            lock (inbox)
            {
                batch = [.. inbox];
                inbox.Clear();
                posted = false;
            }

            foreach (var (read, code) in batch)
            {
                // A Claude Code the window has let go of (a stop, a restart): what it still hands over is the old run's.
                if (_stream is not { } running || !ReferenceEquals(host, running))
                {
                    return;
                }

                if (read is null)
                {
                    StreamEnded(running, code);
                    return;
                }

                TakeStreamLine(model, read);
            }
        }
    }

    /// <summary>
    /// One line read down, on the reader's thread. The reader passes over what it does not understand; whatever it
    /// still threw would end the window with the conversation from that thread, so here it is one more line that is
    /// not understood, counted with the others.
    /// </summary>
    private static StreamLine ReadStreamLine(string text)
    {
        try
        {
            return ClaudeCodeStream.Read(text);
        }
        catch (Exception)
        {
            return StreamLine.Nothing;
        }
    }

    /// <summary>The name of the next request written to Claude Code, which its answer carries.</summary>
    private string NextStreamRequest() => "axclaude-" + (++_streamRequest).ToString(System.Globalization.CultureInfo.InvariantCulture);

    // ---- what the window sends over the stream (SM-5): one method for each of the window's ways to write ----

    /// <summary>What a command says that has no meaning without Claude Code's screen (SM-5.7): one short sentence, the same everywhere.</summary>
    private const string NotWithStreamWords = "Not with the stream source";

    private void SayNotWithStream() => Announce(NotWithStreamWords, true);

    /// <summary>A message or a slash command (SM-5.1, SM-5.5): one line, written whole; its block is the model's, made by the caller's <c>Send</c>.</summary>
    private void StreamMessage(string text) => _stream?.Write(ClaudeCodeStream.UserLine(text));

    /// <summary>
    /// The interrupt (SM-5.3): while a turn runs, the request that ends it and leaves Claude Code running; the model
    /// says Interrupted when the turn's end comes. With no turn running nothing is sent, and the window says so.
    /// </summary>
    private void StreamInterrupt()
    {
        if (_stream is not { } host)
        {
            Announce("Claude stopped. Ctrl+Shift+R starts it", true);
            return;
        }

        if (_asking is { } asking)
        {
            // On a waiting question the interrupt is no to the whole of it; the turn goes on, and an interrupt
            // after that ends it.
            UserActedNow();
            StreamAnswered(asking, StreamAsking.Refusal());
            Announce("Cancelled", false);
            return;
        }

        if (!_model.Working)
        {
            Announce("Nothing to interrupt", true);
            return;
        }

        host.Write(ClaudeCodeStream.InterruptLine(NextStreamRequest()));
    }

    /// <summary>
    /// The next permission mode (SM-5.4): the request that sets the mode after the one in force, in Claude Code's own
    /// round (<see cref="ClaudeCodeStream.NextMode"/>). Claude Code answers with the new mode, which the model
    /// announces in the screen's words, as the screen's own row is announced.
    /// </summary>
    private void StreamNextMode()
    {
        if (_stream is not { } host || _streamModel is not { } model)
        {
            Announce("Claude stopped. Ctrl+Shift+R starts it", true);
            return;
        }

        if (model.ModeName.Length == 0)
        {
            Announce("Claude Code has not named its mode yet", true);
            return;
        }

        host.Write(ClaudeCodeStream.ModeLine(NextStreamRequest(), ClaudeCodeStream.NextMode(model.ModeName, model.ModesSeen)));
    }

    /// <summary>One line of the stream, on the window's thread: counted, and the model's. The version is noted when the model first knows it (FR-13.2).</summary>
    private void TakeStreamLine(StreamModel model, StreamLine line)
    {
        _streamLines++;
        if (line.Parts.Count == 0 || line.Has(StreamKind.Unknown))
        {
            _streamUnknown++;
        }

        model.Take(line);
        if (_claudeVersion is null && model.Version.Length > 0)
        {
            _claudeVersion = model.Version;
            Log.Info($"Claude Code version: {_claudeVersion} (named over its stream)");
        }
    }

    /// <summary>
    /// Lets go of the Claude Code that runs over its stream, when one does: ended at once with what it started, or
    /// told to end by the end of its input and ended two seconds later if it has not. Never waits. What it still
    /// hands over is dropped (<see cref="StartStream"/>). Called by <see cref="StopProcess"/> alone, which every end passes.
    /// </summary>
    private void StopStream(bool terminateAtOnce)
    {
        if (_stream is not { } host)
        {
            return;
        }

        _stream = null;
        // The questions' pipe ends with its Claude Code: a question that waited gets no answer, which the relay
        // turns into a no, and the window no longer shows it.
        _askChannel?.Stop();
        _askChannel = null;
        _asking = null;
        _askedRequest = null;
        _askedWaiting.Clear();
        _streamModel?.Answered();
        // At once also when the window closes: the pool thread that would end a Claude Code that runs on dies with
        // this process, and Claude Code would go on without a window.
        if (terminateAtOnce || _windowCloses)
        {
            host.Dispose();
        }
        else
        {
            host.End(StreamEndGraceMs);
        }
    }

    // ---- the folder's trust and Claude Code's questions (SM-7) ----

    /// <summary>
    /// The folder's trust (SM-7.1): a notice before Claude Code's stream is first started in a folder in this window.
    /// Enter and Escape are no; the yes is its own button, and starts Claude Code.
    /// </summary>
    private void AskStreamTrust(string folder) =>
        ShowNotice(Notice.Ask("Start Claude Code in this folder?",
            "Claude Code's own window asks before it works in a folder it has not seen. Over its stream it does not ask, so axClaude asks for it. "
            + "A folder's own settings can name programs that Claude Code then runs by itself. "
            + $"Start Claude Code in {PathName.Of(folder)} only if you trust what is in that folder.\n{folder}",
            "&Start Claude Code", () =>
            {
                _streamTrusted.Add(folder);
                StartClaude();
            },
            "&Do not start", () =>
            {
                _model.AddSystemLine("Claude Code was not started in this folder. Press Ctrl+Shift+R to be asked again.");
                UpdateStatus();
            },
            cancelIsDefault: true) with { Unprompted = true });

    /// <summary>
    /// A question of Claude Code's came over the relay's pipe (SM-7.3), on the window's thread: read down in Claude
    /// Code's one place, made a question as the window asks one (SM-7.4, SM-7.5), and handed to the model, from
    /// where the control area shows it by the window's own code (SM-4.7). One at a time: another waits its turn.
    /// A question that cannot be read is answered no. Nothing of a question is logged.
    /// </summary>
    private void StreamAsked(AskRequest request)
    {
        // A pipe the window has stopped: its thread is ending, and the relay answers no.
        if (!ReferenceEquals(request.Source, _askChannel) || _streamModel is not { } model)
        {
            return;
        }

        if (_asking is not null)
        {
            _askedWaiting.Enqueue(request);
            return;
        }

        _streamAsked++;
        if (ClaudeCodeStream.ReadAsk(request.Question) is not { } ask)
        {
            Log.Info("Stream: a question of Claude Code's could not be read and was answered no");
            request.Answer(StreamAsking.Refusal());
            return;
        }

        _asking = new StreamAsking(ask, _folder);
        _askedRequest = request;
        Log.Info($"Stream: Claude Code asks {(ask.IsQuestion ? "a question of Claude's own" : "about a tool call")}");
        model.Ask(_asking.Current);
    }

    /// <summary>The user's answer to what Claude Code asks (SM-5.2): the key of an answer, several keys, or their own words after Other.</summary>
    private void StreamAnswer(string given)
    {
        if (_asking is { } asking)
        {
            // A yes that stops the asking for a tool leaves its line in the conversation, where it can be found again.
            var standing = asking.StandingWords(given);
            StreamAnswered(asking, asking.Answer(given));
            if (standing is not null)
            {
                _model.AddSystemLine(standing);
            }
        }
    }

    /// <summary>Escape on what Claude Code asks (SM-5.3, SM-7.5): back to the list from the user's own words, else no to the whole.</summary>
    private void StreamEscape()
    {
        if (_asking is { } asking)
        {
            StreamAnswered(asking, asking.Escape());
        }
    }

    /// <summary>
    /// After an answer or an Escape: with the relay's answer, it goes back to Claude Code, nothing waits any more
    /// and the next question that came meanwhile is asked; without one, the model is told what is asked now, the
    /// next of Claude's questions or the user's own words.
    /// </summary>
    private void StreamAnswered(StreamAsking asking, string? answer)
    {
        if (_streamModel is not { } model || _askedRequest is not { } request)
        {
            return;
        }

        if (answer is null)
        {
            if (asking.WordsPrompt is { } prompt)
            {
                model.AskWords(prompt);
            }
            else
            {
                model.Ask(asking.Current);
            }

            return;
        }

        request.Answer(answer);
        _asking = null;
        _askedRequest = null;
        // What Claude Code asks next is a new request, also when it reads as the one just answered (two calls of one
        // tool about one file, tens of milliseconds apart): what the control area still shows is over, so that the
        // settle shows and says the next question afresh, with its list on No. Without this the old list stayed, on
        // the answer just given, and nothing was said (found by the review of 2026-11-17).
        MarkWaitOver();
        model.Answered();
        if (_askedWaiting.Count > 0)
        {
            StreamAsked(_askedWaiting.Dequeue());
        }
    }

    /// <summary>
    /// The answer of a tool call's question that only explains (SM-7.4, SM-7.6): everything Claude asks to do, the
    /// caution first, as a text with one button; the question stays under it and nothing is answered. False for any
    /// other answer.
    /// </summary>
    private bool StreamExplained(string keys)
    {
        if (_asking is not { } asking || !asking.Explains(keys))
        {
            return false;
        }

        ShowNotice(Notice.Plain(asking.ExplainTitle, asking.ExplainText));
        return true;
    }

    /// <summary>
    /// Claude Code ended by itself, every line it wrote before that taken: the process is stopped as any other, what
    /// it wrote as errors on its way out is shown, the window says that Claude stopped as it does for the screen, and
    /// an end right after the start offers the way back (SM-9.3).
    /// </summary>
    private void StreamEnded(StreamHost host, int code)
    {
        // For the user's eyes alone, and never logged: it can hold a path or a text of the user's.
        var errors = host.ErrorTail;
        var early = Environment.TickCount64 - _streamStartedAt < StreamEarlyEndMs;
        StopProcess();
        Log.Info($"{AppName}'s program exited with code {code}, over its stream; {host.ErrorCharacters} characters written as errors");
        var said = errors.Replace("\r", string.Empty).Split('\n').Select(text => text.Trim()).Where(text => text.Length > 0).TakeLast(StreamErrorLines).ToList();
        if (said.Count > 0)
        {
            _model.AddSystemLine("Claude Code wrote this as it ended:");
            foreach (var text in said)
            {
                _model.AddSystemLine(DisplayText.Visible(text));
            }
        }

        OnProcessExited(code);
        if (early)
        {
            OfferScreen("Claude Code ended right after its start", $"Claude Code ended right after it was started over its stream (exit code {code}). The conversation shows what it wrote.");
        }
    }

    /// <summary>
    /// The questions' pipe gave up (SM-7.3; the 2026-11-21 audit found it silent): from then on the relay would answer
    /// every question no and the user would hear nothing of it, and the pipe's name would be free. Claude Code is
    /// stopped, at once, and the window says why and offers the way back.
    /// </summary>
    private void StreamQuestionsLost(AskChannel channel)
    {
        if (!ReferenceEquals(channel, _askChannel))
        {
            // The pipe of a Claude Code that was stopped already.
            return;
        }

        const string message = "The pipe on which Claude Code asks you its questions broke and could not be opened again, so Claude Code was stopped: nothing runs without your answer.";
        StopProcess();
        Log.Error("Stream: Claude Code stopped, since the questions' pipe was lost");
        _model.AddSystemLine(message);
        UpdateStatus();
        OfferScreen("Claude Code was stopped", message);
    }

    /// <summary>The stream was not started: the reason in the log and as a line of the conversation, and the notice that offers the way back (SM-9.3).</summary>
    private void StreamNotStarted(string message)
    {
        Log.Error(message);
        _model.AddSystemLine(message);
        UpdateStatus();
        OfferScreen("Claude Code's stream did not start", message);
    }

    /// <summary>The way back is the switch (SM-9.3): a notice that says what went wrong and offers the same folder with the screen. Enter opens it, Escape stays.</summary>
    private void OfferScreen(string title, string message) =>
        ShowNotice(Notice.Ask(title, message + "\n" + ScreenOfferWords, "&Open with the screen", ReopenWithScreen, "&Stay in this window") with { Unprompted = true });

    /// <summary>axClaude started again on this folder for the screen, with the user's own arguments for Claude Code, and this window closed.</summary>
    private void ReopenWithScreen()
    {
        if (_folder is not { } folder)
        {
            return;
        }

        List<string> arguments = [folder, "--source", AppSettings.SourceScreen];
        if (_claudeArgs is { } own)
        {
            arguments.Add("--");
            arguments.AddRange(own);
        }

        WindowMenu.StartWith(AppVerb, arguments);
        Close();
    }

    /// <summary>
    /// Options, Developer, Feed the next window from Claude Code's stream (SM-9.1): the setting, for the next
    /// axClaude window. This window keeps the source it was made for (SM-9.2), and the sentence says both, since the
    /// menu closes on the choice and nothing else shows it.
    /// </summary>
    private void SetClaudeSource(bool stream)
    {
        _settings.ClaudeSource = stream ? AppSettings.SourceStream : AppSettings.SourceScreen;
        Announce($"Claude Code's stream {(stream ? "on" : "off")}, from the next axClaude window. This window keeps {(Streams ? "the stream" : "the screen")}", true);
    }

    /// <summary>Say the event sources in a window the stream feeds (SM-9.4): the source first, then its numbers.</summary>
    private string StreamSourceSaid() =>
        $"Source: the stream. Claude Code {(ProcessRunning ? "runs" : "is stopped")}. Lines read: {_streamLines}. Of a kind not known: {_streamUnknown}. Questions asked: {_streamAsked}.";

    /// <summary>The lines Copy diagnostics gains in a window the stream feeds (SM-9.4): numbers and fixed words only.</summary>
    private IReadOnlyList<string> StreamDiagnostics() =>
    [
        "Source: the stream",
        $"Stream: {_streamLines} lines read, {_streamUnknown} of a kind not known, {_streamRequest} requests written, {_streamAsked} questions asked",
    ];
}

/// <summary>One question of Claude Code's that waits on the window's pipe for the user's answer.</summary>
internal sealed class AskRequest(AskChannel source, byte[] question)
{
    private readonly ManualResetEventSlim _decided = new();
    private byte[]? _answer;

    /// <summary>The pipe the question came on: a question of a pipe the window has stopped is no longer the window's to ask.</summary>
    public AskChannel Source { get; } = source;

    /// <summary>The question as Claude Code wrote it for its permission tool.</summary>
    public byte[] Question { get; } = question;

    /// <summary>The answer the relay passes on to Claude Code; given once, on the window's thread.</summary>
    public void Answer(string json)
    {
        _answer = Encoding.UTF8.GetBytes(json);
        _decided.Set();
    }

    /// <summary>The pipe's thread waits here for the answer, however long the user takes; the wait is cancelled when the pipe is stopped.</summary>
    public byte[] Wait(CancellationToken stopped)
    {
        _decided.Wait(stopped);
        return _answer ?? [];
    }
}

/// <summary>
/// The window's end of the questions' pipe (docs/axClaude/stream-mode.md SM-7.3): one named pipe for one start of
/// Claude Code over its stream, <c>AxKit-ask-&lt;user SID&gt;-&lt;random&gt;</c>, for the current user alone and
/// never over the network, with a secret of its own. A connection is one question of <c>AxKit.exe ask</c>, which
/// Claude Code started as its permission tool: the question's frame in (<see cref="HookFrame"/>, the secret first,
/// or the connection is dropped), and the answer's frame out once the user has answered, however long that takes.
/// The thread never touches the window: it reads, posts the question, and waits for the answer to write.
/// <see cref="Stop"/> ends every wait and the pipe at once and never waits for the thread; a question that waited
/// then gets no answer, which the relay turns into a no. No exception leaves the thread.
/// </summary>
internal sealed class AskChannel
{
    /// <summary>How long one connection may take to send its question's frame.</summary>
    private const int ReadLimitMs = 2000;

    /// <summary>The pipe's buffers: a question of the usual size is written and read whole.</summary>
    private const int BufferBytes = 65_536;

    /// <summary>After this many failures in a row the thread gives up; Claude Code's questions are then answered no by the relay.</summary>
    private const int MostFailures = 20;

    private readonly Control _owner;
    private readonly Action<AskRequest> _asked;
    private readonly Action<AskChannel> _lost;
    private readonly string _name;
    private readonly string _secret;
    private readonly CancellationTokenSource _cancel = new();

    private AskChannel(Control owner, Action<AskRequest> asked, Action<AskChannel> lost, string name, string secret)
    {
        _owner = owner;
        _asked = asked;
        _lost = lost;
        _name = name;
        _secret = secret;
        VariableValue = AskRelay.VariableValue(name, secret);
    }

    /// <summary>The value of <see cref="AskRelay.Variable"/> for the environment of the Claude Code this pipe is for.</summary>
    public string VariableValue { get; }

    /// <summary>
    /// Opens a new pipe with a new name and a new secret and serves it on a thread of its own. The pipe exists when
    /// this returns, so that the first question finds it. Null, with one line in the log, when it could not be
    /// opened. <paramref name="asked"/> gets each question on the thread of <paramref name="owner"/>, and
    /// <paramref name="lost"/> is called there once when the pipe gives up for good.
    /// </summary>
    public static AskChannel? Start(Control owner, Action<AskRequest> asked, Action<AskChannel> lost)
    {
        try
        {
            var channel = new AskChannel(owner, asked, lost, AskRelay.NewPipeName(UserPipe.User().Value), HookFrame.NewSecret());
            var pipe = channel.Open();
            new Thread(() => channel.Serve(pipe)) { IsBackground = true, Name = "AxKit questions" }.Start();
            return channel;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // The exception's type alone: its message would name the pipe.
            Log.Info("Stream: the questions' pipe was not opened (" + ex.GetType().Name + ")");
            return null;
        }
    }

    /// <summary>The pipe closes with its Claude Code: every wait of the thread is cancelled, it closes the pipe and ends. Called on the window's thread, which never waits for it.</summary>
    public void Stop() => _cancel.Cancel();

    private NamedPipeServerStream Open()
    {
        // Outside call, pipe, Claude Code questions: the window's own named pipe, which only this Windows account may
        // open, where "AxKit.exe ask", started by Claude Code as its permission tool, writes a question and reads the
        // user's answer; each connection must send the window's secret.
        return NamedPipeServerStreamAcl.Create(_name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, BufferBytes, BufferBytes, UserPipe.Security());
    }

    /// <summary>
    /// The pipe's thread. One instance of the pipe serves every connection in turn, as the hooks' pipe does, so that
    /// the name is never free for another program to take while Claude Code runs; a connection that fails gives the
    /// instance up, and the pipe is opened again after a pause. Every wait and read is under the token
    /// <see cref="Stop"/> cancels.
    /// </summary>
    private void Serve(NamedPipeServerStream first)
    {
        NamedPipeServerStream? pipe = first;
        var failures = 0;
        try
        {
            while (!_cancel.IsCancellationRequested)
            {
                try
                {
                    pipe ??= Open();
                    pipe.WaitForConnectionAsync(_cancel.Token).GetAwaiter().GetResult();
                    Take(pipe);
                    pipe.Disconnect();
                    failures = 0;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ObjectDisposedException)
                {
                    // A relay that left before its answer, a pipe that broke: this instance is given up.
                    pipe?.Dispose();
                    pipe = null;
                    if (++failures > MostFailures)
                    {
                        Log.Error($"Stream: the questions' pipe gave up after {failures} failures in a row ({ex.GetType().Name})");
                        TellLost();
                        return;
                    }

                    _cancel.Token.WaitHandle.WaitOne(Math.Min(2000, 50 * failures));
                }
            }
        }
        catch (Exception ex)
        {
            // The type and the place, never the message: it could quote what a question carried.
            Log.Error($"Stream: the questions' pipe ended on {ex.GetType().Name}\n{ex.StackTrace}");
            TellLost();
        }
        finally
        {
            pipe?.Dispose();
        }
    }

    /// <summary>One connection: the question's frame read within its time, the secret checked first, the question posted to the window, and the answer written when it comes.</summary>
    private void Take(NamedPipeServerStream pipe)
    {
        byte[]? question;
        using (var limit = CancellationTokenSource.CreateLinkedTokenSource(_cancel.Token))
        {
            limit.CancelAfter(ReadLimitMs);
            question = HookFrame.Read(new TimedPipeRead(pipe, limit.Token, BufferBytes), _secret, out var fault);
            if (question is null)
            {
                // A wrong secret, a question over the limit, or a connection that broke or took too long: no answer.
                Log.Info($"Stream: the questions' pipe dropped a connection ({fault})");
                return;
            }
        }

        var request = new AskRequest(this, question);
        if (!Post(request))
        {
            // The window is gone: no answer, and the relay says no.
            return;
        }

        var answer = request.Wait(_cancel.Token);
        HookFrame.Write(pipe, _secret, answer);
        pipe.WaitForPipeDrain();
    }

    /// <summary>The window is told, on its thread, that this pipe serves no more; nothing when the window is gone or the pipe was stopped.</summary>
    private void TellLost()
    {
        if (_cancel.IsCancellationRequested || _owner.IsDisposed || !_owner.IsHandleCreated)
        {
            return;
        }

        try
        {
            _owner.BeginInvoke(() => _lost(this));
        }
        catch (InvalidOperationException)
        {
            // The window is closing.
        }
    }

    private bool Post(AskRequest request)
    {
        if (_owner.IsDisposed || !_owner.IsHandleCreated)
        {
            return false;
        }

        try
        {
            _owner.BeginInvoke(() => _asked(request));
            return true;
        }
        catch (InvalidOperationException)
        {
            // The window is closing.
            return false;
        }
    }
}

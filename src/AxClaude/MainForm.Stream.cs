using System.ComponentModel;
using AxClaude.Core;
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

        // What the reader's thread and the end hand the window: one post for a burst, in the order they came, the
        // end last. A line is read down on the reader's thread, so that nothing of its text reaches this one.
        var inbox = new List<(StreamLine? Line, int ExitCode)>();
        var posted = false;
        StreamHost? host = null;
        try
        {
            host = StreamHost.Start(new StreamHostOptions(line, folder, ClaudeLauncher.BuildEnvironment()), text => Hand((ClaudeCodeStream.Read(text), 0)), code => Hand((null, code)));
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or IOException)
        {
            Log.Error("Claude could not be started over its stream", ex);
            StreamNotStarted("Claude could not be started: " + ex.Message);
            return;
        }

        Log.Info($"Started {line.CommandLine} (pid {host.ProcessId}) in {folder}, over its stream");
        _launchedFolder = folder;
        KeepFilesOutOfGit();
        model.WorksIn(folder);
        _stream = host;
        _streamStartedAt = Environment.TickCount64;
        _streamLines = 0;
        _streamUnknown = 0;
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
        if (terminateAtOnce)
        {
            host.Dispose();
        }
        else
        {
            host.End(StreamEndGraceMs);
        }
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

    /// <summary>Say the event sources in a window the stream feeds (SM-9.4): the source first, then its numbers.</summary>
    private string StreamSourceSaid() =>
        $"Source: the stream. Claude Code {(ProcessRunning ? "runs" : "is stopped")}. Lines read: {_streamLines}. Of a kind not known: {_streamUnknown}.";

    /// <summary>The lines Copy diagnostics gains in a window the stream feeds (SM-9.4): numbers and fixed words only.</summary>
    private IReadOnlyList<string> StreamDiagnostics() =>
    [
        "Source: the stream",
        $"Stream: {_streamLines} lines read, {_streamUnknown} of a kind not known, {_streamRequest} requests written",
    ];
}

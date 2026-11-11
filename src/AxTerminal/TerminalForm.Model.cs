using AxKit.Forms;
using AxTerminal.Core;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- what the model reports (AT-5): the console window has synced the view, the status bar and the music first ----

    protected override void OnTranscriptChanged()
    {
        UpdateTitle();

        // One pass over the transcript for the tick (AT-5.2) and the spoken output (AT-5.3).
        var arrival = _arrivals.Update(_model.Lines, _model.Running, _settings.OutputSpeech == OutputSpeechMode.All);
        if (_settings.ClickOnNewLine && arrival.NewLine)
        {
            Sounds.Click();
        }

        SayWithFinished(arrival.Speech);
    }

    /// <summary>"Ready", "Done" or "Failed", waiting for the frame that ends it to be spoken first; then said with that frame's speech as one announcement.</summary>
    private string? _finished;

    /// <summary>Half a second at most for that frame; then the word goes alone.</summary>
    private readonly System.Windows.Forms.Timer _finishedLate = new() { Interval = 500 };

    /// <summary>
    /// The frame's speech and the word that waits, as one announcement: NVDA drops the second of two notifications
    /// raised back to back (src/AxClaude/CLAUDE.md), so "Done" after a command's last line, and "Ready" after a shell's
    /// banner, were never spoken while each was its own (heard by the NVDA pass, 2026-11-01). Done and Failed bring the
    /// chime and the attention signals; Ready has its ping from <see cref="OnReady"/>.
    /// </summary>
    private void SayWithFinished(string? speech)
    {
        _finishedLate.Stop();
        var word = _finished;
        _finished = null;
        if (speech is not null || word is not null)
        {
            // One full stop between them: a shell's line may end with its own.
            Announce(speech is null ? word! : word is null ? speech : $"{speech.TrimEnd('.', ' ')}. {word}", false);
        }

        // The sound at once; the text may wait a moment to be joined to another (AX-7.3), so the ping can come first.
        if (word == "Ready")
        {
            if (_settings.SoundOnBell)
            {
                Sounds.Ready();
            }
        }
        else if (word is not null)
        {
            Signal(question: false);
        }
    }

    /// <summary>The first prompt of a run (AT-5.1): "Ready" with its ping, after the banner's last line.</summary>
    private void OnReady()
    {
        _finished = "Ready";
        StartFinishedLate();
        UpdateStatus();
    }

    /// <summary>The prompt came back after a command (AT-5.1): Done, or Failed with the code, with the done chime and the attention signals.</summary>
    private void OnCommandFinished(int? code)
    {
        _askedRow = null;
        // The end of a command is reported while its output is still being read, before the frame that makes its last
        // line final: the word waits for that frame's speech (OnTranscriptChanged), so that Speak output: all lines
        // says the last line before "Done", not after it (found by tools/AxAutomate, 2026-10-23).
        _finished = code is null or 0 ? "Done" : code == -1 ? "Failed" : $"Failed, exit code {code}";
        StartFinishedLate();
    }

    private void StartFinishedLate()
    {
        if (!_finishedLate.Enabled)
        {
            _finishedLate.Tick -= OnFinishedLate;
            _finishedLate.Tick += OnFinishedLate;
            _finishedLate.Start();
        }
    }

    private void OnFinishedLate(object? sender, EventArgs e) => SayWithFinished(null);

    /// <summary>
    /// AT-5.4: while a command runs and output has stopped for two seconds, a cursor row that ends like a question
    /// (a colon, a question mark, a closing bracket or parenthesis) is announced once as "Asks: …", with the question
    /// chime. A heuristic: a program's prompt is the only sign the window has that it waits.
    /// </summary>
    private void CheckForQuestion()
    {
        if (!_settings.AnnounceQuestions || !ProcessRunning || !_model.Running || Environment.TickCount64 - LastOutputTick < AsksQuietMs)
        {
            return;
        }

        var row = _model.CursorRowText?.TrimEnd();
        if (row is null || row == _askedRow || row.Length == 0 || row[^1] is not (':' or '?' or ']' or ')'))
        {
            return;
        }

        _askedRow = row;
        Announce($"Asks: {row}", false);
        Signal(question: true);
    }

    /// <summary>Ctrl+Q (AT-2.3, AT-D8): the state, the command and how long it runs, and the prompt, from anywhere.</summary>
    private void SayWhatIsRunning()
    {
        string text;
        if (!ProcessRunning)
        {
            text = "Shell ended";
        }
        else if (_model.Running)
        {
            var seconds = _model.RunningSince is { } since ? (int)(DateTime.Now - since).TotalSeconds : 0;
            text = $"Running: {_model.RunningCommand}, {seconds} seconds";
        }
        else if (!_model.HasMarks)
        {
            text = "No marks from this shell";
        }
        else
        {
            text = _model.Prompt is null ? "Starting" : $"Idle, {ShellName}";
        }

        Announce(_model.Prompt is { } prompt ? $"{text}, {prompt}" : $"{text}, {CurrentDirectory}", true);
    }

    /// <summary>Session → Say current folder (Ctrl+P, AT-D9): the full path of the shell's directory, or of the start folder while the shell gives no marks.</summary>
    private void SayFolder() => Announce(CurrentDirectory, true);

    /// <summary>Session → Clear transcript (Ctrl+L, AT-4.4): the window empties; the shell goes on.</summary>
    private void ClearTranscript()
    {
        _model.Clear();
        _arrivals.Reset();
        Announce("Cleared", true);
    }
}

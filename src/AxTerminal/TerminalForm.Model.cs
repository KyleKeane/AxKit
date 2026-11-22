using AxKit.Core.Events;
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
            EventDoor.Raise(EventKind.Line);
        }

        HoldOutput(arrival.Speech);
        if (_finished is not null)
        {
            // The frame after a command's end, or after the first prompt: the word, with everything still held.
            SayWithFinished();
        }
    }

    private const string ReadyWord = "Ready";
    private const string DoneWord = "Done";

    /// <summary>"Ready", "Done" or "Failed", waiting for the frame that ends it; then said with what is held as one announcement.</summary>
    private string? _finished;

    /// <summary>Half a second at most for that frame; then the word goes without it.</summary>
    private readonly System.Windows.Forms.Timer _finishedLate = new() { Interval = 500 };

    /// <summary>
    /// The output's speech not yet handed to NVDA (AT-5.3), held for <see cref="OutputHoldMs"/> from its first line:
    /// a command that ends within that time says "Done" and then its whole result, and one that runs on says its
    /// output in pieces a third of a second apart and at its end "Done" with what is left.
    /// </summary>
    private readonly System.Text.StringBuilder _heldOutput = new();

    private const int OutputHoldMs = 300;

    private readonly System.Windows.Forms.Timer _outputHold = new() { Interval = OutputHoldMs };

    /// <summary>A frame's speech joins what is held, one full stop between them (a shell's line may end with its own), and the hold starts with the first.</summary>
    private void HoldOutput(string? speech)
    {
        if (speech is null)
        {
            return;
        }

        if (_heldOutput.Length > 0)
        {
            var kept = _heldOutput.ToString().TrimEnd('.', ' ');
            _heldOutput.Clear().Append(kept).Append(". ");
        }

        _heldOutput.Append(speech);
        if (!_outputHold.Enabled)
        {
            _outputHold.Tick -= OnOutputHold;
            _outputHold.Tick += OnOutputHold;
            _outputHold.Start();
        }
    }

    /// <summary>The hold is over: what is held goes, unless a word of an end waits, which takes it along.</summary>
    private void OnOutputHold(object? sender, EventArgs e)
    {
        _outputHold.Stop();
        if (_finished is null && _heldOutput.Length > 0)
        {
            Announce(_heldOutput.ToString(), false);
            _heldOutput.Clear();
        }
    }

    /// <summary>
    /// The word that waits and the output still held, as one announcement: NVDA drops the second of two
    /// notifications raised back to back (src/AxClaude/CLAUDE.md), so "Done" and a command's last line, and "Ready"
    /// after a shell's banner, were never both spoken while each was its own (heard by the NVDA pass, 2026-11-01).
    /// The word of a command's end stands first, "Done. hello" (the maintainer's decision of 2026-11-22, for axClaude
    /// and here alike: Done is said before the result, or not at all, with no threshold of time; until then "hello.
    /// Done", so that after a long output the word was heard only when all of it had been read, and for a short
    /// while that day in the middle of a listing, whose first lines had gone before the command ended: hence the
    /// hold). "Ready" stays after the banner it ends. With <see cref="TerminalSettings.AnnounceDone"/> off "Done" is
    /// not said and the output goes alone; "Failed" with its code is always said. Done and Failed bring the chime
    /// and the attention signals whatever is said; Ready has its ping from <see cref="OnReady"/>.
    /// </summary>
    private void SayWithFinished()
    {
        _finishedLate.Stop();
        _outputHold.Stop();
        var word = _finished;
        _finished = null;
        var speech = _heldOutput.Length > 0 ? _heldOutput.ToString() : null;
        _heldOutput.Clear();
        var said = word == DoneWord && !_settings.AnnounceDone ? null : word;
        if (speech is not null || said is not null)
        {
            Announce(speech is null ? said!
                : said is null ? speech
                : said == ReadyWord ? $"{speech.TrimEnd('.', ' ')}. {said}"
                : $"{said}. {speech}", false);
        }

        // The sound at once; the text may wait a moment to be joined to another (AX-7.3), so the ping can come first.
        if (word == ReadyWord)
        {
            if (_settings.SoundOnBell)
            {
                EventDoor.Raise(EventKind.Ready);
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
        _finished = ReadyWord;
        StartFinishedLate();
        UpdateStatus();
    }

    /// <summary>The prompt came back after a command (AT-5.1): Done, or Failed with the code, with the done chime and the attention signals.</summary>
    private void OnCommandFinished(int? code)
    {
        _askedRow = null;
        // The end of a command is reported while its output is still being read, before the frame that makes its last
        // line final: the word waits for that frame's speech (OnTranscriptChanged), half a second at most, and is
        // said with it as one announcement, the word first (SayWithFinished).
        _finished = code is null or 0 ? DoneWord : code == ShellTexts.ErrorWithoutCode ? "Failed" : $"Failed, exit code {code}";
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

    private void OnFinishedLate(object? sender, EventArgs e) => SayWithFinished();

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
        if (row is null || row == _askedRow || !ShellTexts.EndsLikeQuestion(row))
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

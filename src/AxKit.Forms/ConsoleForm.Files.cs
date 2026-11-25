using System.Text;
using AxKit.Core;
using AxKit.Core.Pty;
using AxKit.Core.Transcript;

namespace AxKit.Forms;

public abstract partial class ConsoleForm
{
    // ---- export, recording (axClaude FR-3.8, FR-13.3; axTerminal AT-4.7, AT-8.2); find is the conversation window's (ConversationForm) ----

    private StreamRecorder? _recorder;

    /// <summary>
    /// The transcript as the reader sees it (hidden rows left out, the reading break the view adds left out) to a
    /// UTF-8 file chosen in the Windows save dialog: <paramref name="what"/> names it in the log and the error ("the
    /// conversation"), the rest is the dialog's title, filter, proposed name, extension and folder.
    /// </summary>
    protected void Export(string what, string title, string filter, string fileName, string extension, string folder)
    {
        using var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = fileName,
            AddExtension = true,
            DefaultExt = extension,
            InitialDirectory = folder,
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, TranscriptMirror.Render(Model.Lines), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail($"The {what} could not be exported: {ex.Message}");
            return;
        }

        Log.Info($"The {what} was exported to {dialog.FileName}");
        Announce("Exported", true);
    }

    /// <summary>Options → Developer → Record raw stream: the console stream to a .vt file in the PtyCapture format, for a fixture; the same item stops it.</summary>
    private void ToggleRecording()
    {
        if (_recorder is not null)
        {
            StopRecording(announce: true);
            return;
        }

        var (title, filter, extension) = RecordingFile;
        using var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = $"{AppName.ToLowerInvariant()}-{DateTime.Now:yyyyMMdd-HHmm}.{extension}",
            AddExtension = true,
            DefaultExt = extension,
            // A recording holds everything on screen, account details and paths included: outside the project (axClaude
            // D34), and in the local profile rather than Documents, which OneDrive often syncs (security audit S-24).
            InitialDirectory = RecordingsFolder(),
        };
        if (ShowSystemDialog(dialog) == DialogResult.OK)
        {
            StartRecording(dialog.FileName);
        }
    }

    /// <summary>
    /// The save dialog's title, filter and extension for a recording: the console's bytes as a <c>.vt</c> file. A window
    /// that is fed otherwise names its own (axClaude over Claude Code's stream: its lines, a <c>.jsonl</c> file).
    /// </summary>
    protected virtual (string Title, string Filter, string Extension) RecordingFile =>
        ("Record the raw console stream", "Console recording (*.vt)|*.vt|All files (*.*)|*.*", "vt");

    /// <summary>A recording runs: what a window fed otherwise asks before it makes the bytes to <see cref="Record"/>.</summary>
    protected bool Recording => _recorder is not null;

    /// <summary>
    /// What a window fed otherwise than by the pseudo console records, as it came, on the thread that read it: the
    /// same file, index and folder as the console's bytes. Nothing while no recording runs.
    /// </summary>
    protected void Record(ReadOnlySpan<byte> data) => _recorder?.Write(data);

    /// <summary>%LOCALAPPDATA%\AxKit\recordings, made when first asked for.</summary>
    private static string RecordingsFolder()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxKit", "recordings");
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Records the raw stream to <paramref name="path"/> from now (the menu, or axClaude's --record).</summary>
    protected void StartRecording(string path)
    {
        try
        {
            _recorder = new StreamRecorder(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The recording could not be started: " + ex.Message);
            return;
        }

        RecordItem.Text = "Stop &recording";
        Log.Info("Recording raw stream to " + path);
        Model.AddSystemLine("Recording to " + path);
        Announce("Recording started", false);
    }

    private void StopRecording(bool announce)
    {
        var recorder = _recorder;
        if (recorder is null)
        {
            return;
        }

        _recorder = null;
        recorder.Dispose();
        RecordItem.Text = RecordItemText;
        Log.Info($"Recording stopped: {recorder.Length} bytes in {recorder.Path}");
        if (announce)
        {
            Model.AddSystemLine($"Recording stopped: {recorder.Length} bytes in {recorder.Path}");
            Announce("Recording stopped", false);
        }
    }

    /// <summary>The recording file could not be written (reported from the reader thread): stop recording and say so.</summary>
    private void RecordingFailed(string reason)
    {
        if (_recorder is null)
        {
            return;
        }

        StopRecording(announce: false);
        Log.Error("Recording failed: " + reason);
        Model.AddSystemLine("Recording stopped, the file could not be written: " + reason);
        Announce("Recording stopped", false);
    }
}

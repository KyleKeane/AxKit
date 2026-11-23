using AxKit.Core;
using AxKit.Core.Transcript;

namespace AxKit.Forms;

/// <summary>
/// The window of a console app (docs/axKit/SPEC.md AX-6.5; docs/axTerminal/SPEC.md AT-2): a program in a pseudo
/// console shown in the conversation window (<see cref="ConversationForm"/>: the transcript with single-key
/// navigation, the multi-line field at the bottom or the control area in its place, the status bar, the focus keys,
/// find, the bookmarks, the notice hooks and the font), and everything axClaude's window and axTerminal's window had
/// twice until 2026-10-23 that is about the program: the pty bridge with its inbox and quiet timer, the paced write
/// queue with one pump, the recording, the export, the signals and the music, the folders, the close. The shape
/// was this class's own until 2026-11-22, when it moved, unchanged, into <see cref="ConversationForm"/> (framework
/// step 9), so that a window without a console program can have it. One class in partial files: this one (the
/// hooks a console window fills in, the close, the menu items both windows have), <c>ConsoleForm.Process.cs</c>
/// (the program's start, output, exit and stop; the writes), <c>ConsoleForm.Files.cs</c> (export, recording)
/// and <c>ConsoleForm.Signals.cs</c> (the chimes, the taskbar flash, the music). axClaude's <c>MainForm</c> keeps
/// what is Claude's and axTerminal's <c>TerminalForm</c> what is the shell's.
/// </summary>
public abstract partial class ConsoleForm : ConversationForm
{
    private const string RecordItemText = "&Record raw stream for a bug report...";

    private ToolStripMenuItem? _musicItem;
    private bool _closeConfirmed;

    /// <param name="model">The app's model on the bundle's <see cref="LineStore"/>, which the window feeds and shows; the app keeps its own typed reference.</param>
    /// <param name="keys">The window's keys for the view, from its table.</param>
    /// <param name="transcriptName">What NVDA calls the view: "Conversation", "Terminal".</param>
    /// <param name="inputName">What NVDA calls the field: "Message to Claude", "Command".</param>
    protected ConsoleForm(string appName, ConsoleSettings settings, string settingsPath, string? settingsError, LineStore model, TranscriptKeys keys, string transcriptName, string inputName)
        : base(appName, settings, settingsPath, settingsError, model, keys, transcriptName, inputName, settings.ShowMarkers)
    {
        ConsoleSettings = settings;

        Model.MaxLines = settings.MaxTranscriptLines;
        Model.JoinWrappedLines = settings.JoinWrappedLines;
        Model.TimeStamps = settings.MarkerTimeStamps;

        RecordItem = CommandPalette.Marked(new ToolStripMenuItem(RecordItemText), CommandMark.Act); // Writes the raw stream, everything typed and shown, to a file.
        RecordItem.Click += (_, _) => ToggleRecording();
        CommandPalette.RefreshedOnOpening(RecentFoldersItem, FillRecentFolders);
        FillRecentFolders();
        CommandPalette.RefreshedOnOpening(RecentFoldersInNewWindowItem, FillRecentFoldersInNewWindow);
        FillRecentFoldersInNewWindow();

        // The field's keys, Ctrl+Enter and a line taken into the field (ConsoleForm.Field.cs).
        WireField();

        _quiet.Tick += (_, _) =>
        {
            _quiet.Stop();
            Model.EndFrame();
        };
        _exitSettle.Tick += (_, _) =>
        {
            _exitSettle.Stop();
            FinishExit();
        };
        _pump.Tick += (_, _) =>
        {
            _pump.Stop();
            PumpWrites();
        };
    }

    /// <summary>The console fields of the app's settings: the size, the recent folders, the sounds, the signals, the transcript options.</summary>
    protected ConsoleSettings ConsoleSettings { get; }

    /// <summary>Options → Developer → Record raw stream for a bug report, or Stop recording while one runs.</summary>
    protected ToolStripMenuItem RecordItem { get; }

    /// <summary>The first menu's Open recent folder submenu, filled from the settings as it opens; a folder chosen goes to <see cref="SwitchFolder"/>.</summary>
    protected ToolStripMenuItem RecentFoldersItem { get; } = new("Open &recent folder");

    /// <summary>The same list, each folder opened in a new window of the app with this one left as it is (<see cref="OpenInNewWindow"/>).</summary>
    protected ToolStripMenuItem RecentFoldersInNewWindowItem { get; } = new("Open recent &folder in new window");

    // ---- what the console window fills in ----

    /// <summary>Whether the program is busy, for the music (axClaude FR-7.9, axTerminal AT-5.5): Claude working and not asking, a command running.</summary>
    protected abstract bool Working { get; }

    /// <summary>The program's process ended with <paramref name="code"/>, its last output in and the process stopped: the window's system line and announcement.</summary>
    protected abstract void OnProcessExited(int code);

    // The question before closing, the restart with its question and the switch to another folder are in
    // ConsoleForm.Session.cs: the steps are the console window's, the words and the conditions each app's.

    // ---- the window base's hooks, filled in for both console windows ----

    /// <summary>The notice chime follows the sounds setting (axClaude FR-7.3, axTerminal AT-5.1).</summary>
    protected override bool NoticeChime => ConsoleSettings.SoundOnBell;

    /// <summary>A console window's announcements are the user's conversation and the shell's output: a report gets its commands and notices alone (docs/axKit/reports.md RP-4.2).</summary>
    protected override bool RecordsAnnouncements => false;

    /// <summary>The music follows the program's state with every change of the transcript, after the status bar and before the window's own pass.</summary>
    protected override void OnModelSynced() => UpdateMusic();

    /// <summary>
    /// Closing: the window asks first while the program is in the middle of something (axClaude FR-1.7, axTerminal
    /// AT-1.7), unless Windows or Close all windows closes it; then the folder joins Recently closed (AX-9.4), the
    /// process and the recording stop, and the base saves the window and starts the installed new version (AX-3.2).
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !_closeConfirmed && AskBeforeClosing())
        {
            e.Cancel = true;
            base.OnFormClosing(e);
            return;
        }


        // A close the user confirmed while the program was busy ends it at once; Windows' own close gives it its two seconds.
        StopProcess(terminateAtOnce: _closeConfirmed);
        StopRecording(announce: false);
        base.OnFormClosing(e);
    }

    /// <summary>Whether the closing question was answered with Close anyway: the close under way is that one (axClaude's window that hides on closing closes for real then, FR-9.6).</summary>
    protected bool CloseWasConfirmed => _closeConfirmed;

    /// <summary>The Close anyway of the closing question: the window closes without asking again.</summary>
    protected void CloseConfirmed()
    {
        _closeConfirmed = true;
        Close();
    }

    // ---- the menu items both windows have, for the window to place ----

    /// <summary>The transcript's two switches for Options → Text: the time stamps on the markers and the # markers in the text (axClaude FR-4.6, FR-4.6a).</summary>
    protected ToolStripItem[] TranscriptTextItems() =>
    [
        Toggle("Time &stamps on Input and Output lines", ConsoleSettings.MarkerTimeStamps, v =>
        {
            ConsoleSettings.MarkerTimeStamps = v;
            Model.TimeStamps = v;
        }),
        Toggle("Mar&kers in the text", ConsoleSettings.ShowMarkers, v =>
        {
            ConsoleSettings.ShowMarkers = v;
            Transcript.ShowMarkers = v;
            Transcript.Sync(Model.Lines);
        }),
    ];

    /// <summary>Options → Sounds → Background music …, made with the window's words and key, the key a command that flips it from anywhere (<see cref="ToggleMusic"/>).</summary>
    protected ToolStripMenuItem CreateMusicItem(string text, Keys key)
    {
        AddKey(new Command("Background music on or off", key, ToggleMusic) { Mark = CommandMark.Change });
        _musicItem = Toggle(text, ConsoleSettings.WorkingMusic, v =>
        {
            ConsoleSettings.WorkingMusic = v;
            UpdateMusic();
        });
        _musicItem.ShortcutKeyDisplayString = BundleKeys.Text(key);
        return _musicItem;
    }

    private void FillRecentFolders() => RecentMenu.FillFolders(RecentFoldersItem, ConsoleSettings.RecentFolders, "&No recent folders", CommandMark.Act, SwitchFolder);

    private void FillRecentFoldersInNewWindow() => RecentMenu.FillFolders(RecentFoldersInNewWindowItem, ConsoleSettings.RecentFolders, "&No recent folders", CommandMark.Act, OpenInNewWindow);

    /// <summary>The program in <paramref name="folder"/> in a window of its own, this one left as it is (axClaude FR-8.5, axTerminal AT-1.5).</summary>
    protected void OpenInNewWindow(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        WindowMenu.Start(AppVerb, folder);
        Announce($"Opening {PathName.Of(folder)} in a new window", false);
    }
}

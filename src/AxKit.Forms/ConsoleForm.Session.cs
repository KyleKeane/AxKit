using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// A question a console window asks before it ends or restarts its program in the middle of something: the
/// notice's title, its text, and the label of the button that leaves things as they are ("&amp;Keep working").
/// </summary>
public sealed record ConsoleQuestion(string Title, string Text, string Keep);

/// <summary>
/// The console window's session (docs/axKit/framework.md step 11, third part, 2026-11-22; from axClaude's
/// <c>MainForm</c> and axTerminal's <c>TerminalForm</c>, which each went through the same steps with their own
/// words): the question before closing, the restart with its question, and the switch to another folder. When a
/// question is asked and what it says are the app's, since only the app knows what its program is in the middle
/// of; the buttons, what they do and the order of the steps are here.
/// </summary>
public abstract partial class ConsoleForm
{
    // ---- closing (axClaude FR-1.7, axTerminal AT-1.7) ----

    /// <summary>
    /// The question before the window closes with the program in the middle of something, or null to close now.
    /// The app writes the whole text, with what closing does after an update that waits for a restart
    /// (<see cref="AxKitForm.RestartPending"/>).
    /// </summary>
    protected abstract ConsoleQuestion? ClosingQuestion();

    /// <summary>
    /// Alt+F4 with the program in the middle of something: true when the window asked first. Close anyway closes
    /// without asking again (<see cref="CloseConfirmed"/>); the other button keeps the window, and after Update and
    /// restart keeps the installed update for the next start and says so (bundle AX-3.5).
    /// </summary>
    private bool AskBeforeClosing()
    {
        if (ClosingQuestion() is not { } question)
        {
            return false;
        }

        ShowNotice(Notice.Ask(question.Title, question.Text, "&Close anyway", CloseConfirmed, question.Keep, KeepWorkingAfterUpdate));
        return true;
    }

    // ---- the restart (axClaude FR-1.8, axTerminal AT-1.6) ----

    /// <summary>The question before a restart ends what the program is doing, or null to restart at once (the program has ended, or nothing runs in it).</summary>
    protected abstract ConsoleQuestion? RestartQuestion();

    /// <summary>The program stopped and started again, with the app's system line; "Restarting" has been said.</summary>
    protected abstract void RestartNow();

    /// <summary>The restart command: the question first while the app has one, Restart now or no question restarts.</summary>
    protected void RestartProgram()
    {
        if (RestartQuestion() is { } question)
        {
            ShowNotice(Notice.Ask(question.Title, question.Text, "&Restart now", RestartSaid, question.Keep));
            return;
        }

        RestartSaid();
    }

    private void RestartSaid()
    {
        Announce("Restarting", false);
        RestartNow();
    }

    // ---- the folder (axClaude FR-8, axTerminal AT-1.5) ----

    /// <summary>What the folder picker says it is for: "Choose the project folder for Claude".</summary>
    protected abstract string FolderPickerText { get; }

    /// <summary>The bundle's folder picker (one of the few separate windows, AX-7.2), on <paramref name="start"/> or the user's profile; null when it was cancelled.</summary>
    protected string? PickFolder(string? start) =>
        ChooseFolder(FolderPickerText, start ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>The folder made the window's: the app keeps it, with what follows it there (the title, the folder left behind into Recently closed, AX-9.4).</summary>
    protected abstract void SetFolder(string folder);

    /// <summary>The program started again in the folder just set, with the app's system line for it.</summary>
    protected abstract void StartInFolder(string folder);

    /// <summary>
    /// Open recent folder, and the app's own ways to another folder (axClaude FR-8.1 and FR-8.3, axTerminal AT-1.5):
    /// the program again in that folder, after "Folder name"; a folder that is gone says so and nothing changes.
    /// </summary>
    protected void SwitchFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        SetFolder(folder);
        Announce($"Folder {PathName.Of(folder)}", false);
        StartInFolder(folder);
    }
}

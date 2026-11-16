namespace AxKit.Core;

/// <summary>What a second start of a background app does about the first copy (<see cref="BackgroundStart.Decide"/>).</summary>
public enum SecondStart
{
    /// <summary>The first copy runs and answered, or this start may not ask: this process ends.</summary>
    End,

    /// <summary>The first copy holds the app's mutex and does not answer: the user is asked whether to end it and start again.</summary>
    Ask,
}

/// <summary>
/// A background app that does not answer (docs/axKit/SPEC.md AX-1.9; repairs.md R-65). axStart, axClip and axHelp
/// run one copy each, hidden, held by a named mutex; a second start hands its wish to the first copy's window and
/// ends. When the first copy hangs, its hotkey is dead and a second start did nothing, without a word. Here are the
/// decision, the question's words and the test of which processes are that copy; the window side is
/// <c>AxKit.Forms/AppHost.OneCopy</c>. Pure, tested in <c>tests/AxKit.Tests/BackgroundStartTests</c>.
/// </summary>
public static class BackgroundStart
{
    /// <summary>
    /// What the second start does. <paramref name="windowFound"/>: the first copy's window is there;
    /// <paramref name="answered"/>: it took the hand-over, or answered at all, in time. A start with --hidden (the
    /// sign-in's, which only wants the app running) and a start inside a test run never ask: nobody is there to
    /// answer, so they end as before. A window found and silent, and no window while the mutex is held (after the
    /// caller has waited for a copy that was still starting), both ask.
    /// </summary>
    public static SecondStart Decide(bool windowFound, bool answered, bool hidden, bool testRun)
    {
        if (hidden || testRun)
        {
            return SecondStart.End;
        }

        return windowFound && answered ? SecondStart.End : SecondStart.Ask;
    }

    /// <summary>The question, in a message box of Windows since no window of the app exists yet (AX-7.2); No is the default.</summary>
    public static string Question(string app) => $"{app} is running but does not answer. End it and start again?";

    /// <summary>What is said when Yes found nothing it may end, or the copy would not go: nothing was started.</summary>
    public static string NotEnded(string app) =>
        $"{app} could not be ended, so it was not started again. Sign out of Windows and in again, or end its AxKit.exe in Task Manager, then start {app}.";

    /// <summary>
    /// Whether a process's command line is the app's: its first argument after the program is the app's verb
    /// ("clip", "start", "help"), the case not minded. The caller has checked that the process runs this very
    /// program file; the name of a process alone is never enough, and every other app of the bundle runs the same
    /// file with another verb.
    /// </summary>
    public static bool IsCopyOf(string? commandLine, string verb)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        var arguments = Arguments(commandLine);
        return arguments.Count >= 2 && string.Equals(arguments[1], verb, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A command line as its words: separated by spaces or tabs, a part in double quotes keeping its spaces. Enough for a program path and a verb; no backslash rules.</summary>
    private static List<string> Arguments(string commandLine)
    {
        var words = new List<string>();
        var word = new System.Text.StringBuilder();
        var quoted = false;
        var inWord = false;
        foreach (var c in commandLine)
        {
            if (c == '"')
            {
                quoted = !quoted;
                inWord = true;
            }
            else if ((c == ' ' || c == '\t') && !quoted)
            {
                if (inWord)
                {
                    words.Add(word.ToString());
                    word.Clear();
                    inWord = false;
                }
            }
            else
            {
                word.Append(c);
                inWord = true;
            }
        }

        if (inWord)
        {
            words.Add(word.ToString());
        }

        return words;
    }
}

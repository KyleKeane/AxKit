namespace AxKit.Forms;

/// <summary>
/// The navigator's work off the UI thread (docs/axKit/framework.md FW-4.7 point 1, step 3; the maintainer's word of
/// 2026-11-23, "build the common one"): what axFile, axGIT and axProcess each kept in a field of their own. Two
/// rules, and an app takes the one its work needs:
/// <list type="bullet">
/// <item>One piece of work at a time (<see cref="BeginWork"/>, <see cref="EndWork"/>): a command that arrives while
/// another runs is refused with "Working…". axGIT runs every git command this way, which is a rule of its own
/// (one command at a time, AG-8), and axProcess its reads and its changes of a service or a startup entry.</item>
/// <item>A read whose late answer is dropped (<see cref="BeginRead"/>, <see cref="EndRead"/>): a later read may start
/// while an earlier one still runs, and only the latest one's answer is shown. axFile reads a folder this way, so
/// that a second folder chosen while a slow one is read is the one that opens.</item>
/// </list>
/// Either shows as the status bar's state (<see cref="Working"/>, which <see cref="StatusState"/> gives unless the
/// app says more): the app's words for the work, or "Reading…". Nothing is said by itself: the state is read on
/// demand, with the status bar. The work itself, and what is done with its answer, stay the app's.
/// </summary>
public abstract partial class ItemForm
{
    /// <summary>What a read shows as the state while it runs.</summary>
    protected const string ReadingState = "Reading…";

    private string? _working;
    private int _reads;
    private bool _reading;

    /// <summary>What the window is busy with, for the status bar: the work that runs, in the app's words, or <see cref="ReadingState"/> while a read does; null when nothing runs.</summary>
    protected string? Working => _working ?? (_reading ? ReadingState : null);

    /// <summary>True, with "Working…" said, while a piece of work runs that nothing may run beside: the command that asked is refused.</summary>
    protected bool RefusedWhileWorking()
    {
        if (_working is null)
        {
            return false;
        }

        Announce("Working…", true);
        return true;
    }

    /// <summary>
    /// A piece of work that nothing may run beside starts: false, with "Working…" said, while another runs.
    /// <paramref name="what"/> is the state shown meanwhile ("Reading…", "Stopping Print Spooler…"). Every true
    /// answer is followed by one <see cref="EndWork"/>, in a finally.
    /// </summary>
    protected bool BeginWork(string what)
    {
        if (RefusedWhileWorking())
        {
            return false;
        }

        _working = what;
        ShowStatus();
        return true;
    }

    /// <summary>The work of <see cref="BeginWork"/> is over: the state goes back to what the status bar shows otherwise.</summary>
    protected void EndWork()
    {
        _working = null;
        if (!IsDisposed)
        {
            ShowStatus();
        }
    }

    /// <summary>A read starts whose answer counts only while no later read was started: its ticket, for <see cref="EndRead"/>.</summary>
    protected int BeginRead()
    {
        _reading = true;
        ShowStatus();
        return ++_reads;
    }

    /// <summary>
    /// The read of <paramref name="ticket"/> came back, with its answer or with a failure: true when it is still
    /// the latest read and the window lives, so that its answer is shown or its failure said; false for a late
    /// answer, which is dropped without a word, since a later read is the one the user asked for.
    /// </summary>
    protected bool EndRead(int ticket)
    {
        if (ticket != _reads || IsDisposed)
        {
            return false;
        }

        _reading = false;
        ShowStatus();
        return true;
    }
}

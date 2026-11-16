namespace AxAutomate;

/// <summary>
/// One window run at a time on a computer (docs/axKit/user-workflows.md WF-D-29): the runs share the clipboard and the
/// system hotkeys, and several agents may start runs from worktrees of their own in the same hour. A named system
/// mutex; a run that does not get it at once says so, once, and waits. The NVDA tier takes the same one, so no hidden
/// run starts under a pass and no pass starts under a hidden run.
/// </summary>
internal static class RunLock
{
    /// <summary>The mutex every window run takes around its scripts (the build before them runs beside other runs).</summary>
    private const string Name = "AxKit.AxAutomate.WindowRun";

    /// <summary>Said once by a run that has to wait.</summary>
    public const string Waiting = "Waiting for another window run to end: one plays at a time, since the clipboard and the system hotkeys are shared.";

    /// <summary>Said by a run that waited the whole limit; such a run ends as one that could not start.</summary>
    public const string GaveUp = "Another window run did not end within 60 minutes.";

    /// <summary>
    /// The lock, taken on the calling thread, which also releases it (ReleaseMutex, then Dispose); null when another
    /// run still held it after the limit. A mutex abandoned by a run that was ended counts as taken: it is this run's.
    /// </summary>
    public static Mutex? Take(TimeSpan limit, Action<string> say)
    {
        var mutex = new Mutex(false, Name);
        try
        {
            if (mutex.WaitOne(0))
            {
                return mutex;
            }

            say(Waiting);
            if (mutex.WaitOne(limit))
            {
                return mutex;
            }
        }
        catch (AbandonedMutexException)
        {
            // The run that held it ended without releasing it: the wait that threw this owns the mutex.
            return mutex;
        }

        mutex.Dispose();
        return null;
    }
}

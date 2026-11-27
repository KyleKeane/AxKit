namespace AxPass.Core;

/// <summary>
/// What an unlock whose answer came too late may say (docs/axPass/SPEC.md PW-4.3; the review's third pass of
/// 2026-11-27, L1). The window counts the unlocks asked for and the locks (Ctrl+L and Windows' own); an unlock
/// whose answer is dropped says "the window was locked while KeePassXC's tool was answering" only when a lock
/// came since it was asked and no later unlock was asked. Until then a second Unlock pressed while the tool was
/// slow dropped the first answer, which then said the window had been locked though it had not, and its notice
/// stayed over the database the second unlock opened. Pure, tested.
/// </summary>
public static class PassLocks
{
    /// <summary>Whether a dropped unlock says it was dropped by a lock: the locks moved since it was asked (<paramref name="locksThen"/> to <paramref name="locksNow"/>), and it is still the last unlock asked for.</summary>
    public static bool DroppedByLock(int askedThen, int askedNow, int locksThen, int locksNow) =>
        locksThen != locksNow && askedThen == askedNow;
}

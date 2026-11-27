namespace AxProcess.Core;

/// <summary>
/// The decisions and the words of ending a process with the processes it started (SPEC.md PR-3.3; the review of
/// 2026-11-27), pure: whether the tree asked for holds this window itself, which Windows' own end of a tree refuses,
/// and what is said when not every process of a tree could be ended. Until then the first was said as "has already
/// ended", of a process that was running, and the second was an error nobody caught.
/// </summary>
public static class ProcessEnd
{
    /// <summary>
    /// Whether <paramref name="process"/> was started by <paramref name="ancestor"/>, itself or through the
    /// processes between: its parent, that one's parent and so on, each asked of <paramref name="parentOf"/>
    /// (Windows keeps the id of the process that started each one; null where it cannot be asked). An id met twice
    /// ends the walk: Windows gives an ended process's id to a new one, so the ids can run in a circle.
    ///
    /// The id alone does not say who started a process (the review's second pass of 2026-11-27): the one that did
    /// may have ended, and its id gone to a process started later, which then stood as an ancestor and had End
    /// process tree refused on it wrongly. A process is started by one that was there before it, so each step of
    /// the walk asks <paramref name="startedOf"/> for both times, and the walk ends at a parent that started after
    /// its child: that is another process under an old id. Where a time cannot be asked (null) the id counts, the
    /// careful way: End process tree is refused, and End process still ends the one process.
    /// </summary>
    public static bool StartedBy(int process, int ancestor, Func<int, int?> parentOf, Func<int, DateTime?> startedOf)
    {
        var seen = new HashSet<int> { process };
        var child = process;
        for (var at = parentOf(process); at is { } id && seen.Add(id); at = parentOf(id))
        {
            if (startedOf(id) is { } parentStarted && startedOf(child) is { } childStarted && parentStarted > childStarted)
            {
                return false;
            }

            if (id == ancestor)
            {
                return true;
            }

            child = id;
        }

        return false;
    }

    /// <summary>End process tree on a process that started this window: said in place of the question, since ending that tree would end the window that asks.</summary>
    public static string HoldsThisWindow(string name) =>
        $"{name} started this window, so ending it with the processes it started would end axProcess too. End process ends {name} alone";

    /// <summary>
    /// A tree of which Windows could not end every process: how many it refused, with the first reason it gave.
    /// The process itself and the others are ended or not as the list, read again, shows.
    /// </summary>
    public static string NotAllEnded(string name, IReadOnlyList<string> reasons) =>
        reasons.Count == 0
            ? $"Not every process {name} started could be ended"
            : $"Not every process {name} started could be ended: {reasons.Count} could not, {reasons[0].TrimEnd('.')}";
}

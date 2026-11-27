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
    /// </summary>
    public static bool StartedBy(int process, int ancestor, Func<int, int?> parentOf)
    {
        var seen = new HashSet<int> { process };
        for (var at = parentOf(process); at is { } id && seen.Add(id); at = parentOf(id))
        {
            if (id == ancestor)
            {
                return true;
            }
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

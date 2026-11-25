using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// The rules and the words of a command on several selected items (SPEC.md TK-4.7; bundle AX-8.16): what Done makes
/// of tasks that are in different states, and what each command says when it changed many. Pure, tested; the window
/// makes the writes and keeps them as one step for Ctrl+Z.
/// </summary>
public static class ManyItems
{
    /// <summary>
    /// What Ctrl+Enter makes of several tasks: done, while any of them is not; not done only when every one is
    /// done already. So one press never leaves them mixed, and a second press takes the first back.
    /// </summary>
    public static bool DoneNow(IEnumerable<ItemDocument> tasks) => tasks.Any(task => !TaskRules.IsDone(task));

    /// <summary>
    /// "Done: 3 tasks", "Not done: 2 tasks", with what was passed over after it: "Done: 3 tasks. 1 was done
    /// already. 2 selected items are not tasks."
    /// </summary>
    public static string DoneSaid(bool now, int changed, int already, int notTasks)
    {
        var said = $"{(now ? "Done" : "Not done")}: {MenuText.Count(changed, "task")}";
        if (already > 0)
        {
            said += $". {already} {(already == 1 ? "was" : "were")} {(now ? "done" : "not done")} already";
        }

        if (notTasks > 0)
        {
            said += $". {MenuText.Count(notTasks, "selected item")} {(notTasks == 1 ? "is not a task" : "are not tasks")}";
        }

        return said + (already > 0 || notTasks > 0 ? "." : string.Empty);
    }

    /// <summary>"3 items", "1 item".</summary>
    public static string Items(int count) => Marks.Items(count);
}

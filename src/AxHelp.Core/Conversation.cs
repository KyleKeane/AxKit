namespace AxHelp.Core;

/// <summary>What axHelp does about its conversation when a hotkey asks for it (SPEC.md HE-2.1).</summary>
public enum ConversationStep
{
    /// <summary>The window exists, on view or hidden: it is handed "show" or the "message" and brings itself up.</summary>
    HandOver,

    /// <summary>axHelp started the window's process, which still runs, and the window is not there yet: wait for it.</summary>
    Wait,

    /// <summary>No window and no process of axHelp's starting: start the conversation.</summary>
    Start,
}

/// <summary>What axHelp does about its conversation as axHelp itself ends (SPEC.md HE-1.3).</summary>
public enum ConversationEnd
{
    /// <summary>The window exists: it is asked to end, and asks its own question first when something would be lost.</summary>
    AskToEnd,

    /// <summary>The process axHelp started runs and has no window yet: the process is ended, so that nothing is left running unseen.</summary>
    EndProcess,

    /// <summary>There is no conversation.</summary>
    Nothing,
}

/// <summary>
/// One conversation at a time, in every order of start, press and quit (SPEC.md HE-2.1, HE-1.3; the review of
/// 2026-11-15): the two decisions as pure rules over what the window sees, whether the conversation's window exists
/// and whether the process axHelp itself started for it is still running. A wait by the clock could run out on a slow
/// start (the development computer at sign-in) and start a second window; the process's own life cannot.
/// </summary>
public static class Conversation
{
    public static ConversationStep Next(bool windowExists, bool startedProcessRuns) =>
        windowExists ? ConversationStep.HandOver
        : startedProcessRuns ? ConversationStep.Wait
        : ConversationStep.Start;

    public static ConversationEnd AtQuit(bool windowExists, bool startedProcessRuns) =>
        windowExists ? ConversationEnd.AskToEnd
        : startedProcessRuns ? ConversationEnd.EndProcess
        : ConversationEnd.Nothing;
}

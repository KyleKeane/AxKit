using System.Windows.Forms.Automation;

namespace AxKit.Forms;

/// <summary>
/// One UI Automation notification on a control (AX-7.1): NVDA speaks it without a focus change. "Most recent" cancels
/// what is queued, for the result of the user's own key; "all" queues behind it, for status. False when the control has
/// no handle yet, so that the caller can fall back to a selection. Internal, and called by the door alone
/// (<see cref="EventDoor"/>, docs/axKit/events.md section 10, 2026-11-25): words reach NVDA only as the words of an
/// event; the apps cannot name it, and <c>EventDoorTests</c> holds the shared pieces to it.
/// </summary>
internal static class Announcer
{
    /// <summary>Every notification raised, with whether it interrupts: the automation channel's record of what was said (docs/axKit/automation.md).</summary>
    public static event Action<string, bool>? Raised;

    public static bool Raise(Control source, string text, bool interrupt)
    {
        if (!source.IsHandleCreated)
        {
            return false;
        }

        Raised?.Invoke(text, interrupt);
        var processing = interrupt ? AutomationNotificationProcessing.MostRecent : AutomationNotificationProcessing.All;
        return source.AccessibilityObject.RaiseAutomationNotification(AutomationNotificationKind.ActionCompleted, processing, text);
    }
}

using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// What the question area (<see cref="ControlArea"/>) and the notices (<see cref="OverlayPanel"/>) share about a key
/// that answers (docs/axKit/SPEC.md AX-6.4), beside the hold itself (<see cref="AxKit.Core.KeyHold"/>): how a repeat is
/// told from a press, and how a key the hold took is heard.
/// </summary>
internal static class AnswerKeys
{
    /// <summary>Bit 30 of a key-down message's second parameter: the key was down already, so this message is its repeat.</summary>
    private const long WasDown = 1L << 30;

    /// <summary>
    /// Whether a key message is the repeat of a key held down, and no press: Windows sends key-down again and again
    /// while a key stays down, each with the bit that says the key was down before. A repeat of Enter, Escape or
    /// Space never answers a question or a notice, held or not: the first press answered what the user heard, and a
    /// key still down a moment later would answer what came in its place.
    /// </summary>
    public static bool IsRepeat(in Message message) =>
        message.Msg == User32.WM_KEYDOWN && (message.LParam.ToInt64() & WasDown) != 0;

    /// <summary>
    /// A fresh press of an answering key that a hold took is heard: the refusal raised at the door, whose sound is the
    /// kind's own (bundle rule 13; no words, which would cut off NVDA's reading of the question that has just come).
    /// Each press, with no limit: a key that did nothing is never silence.
    /// </summary>
    public static void HeldBack() => EventDoor.Raise(EventKind.Refused);
}

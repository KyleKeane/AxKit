using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// What the question area (<see cref="ControlArea"/>), the notices (<see cref="OverlayPanel"/>) and the field a
/// question gives the focus back to (<see cref="ConversationForm"/>) share about a key that answers
/// (docs/axKit/SPEC.md AX-6.4), beside the hold itself (<see cref="AxKit.Core.KeyHold"/>): how a repeat is told from
/// a press, and how a key the hold took is heard.
/// </summary>
internal static class AnswerKeys
{
    /// <summary>Bit 30 of a key message's second parameter: the key was down already, so this message is its repeat.</summary>
    private const long WasDown = 1L << 30;

    /// <summary>A key pressed while Alt is down, or F10: Windows sends it as a system key.</summary>
    private const int WM_SYSKEYDOWN = 0x0104;

    /// <summary>The character a key press typed; a repeat's character carries the same bit as its key message.</summary>
    private const int WM_CHAR = 0x0102;

    /// <summary>
    /// Whether a key message is the repeat of a key held down, and no press: Windows sends key-down again and again
    /// while a key stays down, each with the bit that says the key was down before, and the character such a message
    /// types carries the bit too. A repeat of a key that answers never answers a question or a notice, held or not:
    /// the first press answered what the user heard, and a key still down a moment later would answer what came in
    /// its place. With Alt down the message is a system key's, which is read the same way (a button's letter).
    /// </summary>
    public static bool IsRepeat(in Message message) =>
        message.Msg is User32.WM_KEYDOWN or WM_SYSKEYDOWN or WM_CHAR && (message.LParam.ToInt64() & WasDown) != 0;

    /// <summary>
    /// Whether a key of the user's can answer no notice, so that a notice it opens may take its keys at once
    /// (<see cref="Notice.AsShown"/>; the fifth review of 2026-11-27, E3): a key with Ctrl or Alt, or a function key.
    /// Not Ctrl+Enter, the main action of every notice, and not a letter or a digit with Alt (Shift or not), which
    /// presses the notice's button of that letter (<see cref="OverlayPanel"/>'s answering keys). Enter, Escape, Space, a letter, a
    /// digit and every other plain key can: a notice such a key opens holds its keys for its moment, whatever its
    /// maker said, so that a doubled press answers nothing unheard.
    /// </summary>
    public static bool CannotAnswer(Keys key)
    {
        var code = key & Keys.KeyCode;
        var modifiers = key & Keys.Modifiers;
        if (key == BundleKeys.MainAction)
        {
            return false;
        }

        if ((modifiers & ~Keys.Shift) == Keys.Alt && code is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
        {
            return false;
        }

        return (modifiers & (Keys.Control | Keys.Alt)) != 0 || code is >= Keys.F1 and <= Keys.F24;
    }

    /// <summary>
    /// A fresh press of an answering key that a hold took is heard: the refusal raised at the door, whose sound is the
    /// kind's own (bundle rule 13; no words, which would cut off NVDA's reading of the question that has just come).
    /// Each press, with no limit: a key that did nothing is never silence.
    /// </summary>
    public static void HeldBack() => EventDoor.Raise(EventKind.Refused);
}

namespace AxKit.Core;

/// <summary>
/// The hold on the keys that answer (docs/axKit/SPEC.md AX-6.4, "A question that arrives by itself"): for a moment
/// after a question or a notice appears that the user did not open with a command of their own, Enter, Escape, Space
/// and typed keys do nothing in it. The user heard nothing of it yet, and a key that reaches it then was meant for
/// what stood there before: a message being typed, or the question just answered, whose Enter was pressed twice.
/// The one rule, for the question area (<c>AxKit.Forms/ControlArea</c>) and the notices
/// (<c>AxKit.Forms/OverlayPanel</c>) alike, and the one place it is written:
///
/// 1. What appears by itself holds its answering keys for <see cref="DefaultMs"/> from the moment it is shown,
///    whatever came before. A question that follows the user's own answer arrives by itself too: that the user acted
///    a moment ago says nothing about the new question, which they have not heard (the review of 2026-11-27: the
///    second of two files to create was approved by a doubled Enter before a word of it was said). Whether the user
///    acted lately may decide whether a chime plays, never whether the keys are held.
/// 2. A hold that runs is never shortened. There is no way to end one early: <see cref="Start"/> only ever moves the
///    end later. In the question area each control has a hold of its own, new with the control: its moment from when
///    it is shown, and, once its own answer has gone, as long as the window says (three seconds in axClaude, so that
///    a second Enter does not send the answer twice). The control shown next inherits nothing of it: what protects a
///    control is its own moment, and what was left of the answer's seconds only kept the user waiting at a list they
///    had heard (tried for an hour on 2026-11-27, and taken out).
///
/// Two rules stand beside the hold, in <c>AxKit.Forms/AnswerKeys</c> for both panels: a repeat of Enter, Escape or
/// Space (the key was down already) never answers, held or not, so that a key held down longer than any hold answers
/// nothing that came meanwhile; and a fresh press that a hold takes is heard, as the kind of event Refused.
///
/// Every question of axKit is to become a list that starts on its confirming answer (the maintainer's decision of
/// 2026-11-27, "Enter is always go forward, Escape is go back"): this hold is then the one thing between a doubled or
/// typed-ahead Enter and a yes the user never heard, so nothing is added here that lets a caller switch it off.
/// The clock is handed in (the milliseconds of <see cref="Environment.TickCount64"/> in the windows), so that the
/// rule is tested with a clock of the test's own (<c>KeyHoldTests</c>). In <c>AxKit.Forms</c> until 2026-11-27, with a
/// <c>Clear</c> that the question area called for every control "the user caused".
/// </summary>
public sealed class KeyHold
{
    /// <summary>The moment the answering keys of what appeared by itself do nothing, in milliseconds.</summary>
    public const int DefaultMs = 1000;

    private long _until;

    /// <summary>
    /// The keys do nothing until <paramref name="milliseconds"/> after <paramref name="now"/>, or until the end of a
    /// hold that already runs longer than that: a hold is only ever made longer.
    /// </summary>
    public void Start(long now, int milliseconds = DefaultMs) => _until = Math.Max(_until, now + milliseconds);

    /// <summary>Whether the keys are held at <paramref name="now"/>.</summary>
    public bool Holding(long now) => now < _until;
}

namespace AxClaude.Core.Transcript;

/// <summary>
/// Whether what the control area shows plays its chime as it opens (FR-7.3, FR-2.12). A question or a screen of
/// Claude's that the user did not cause plays the notice chime, unless the window is in the background with the louder
/// alert on: the signal then plays the alert in its place. An offered command (a command Claude offers, or an AI
/// tool's request to run one) has a chime of its own and no signal, so its chime plays wherever the window is: what
/// asks to run code must be known at once anywhere. Until 2026-11-16 the offer followed the notice's rule, and an
/// offer shown in the background with the alert on made no sound at all. Pure, so that the rule is tested without a
/// window.
/// </summary>
public static class AreaChime
{
    /// <summary>
    /// True when the chime plays: only for what the user did not cause (<paramref name="unprompted"/>), only with the
    /// chimes on; for an <paramref name="offer"/> then always, for anything else not while the louder alert is on and
    /// the window does not hold the focus.
    /// </summary>
    public static bool Plays(bool unprompted, bool chimesOn, bool backgroundAlert, bool windowHasFocus, bool offer) =>
        unprompted && chimesOn && (offer || !backgroundAlert || windowHasFocus);
}

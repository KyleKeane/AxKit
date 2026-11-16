namespace AxKit.Core;

/// <summary>
/// The words of a menu item or a button as they are spoken or searched (docs/axKit/SPEC.md AX-6.4, AX-10.2): the
/// text without its mnemonic marker (a doubled ampersand stays one) and without a trailing ellipsis or full stop,
/// so that "Save &amp;as..." is "Save as". The palette's entries, a notice's key line and a menu choice's announcement
/// all read a text this way; until 2026-10-23 four places stripped the marker themselves.
/// </summary>
public static class MenuText
{
    public static string Plain(string text) =>
        text.Replace("&&", "\0").Replace("&", string.Empty).Replace("\0", "&").TrimEnd('.', '…').Trim();

    /// <summary>
    /// What a check item says after it was chosen where no menu shows its new state (AX-10.2,
    /// docs/axKit/command-language.md CL-4.7): "Word wrap, checked", "Word wrap, not checked". The one place for the
    /// words: the command palette and the automation channel's choose both say it through the window base's
    /// <c>ClickFor</c>.
    /// </summary>
    public static string CheckSaid(string name, bool isChecked) => $"{name}, {(isChecked ? "checked" : "not checked")}";
}

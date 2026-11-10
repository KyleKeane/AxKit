using System.Text;

namespace AxKit.Core;

/// <summary>
/// The start of every window's F1 text (docs/axKit/app-kit.md AK-2.3, bundle AX-7.2, AX-8.2): the first line naming
/// the text's own keys, then the section of the keys every window of axKit has, with the app's own lines in it, written
/// once here. Each app's <c>HelpText.Shortcuts</c> is this followed by its own sections.
/// </summary>
public static class BundleHelp
{
    /// <summary>The heading of the bundle's section in most windows.</summary>
    public const string InEveryWindow = "In every window of axKit";

    /// <summary>The axKit menu's line, the same in every F1 text (AX-9; <c>KeyTests</c> checks it whole).</summary>
    public const string AxKitMenuLine = "Alt+A: the axKit menu: the command palette, a new window of any app, the files and folders closed lately, the open axKit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, AI help, the AI tools, Check for updates, and About";

    /// <summary>
    /// The first line and the bundle's section, ending in a blank line so that the app's own sections follow. In the
    /// section: F1, the command palette, the app's own <paramref name="lines"/> in their order (what Tab moves between,
    /// what Ctrl+F finds, what Ctrl+S does, and in axClaude and axTerminal all their window keys), the text size, the
    /// menu with Alt+F4, the axKit menu. Lines are CRLF, as the notice's edit control shows them.
    /// </summary>
    /// <param name="app">The app's name, as in "Alt+F4: close axCalc".</param>
    /// <param name="lines">The app's lines of the section, without their indent.</param>
    /// <param name="altF4">What Alt+F4 does, when it is more than "close" and the app's name.</param>
    /// <param name="heading">The section's heading: axClaude and axTerminal call theirs "Anywhere".</param>
    /// <param name="navigateMenu">Whether the first line says that the Navigate menu's Next and Previous keys work in the text too (the editors with that menu).</param>
    public static string Section(string app, string[] lines, string? altF4 = null, string heading = InEveryWindow, bool navigateMenu = false)
    {
        var text = new StringBuilder();
        text.Append("In this text: h and Shift+H move by heading, 1 to 6 by level");
        if (navigateMenu)
        {
            text.Append("; the Next and Previous keys of the Navigate menu work here too");
        }

        text.Append(". Ctrl+F finds, F3 and Shift+F3 the next and the previous match\r\n");
        // AX-11.2: an access layer says what it is before its keys.
        if (Dispatch.ByName(app) is { IsAccessLayer: true } layer)
        {
            text.Append($"{layer.Title}: {layer.Name} only shows what {layer.AccessLayerFor} does and passes your commands to it; {layer.AccessLayerFor} does the work under its maker's terms, which are yours to follow. axKit menu, About, Licence has the reminders\r\n");
        }

        text.Append("\r\n");
        text.Append("## ").Append(heading).Append("\r\n");
        text.Append("  F1: this list\r\n");
        text.Append("  Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it\r\n");
        text.Append("  F4: report a bug, an idea or feedback to axKit's maintainer: type your words and press Enter; Tab reviews the kind and what the window adds before you send\r\n");
        text.Append("  Shift+F1: ask Claude about this window, once AI help is on (axKit menu, AI help): type your question and press Enter; the answer is a notice, with a field under it to ask more\r\n");
        foreach (var line in lines)
        {
            text.Append("  ").Append(line).Append("\r\n");
        }

        text.Append("  Ctrl+Plus, Ctrl+Minus: bigger or smaller text\r\n");
        text.Append("  Alt: the menu. Alt+F4: ").Append(altF4 ?? "close " + app).Append("\r\n");
        text.Append("  ").Append(AxKitMenuLine).Append("\r\n");
        text.Append("\r\n");
        return text.ToString();
    }

    /// <summary>
    /// The Notices section every F1 text ends with (AX-7.2; the same words in every window since 2026-11-09, when five
    /// texts had a shorter one or none): where the app's notices appear, and a notice's keys. It starts with the blank
    /// line that parts it from the app's last section and ends without a line break, as the texts do.
    /// </summary>
    /// <param name="app">The app's name.</param>
    /// <param name="inPlaceOf">What a notice covers: "the conversation", "the text", "the lists".</param>
    /// <param name="navigateMenu">Whether the Navigate menu's keys work in a notice's text too (the editors with that menu).</param>
    public static string NoticesSection(string app, string inPlaceOf, bool navigateMenu = false) =>
        "\r\n\r\n## Notices\r\n"
        + $"  {app}'s questions, errors and help texts appear inside the window, in place of {inPlaceOf}.\r\n"
        + $"  Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6{(navigateMenu ? ", the Navigate keys" : string.Empty)} and Ctrl+F work as in this one";
}

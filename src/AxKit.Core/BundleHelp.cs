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

    /// <summary>The bundle's menus in one line, the same in every F1 text (AX-9.1: Window, axKit, Help; <c>KeyTests</c> checks it whole).</summary>
    public const string BundleMenusLine = "Alt+W: the Window menu: a new window of any app, the open axKit windows, most recent first (Alt+W, O, 1 is the one you were in last), the files and folders closed lately, Close all windows, Say the status. Alt+A: the axKit menu: Claude (Use Claude in axKit, Ask about this window, the folders Claude may read, Claude's working folder), the AI tools and the theme. Alt+H: Help: the command palette, this list, the user guide, Send a report, Your reports, Diagnostics (Test a key, Copy diagnostics), Check for updates, and About";

    /// <summary>F4's line, the same in every F1 text: the report is a form of standard controls (AX-8.10, reports.md RP-3).</summary>
    public const string ReportLine = "  F4: report a bug, an idea or feedback to axKit's maintainer: type your words, Enter makes a new line; Tab goes on to the kind, a box for more detail and what is sent with your report; Ctrl+Enter or the Send button sends; Escape cancels and keeps nothing. The report is encrypted on your computer, and only the maintainer can read it";

    /// <summary>Ctrl+Enter's line, the same in every F1 text (AX-8.10 rule 3).</summary>
    public const string MainActionLine = "  Ctrl+Enter: the main action of where you are: it sends a form from any of its fields, keeps a long text from inside its box, presses a notice's main button, and does a window's main action where it has one; anywhere else, the edge sound and nothing more";

    /// <summary>The mode key's line, the same in every F1 text (AX-8.13, 2026-11-15).</summary>
    public const string ModeLine = "  Ctrl+Space in a text of several lines that you can type in: navigation mode or edit mode, said each time. In navigation mode the letters move without typing: h and Shift+H by heading, 1 to 6 by level, and the other letters of the window's Ctrl jumps without Ctrl (k a link, l a list, i a list item in Markdown); the arrows still move and select, Ctrl+C copies, and a key that would change the text plays the edge sound. Each letter has its sound: a high tick when it moves, a low buzz when there is nothing of its kind to go to, a wooden bonk when the key has no command. In a text you cannot type in, Ctrl+Space says it is read only";

    /// <summary>The key test's line, the same in every F1 text (AX-8.12, 2026-11-15).</summary>
    public const string KeyTestLine = "  A key that seems to do nothing: Help, Diagnostics, Test a key says each key you press and what it does here, and carries none of them out; Escape ends it. A key that is not said never reached axKit";

    /// <summary>The entry sounds and a form's words, the same in every F1 text (AX-7.11, 2026-11-22).</summary>
    public const string NoticeLine = "  Whenever the focus enters a list, a text, a field or a form, a sound of its kind plays first, bell notes that run up and then go down: one long tone for a field to type in, a lower one for a text to read, four quick beeps for a list, beeps and tones for a form. Moving inside it is silent. A form says its title and the word form; Ctrl+Q reads its explanation, as it reads the text of any question or notice again. Options, Text, Entry sounds turns the sounds off for every window";

    /// <summary>F8's line, the same in every F1 text (AX-7.10, 2026-11-15).</summary>
    public const string StatusLine = "  F8: say the status, what the status bar at the bottom of the window holds, said by axKit itself; in a notice too";

    /// <summary>The app keys (AX-8.18 rule 6), in every window's F1 text; the Window menu's Apps and Access layers show each app's own.</summary>
    public static string AppKeysLine { get; } =
        "  Ctrl with a function key: open an axKit app, each group on its block of four keys: " + Blocks(shift: false)
        + ". Ctrl+Shift with a function key: open an access layer: " + Blocks(shift: true)
        + ". Alt+W, A lists the apps and Alt+W, L the access layers, each with its key";

    /// <summary>"F1 to F4 getting around, axStart, axFile, ...; F5 to F8 ...": the blocks of one kind, from <see cref="AppLaunchKeys"/>.</summary>
    private static string Blocks(bool shift) => string.Join("; ", AppLaunchKeys.Default.Where(key => key.Shift == shift).GroupBy(key => key.App.Group).Select(group =>
    {
        var first = ((group.First().Number - 1) / AppLaunchKeys.Block * AppLaunchKeys.Block) + 1;
        return $"F{first} to F{first + AppLaunchKeys.Block - 1} {Dispatch.GroupName(group.Key)}, " + string.Join(", ", group.Select(key => key.App.Name));
    }));

    /// <summary>
    /// The first line and the bundle's section, ending in a blank line so that the app's own sections follow. In the
    /// section: F1, the command palette, the app's own <paramref name="lines"/> in their order (what Tab moves between,
    /// what Ctrl+F finds, what Ctrl+S does, and in axClaude and axTerminal all their window keys), the text size, the
    /// menu with Alt+F4, the bundle's menus. Lines are CRLF, as the notice's edit control shows them.
    /// </summary>
    /// <param name="app">The app's name, as in "Alt+F4: close axCalc".</param>
    /// <param name="lines">The app's lines of the section, without their indent.</param>
    /// <param name="altF4">What Alt+F4 does, when it is more than "close" and the app's name.</param>
    /// <param name="heading">The section's heading: axClaude and axTerminal call theirs "Anywhere".</param>
    /// <param name="navigateMenu">Whether the first line says that the Navigation menu's Next and Previous keys work in the text too (the editors with that menu).</param>
    public static string Section(string app, string[] lines, string? altF4 = null, string heading = InEveryWindow, bool navigateMenu = false)
    {
        var text = new StringBuilder();
        text.Append("In this text: h and Shift+H move by heading, 1 to 6 by level");
        if (navigateMenu)
        {
            text.Append("; the Next and Previous keys of the Navigation menu work here too");
        }

        text.Append(". Ctrl+F finds, F3 and Shift+F3 the next and the previous match\r\n");
        // AX-11.2: an access layer says what it is before its keys.
        if (Dispatch.ByName(app) is { IsAccessLayer: true } layer)
        {
            text.Append($"{layer.Title}: {layer.Name} only shows what {layer.AccessLayerFor} does and passes your commands to it; {layer.AccessLayerFor} does the work under its maker's terms, which are yours to follow. Help, About, Licence has the reminders\r\n");
        }

        text.Append("\r\n");
        text.Append("## ").Append(heading).Append("\r\n");
        text.Append("  F1: this list\r\n");
        text.Append("  Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it\r\n");
        text.Append(ReportLine).Append("\r\n");
        text.Append(MainActionLine).Append("\r\n");
        text.Append(ModeLine).Append("\r\n");
        text.Append("  Shift+F1 or Ctrl+Slash: ask Claude about this window: type your question and press Enter; the first time, a reminder asks you to turn Claude on; the answer is a notice, with a field under it to ask more\r\n");
        text.Append(StatusLine).Append("\r\n");
        text.Append(AppKeysLine).Append("\r\n");
        foreach (var line in lines)
        {
            text.Append("  ").Append(line).Append("\r\n");
        }

        text.Append("  Ctrl+Plus, Ctrl+Minus: bigger or smaller text\r\n");
        // AX-7.8 (2026-11-11): NVDA says nothing for Enter, so every field that takes a line break says it.
        text.Append("  A line break typed into a field of several lines is said, new line; Options, Text, Say new line turns that off for every window\r\n");
        text.Append("  A key that lands on a heading, in a list or a text, plays a short rising sound before it is read; Options, Text, Sound on headings turns that off for every window\r\n");
        // AX-7.11 (2026-11-22): what opens says its kind with a sound, and a form its title and one word.
        text.Append(NoticeLine).Append("\r\n");
        // AX-9.11 (2026-11-11): one Context menu in every window, opened the same way everywhere.
        text.Append("  Shift+F10, the Applications key or Ctrl+Period: the Context menu, what acts on what you are on\r\n");
        text.Append(KeyTestLine).Append("\r\n");
        text.Append("  Alt: the menu. Alt+F4: ").Append(altF4 ?? "close " + app).Append("\r\n");
        text.Append("  ").Append(BundleMenusLine).Append("\r\n");
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
    /// <param name="navigateMenu">Whether the Navigation menu's keys work in a notice's text too (the editors with that menu).</param>
    /// <summary>
    /// The Filter and sort section of every navigator's F1 text (docs/axKit/framework.md FW-4.10a, 2026-11-22): the
    /// same words in each, since the two are the item window's, the same in each. It starts with the blank line
    /// that parts it from the section before and ends without a line break, as <see cref="NoticesSection"/> does,
    /// which follows it.
    /// </summary>
    /// <param name="orders">The orders the list can be put in, as the menu names them: "Name, Date or Size"; null for a list with no Sort by.</param>
    public static string FilterAndSortSection(string? orders = "The list's own order or Name") =>
        "\r\n\r\n## Filter and sort\r\n"
        + "  Options, Filter: only what has a text in its name is listed, and the list's name says so, \"24 items, filter chap, 3 shown\". An empty text clears the filter, and so does Escape in the list once nothing is selected. Going to another list drops it"
        + (orders is null ? string.Empty : $"\r\n  Options, Sort by: {orders}");

    public static string NoticesSection(string app, string inPlaceOf, bool navigateMenu = false) =>
        "\r\n\r\n## Notices\r\n"
        + $"  {app}'s questions, errors and help texts appear inside the window, in place of {inPlaceOf}.\r\n"
        + $"  Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6{(navigateMenu ? ", the Navigation keys" : string.Empty)} and Ctrl+F work as in this one";
}

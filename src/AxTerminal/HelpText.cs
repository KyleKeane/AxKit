using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxTerminal;

/// <summary>The texts the window shows as notices: the keyboard shortcuts (F1) and the user guide.</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AT-6.2): a first line naming the text's own keys, then one Markdown heading per group, so that the heading letters walk it (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context, and the terminal's letters, which no command carries.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axTerminal",
        [
            "Alt+1 and Alt+2, the command field and the terminal, say where you are, also when you are there already. Ctrl+1, Ctrl+2 and Ctrl+3, the keys of the three places until now, still work.",
            "Tab, Shift+Tab or F6 moves between the command field, or the recent commands list in its place, and the terminal.",
            "Interrupt sends Ctrl+C to the shell.",
            "Recent commands shows the recent commands of this window as a list; Enter puts one back in the field.",
            "What is running says what runs, for how long, and the prompt; the shell too while idle.",
            "New session asks for the folder and the shell: Windows PowerShell, Command Prompt, PowerShell 7 and Git Bash when they are installed, or a command line of your own; Session offers to install the two that are not. Session, Switch shell changes the shell in the same folder and keeps the text.",
            $"Session, Add a shell adds a shell of your own, in one form: a name and the command line that starts it, Tab between them; Enter or {BundleKeys.Text(BundleKeys.MainAction)} adds it. It is listed in Switch shell and the New session notice, and Session, Remove a shell takes it off; removing the shell in use starts the default shell in its place.",
            "Restart shell starts the shell again in its current folder, after a confirmation while a command runs.",
            "Clear transcript clears axTerminal's text; the shell's own cls clears only its screen.",
            "Ctrl+Alt and a letter of the Terminal section below reads every line of that kind from the caret to the end of the terminal in one go, without moving; Ctrl+Alt+P reads the paragraphs whole. Control stops NVDA.",
            "Background music says whether it is on or off.",
        ],
        altF4: "close axTerminal (asks first while a command runs)",
        heading: "Anywhere") + $"""
        ## Command field
          Enter sends the command; in an empty field it sends a plain Enter to the shell. A command of several lines goes line by line, with one more Enter at the end for PowerShell.
          Actions, Send without recording, which has no key and which the command palette finds, sends the text with no Input and Output lines, not into the recent commands and not into the log. It is for passphrases and tokens a program asks for.
          Shift+Enter makes a new line.
          Page Up and Page Down move one screen of the field, or to the first or the last line.
          Escape does nothing; a reminder says that {BundleKeys.Text(AxTerminalKeys.SendEscape)} sends Escape and {BundleKeys.Text(AxTerminalKeys.Interrupt)} interrupts.
          {BundleKeys.Text(AxTerminalKeys.OlderCommand)} and {BundleKeys.Text(AxTerminalKeys.NewerCommand)} bring back the previous and the next command, then what you typed or last changed. With Shift, they select to the previous or the next paragraph, and in navigation mode they go by paragraph.

        ## Terminal
          Arrow keys, Home, End, Ctrl+Home and Ctrl+End move as in any text. Page Up and Page Down move one screen.
          {BundleKeys.Text(BundleKeys.ParagraphUp)} and {BundleKeys.Text(BundleKeys.ParagraphDown)} go to the previous or the next paragraph, the first line after a blank line; with Shift, they select to there.
          l: say the line number, like "Line 12 of 340".
          i and Shift+I: the next and the previous "# Input" line, a command you sent.
          o and Shift+O: the next and the previous "# Output" line, what the shell printed for it.
          h and Shift+H: the next and the previous heading. 1 to 6: a heading of that level; Input and Output lines are level 1. Both work in the user guide and the other help texts too, by their Markdown headings.
          p and Shift+P: the next and the previous paragraph, the first line after a blank line.
          s and Shift+S: the next and the previous system line.
          m bookmarks the line, and a "Bookmark 1" line appears above it; m again takes the bookmark away. k and Shift+K read the next and the previous bookmark together with the line it marks.
          Enter copies the line into the command field and goes there, to run or edit it.
          Backspace goes back to the line you jumped from.
          Shift with the arrows selects, and a copy says "Copied".
          Escape clears the selection, or else goes to the command field.

        ## A full-screen program
          While a program such as Windows' edit takes the whole screen, its screen is shown in place of the terminal and the field, "Screen of edit", and the field comes back when it ends.
          {BundleKeys.Text(BundleKeys.ToggleMode)} turns between keys to the program and reading the screen; the status bar says which, "Keys to edit" or "Reading the screen".
          Keys to the program: every key goes to the program as a terminal sends it, but {BundleKeys.Text(BundleKeys.ToggleMode)}, {BundleKeys.Text(BundleKeys.Shortcuts)} and the key that closes the window; the caret stands on the program's cursor. Alt alone opens axTerminal's menus.
          Reading the screen: the keys read the screen, and axTerminal's keys work; nothing reaches the program.
          An open menu's selected entry, or a dialog's button in focus, is said once when it moves.
        """ + BundleHelp.NoticesSection("axTerminal", "the terminal");

    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axTerminal/user-guide.md");
}

using System.Reflection;
using AxClaude.Core.Pty;
using AxKit.Core;
using AxKit.Forms;

namespace AxClaude;

/// <summary>The texts the window shows as notices: the keyboard shortcuts (F1), the user guide, and the help when Claude Code is missing.</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md §6.3): a first line naming the text's own keys, then one Markdown heading per group, so that the heading letters walk it (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context, and the conversation's letters, which no command carries.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axClaude",
        [
            "Alt+1 and Alt+2, the message field and the conversation, say where you are, also when you are there already.",
            $"Tab, Shift+Tab or F6 moves between the message field, or Claude's question in its place, and the conversation.",
            $"{BundleKeys.Text(AxClaudeKeys.Interrupt)} interrupts Claude, or closes one of Claude's dialogs.",
            $"{BundleKeys.Text(AxClaudeKeys.SendNow)} makes Claude take up the waiting message now instead of after its current step.",
            "New session asks for the folder and how Claude starts: continue, resume, plan mode or your own arguments.",
            "Force Claude to restart starts Claude again in the same folder with the same arguments, after a confirmation.",
            $"{BundleKeys.Text(AxClaudeKeys.CtrlOGuard)} is switched off, and a reminder says why: Claude's detailed view redraws the conversation in screen reader mode.",
            "Ctrl+Alt and a letter of the Conversation section below reads every line of that kind from the caret to the end of the conversation in one go, without moving; Ctrl+Alt+P reads the paragraphs whole. Control stops NVDA.",
            "Background music says whether it is on or off.",
            $"What runs in the background says how many things run, each agent's type and what it does, each workflow with how far it is, and the shells. Claude Code's rows about it are never spoken, and stay out of the conversation unless you show them (Options, Background work). The status bar ({BundleKeys.Text(BundleKeys.SayStatus)}) says the number. Three quick notes going up say that background work started, the same three going down that it ended, and while it runs a slow soft chord with a long fading tail comes again and again with a short pause between, a different chord each time, fuller with each thing running (Options, Background work).",
            "Claude's question again reads Claude's question or screen while it waits at the bottom, all its lines and the answer the focus is on, without moving.",
        ],
        altF4: "close axClaude (asks first while Claude is working); in axHelp's conversation it hides the window and Claude runs on, and Session, End Claude and close this window ends it",
        heading: "Anywhere") + $"""
        ## Message field
          Enter sends the message; in an empty field it sends a plain Enter to Claude.
          Shift+Enter makes a new line.
          Page Up and Page Down move one screen of the field, or to the first or the last line of the message.
          Escape does nothing; a reminder says that {BundleKeys.Text(AxClaudeKeys.Interrupt)} interrupts Claude.
          {BundleKeys.Text(BundleKeys.ParagraphUp)} and {BundleKeys.Text(BundleKeys.ParagraphDown)} go to the previous or the next paragraph of the message; with Shift, they select to there.

        ## Conversation
          Arrow keys, Home, End, Ctrl+Home and Ctrl+End move as in any text. Page Up and Page Down move one screen.
          {BundleKeys.Text(BundleKeys.ParagraphUp)} and {BundleKeys.Text(BundleKeys.ParagraphDown)} go to the previous or the next paragraph, the first line after a blank line; with Shift, they select to there.
          l: say the line number, like "Line 12 of 340".
          i and Shift+I: the next and the previous "# Input" line, a message you sent.
          o and Shift+O, or r and Shift+R: the next and the previous "# Output" line, a reply from Claude.
          h and Shift+H: the next and the previous heading. 1 to 6: a heading of that level. Input and Output lines are level 1, Claude's headings level 2 and deeper. Both work in the user guide and the other help texts too, by their Markdown headings.
          c and Shift+C: the next and the previous "claude:" line.
          t and Shift+T: the next and the previous tool line.
          p and Shift+P: the next and the previous paragraph: the first line after a blank line, or a "claude:" line, never a tool or thinking line.
          e and Shift+E: the next and the previous error or warning.
          d and Shift+D: the next and the previous "done" line at the end of a reply.
          s and Shift+S: the next and the previous system line.
          q and Shift+Q: the next and the previous question Claude asked, its "Question:" line.
          b and Shift+B: the next and the previous side question you asked with /btw, its "btw:" line.
          m bookmarks the line, and a "Bookmark 1" line appears above it; m again takes the bookmark away. k and Shift+K read the next and the previous bookmark together with the line it marks.
          Enter copies the line into the message field under a marker line and goes there, so that you can comment on it.
          Backspace goes back to the line you were on before the last jump; again, to the jump before that.
          Ctrl+End, then Shift and a letter, goes to the last line of that kind.
          Shift with the arrow keys, or Ctrl+A, selects text. New output waits while text is selected.
          Escape clears the selection, or goes to the message field.

        ## When Claude asks something
          A list of the answers takes the message field's place; the question is read each time you arrive there.
          Tab, Shift+Tab or F6 moves between the conversation and the answers.
          Up, Down, or an answer's number chooses. The number again goes to the next answer starting with it, Shift and the number back, and 0 to answers 1 to 9.
          Any other letter typed reads the whole question again. A permission question's answers each end with its tool and target.
          Page Up and Page Down move ten answers, Home and End to the first or the last. Space ticks an answer when several are allowed.
          Enter answers. Escape cancels the question, or goes back a step.
          The message field comes back when Claude has your answer.

        ## Claude Code itself
          Type /help in the message field and press Enter for Claude Code's own commands.
          The Help menu opens the Claude Code documentation in your browser.
        """ + BundleHelp.NoticesSection("axClaude", "the conversation");

    /// <summary>docs/axClaude/user-guide.md, embedded in the executable at build time.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axClaude/user-guide.md");

    /// <summary>FR-1.9: where the app looked, the install command, and the ways back.</summary>
    public static string ClaudeNotFound(IReadOnlyList<string> candidates)
    {
        var lines = new List<string>
        {
            "Claude Code was not found, so axClaude cannot start it.",
            string.Empty,
            "axClaude looked here:",
        };
        lines.AddRange(candidates.Select(c => "  " + c));
        lines.AddRange(
        [
            string.Empty,
            "Install Claude Code now runs Claude Code's own installer, for your account only; axClaude then starts it, and Claude Code asks you to log in. Or install it by hand: open Windows PowerShell and run this command (the Copy install command button copies it):",
            "  " + ClaudeLauncher.InstallCommand,
            string.Empty,
            "Then run claude once in PowerShell to log in. Come back here, close this notice and press Ctrl+Shift+R.",
            string.Empty,
            "If Claude Code is installed somewhere else, use Locate claude.exe. axClaude remembers the path.",
            string.Empty,
            "Install page: " + ClaudeLauncher.InstallUrl,
        ]);
        return string.Join("\n", lines);
    }
}

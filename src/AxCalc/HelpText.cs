using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxCalc;

/// <summary>The texts axCalc shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AC-7.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axCalc's own, what an expression takes. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axCalc",
        [
            $"Tab, Shift+Tab, {BundleKeys.Text(BundleKeys.NextControl)} or {BundleKeys.Text(BundleKeys.NextControlAlso)}: switch between the history and the expression field",
            $"{BundleKeys.Text(BundleKeys.Find)}: find text in the history. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
            $"{BundleKeys.Text(BundleKeys.Save)}: save the history as a text file",
        ]) + $"""
        ## axCalc
          In the expression field
            Enter: the answer, spoken; the line joins the history and the field empties
            Escape: clear the field
          In the history, a text with one line for each answer, the newest last, read as the conversation in axClaude and the terminal in axTerminal
            Up, Down: one line at a time. a, Shift+A: the next or the previous answer. s, Shift+S: the next or the previous line of axCalc's own. l: the line number
            Enter: the line's expression back in the field, to change it
            Escape: back to the field
            m: bookmark this line, or take its bookmark away; {BundleKeys.Text(AxCalcKeys.BookmarkLine)} does it from the field too. k, Shift+K: the next or the previous bookmark. Backspace: back to the line before a jump
            Ctrl+Alt with a, s or k, from anywhere: every line of that kind from the line you are on to the end
            Shift with the arrows selects; {BundleKeys.Text(AxCalcKeys.CopySelection)} copies the selection
            Shift+F10, the Applications key or {BundleKeys.Text(BundleKeys.ContextMenu)}: the Context menu, what acts on the line you are on
          {BundleKeys.Text(AxCalcKeys.CopyAnswer)}: copy the answer (the last one, or the line's in the history)
          {BundleKeys.Text(AxCalcKeys.OtherBases)}: the answer in hexadecimal, binary and octal
          {BundleKeys.Text(AxCalcKeys.LastAnswer)}: say the last answer again
          {BundleKeys.Text(AxCalcKeys.ClearHistory)}: clear the history, after a question

        ## Expressions
          + - * / as usual, % or mod for the remainder, ^ or ** for a power, ! for a factorial, parentheses to group
          0xFF, 0b1010 and 0o17 for hexadecimal, binary and octal; 1_000_000 with underscores; 6.02e23
          Functions: sqrt, cbrt, abs, exp, ln, log, log2, sin, cos, tan, asin, acos, atan, floor, ceil, trunc, round, min, max, hypot
          Names: pi, e, ans (the last answer); name = 5 sets a variable
          Starting with an operator continues from the last answer: *2 doubles it. A leading minus is a negative number
        """ + BundleHelp.NoticesSection("axCalc", "the field and the history");

    /// <summary>docs/axCalc/user-guide.md, embedded by AxCalc.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axCalc/user-guide.md");
}

using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxCalc;

/// <summary>The texts axCalc shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AC-7.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axCalc's own, what an expression takes. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axCalc", []) + $"""
        ## axCalc
          The window has the history, a text with one line for each answer, the newest last, and the expression field under it. The history reads as the conversation in axClaude and the terminal in axTerminal.
          In the expression field, Enter says the answer; the line joins the history and the field empties. {BundleKeys.Text(AxCalcKeys.ClearField)} empties the field.
          In the history
            Up and Down read one line at a time. a and Shift+A go to the next or the previous answer, s and Shift+S to the next or the previous line of axCalc's own, and l says the line number.
            Escape goes back to the field.
            m bookmarks the line, and on a bookmarked line takes the bookmark away; {BundleKeys.Text(AxCalcKeys.BookmarkLine)} does the same from the field too. Backspace goes back to the line before a jump.
            Ctrl+Alt with a, s or k, from anywhere, reads every line of that kind from the line you are on to the end.
            Shift with the arrows selects.
          Copy answer copies the last answer, or in the history the answer of the line you are on.
          Answer in other bases says the answer in hexadecimal, binary and octal.
          Clear history asks first.

        ## Expressions
          + - * / as usual, % or mod for the remainder, ^ or ** for a power, ! for a factorial, parentheses to group.
          0xFF, 0b1010 and 0o17 for hexadecimal, binary and octal; 1_000_000 with underscores; 6.02e23.
          Functions: sqrt, cbrt, abs, exp, ln, log, log2, sin, cos, tan, asin, acos, atan, floor, ceil, trunc, round, min, max, hypot.
          Names: pi, e, ans (the last answer); name = 5 sets a variable.
          Starting with an operator continues from the last answer: *2 doubles it. A leading minus is a negative number.
        """ + BundleHelp.NoticesSection("axCalc", "the field and the history");

    /// <summary>docs/axCalc/user-guide.md, embedded by AxCalc.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axCalc/user-guide.md");
}

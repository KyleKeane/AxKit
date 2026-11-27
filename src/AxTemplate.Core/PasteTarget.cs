namespace AxTemplate.Core;

/// <summary>What the place a template goes into does with its line breaks (SPEC.md TP-4.8).</summary>
public enum LineBreaks
{
    /// <summary>A text of several lines, or a control that says nothing of it (a browser's field, which strips breaks from a field of one line itself): the template as it is.</summary>
    Kept,

    /// <summary>A field of one line, which keeps only the first line of a paste: the template made one line.</summary>
    OneLine,

    /// <summary>A console, which runs each line as it is pasted: asked first.</summary>
    EachLineRuns,
}

/// <summary>
/// The window in front as the launcher reads it before it pastes (TP-4.8; the maintainer's question of 2026-11-24:
/// "potentially we need to have a way for things to communicate whether they can handle newlines"). Windows says it
/// of its own controls: an edit control is one line unless its style has <c>ES_MULTILINE</c>, and a console window
/// is known by its class. Nothing else is guessed; a control that says nothing keeps the template as it is. Pure:
/// the window's class names and style come in; tested in <c>PasteTargetTests</c>.
/// </summary>
public static class PasteTarget
{
    /// <summary>The edit control's style bit of several lines.</summary>
    public const int EsMultiline = 0x0004;

    /// <summary>The classes of a console's top window: Windows' console host, and Windows Terminal.</summary>
    public static readonly IReadOnlyList<string> ConsoleClasses = ["ConsoleWindowClass", "CASCADIA_HOSTING_WINDOW_CLASS", "PseudoConsoleWindow"];

    /// <summary>What the window of class <paramref name="topClass"/>, whose focused control is of class <paramref name="focusClass"/> with <paramref name="focusStyle"/>, does with line breaks.</summary>
    public static LineBreaks Of(string topClass, string focusClass, long focusStyle)
    {
        if (ConsoleClasses.Contains(topClass, StringComparer.OrdinalIgnoreCase) || ConsoleClasses.Contains(focusClass, StringComparer.OrdinalIgnoreCase))
        {
            return LineBreaks.EachLineRuns;
        }

        return IsEdit(focusClass) && (focusStyle & EsMultiline) == 0 ? LineBreaks.OneLine : LineBreaks.Kept;
    }

    /// <summary>An edit control of Windows: its own class, a rich edit, or one WinForms makes ("WindowsForms10.EDIT.app...").</summary>
    private static bool IsEdit(string name) =>
        name.Equals("Edit", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase)
        || name.Contains(".EDIT.", StringComparison.OrdinalIgnoreCase);

    /// <summary>The question before several lines go into a console (TP-4.8).</summary>
    public static string ConsoleQuestion(string title, int lines) =>
        $"{title} has {lines} lines, and a terminal runs each line as it is pasted";

    /// <summary>
    /// TP-4.8: whether the filled text is made one line before it is pasted. Into a field of one line, always. Into
    /// a terminal, which runs each line, when no answer was given to the question: the question is asked on the
    /// template as it reads before the selection is taken, so a text that has lines here without an answer got
    /// them from the selection, after the window hid, where nothing can be asked; it then goes in the question's
    /// safe way, on one line, nothing run (until 2026-11-27 it went in as it was, each line run).
    /// <paramref name="answer"/> is what the user chose, null when nothing was asked; <paramref name="target"/> what
    /// the window in front does with line breaks.
    /// </summary>
    public static bool MadeOneLine(LineBreaks? answer, LineBreaks target, bool hasLines) =>
        hasLines && (answer ?? target) switch
        {
            LineBreaks.OneLine => true,
            LineBreaks.EachLineRuns => answer is null,
            _ => false,
        };

    /// <summary>
    /// TP-4.3: whether a filled text is put on the clipboard as a private copy, marked as not for a clipboard history:
    /// when the clipboard the launcher met was private (a password from axPass) and the template takes its text in
    /// with <c>{clipboard}</c>, so that the password does not leave the launcher unmarked inside the template.
    /// </summary>
    public static bool CopiedAsPrivate(string template, bool clipboardWasPrivate) =>
        clipboardWasPrivate && template.Contains(AxKit.Core.TextTemplate.ClipboardMark, StringComparison.Ordinal);

    /// <summary>
    /// How many times a mark stands in a text (TP-D10): the caret's mark is counted before the caret before the
    /// paste, and a mark found after it is the pasted one only when there are more than that, so that a
    /// <c>{caret}</c> the document held already is never taken for it and deleted.
    /// </summary>
    public static int MarksIn(string text, string mark)
    {
        var count = 0;
        for (var at = text.IndexOf(mark, StringComparison.Ordinal); at >= 0 && mark.Length > 0; at = text.IndexOf(mark, at + mark.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    /// <summary>The number of lines of a text, counted by its breaks.</summary>
    public static int LinesOf(string text) => text.ReplaceLineEndings("\n").Trim('\n').Split('\n').Length;

    /// <summary>
    /// TP-4.3, TP-D6: whether the text the clipboard held when the launcher came up is put back after the paste.
    /// Only a text; only while the clipboard still holds the template the launcher put there, so that nothing copied
    /// since is lost; and never a text that was copied as private (a password from axPass or another password
    /// manager, <paramref name="beforeWasPrivate"/>): put back as plain text it would reach Windows' clipboard
    /// history, the cloud clipboard and axClip, and its owner's timed clearing would no longer find it (review,
    /// 2026-11-27). Such a text is not put back at all; the user copies it again where it came from.
    /// </summary>
    public static bool GivesBack(string before, bool beforeWasPrivate, bool stillTheTemplate) =>
        before.Length > 0 && !beforeWasPrivate && stillTheTemplate;
}

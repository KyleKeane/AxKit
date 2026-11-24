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

    /// <summary>The number of lines of a text, counted by its breaks.</summary>
    public static int LinesOf(string text) => text.ReplaceLineEndings("\n").Trim('\n').Split('\n').Length;
}

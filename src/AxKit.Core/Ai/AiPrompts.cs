using System.Text;

namespace AxKit.Core.Ai;

/// <summary>
/// What a window knows about itself for a question about it (docs/axkit/ai-help.md AH-6.2): the app, the window's
/// title, the focused control's name and place, the F1 text, the user guide and the last actions; never the text of a
/// document, which stays the user's.
/// </summary>
public sealed record WindowContext(string App, string Title, string Focus, string Shortcuts, string? Guide, IReadOnlyList<string> Recent);

/// <summary>
/// The prompts (docs/axkit/ai-help.md AH-4): one text each, in plain English, the user's own words last and unchanged
/// (AH-4.2), every one saying who reads the answer (AH-4.1).
/// </summary>
public static class AiPrompts
{
    /// <summary>Who reads the answer, in every prompt.</summary>
    public const string Reader = "The reader is blind and hears your answer through the NVDA screen reader. Write plain prose that reads well aloud: short paragraphs, no tables, no bullet symbols, no Markdown marks, no headings, no decoration. Be brief and concrete, name keys as they are pressed (Ctrl+Shift+W), and say when you are not sure.";

    /// <summary>The line before the user's own words.</summary>
    public const string Marker = "The user's question:";

    /// <summary>Shift+F1 in an axKit window (AH-6): the window's context, then the question.</summary>
    public static string AboutWindow(WindowContext context, string question)
    {
        var text = new StringBuilder();
        text.Append("You are helping the user of axKit, a set of accessible Windows apps for a blind developer who uses the NVDA screen reader; axKit's source and documents are at https://github.com/KyleKeane/AxKit. ");
        text.Append("The question is about the axKit window the user is in. Below is what that window knows about itself, then the question, which you answer from that and from what you know; if a folder is open to you, you may read files in it.\n");
        text.Append(Reader).Append("\n\n");
        text.Append("The app: ").Append(context.App).Append(".\n");
        text.Append("The window's title: ").Append(context.Title).Append(".\n");
        text.Append("The focus is on: ").Append(context.Focus).Append(".\n");
        if (context.Recent.Count > 0)
        {
            text.Append("\nThe last things that happened in this window, oldest first:\n");
            foreach (var line in context.Recent)
            {
                text.Append(line).Append('\n');
            }
        }

        text.Append("\nThe window's keyboard shortcuts, as its F1 text shows them:\n").Append(context.Shortcuts.Replace("\r\n", "\n").TrimEnd()).Append('\n');
        if (!string.IsNullOrWhiteSpace(context.Guide))
        {
            text.Append("\nThe app's user guide:\n").Append(context.Guide.Replace("\r\n", "\n").TrimEnd()).Append('\n');
        }

        return text.Append('\n').Append(Marker).Append('\n').Append(question.Trim()).ToString();
    }

    /// <summary>axHelp's field (AH-7.1): the program in front when the hotkey was pressed, then the question.</summary>
    public static string Anywhere(string? program, string? title, string question)
    {
        var text = new StringBuilder();
        text.Append("You are the help of axKit, a set of accessible Windows apps for a blind developer who uses the NVDA screen reader. The user pressed a hotkey from whatever program they were in and typed a question; answer it from what you know and from the files in the folders open to you, if any. ");
        text.Append("When the answer is a file of the user's, name its full path on a line of its own.\n");
        text.Append(Reader).Append("\n\n");
        if (!string.IsNullOrWhiteSpace(program) || !string.IsNullOrWhiteSpace(title))
        {
            text.Append("The program in front when the user asked: ").Append(string.IsNullOrWhiteSpace(program) ? "unknown" : program);
            if (!string.IsNullOrWhiteSpace(title))
            {
                text.Append(", its window titled \"").Append(title).Append('"');
            }

            text.Append(".\n\n");
        }

        return text.Append(Marker).Append('\n').Append(question.Trim()).ToString();
    }

    /// <summary>axDescribe's hotkey (AH-7.2): the picture file to read, and what to say about it.</summary>
    public static string Screen(string picturePath, string? program, string? title)
    {
        var text = new StringBuilder();
        text.Append("You are the eyes of a blind user of Windows who uses the NVDA screen reader. Read the picture file at this path with your Read tool; it is a screenshot of the window that was in front when the user asked:\n");
        text.Append(picturePath).Append('\n');
        if (!string.IsNullOrWhiteSpace(program) || !string.IsNullOrWhiteSpace(title))
        {
            text.Append("The window belongs to ").Append(string.IsNullOrWhiteSpace(program) ? "an unknown program" : program);
            if (!string.IsNullOrWhiteSpace(title))
            {
                text.Append(" and is titled \"").Append(title).Append('"');
            }

            text.Append(".\n");
        }

        text.Append('\n').Append(Reader).Append("\n\n");
        text.Append("Describe the screenshot for the user: first what the window is and what it is for, in a sentence; then what it shows, from top to bottom, including any text that matters and any message, dialog or error; then the controls a keyboard user could reach, with their labels. Leave out decoration and colours unless they carry meaning. The user may ask more about it afterwards.");
        return text.ToString();
    }

    /// <summary>A follow-up in the same session (AH-6.3): the user's words alone, since the session holds the rest.</summary>
    public static string FollowUp(string question) => question.Trim();
}

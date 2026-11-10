using System.Text;

namespace AxKit.Core.Ai;

/// <summary>
/// What a window knows about itself for a question about it (docs/axKit/ai-help.md AH-6.2): the app, the window's
/// title, the focused control's name and place, the F1 text, the user guide and the last actions; never the text of a
/// document, which stays the user's.
/// </summary>
public sealed record WindowContext(string App, string Title, string Focus, string Shortcuts, string? Guide, IReadOnlyList<string> Recent);

/// <summary>
/// The prompts (docs/axKit/ai-help.md AH-4): one text each, in plain English, the user's own words last and unchanged
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

    /// <summary>A follow-up in the same session (AH-6.3): the user's words alone, since the session holds the rest.</summary>
    public static string FollowUp(string question) => question.Trim();
}

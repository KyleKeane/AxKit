using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxHelp;

/// <summary>The texts axHelp shows: the window's own text, the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The window's one text (SPEC.md HE-2.2), with the hotkeys as they are set and whether each is registered.</summary>
    public static string Window(string conversationKey, string screenshotKey, bool screenshotRegistered, string workingFolder) => $"""
        axHelp: your conversation with Claude, from any program.

        {conversationKey}: the conversation, an axClaude window titled axHelp, the focus in its message field. It is one conversation that goes on until you clear it there with Session, New session, so that earlier answers stay readable with axClaude's keys. Alt+F4 there hides it, Claude Code running on, and {conversationKey} brings the same window back at once; Session, End Claude and close this window ends it. Claude Code works in its own folder, {workingFolder}, and asks before it changes anything on your computer. Ask it about axKit too: it answers from axKit's guides, kept in the documentation folder there.

        {screenshotKey}: a screenshot of the window in front, put into the conversation's message field with its path, which is on the clipboard too; type your question after it and press Enter.{(screenshotRegistered ? string.Empty : $" {screenshotKey} is taken by another program now: change screenshotHotkey in axHelp.json (Options, Developer, Open settings file) and start axHelp again.")}

        Here: Enter opens the conversation, {BundleKeys.Text(AxHelpKeys.PictureFile)} puts a picture file of yours into it, {BundleKeys.Text(AxHelpKeys.OpenScreenshot)} opens the last screenshot in axPreview, Escape hides this window. Options, Hotkey changes {conversationKey}.
        """;

    /// <summary>The F1 text (SPEC.md HE-6): the bundle-wide keys, then axHelp's own, then the bundle's Notices section.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axHelp",
        [
            $"{BundleKeys.Text(BundleKeys.Save)}: nothing to save here; axHelp says so",
        ],
        altF4: "hide axHelp (Session, Quit axHelp ends it)") + $"""
        ## From any program
          The conversation's hotkey, Ctrl+Shift+Slash (the question mark's key) unless you chose another in Options, Hotkey: the conversation with Claude, an axClaude window titled axHelp, the focus in its message field
          The screenshot's hotkey, Ctrl+Shift+Comma (the less-than sign's key) unless axHelp.json names another: a picture of the window in front, its path put into the conversation's message field and on the clipboard; type your question after it and press Enter
          Alt+F4 in the conversation hides it and Claude Code runs on; the hotkey brings the same window back with the caret in the message field. Its Session menu has End Claude and close this window; Quit axHelp ends it too, and it asks first when Claude is working or a message is typed and not sent
          Options, Keep the conversation ready in the background, off at first: Claude Code starts at your first press. On, axHelp started at sign-in starts Claude Code hidden, so that the first press is quick too; Claude Code then runs from sign-in
          A question about axKit or one of its apps is answered from axKit's guides, kept in the documentation folder of Claude's working folder
          While axKit's AI tools are on (axKit menu, AI tools, Let AI tools read and open axKit), Claude in the conversation can read your axKit windows, open apps and add tasks when you ask. While they are off it cannot, and says where to turn them on
          The first time, axClaude's reminder of what goes to Anthropic, and Claude Code's question whether to trust its working folder

        ## In this window
          Enter: the conversation
          {BundleKeys.Text(AxHelpKeys.PictureFile)}: a picture file of yours put into the conversation (a .png, .jpg, .jpeg, .gif or .webp file), as the screenshot is
          {BundleKeys.Text(AxHelpKeys.OpenScreenshot)}: the last screenshot in axPreview, which reads the text in it with Windows' own reader
          Escape: hide axHelp at once
        """ + BundleHelp.NoticesSection("axHelp", "the text");

    /// <summary>docs/axHelp/user-guide.md, embedded by AxHelp.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axHelp/user-guide.md");
}

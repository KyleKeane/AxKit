using System.Reflection;
using AxKit.Core;

namespace AxMCP;

/// <summary>The texts axMCP shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md MP-6): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the navigator, the detail pane, the keys that work everywhere, then the bundle's Notices section. The first line, the bundle's section and the Notices section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axMCP",
        [
            "Ctrl+F: find an entry in the navigator. F3 and Shift+F3: next or previous match",
        ]) + """
        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          The top level is in sections: Claude Code, the servers of its configuration, and Your servers, each under a header line
          Ctrl+Up, Ctrl+Down: the section before or after; the edge sound past the first or the last
          Right or Enter on a server: start it, the first time after your agreement, and show what it offers: Tools, Resources, Prompts
          Right or Enter on Tools, Resources or Prompts: its list. On a tool or a prompt, Enter moves to its arguments; on a resource, Enter reads it
          Left or Backspace: up a level; the server keeps running
          Tab, Ctrl+Tab or F6: to the detail pane
          Ctrl+Enter: call the tool, or fetch the prompt, with the arguments in the detail pane; the result opens as a notice text
          Ctrl+J: the last result, whole, as JSON in axJSON
          Ctrl+Shift+S: stop the server
          Ctrl+N at the top level: add a server of your own: a name, its command line, a folder. F2: rename one of yours. Delete: remove one of yours, after a question
          Ctrl+Shift+N: nothing here; servers are added with Ctrl+N
          Ctrl+Q: read every field of the selection
          Shift+F10 or the Applications key: the menu of the selection

        ## The detail pane, on the right
          On a server: where it is defined, its command line, its folder, the names of its environment variables, its state
          On a tool or a prompt: its description, then one field per argument, "(required)" after the ones it needs
          On a resource: its name, address, type and description
          Up, Down: move through the fields
          Enter: change the field; on the description, read it whole. Space: a check on or off
          Alt+Up, Alt+Down: nothing here; the arguments keep the server's order
          Ctrl+Q: read the field's whole value
          Escape, Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6: back to the navigator

        ## Everywhere in axMCP
          Ctrl+Z: undo the last change to Your servers
          F5: read the servers again, and a running server's lists again
          Ctrl+S: nothing to save here; a server of yours is kept as you add it
        """ + BundleHelp.NoticesSection("axMCP", "the panes");

    /// <summary>docs/axMCP/user-guide.md, embedded by AxMCP.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axMCP/user-guide.md");
}

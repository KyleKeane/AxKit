using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxMCP;

/// <summary>The texts axMCP shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md MP-6): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the navigator, the detail pane, the keys that work everywhere, then the bundle's Notices section. The first line, the bundle's section and the Notices section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axMCP", []) + $"""
        ## The navigator, on the left
          Up, Down, Home and End move, and the edge sound plays at an end. Type the start of a name to jump to it; Shift with a letter goes backwards.
          The top level is in sections, each under a header line: Claude Code, the servers of its configuration, and Your servers. The keys for the next and the previous section play the edge sound past the first or the last.
          Right or Enter on a server starts it, the first time after your agreement, and shows what it offers: Tools, Resources and Prompts.
          Right or Enter on Tools, Resources or Prompts shows its list. On a tool or a prompt, Enter moves to its arguments; on a resource, Enter reads it.
          Left or Backspace goes up a level, and the server keeps running.
          {BundleKeys.Text(BundleKeys.MainAction)} calls the tool, or fetches the prompt, with the arguments in the detail pane; the result opens as a notice text.
          {BundleKeys.Text(AxMCPKeys.LastResult)} opens the last result, whole, as JSON in axJSON. {BundleKeys.Text(AxMCPKeys.StopServer)} stops the server.
          New item at the top level adds a server of your own, in one form: a name, its command line and a folder, Tab between them; Enter or {BundleKeys.Text(BundleKeys.MainAction)} adds it. F2 renames one of yours, and Delete removes one of yours, after a question.
          Only your servers are selected; a selected server reads ", selected". Escape deselects all. Delete then removes all the selected after one question, and one Undo brings them all back.
          New list does nothing here: servers are added with New item.

        ## The detail pane, on the right
          On a server: where it is defined, its command line, its folder, the names of its environment variables, and its state.
          On a tool or a prompt: its description, then one field per argument, "(required)" after the ones it needs.
          On a resource: its name, address, type and description.
          Up and Down move through the fields.
          Enter changes the field; on the description, it reads it whole. Space turns a check on or off.
          Right and Left go to the next and the previous value of a choice; on a check, Right checks and Left unchecks. The edge sound plays at an end and on any other field.
          Escape, Tab or F6 goes back to the navigator.

        ## Everywhere in axMCP
          Undo takes back the last change to Your servers.
          F5 reads the servers again, and a running server's lists again.
          {BundleKeys.Text(BundleKeys.Save)} has nothing to save here: a server of yours is kept as you add it.
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axMCP", "the panes");

    /// <summary>docs/axMCP/user-guide.md, embedded by AxMCP.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axMCP/user-guide.md");
}

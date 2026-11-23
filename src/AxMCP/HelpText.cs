using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxMCP;

/// <summary>The texts axMCP shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md MP-6): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the navigator, the detail pane, the keys that work everywhere, then the bundle's Notices section. The first line, the bundle's section and the Notices section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axMCP",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}: find an entry in the navigator. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
        ]) + $"""
        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          The top level is in sections: Claude Code, the servers of its configuration, and Your servers, each under a header line
          {BundleKeys.Text(ItemKeys.PreviousSection)}, {BundleKeys.Text(ItemKeys.NextSection)}: the section before or after; the edge sound past the first or the last
          Right or Enter on a server: start it, the first time after your agreement, and show what it offers: Tools, Resources, Prompts
          Right or Enter on Tools, Resources or Prompts: its list. On a tool or a prompt, Enter moves to its arguments; on a resource, Enter reads it
          Left or Backspace: up a level; the server keeps running
          Tab, {BundleKeys.Text(BundleKeys.NextControl)} or F6: to the detail pane
          {BundleKeys.Text(BundleKeys.MainAction)}: call the tool, or fetch the prompt, with the arguments in the detail pane; the result opens as a notice text
          {BundleKeys.Text(AxMCPKeys.LastResult)}: the last result, whole, as JSON in axJSON
          {BundleKeys.Text(AxMCPKeys.StopServer)}: stop the server
          {BundleKeys.Text(ItemKeys.New)} at the top level: add a server of your own, in one form: a name, its command line, a folder, Tab between them, Enter or {BundleKeys.Text(BundleKeys.MainAction)} adds it. F2: rename one of yours. Delete: remove one of yours, after a question
          Space: select one of your servers, or deselect it again; a selected server reads ", selected". {BundleKeys.Text(SelectionKeys.SelectDown)}, {BundleKeys.Text(SelectionKeys.SelectUp)}, {BundleKeys.Text(SelectionKeys.SelectToStart)}, {BundleKeys.Text(SelectionKeys.SelectToEnd)}: select as you move. {BundleKeys.Text(SelectionKeys.SelectTo)}: select from the nearest selected server to this one. {BundleKeys.Text(SelectionKeys.SelectAll)}: select every server of yours. {BundleKeys.Text(SelectionKeys.SelectNone)}, or Escape: deselect all. Delete then removes all the selected after one question, and one {BundleKeys.Text(ItemKeys.Undo)} brings them all back
          {BundleKeys.Text(ItemKeys.NewList)}: nothing here; servers are added with {BundleKeys.Text(ItemKeys.New)}
          {BundleKeys.Text(ItemKeys.ReadAll)}: read every field of the selection
          {BundleKeys.Text(ItemKeys.ItemMenu)}, the Applications key or {BundleKeys.Text(BundleKeys.ContextMenu)}: the menu of the selection

        ## The detail pane, on the right
          On a server: where it is defined, its command line, its folder, the names of its environment variables, its state
          On a tool or a prompt: its description, then one field per argument, "(required)" after the ones it needs
          On a resource: its name, address, type and description
          Up, Down: move through the fields
          Enter: change the field; on the description, read it whole. Space: a check on or off
          Right and Left: on a choice, the next and the previous value; on a check, Right checks and Left unchecks; the edge sound at an end and on any other field
          {BundleKeys.Text(ItemKeys.ReadAll)}: read the field's whole value
          Escape, Tab, {BundleKeys.Text(ItemKeys.OtherPaneBack)}, {BundleKeys.Text(BundleKeys.NextControl)}, {BundleKeys.Text(BundleKeys.PreviousControl)} or F6: back to the navigator

        ## Everywhere in axMCP
          {BundleKeys.Text(ItemKeys.Undo)}: undo the last change to Your servers
          F5: read the servers again, and a running server's lists again
          {BundleKeys.Text(BundleKeys.Save)}: nothing to save here; a server of yours is kept as you add it
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axMCP", "the panes");

    /// <summary>docs/axMCP/user-guide.md, embedded by AxMCP.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axMCP/user-guide.md");
}

using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxProcess;

/// <summary>The texts axProcess shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md PR-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axProcess",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}: find an item in the list. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
        ]) + $"""
        ## axProcess
          The window starts in Processes, one of three lists of This computer: Processes, Services and Startup
          Left or Backspace: up to This computer, where the three lists are rows. Right or Enter on one: into it
          Alt+1, Alt+2, Alt+3: straight to Processes, Services or Startup from anywhere; pressed in the list already shown, the key says its name. Ctrl+1, Ctrl+2 and Ctrl+3, their keys until now, still work
          F5: read the list again; nothing changes by itself. At This computer, all three
          {BundleKeys.Text(AxProcessKeys.Summary)}: the summary: the processes, the memory in use, the processor
          Type the start of a row to jump to it, the same letter again to the next; Shift with a letter backwards
          In a list
            Enter: the row's details, with what can be done to it: end a process, start or stop a service, turn a startup program off or on, remove it
            Delete: end the process, after a question; in Startup, remove the startup program, after a question whose default is Cancel. With rows selected, all of them after one question: "Ended 3 processes"
            Space: select the process or startup program you are on, or deselect it again; a selected row reads ", selected". {BundleKeys.Text(SelectionKeys.SelectDown)}, {BundleKeys.Text(SelectionKeys.SelectUp)}, {BundleKeys.Text(SelectionKeys.SelectToStart)}, {BundleKeys.Text(SelectionKeys.SelectToEnd)}: select as you move. {BundleKeys.Text(SelectionKeys.SelectTo)}: select from the nearest selected row to this one. {BundleKeys.Text(SelectionKeys.SelectAll)}: select every row. {BundleKeys.Text(SelectionKeys.SelectNone)}, or Escape: deselect all. Services are not selected
            Context, Turn off and Turn on, in Startup: the startup program no longer starts with Windows, or starts again; nothing is deleted
            {BundleKeys.Text(ItemKeys.ItemMenu)}, the Applications key or {BundleKeys.Text(BundleKeys.ContextMenu)}: the Context menu, what acts on the row you are on
            Up or Home on the first row, Down or End on the last, Right on a row that is no list, Left at This computer: the edge sound, since nothing moves
          {BundleKeys.Text(AxProcessKeys.ShowProgram)}: show the program file in axFile
          {BundleKeys.Text(AxProcessKeys.CopyLine)}: copy the line
          {BundleKeys.Text(BundleKeys.Save)}: nothing to save here; axProcess says so
        """ + BundleHelp.FilterAndSortSection("Memory, Processor or Name, the order of the processes") + BundleHelp.NoticesSection("axProcess", "the list");

    /// <summary>docs/axProcess/user-guide.md, embedded by AxProcess.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axProcess/user-guide.md");
}

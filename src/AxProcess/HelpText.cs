using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxProcess;

/// <summary>The texts axProcess shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md PR-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axProcess", []) + $"""
        ## axProcess
          The window starts in Processes, one of three lists of This computer: Processes, Services and Startup.
          Left or Backspace goes up to This computer, where the three lists are rows. Right or Enter on one goes into it.
          Alt+1, Alt+2, Alt+3 go straight to Processes, Services or Startup from anywhere; pressed in the list already shown, the key says its name.
          Refresh reads the list again; nothing changes by itself. At This computer, it reads all three.
          Summary says the processes, the memory in use and the processor.
          Type the start of a row to jump to it, the same letter again to the next; Shift with a letter goes backwards.
          In a list
            Enter shows the row's details, with what can be done to it: end a process, start or stop a service, turn a startup program off or on, or remove it.
            Delete ends the process, after a question; in Startup, it removes the startup program, after a question whose default is Cancel. With rows selected, it acts on all of them after one question: "Ended 3 processes". {BundleKeys.Text(BundleKeys.MainAction)} on a process ends it the same way, after the same question.
            Only processes and startup programs are selected, never services; a selected row reads ", selected". Escape deselects all.
            Context, Turn off and Turn on, in Startup: the startup program no longer starts with Windows, or starts again; nothing is deleted.
            Up or Home on the first row, Down or End on the last, Right on a row that is no list, and Left at This computer play the edge sound, since nothing moves.
          {BundleKeys.Text(BundleKeys.Save)} has nothing to save here, and axProcess says so.
        """ + BundleHelp.FilterAndSortSection("Memory, Processor or Name, the order of the processes") + BundleHelp.NoticesSection("axProcess", "the list");

    /// <summary>docs/axProcess/user-guide.md, embedded by AxProcess.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axProcess/user-guide.md");
}

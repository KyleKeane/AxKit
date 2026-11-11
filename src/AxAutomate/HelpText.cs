using System.Reflection;
using AxKit.Core;

namespace AxAutomate;

/// <summary>The texts axAutomate shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AU-4.4): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the navigator, the detail pane, the keys that work everywhere, then the bundle's Notices section. The first line, the bundle's section and the Notices section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axAutomate",
        [
            "Ctrl+F: find a row in the navigator. F3 and Shift+F3: next or previous match",
        ]) + """
        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          A folder of workflows lists its folders, then its workflows, each with how many steps it has and, after a run, "passed", "failed at step 3" or "cannot run"
          Right or Enter on a folder or a workflow: go into it. Left or Backspace: up a level
          Ctrl+Up, Ctrl+Down: the section before or after, where a level has sections; the edge sound otherwise
          In a workflow, one row per step in the order they run: "4. Expect said: Command palette"
          A step's marks after its sentence: "may not come" or "goes on if it fails" when a failure does not stop the workflow, "reports" when it has its own failure message, "then 2 steps" when steps run only after it passed, "otherwise 1 step" when steps run only after it failed
          The steps of a then or otherwise block come right after their step, numbered under it: "5.1 then: Wait 1.5 seconds"
          A row that starts with "Note:" is a sentence for the reader, never run
          Enter, Tab, Ctrl+Tab or F6: to the step's details
          Ctrl+J: the workflow's file as text, a script in axDown, a workflow file in axJSON
          Ctrl+Q: read every line of the selection's details
          Shift+F10 or the Applications key: the menu of the selection

        ## The detail pane, on the right
          On a step: its number, what it does, each of its values, what happens when it fails, what it reports, its blocks, its note, and the line of the file it comes from
          On a workflow: its title, what it covers, how many steps, its file and its format
          Up, Down: move through the lines. Ctrl+Q: read the line whole
          Escape, Tab, Shift+Tab, Ctrl+Tab or F6: back to the navigator; during a run Escape stops the run instead
          After a run: "Last run:" with the outcome and what the runner saw, on the workflow and on the step that failed

        ## Everywhere in axAutomate
          Ctrl+O: open a folder of workflows; axAutomate opens it again at the next start
          F5: run the workflow you are in, on a desktop of its own, never with your keyboard; in a folder, every workflow of that folder, one after another. A failing step is said as it ends ("Step 3 failed: ..."), and one line when each workflow ends ("02-palette passed, 6 steps" or "02-palette failed at step 3"); the rows then carry the outcome, and the details what the runner saw
          A workflow with a run step asks first, naming its command lines: they run on your own desktop and files. Cancel is the default; Alt+R runs it
          A step that makes a file or a folder outside the run's own folder cannot run from here. These guard against slips only: a workflow drives axKit's windows, which reach your files, so run only workflows you wrote or trust
          Escape during a run: stop it after the step going on
          Session, Refresh: read the folder or the workflow again; a run reads its file again by itself
          This version views and runs the workflows: changing them comes later. Ctrl+N, Ctrl+Shift+N, F2, Delete, Alt+Up, Alt+Down and Ctrl+Z say so
        """ + BundleHelp.NoticesSection("axAutomate", "the panes");

    /// <summary>docs/axAutomate/user-guide.md, embedded by AxAutomate.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axAutomate/user-guide.md");
}

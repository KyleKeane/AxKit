using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxAutomate;

/// <summary>The texts axAutomate shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AU-4.4): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the navigator, the detail pane, the keys that work everywhere, then the bundle's Notices section. The first line, the bundle's section and the Notices section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axAutomate", []) + $"""
        ## The navigator, on the left
          Up, Down, Home and End move through the rows, and the edge sound plays at an end. Type the start of a name to jump to it; Shift with a letter goes backwards.
          A folder of workflows lists its folders, then its workflows, each with how many steps it has and, after a run, "passed", "failed at step 3" or "cannot run".
          Right or Enter on a folder or a workflow goes into it. Left or Backspace goes up a level.
          Only the workflows of a folder are selected; a selected workflow reads ", selected". Escape, while no run is going on, deselects all. {BundleKeys.Text(AxAutomateKeys.Run)} then runs the selected workflows.
          Where a level has no sections, the keys for the next and the previous section play the edge sound.
          In a workflow there is one row per step, in the order they run: "4. Expect said: Command palette".
          A step's marks come after its sentence: "may not come" or "goes on if it fails" when a failure does not stop the workflow, "reports" when it has its own failure message, "then 2 steps" when steps run only after it passed, "otherwise 1 step" when steps run only after it failed.
          The steps of a then or otherwise block come right after their step, numbered under it: "5.1 then: Wait 1.5 seconds".
          A row that starts with "Note:" is a sentence for the reader and never runs.
          Enter on a step, Tab or F6 moves to the step's details.
          The workflow's file as text opens a script in axDown and a workflow file in axJSON.
          {BundleKeys.Text(ItemKeys.ReadAll)} reads every line of the details of the row you are on.

        ## The detail pane, on the right
          On a step: its number, what it does, each of its values, what happens when it fails, what it reports, its blocks, its note, and the line of the file it comes from.
          On a workflow: its title, what it covers, how many steps, its file and its format.
          Up and Down move through the lines, and {BundleKeys.Text(ItemKeys.ReadAll)} reads a line whole. Right and Left change nothing in this version and play the edge sound.
          Escape, Tab or F6 goes back to the navigator; during a run, Escape stops the run instead.
          After a run, "Last run:" gives the outcome and what the runner saw, on the workflow and on the step that failed.

        ## Everywhere in axAutomate
          The folder of workflows you open is opened again at the next start.
          {BundleKeys.Text(AxAutomateKeys.Run)}: run the workflow you are in, on a desktop of its own, never with your keyboard; in a folder, every workflow of that folder runs, one after another. A failing step is said as it ends, "Step 3 failed: ...", and one line when each workflow ends, "02-palette passed, 6 steps" or "02-palette failed at step 3". The rows then carry the outcome, and the details what the runner saw. {BundleKeys.Text(BundleKeys.MainAction)} runs too.
          A workflow with a run step asks first, naming its command lines, since they run on your own desktop and files. Cancel is the default; Alt+R runs it.
          A step that makes a file or a folder outside the run's own folder cannot run from here. These guard against slips only: a workflow drives axKit's windows, which reach your files, so run only workflows you wrote or trust.
          Escape during a run stops it after the step going on.
          The last run's report holds how the run ended, every step played with what the runner saw, and the timeline of what was said and focused, as a text with the reading keys and find. It is kept until the window closes.
          A run reads its file again by itself; {BundleKeys.Text(ItemKeys.Refresh)}, Session, Refresh reads the folder or the workflow again.
          This version views and runs the workflows; changing them comes later, and the keys that would change one say so.
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axAutomate", "the panes");

    /// <summary>docs/axAutomate/user-guide.md, embedded by AxAutomate.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axAutomate/user-guide.md");
}

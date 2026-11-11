using AxAutomate.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxAutomate;

/// <summary>
/// The runs of axAutomate's window (SPEC.md AU-4.4, AU-4.5, AU-5.3): F5 runs the workflow in hand, or at a folder's
/// level every workflow of that folder one after another, through the engine of <c>AxAutomate.Core</c>
/// (<see cref="ScriptRunner"/>), on a thread of its own and on a desktop of the run's own, so that the run never takes
/// the user's keyboard; the run's windows are this very program file, started with the run's secret and folder alone
/// (S-11). Escape stops the run after its current step. Nothing is said for a step that passes; a failing step is said
/// as it ends, and one line at each workflow's end; the rows then carry the outcome.
/// </summary>
public sealed partial class AutomateForm
{
    /// <summary>The outcome of each workflow's last run in this window, by its file's full path.</summary>
    private readonly Dictionary<string, RunOutcome> _outcomes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The run going on, or null.</summary>
    private Thread? _run;

    /// <summary>Escape during a run: the engine asks it between steps.</summary>
    private volatile bool _stopAsked;

    private bool Running => _run is not null;

    /// <summary>One workflow of a run: the workflow, its rows (for the failing step's number) and its lines, or why it cannot run.</summary>
    private sealed record RunJob(Workflow Workflow, IReadOnlyList<StepRow> Rows, IReadOnlyList<ScriptLine> Lines, string? Refusal);

    /// <summary>F5 (AU-4.4): the workflow in hand, read again from its file first, or every workflow of the folder in hand.</summary>
    private void RunCommand()
    {
        if (Running)
        {
            Announce("A run is going on; Escape stops it", true);
            return;
        }

        if (_folders.Count == 0)
        {
            Announce("No folder open: Ctrl+O opens a folder of workflows", true);
            return;
        }

        var jobs = new List<RunJob>();
        if (_workflow is { } open)
        {
            // The file as it is now: the rows and the lines from the same reading.
            _workflow = WorkflowFolder.ReadWorkflow(open.Path);
            _steps = StepText.Rows(_workflow);
            Fill(select: SelectedKey());
            var job = Job(_workflow);
            if (job.Refusal is { } refusal)
            {
                Announce("This workflow cannot run: " + refusal, true);
                return;
            }

            jobs.Add(job);
        }
        else
        {
            jobs.AddRange(WorkflowFolder.Read(_folders[^1]).Where(entry => !entry.IsFolder).Select(entry => Job(WorkflowFolder.ReadWorkflow(entry.Path))));
            if (jobs.Count == 0)
            {
                Announce("No workflows in this folder to run; a folder inside it runs from inside it", true);
                return;
            }
        }

        _stopAsked = false;
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("The program's own path is unknown.");
        // A thread that keeps the process alive: a window closed during a run still lets the run close its windows.
        _run = new Thread(() => RunJobs(jobs, exe)) { IsBackground = false, Name = "axAutomate run" };
        _run.Start();
        Announce(jobs.Count == 1 ? $"Running {jobs[0].Workflow.Title}; Escape stops it" : $"Running {StepText.Count(jobs.Count, "workflow")}; Escape stops the run", true);
    }

    private static RunJob Job(Workflow workflow)
    {
        var (lines, refusal) = ScriptLines.Prepare(workflow, nvda: false);
        return new RunJob(workflow, StepText.Rows(workflow), lines, refusal);
    }

    /// <summary>
    /// The Operations menu as it opens: Run only on what can run (AU-3.3.2), a workflow that can or a folder with a
    /// workflow, and never during a run; Stop the run only during one.
    /// </summary>
    private void UpdateRunItems()
    {
        var runnable = _workflow is { } open
            ? ScriptLines.Prepare(open, nvda: false).Refusal is null
            : _folders.Count > 0 && WorkflowFolder.Read(_folders[^1]).Any(entry => !entry.IsFolder);
        _runItem.Enabled = runnable && !Running;
        _stopItem.Enabled = Running;
    }

    /// <summary>Escape during a run (AU-4.4): the run ends after the step going on.</summary>
    private void StopRun()
    {
        if (!Running)
        {
            Announce("No run is going on", true);
            return;
        }

        _stopAsked = true;
        Announce("Stopping after the current step", true);
    }

    /// <summary>The run's thread: each workflow on its own desktop with its own folder, the outcomes posted to the window as each ends.</summary>
    private void RunJobs(IReadOnlyList<RunJob> jobs, string exe)
    {
        var (passed, failed, cannotRun) = (0, 0, 0);
        var root = Path.Combine(Path.GetTempPath(), "axkit-automate", $"{Environment.ProcessId}-{DateTime.Now:HHmmss}");
        try
        {
            for (var i = 0; i < jobs.Count && !_stopAsked; i++)
            {
                var job = jobs[i];
                if (job.Refusal is { } refusal)
                {
                    cannotRun++;
                    var (said, outcome) = RunSentences.CannotRun(job.Workflow, refusal);
                    Post(() => WorkflowEnded(job.Workflow, said, outcome));
                    continue;
                }

                var temp = Path.Combine(root, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                Directory.CreateDirectory(temp);
                ScriptResult result;
                try
                {
                    result = ScriptRunner.Play(
                        job.Lines,
                        exe,
                        temp,
                        output: _ => { },
                        stepEnded: ended =>
                        {
                            if (!ended.Ok)
                            {
                                var text = RunSentences.StepFailed(RunSentences.RowOf(job.Rows, ended.Line.Step), ended);
                                Post(() => Announce(text, false));
                            }
                        },
                        stop: () => _stopAsked,
                        hosted: true);
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
                {
                    Log.Error("axAutomate: a run could not go on", ex);
                    result = new ScriptResult(false, ex.Message, null, [ex.Message], false);
                }

                var failedRow = result.Failed is { } line ? RunSentences.RowOf(job.Rows, line.Line.Step) : null;
                var (end, kept) = RunSentences.Ended(job.Workflow, result, failedRow);
                if (result.Ok && !result.Stopped)
                {
                    passed++;
                }
                else if (!result.Ok)
                {
                    failed++;
                }

                Post(() => WorkflowEnded(job.Workflow, end, kept));
            }
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A process still closing holds its folder; the temp folder is Windows' to clean.
            }

            var stopped = _stopAsked;
            var summary = jobs.Count > 1 ? RunSentences.FolderEnded(passed, failed, cannotRun, stopped) : null;
            Post(() => RunEnded(summary));
        }
    }

    /// <summary>One workflow's end, on the window's thread: its outcome kept and its line said; the rows wait for the run's end.</summary>
    private void WorkflowEnded(Workflow workflow, string said, RunOutcome outcome)
    {
        _outcomes[workflow.Path] = outcome;
        Announce(said, false);
    }

    /// <summary>
    /// The run's end: the rows filled again once, with every outcome (batched, so that the selected row's words change
    /// once and not under each workflow's line), then the folder's line.
    /// </summary>
    private void RunEnded(string? summary)
    {
        _run = null;
        Fill(select: SelectedKey());
        if (summary is not null)
        {
            Announce(summary, false);
        }
    }

    /// <summary>Runs the action on the window's thread; nothing when the window is gone.</summary>
    private void Post(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // The window is closing.
        }
    }

    /// <summary>A workflow's row's mark after a run: ", passed", ", failed at step 3"; nothing before one.</summary>
    private string OutcomeMark(string path) => _outcomes.TryGetValue(path, out var outcome) ? ", " + outcome.Mark : string.Empty;

    /// <summary>A step's row's mark after a run of the workflow open: ", failed" on the step that failed.</summary>
    private string StepMark(StepRow row) =>
        _workflow is { } open && _outcomes.TryGetValue(open.Path, out var outcome) && outcome.FailedStep.Length > 0 && outcome.FailedStep == row.Number ? ", failed" : string.Empty;

    /// <summary>The detail pane's line of the last run: the workflow's outcome, or the failed step's.</summary>
    private ItemDetailLine? OutcomeLine(string path, StepRow? row)
    {
        if (!_outcomes.TryGetValue(path, out var outcome))
        {
            return null;
        }

        if (row is null)
        {
            return new ItemDetailLine($"Last run: {outcome.Mark}. {outcome.Detail}".TrimEnd());
        }

        return StepMark(row).Length > 0 ? new ItemDetailLine("Last run: failed. " + outcome.Detail) : null;
    }
}

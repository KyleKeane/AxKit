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
/// as it ends, and one line at each workflow's end; the rows then carry the outcome. Every file the run reads (the
/// folder, the workflows, a script's lines) is read on the run's thread, never on the window's (bundle rule 3).
/// </summary>
public sealed partial class AutomateForm
{
    /// <summary>How long a window closed during a run waits for the run to end by itself before ending its processes (AU-6b.4).</summary>
    private const int CloseWaitMs = 15_000;

    /// <summary>The outcome of each workflow's last run in this window, by its file's full path.</summary>
    private readonly Dictionary<string, RunOutcome> _outcomes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The run going on, or null.</summary>
    private Thread? _run;

    /// <summary>The engine's run of the workflow going on, for <see cref="AxAutomate.Core.Run.Abandon"/> when the window closes; set by the run's thread.</summary>
    private volatile AxAutomate.Core.Run? _active;

    /// <summary>Escape during a run: the engine asks it between steps.</summary>
    private volatile bool _stopAsked;

    private bool Running => _run is not null;

    /// <summary>One workflow of a run: the workflow, its rows (for the failing step's number) and its lines, or why it cannot run.</summary>
    private sealed record RunJob(Workflow Workflow, IReadOnlyList<StepRow> Rows, IReadOnlyList<ScriptLine> Lines, string? Refusal);

    /// <summary>
    /// F5 (AU-4.4): the workflow in hand, read again from its file first, or every workflow of the folder in hand. What
    /// is checked here is what the window holds already; the files are read on the run's thread.
    /// </summary>
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

        string? workflowPath = null;
        string? folder = null;
        string said;
        if (_workflow is { } open)
        {
            if (ScriptLines.CannotRun(open, nvda: false) is { } refusal)
            {
                Announce("This workflow cannot run: " + refusal, true);
                return;
            }

            workflowPath = open.Path;
            said = $"Running {open.Title}; Escape stops it";
        }
        else
        {
            if (_levelWorkflows == 0)
            {
                Announce("No workflows in this folder to run; a folder inside it runs from inside it", true);
                return;
            }

            folder = _folders[^1];
            said = $"Running {StepText.Count(_levelWorkflows, "workflow")}; Escape stops the run";
        }

        _stopAsked = false;
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("The program's own path is unknown.");
        // A background thread: a window closed during a run waits for it on a thread of its own (AbandonOnClose), and
        // never longer than that wait.
        _run = new Thread(() => RunJobs(workflowPath, folder, exe)) { IsBackground = true, Name = "axAutomate run" };
        _run.Start();
        Announce(said, true);
    }

    /// <summary>A workflow's job, on the run's thread: its lines (a script's file read here) or why it cannot run.</summary>
    private static RunJob Job(Workflow workflow)
    {
        var (lines, refusal) = ScriptLines.Prepare(workflow, nvda: false);
        return new RunJob(workflow, StepText.Rows(workflow), lines, refusal);
    }

    /// <summary>Whether F5 has something to run here, from what the window holds (no file read): the workflow in hand can run, or the level has a workflow.</summary>
    private bool CanRun() => _workflow is { } open ? ScriptLines.CannotRun(open, nvda: false) is null : _folders.Count > 0 && _levelWorkflows > 0;

    /// <summary>
    /// The Operations menu as it opens, and the command palette as it reads it: Run only on what can run (AU-3.3.2),
    /// a workflow that can or a folder with a workflow, and never during a run; Stop the run only during one.
    /// </summary>
    private void UpdateRunItems()
    {
        _runItem.Enabled = CanRun() && !Running;
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

    /// <summary>
    /// The window closed during a run (AU-6b.4): the run is asked to stop, and a thread of its own keeps the process
    /// for at most <see cref="CloseWaitMs"/> while the run closes its windows; after that the run's own processes are
    /// ended (<see cref="AxAutomate.Core.Run.Abandon"/>), so that a step that waits or a window that hangs never leaves an invisible
    /// process with windows on a desktop the user cannot reach.
    /// </summary>
    private void AbandonOnClose()
    {
        if (_run is not { } run)
        {
            return;
        }

        _stopAsked = true;
        new Thread(() =>
        {
            if (run.Join(CloseWaitMs))
            {
                return;
            }

            var ended = _active?.Abandon() ?? 0;
            Log.Info($"axAutomate: the window closed during a run that did not end within {CloseWaitMs / 1000} s; ended {ended} of its processes");
            if (!run.Join(10_000))
            {
                Log.Info("axAutomate: the run's thread did not end after its processes; the process ends without it");
            }
        })
        { IsBackground = false, Name = "axAutomate run closing" }.Start();
    }

    /// <summary>
    /// The run's thread: the jobs made (the workflow in hand read again, or the folder's workflows), then each workflow
    /// on its own desktop with its own folder, the outcomes posted to the window as each ends. Any exception ends the
    /// workflow it came from as failed and never the process (the user's window).
    /// </summary>
    private void RunJobs(string? workflowPath, string? folder, string exe)
    {
        var (passed, failed, cannotRun) = (0, 0, 0);
        var count = 0;
        var root = Path.Combine(Path.GetTempPath(), "axkit-automate", $"{Environment.ProcessId}-{DateTime.Now:HHmmss}");
        try
        {
            var jobs = new List<RunJob>();
            try
            {
                if (workflowPath is not null)
                {
                    // The file as it is now: the rows and the lines from the same reading.
                    var workflow = WorkflowFolder.ReadWorkflow(workflowPath);
                    Post(() => WorkflowReread(workflow));
                    jobs.Add(Job(workflow));
                }
                else if (folder is not null)
                {
                    jobs.AddRange(WorkflowFolder.Read(folder).Where(entry => !entry.IsFolder).Select(entry => Job(WorkflowFolder.ReadWorkflow(entry.Path))));
                }
            }
            catch (Exception ex)
            {
                Log.Error("axAutomate: a run's workflows could not be read", ex);
                Post(() => Announce("The run could not read its workflows: " + ex.Message, false));
                return;
            }

            count = jobs.Count;
            if (count == 0)
            {
                Post(() => Announce("No workflows in this folder to run", false));
            }

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

                ScriptResult result;
                try
                {
                    var temp = Path.Combine(root, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(temp);
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
                        hosted: true,
                        begun: run => _active = run);
                }
                catch (Exception ex)
                {
                    // Whatever escaped the engine fails this workflow; the window and the rest of the run go on.
                    Log.Error("axAutomate: a run could not go on", ex);
                    result = new ScriptResult(false, ex.Message, null, [$"{ex.GetType().Name}: {ex.Message}"], false);
                }
                finally
                {
                    _active = null;
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
        catch (Exception ex)
        {
            // A last guard: an exception out of this thread would end the user's window.
            Log.Error("axAutomate: a run ended on an error", ex);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A process still closing holds its folder, or none was made; the temp folder is Windows' to clean.
            }

            var stopped = _stopAsked;
            var summary = count > 1 ? RunSentences.FolderEnded(passed, failed, cannotRun, stopped) : null;
            Post(() => RunEnded(summary));
        }
    }

    /// <summary>The workflow in hand as the run read it again, on the window's thread: its rows shown, if it is still the one in hand.</summary>
    private void WorkflowReread(Workflow workflow)
    {
        if (_workflow is { } open && string.Equals(open.Path, workflow.Path, StringComparison.OrdinalIgnoreCase))
        {
            _workflow = workflow;
            _steps = StepText.Rows(workflow);
            Fill(select: SelectedKey());
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

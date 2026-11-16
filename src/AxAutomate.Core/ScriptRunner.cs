using System.Text;
using System.Text.Json;

namespace AxAutomate.Core;

/// <summary>
/// One line a run plays (SPEC.md AU-5.2): its number (the line of a script; a workflow file's steps counted in the
/// order they run), the line as the runner reads it, and the workflow step it stands for (null when none is known).
/// </summary>
public sealed record ScriptLine(int Number, string Text, WorkflowStep? Step);

/// <summary>What one line did: passed or not, and what the runner saw.</summary>
public sealed record LineResult(ScriptLine Line, bool Ok, string? Detail);

/// <summary>
/// A run's end: whether it passed, the log for the report (every line and the timeline), the line that failed (null
/// when none did), what failed after the steps (a window that ended by itself, an ERROR in a log), and whether it was
/// stopped before its end (<see cref="ScriptRunner.Play"/>).
/// </summary>
public sealed record ScriptResult(bool Ok, string Log, LineResult? Failed, IReadOnlyList<string> Problems, bool Stopped)
{
    /// <summary>Each section's result (docs/axKit/user-workflows.md WF-3.9), in the script's order; none when the run was given no sections.</summary>
    public IReadOnlyList<GoalResult> Goals { get; init; } = [];

    /// <summary>
    /// The listings the run saw (<see cref="Run.Snapshots"/>, WF-2.7), in the order of its steps: a window's whole
    /// listing at each <c>check commands</c> step, the one resolved row at each <c>do</c> step; none when the script
    /// has neither step or the window does not list its commands as data.
    /// </summary>
    public IReadOnlyList<AxKit.Core.WindowCommands> Commands { get; init; } = [];

    /// <summary>
    /// The run's trace (WF-5.1, <see cref="Traces.Sections"/>), section by section in the script's order, the setup
    /// among them: each input step and what the windows said under it, as far as the run got; none unless the run was
    /// played with <see cref="PlayOptions.Trace"/>.
    /// </summary>
    public IReadOnlyList<TraceSection> Trace { get; init; } = [];

    /// <summary>
    /// The presses of the script's <c>keys</c> lines that the converter would write as <c>do</c> lines (WF-10.1), each
    /// with its line's number, in the order they were pressed; none unless the run was played with
    /// <see cref="PlayOptions.Convert"/>.
    /// </summary>
    public IReadOnlyList<Conversion> Conversions { get; init; } = [];
}

/// <summary>
/// What a run of the command line adds to playing the lines: the script's sections (<see cref="ScriptReader.Goals"/>),
/// whose lines the report then gives (WF-3.10) and whose results <see cref="ScriptResult.Goals"/> holds; and, with
/// <paramref name="Trace"/>, the run's trace (WF-5.1) in <see cref="ScriptResult.Trace"/>: after each line every
/// window is asked once more for what it recorded, and nothing else of the run changes. With
/// <paramref name="Convert"/>, the converter's run (WF-10.1, <see cref="Run.Converting"/>): the window is asked for
/// its commands before each press that may be a command's, and <see cref="ScriptResult.Conversions"/> holds the
/// presses to write as <c>do</c> lines. axAutomate's window plays without options and its run says what it said
/// before (WF-D-21).
/// </summary>
public sealed record PlayOptions(IReadOnlyList<Goal>? Goals = null, bool Trace = false, bool Convert = false);

/// <summary>
/// The runner's engine (SPEC.md AU-5.1), moved here from <c>tools/AxAutomate</c> unchanged: the lines of one
/// workflow, each a step of <see cref="Run"/>, played one after another on the run's own desktop until one fails; then
/// every window closed and what went wrong outside the steps said. The command line and axAutomate's window both play
/// a workflow through <see cref="Play"/>, with the lines <see cref="ScriptLines.Prepare"/> made of it.
/// </summary>
public static class ScriptRunner
{
    /// <summary>
    /// Plays the lines with their own windows and folder (<paramref name="temp"/>, <c>{temp}</c> in a line). Each
    /// line's result goes to <paramref name="output"/> as the console shows it, and to <paramref name="stepEnded"/>
    /// (when given); <paramref name="stop"/> is asked before each line after the first, and a true ends the run there
    /// (AU-4.4, Escape). <paramref name="hosted"/>: the run lives in axAutomate's window (<see cref="Run"/>);
    /// <paramref name="begun"/> gets the run as it begins, for <see cref="Run.Abandon"/> when the window closes.
    /// <paramref name="options"/>: the script's sections; when one is named by a goal or check line, a line before
    /// each section's first step (<c>-- Setup</c>, <c>-- Goal: </c>, <c>-- Check: </c>) and, after the last step and
    /// what failed after it, the counts and each section's result (<see cref="GoalResults.Lines"/>); with
    /// <see cref="PlayOptions.Trace"/> the run's trace is collected line by line (<see cref="Run.TakeTraceEvents"/>,
    /// WF-5.1), which prints nothing; with <see cref="PlayOptions.Convert"/> the presses the converter would move
    /// are collected (<see cref="Run.TakeConversions"/>, WF-10.1), which prints nothing either.
    /// </summary>
    public static ScriptResult Play(IReadOnlyList<ScriptLine> lines, string exe, string temp, Action<string> output, Action<LineResult>? stepEnded = null, Func<bool>? stop = null, bool hosted = false, Action<Run>? begun = null, PlayOptions? options = null)
    {
        var log = new StringBuilder();
        var ok = true;
        var stopped = false;
        LineResult? failed = null;
        var problems = new List<string>();
        var goals = options?.Goals ?? [];
        var sections = goals.Any(goal => goal.Named);
        var headed = false;
        Goal? section = null;
        var lastLinePlayed = int.MaxValue;
        var trace = options?.Trace ?? false;
        var traced = new List<TracedLine>();
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var convert = options?.Convert ?? false;
        var conversions = new List<Conversion>();
        var run = new Run(exe, temp, hosted) { Converting = convert };
        begun?.Invoke(run);
        try
        {
            for (var index = 0; index < lines.Count; index++)
            {
                var script = lines[index];
                if (stop is not null && index > 0 && stop())
                {
                    stopped = true;
                    lastLinePlayed = script.Number - 1;
                    var text = $"stopped before line {script.Number}";
                    output(text);
                    log.AppendLine(text);
                    break;
                }

                if (sections && (!headed || ScriptReader.GoalAt(goals, script.Number) != section))
                {
                    // WF-3.10: the section's line before its first step, the setup's too.
                    headed = true;
                    section = ScriptReader.GoalAt(goals, script.Number);
                    var heading = GoalResults.Heading(section);
                    output(heading);
                    log.AppendLine(heading);
                }

                // {temp}, and \uXXXX escapes, which a script may use for a character it cannot type (· + > ").
                var line = ScriptLines.Filled(script.Text, temp);
                (bool Ok, string? Detail) result;
                try
                {
                    result = run.Step(line);
                }
                catch (Exception ex)
                {
                    // Any exception fails the step and never the process: in axAutomate's window an escaped one would
                    // end the user's app (a malformed number, a window gone mid-step).
                    result = (false, $"{ex.GetType().Name}: {ex.Message}");
                }

                var shown = $"{(result.Ok ? "ok  " : "FAIL")} {script.Number}: {script.Text}{(result.Detail is null ? string.Empty : "  -> " + result.Detail)}";
                output(shown);
                log.AppendLine(shown);
                var ended = new LineResult(script, result.Ok, result.Detail);
                stepEnded?.Invoke(ended);
                if (convert)
                {
                    // WF-10.1: the line's presses the window listed as one command each, with the line's number.
                    conversions.AddRange(run.TakeConversions().Select(taken => new Conversion(script.Number, taken.Press, taken.Name)));
                }

                if (trace)
                {
                    // WF-5.1, WF-5.3: the line as an input step (the script's own line, before {temp} is filled),
                    // then what every window recorded up to here, each line without the run's folder and the profile.
                    var input = Traces.InputLine(script.Text, result.Detail);
                    traced.Add(new TracedLine(
                        script.Number,
                        input is null ? null : Traces.Clean(input, temp, profile),
                        [.. run.TakeTraceEvents().Select(taken => Traces.Clean(taken, temp, profile))]));
                }

                if (!result.Ok)
                {
                    ok = false;
                    failed = ended;
                    log.AppendLine("The timeline so far:");
                    log.Append(run.Timeline());
                    if (run.LastOfTimeline(20) is { Length: > 0 } last)
                    {
                        output(last.TrimEnd('\r', '\n'));
                    }
                    break;
                }
            }
        }
        finally
        {
            foreach (var problem in run.Finish())
            {
                ok = false;
                problems.Add(problem);
                var text = "FAIL after the steps: " + problem;
                output(text);
                log.AppendLine(text);
            }

            foreach (var swept in run.Swept)
            {
                var text = "after the steps: " + swept;
                output(text);
                log.AppendLine(text);
            }
        }

        // WF-3.9: which sections passed, failed and were not run; said only for a script that names its sections.
        var failedLine = failed?.Line.Number ?? 0;
        IReadOnlyList<GoalResult> results = goals.Count == 0 ? [] : GoalResults.Of(goals, failedLine, lastLinePlayed, problems);
        if (sections)
        {
            var setupFailed = failed is not null && ScriptReader.GoalAt(goals, failedLine) is null ? failedLine : 0;
            foreach (var text in GoalResults.Lines(results, setupFailed))
            {
                output(text);
                log.AppendLine(text);
            }
        }

        if (ok)
        {
            log.AppendLine("The timeline:");
            log.Append(run.Timeline());
        }

        return new ScriptResult(ok, log.ToString(), failed, problems, stopped)
        {
            Goals = results,
            Commands = run.Snapshots,
            Trace = trace ? Traces.Sections(traced, goals) : [],
            Conversions = conversions,
        };
    }
}

using System.Text;
using AxAutomate.Core;

namespace AxAutomate;

/// <summary>
/// The traces in a run of the command line (docs/axKit/user-workflows.md WF-5.4 to WF-5.8): which scripts the engine
/// collects a trace of (<see cref="Playing"/>), and after each script (<see cref="After"/>) the kept trace of its
/// <c># Stable</c> sections, <c>tests/traces/&lt;folder&gt;/&lt;script&gt;.trace.txt</c>, compared under
/// <c>--traces check</c> or written under <c>--traces write</c>; the run's last line of them (<see cref="End"/>) and
/// a script's trace for the report (<see cref="Report"/>). The lines, the sections, the file and the comparison are
/// <see cref="Traces"/>'; this class only holds the run's part. Under <c>--traces off</c> (always so on the NVDA tier
/// and without a repository) it does nothing and no trace is collected: such a run plays exactly as one did before
/// there were traces.
/// </summary>
internal static class TraceRun
{
    private static string _mode = RunOptions.Off;
    private static string _repository = string.Empty;
    private static bool _report;

    /// <summary>The repository's scripts by their label, each with its path from the repository's root; read when a script from elsewhere is first played.</summary>
    private static Dictionary<string, List<string>>? _byLabel;

    /// <summary>The script being played: its path from the repository's root with forward slashes, null when it may keep no trace or is none of the repository's; and its sections.</summary>
    private static string? _script;

    private static IReadOnlyList<Goal> _goals = [];

    private static int _equal;
    private static int _differ;
    private static int _without;
    private static int _written;
    private static int _deleted;

    /// <summary>The run's mode (<see cref="RunOptions.Traces"/>), its repository, and whether its report holds every script's trace (<see cref="RunOptions.TraceReport"/>).</summary>
    public static void Start(string mode, string? repository, bool report)
    {
        _mode = repository is null ? RunOptions.Off : mode;
        _repository = repository ?? string.Empty;
        _report = report;
    }

    /// <summary>
    /// Before a script plays: whether the engine collects its trace (<see cref="PlayOptions.Trace"/>). It does for a
    /// script with a <c># Stable</c> section that may keep a trace (<see cref="Traces.MayKeep"/>), since that section
    /// is compared or written after it, and for every script of a run whose report holds the traces
    /// (<c>--report</c> with <c>--traces check</c> or <c>write</c> given by name); for no other, and for none under
    /// <c>--traces off</c>: a traced script is asked once more after each line, so it is traced only where asked. <paramref name="workflow"/> is the script as read, null for a workflow file,
    /// which has no sections. The script's place in the repository is its own path when it lies there, else that of
    /// the one script of the repository with its label (<see cref="Place"/>).
    /// </summary>
    public static bool Playing(string script, Workflow? workflow)
    {
        _script = null;
        _goals = workflow?.Goals ?? [];
        if (_mode == RunOptions.Off)
        {
            return false;
        }

        if (workflow is not null && Place(script) is { } place && Traces.MayKeep(place, workflow.Needs))
        {
            _script = place;
        }

        return _report || (_script is not null && _goals.Any(goal => goal.Stable));
    }

    /// <summary>
    /// After a script, its result as the traces leave it. <c>check</c>, for a script whose steps passed (WF-5.5): the
    /// trace of each <c># Stable</c> section held against its kept section; one that differs or has none fails its
    /// section alone (<see cref="GoalResults.WithFailed"/>) and so the script, with "FAIL after the steps: ..." said
    /// and, for a script that names its sections, the sections' lines once more, as they now stand. <c>write</c>, for
    /// a script that passed (WF-5.8): its file written whole, or deleted when the script has no stable section, one
    /// line saying which. A script that may keep no trace, or is none of the repository's, comes back as it was.
    /// </summary>
    public static ScriptResult After(ScriptResult result, Action<string> say)
    {
        if (_mode == RunOptions.Off || _script is null)
        {
            return result;
        }

        var kept = Traces.PathFor(_script);
        var path = Path.Combine(_repository, kept.Replace('/', Path.DirectorySeparatorChar));
        var marked = _goals.Any(goal => goal.Stable);
        var stable = Traces.Stable(result.Trace, _goals);
        if (_mode == RunOptions.Write)
        {
            if (!result.Ok || result.Stopped)
            {
                return result;
            }

            if (!marked)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _deleted++;
                    say($"Trace deleted: {kept}: the script has no stable section");
                }

                return result;
            }

            // The repository's files are checked out with CRLF (.gitattributes, text=auto): written so, a trace that
            // did not change is not changed on disk either. UTF-8 without a byte order mark.
            var text = Traces.Write(_script, stable);
            if (File.Exists(path) && Lf(File.ReadAllText(path)) == text)
            {
                say($"Trace as kept: {kept}: nothing written");
                return result;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(false));
            _written++;
            say($"Trace written: {kept}, {stable.Count} stable section(s). Read it whole before it is committed (docs/axKit/user-workflows.md WF-5.6)");
            return result;
        }

        if (!marked || result.Failed is not null || result.Stopped)
        {
            return result;
        }

        var differences = Traces.Compare(File.Exists(path) ? Traces.Read(File.ReadAllText(path)) : [], stable);
        _equal += stable.Count - differences.Count;
        _differ += differences.Count(difference => difference.Line > 0);
        _without += differences.Count(difference => difference.Line == 0);
        if (differences.Count == 0)
        {
            return result;
        }

        var log = new StringBuilder(result.Log);
        var problems = result.Problems.ToList();
        var goals = result.Goals;
        foreach (var difference in differences)
        {
            var problem = Traces.Problem(difference);
            var text = "FAIL after the steps: " + problem;
            say(text);
            log.AppendLine(text);
            problems.Add(problem);
            var title = _goals.FirstOrDefault(goal => Traces.Heading(goal) == difference.Heading)?.Title ?? string.Empty;
            var detail = difference.Line == 0 ? "after the steps: it has no kept trace" : $"after the steps: its trace differs from the kept one at line {difference.Line}";
            goals = GoalResults.WithFailed(goals, title, detail);
        }

        if (_goals.Any(goal => goal.Named))
        {
            // The sections' lines were said before the traces were compared: once more, as they now stand.
            foreach (var text in GoalResults.Lines(goals, 0))
            {
                say(text);
                log.AppendLine(text);
            }
        }

        return result with { Ok = false, Log = log.ToString(), Problems = problems, Goals = goals };
    }

    /// <summary>
    /// The run's last line of the traces, none when it met no stable section: under <c>check</c> how many stable
    /// sections were equal to their kept trace, differed, and had none; under <c>write</c> how many files were written
    /// and deleted.
    /// </summary>
    public static IReadOnlyList<string> End()
    {
        if (_mode == RunOptions.Check && _equal + _differ + _without > 0)
        {
            return [$"Traces: {_equal} stable sections equal, {_differ} differ, {_without} without a kept trace"];
        }

        if (_mode == RunOptions.Write && _written + _deleted > 0)
        {
            return [$"Traces: {_written} file(s) written, {_deleted} deleted. Commit them with the scripts' # Stable lines"];
        }

        return [];
    }

    /// <summary>
    /// A script's trace for the report (WF-5.4), every section and the setup among them, in the form of a kept file
    /// without its header; empty when none was collected.
    /// </summary>
    public static string Report(ScriptResult result)
    {
        if (result.Trace.Count == 0)
        {
            return string.Empty;
        }

        var text = new StringBuilder("The trace (docs/axKit/user-workflows.md WF-5), each input and what the windows said under it:\n\n```\n");
        for (var i = 0; i < result.Trace.Count; i++)
        {
            var section = result.Trace[i];
            text.Append(i == 0 ? string.Empty : "\n").Append("== ").Append(section.Heading).Append('\n');
            foreach (var line in section.Lines)
            {
                text.Append(line).Append('\n');
            }
        }

        return text.Append("```\n\n").ToString();
    }

    /// <summary>
    /// The script's path from the repository's root, forward slashes: its own when it lies in the repository; else
    /// that of the one script of <c>tests/scripts</c> and <c>tests/nvda-scripts</c> that has its label (a copy played
    /// from a scratch folder is that script); null when it has no label or no one script of the repository has it.
    /// </summary>
    private static string? Place(string script)
    {
        var relative = Path.GetRelativePath(_repository, Path.GetFullPath(script));
        if (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return relative.Replace('\\', '/');
        }

        var label = Label(script);
        if (label.Length == 0)
        {
            return null;
        }

        if (_byLabel is null)
        {
            _byLabel = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var folder in new[] { "scripts", "nvda-scripts" })
            {
                var under = Path.Combine(_repository, "tests", folder);
                if (!Directory.Exists(under))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(under, "*" + ScriptReader.Extension, SearchOption.AllDirectories))
                {
                    var its = Label(file);
                    if (its.Length == 0)
                    {
                        continue;
                    }

                    if (!_byLabel.TryGetValue(its, out var paths))
                    {
                        _byLabel[its] = paths = [];
                    }

                    paths.Add(Path.GetRelativePath(_repository, file).Replace('\\', '/'));
                }
            }
        }

        return _byLabel.TryGetValue(label, out var found) && found.Count == 1 ? found[0] : null;
    }

    /// <summary>A script's label, its <c># Workflow:</c> line; empty when it has none or cannot be read.</summary>
    private static string Label(string script)
    {
        try
        {
            return ScriptReader.Parse(File.ReadAllLines(script), string.Empty, script).Title;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}

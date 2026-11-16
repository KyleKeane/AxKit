using System.Diagnostics;
using System.Text;
using AxAutomate.Core;

namespace AxAutomate;

/// <summary>
/// The workflow record in the command line (docs/axKit/user-workflows.md WF-6, WP-B13): under <c>--record</c> every
/// section of every script the run played is entered into <c>docs/axKit/workflow-record.json</c> for the run's tier,
/// after the last script. A section is known by its workflow's label and its title, so a script played from a copy
/// (<c>nvda-pass.ps1</c>) finds its entry; a script that is none of the repository's is not entered. The pure part is
/// <see cref="WorkflowRecord"/>; this class reads the scripts, asks git for the commit and writes the file. A recorded
/// run changes this one file and no page (WF-6.6).
/// </summary>
internal static class RecordRun
{
    /// <summary>The record's path from the repository's root, as the run's line names it.</summary>
    public const string File = "docs/axKit/workflow-record.json";

    private static bool _on;
    private static string _repository = string.Empty;
    private static string _tier = WorkflowRecord.Desktop;
    private static string _version = "unknown";

    /// <summary>The sections the run played: the workflow's label, the section's title and its result.</summary>
    private static readonly List<(string Workflow, string Goal, string Result)> Played = [];

    /// <summary>Whether the run records, its repository, its tier (<c>nvda</c> under <c>--nvda</c>, else <c>desktop</c>) and the AxKit.exe it drives, whose product version up to a <c>+</c> is the version written (WF-6.3).</summary>
    public static void Start(bool record, string? repository, bool nvda, string exe)
    {
        _on = record && repository is not null;
        _repository = repository ?? string.Empty;
        _tier = nvda ? WorkflowRecord.Nvda : WorkflowRecord.Desktop;
        var product = FileVersionInfo.GetVersionInfo(exe).ProductVersion;
        _version = string.IsNullOrEmpty(product) ? "unknown" : product.Split('+')[0];
        Played.Clear();
    }

    /// <summary>After a script: its sections' results kept for the end, under the script's label. A workflow file has no sections and a script without a label is known nowhere: neither is entered.</summary>
    public static void After(string script, ScriptResult result)
    {
        if (!_on || script.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var label = Label(script);
        if (label.Length == 0)
        {
            return;
        }

        foreach (var goal in result.Goals)
        {
            Played.Add((label, goal.Goal.Title, goal.Outcome));
        }
    }

    /// <summary>
    /// After the last script: the record read, every section of the run that a script of the repository has entered,
    /// what no script has any more dropped, and the file written when it changed. The line that says it, or none when
    /// the run does not record.
    /// </summary>
    public static IReadOnlyList<string> End()
    {
        if (!_on)
        {
            return [];
        }

        var existing = Existing();
        var path = Path.Combine(_repository, File.Replace('/', Path.DirectorySeparatorChar));
        var before = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
        var goals = before is null ? [] : WorkflowRecord.Read(before);
        var commit = Commit();
        var date = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var entered = 0;
        foreach (var (workflow, goal, result) in Played)
        {
            var place = existing.FirstOrDefault(known => known.Workflow == workflow && known.Goal == goal);
            if (place.Script is null)
            {
                continue;
            }

            goals = WorkflowRecord.With(goals, workflow, goal, place.Script, _tier, new RecordedRun(result, _version, commit, date));
            entered++;
        }

        var text = WorkflowRecord.Write(WorkflowRecord.Pruned(goals, existing));
        if (text != before)
        {
            // The repository's files are checked out with CRLF (.gitattributes, text=auto): written so, as the command
            // inventory is. UTF-8 without a byte order mark.
            System.IO.File.WriteAllText(path, text.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(false));
        }

        return [$"Recorded {entered} goals and checks in the workflow record ({File})."];
    }

    /// <summary>Every section of every script of <c>tests/scripts</c> and <c>tests/nvda-scripts</c>, in the order of the scripts' paths: the label, the title and the script's path from the repository's root with forward slashes. A label two scripts share belongs to neither.</summary>
    private static IReadOnlyList<(string Workflow, string Goal, string Script)> Existing()
    {
        var sections = new List<(string Workflow, string Goal, string Script)>();
        foreach (var folder in new[] { "scripts", "nvda-scripts" })
        {
            var under = Path.Combine(_repository, "tests", folder);
            if (!Directory.Exists(under))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(under, "*" + ScriptReader.Extension, SearchOption.AllDirectories))
            {
                var workflow = ScriptReader.Parse(System.IO.File.ReadAllLines(file), Path.GetFileNameWithoutExtension(file), file);
                var label = Label(file);
                if (label.Length == 0)
                {
                    continue;
                }

                var place = Path.GetRelativePath(_repository, file).Replace('\\', '/');
                foreach (var goal in workflow.Goals)
                {
                    sections.Add((label, goal.Title, place));
                }
            }
        }

        var shared = sections.GroupBy(section => section.Workflow, StringComparer.Ordinal)
            .Where(group => group.Select(section => section.Script).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        return sections.Where(section => !shared.Contains(section.Workflow))
            .OrderBy(section => section.Script.ToLowerInvariant(), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>A script's label, its <c># Workflow:</c> line; empty when it has none or cannot be read.</summary>
    private static string Label(string script)
    {
        try
        {
            return ScriptReader.Parse(System.IO.File.ReadAllLines(script), string.Empty, script).Title;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    /// <summary>The repository's commit, short, with a <c>+</c> after it when <c>src</c> or <c>tests</c> hold a change not committed; "unknown" when git does not answer (WF-6.3).</summary>
    private static string Commit()
    {
        var head = Git("rev-parse", "--short", "HEAD");
        var status = Git("status", "--porcelain", "--", "src", "tests");
        if (head is null || status is null || head.Trim().Length == 0)
        {
            return "unknown";
        }

        return head.Trim() + (status.Trim().Length > 0 ? "+" : string.Empty);
    }

    /// <summary>What git prints for the arguments in the repository; null when it cannot be started, does not end within 30 s or fails.</summary>
    private static string? Git(params string[] arguments)
    {
        try
        {
            var start = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = _repository,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(start);
            if (process is null)
            {
                return null;
            }

            process.ErrorDataReceived += (_, _) => { };
            process.BeginErrorReadLine();
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                return null;
            }

            return process.ExitCode == 0 ? output.GetAwaiter().GetResult() : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}

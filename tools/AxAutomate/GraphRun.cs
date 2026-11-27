using System.Text;
using System.Text.Json;
using AxAutomate.Core;

namespace AxAutomate;

/// <summary>
/// The workflow graph in a run of the command line (docs/axKit/workflow-graph.md WG-2): under <c>--inventory write</c>
/// and <c>rewrite</c>, after the first script of an app that passed and lists the window's commands (a <c>check
/// commands</c> step after the app's <c>start</c>), the app's window is started afresh on the hidden desktop as that
/// script starts it (its <c>file</c>, <c>mkdir</c>, <c>silence</c>, <c>run</c> and <c>allow</c> steps before the start,
/// then the start, in a folder of its own) and crawled (<see cref="GraphCrawl"/>); at the run's end the crawled apps
/// are merged into <c>docs/axKit/workflow-graph.json</c> (<see cref="WorkflowGraph.Merge"/>), the other apps kept, and
/// the file written when it changed. A script run without a repository, or under <c>check</c> or <c>off</c>, crawls
/// nothing. Never crawled, and said once: an app whose scripts may keep no trace (<see cref="Traces.MayKeep"/>: its
/// window reads the computer or the user's session, or its script needs an outside program, so its words would be the
/// computer's), and an app whose keys reach outside its window (<see cref="CrawlsNot"/>). <c>docs/axKit/map.md</c> is
/// written from the file by <c>tools/map.ps1</c>.
/// </summary>
internal static class GraphRun
{
    /// <summary>The file's place in the repository, as the run's lines name it.</summary>
    public const string File = "docs/axKit/workflow-graph.json";

    private static bool _on;
    private static string _path = string.Empty;
    private static string _root = string.Empty;
    private static string _exe = string.Empty;

    /// <summary>Each app's kind of window, from docs/axKit/components.json.</summary>
    private static readonly Dictionary<string, string> Types = new(StringComparer.Ordinal);

    private static readonly List<CrawlResult> Crawled = [];

    /// <summary>The apps this run passed over, each said once.</summary>
    private static readonly HashSet<string> Passed = new(StringComparer.Ordinal);

    private static string _repository = string.Empty;

    /// <summary>
    /// The apps whose windows a crawl must not try key by key, each with why: their keys reach outside the window, where
    /// a script steps around them and a crawl cannot (docs/axKit/automation.md, "What stays manual").
    /// </summary>
    private static readonly Dictionary<string, string> CrawlsNot = new(StringComparer.Ordinal)
    {
        ["axFile"] = "Enter opens a file in the computer's own program and Left at the top lists the computer's drives",
        ["axTemplate"] = "Enter types a template into the program in front",
    };

    /// <summary>The run's start: whether it crawls (write or rewrite, with a repository), where the file is, and the run's folder and program.</summary>
    public static void Start(string inventory, string? repository, string root, string exe)
    {
        _on = repository is not null && inventory is RunOptions.Write or RunOptions.Rewrite;
        if (!_on)
        {
            return;
        }

        _repository = repository!;
        _path = Path.Combine(repository!, "docs", "axKit", "workflow-graph.json");
        _root = root;
        _exe = exe;
        var components = Path.Combine(repository!, "docs", "axKit", "components.json");
        if (System.IO.File.Exists(components))
        {
            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(components));
            foreach (var app in document.RootElement.GetProperty("apps").EnumerateArray())
            {
                Types[app.GetProperty("app").GetString()!] = app.GetProperty("type").GetString()!;
            }
        }
    }

    /// <summary>After a script: its app crawled when the script passed, lists the commands of a window it started, and the app was not crawled yet in this run.</summary>
    public static void After(string script, ScriptResult result, Action<string> say)
    {
        if (!_on || !result.Ok || script.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var lines = System.IO.File.ReadAllLines(script);
        if (StartOf(lines) is not { } found || Crawled.Any(done => done.App == found.App) || Passed.Contains(found.App))
        {
            return;
        }

        // The graph holds axKit's own words, as a kept trace does (WG-2.1, WF-5.6): never the windows that read the
        // computer or the user's session, nor a script that needs an outside program; nor the windows whose keys reach
        // outside them (CrawlsNot).
        var relative = Path.GetRelativePath(_repository, script).Replace('\\', '/');
        if (!Traces.MayKeep(relative, ScriptReader.NeedsOfFile(script)) || CrawlsNot.TryGetValue(found.App, out _))
        {
            Passed.Add(found.App);
            var why = CrawlsNot.TryGetValue(found.App, out var reason) ? reason : "its window reads the computer, the user's session or an outside program, whose words the file never holds";
            say($"-- The workflow graph: {found.App} is not crawled: {why}");
            return;
        }

        var temp = Path.Combine(_root, "graph-" + found.App);
        Directory.CreateDirectory(temp);
        say($"-- The workflow graph: {found.App}'s window crawled ({File})");
        var run = new Run(_exe, temp);
        var notes = new List<string>();
        try
        {
            foreach (var setup in found.Setup)
            {
                var (ok, detail) = run.Step(ScriptLines.Filled(setup, temp));
                if (!ok)
                {
                    notes.Add($"{found.App}: the setup step {setup} failed: {detail}");
                }
            }

            var start = ScriptLines.Filled(found.Start, temp);
            var (started, why) = run.Step(start);
            if (!started)
            {
                say($"{found.App}: the window did not start for the crawl: {why}");
                return;
            }

            var crawl = GraphCrawl.Crawl(run, found.App, start, temp, say);
            Crawled.Add(crawl with { Notes = [.. notes, .. crawl.Notes] });
            foreach (var note in crawl.Notes)
            {
                say("  " + note);
            }
        }
        catch (Exception ex)
        {
            // The crawl never fails the run's scripts: the app is left as the file had it.
            say($"{found.App}: the crawl stopped: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            foreach (var problem in run.Finish())
            {
                say($"  {found.App}, after the crawl: {problem}");
            }
        }
    }

    /// <summary>
    /// The window a script lists the commands of: the app of its first <c>check commands</c> step whose window a
    /// <c>start</c> opened, that start line, and the steps before it that make files and folders (filled at the crawl).
    /// </summary>
    private static (string App, string Start, List<string> Setup)? StartOf(string[] lines)
    {
        var apps = ScriptApps.ByLine(lines);
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() != "check commands" || apps[i] is not { } app)
            {
                continue;
            }

            for (var j = i - 1; j >= 0; j--)
            {
                var line = lines[j].Trim();
                if (line.StartsWith("start ", StringComparison.Ordinal) && apps[j] == app)
                {
                    var setup = lines.Take(j).Select(text => text.Trim())
                        .Where(text => text.Split(' ')[0] is "file" or "mkdir" or "silence" or "run" or "allow").ToList();
                    return (app, line, setup);
                }

                if (line.StartsWith("start ", StringComparison.Ordinal) || line.StartsWith("adopt", StringComparison.Ordinal) || line.StartsWith("use ", StringComparison.Ordinal))
                {
                    break;
                }
            }

            return null;
        }

        return null;
    }

    /// <summary>The run's end: the crawled apps merged into the file, written when it changed; the lines that say so.</summary>
    public static IReadOnlyList<string> End(bool stopped)
    {
        if (!_on || stopped || Crawled.Count == 0)
        {
            return [];
        }

        var before = System.IO.File.Exists(_path) ? System.IO.File.ReadAllText(_path).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
        var graph = before is null ? WorkflowGraph.Empty : WorkflowGraph.Read(before);
        if (graph is null)
        {
            return [$"{File} is not a workflow graph of the format \"{WorkflowGraph.Format}\": not written. Put it right, or delete it, and run again."];
        }

        var crawled = Crawled.Where(crawl => crawl.Nodes.Count > 0).ToList();
        var apps = crawled.Select(crawl => new GraphApp(crawl.App, Types.GetValueOrDefault(crawl.App, string.Empty), crawl.Start)).ToList();
        var merged = WorkflowGraph.Merge(graph, apps, [.. crawled.SelectMany(crawl => crawl.Nodes)], [.. crawled.SelectMany(crawl => crawl.Unplaced)]);
        var text = WorkflowGraph.Write(merged);
        var lines = new List<string>();
        var nodes = merged.Nodes.Count;
        var edges = merged.Nodes.Sum(node => node.Edges.Count);
        if (before == text)
        {
            lines.Add($"The workflow graph ({File}) holds the crawled apps as they are: nothing written.");
        }
        else
        {
            System.IO.File.WriteAllText(_path, text.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(false));
            lines.Add($"The workflow graph ({File}) written: {string.Join(", ", crawled.Select(crawl => crawl.App))} crawled; {merged.Apps.Count} app(s), {nodes} node(s), {edges} edge(s). Run tools\\map.ps1, and commit both.");
        }

        foreach (var note in Crawled.SelectMany(crawl => crawl.Notes))
        {
            lines.Add("  " + note);
        }

        if (merged.Unplaced.Count > 0)
        {
            lines.Add($"  Controls that take the focus and are no place (the places test fails on them): {string.Join("; ", merged.Unplaced)}");
        }

        return lines;
    }
}

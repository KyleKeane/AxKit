using System.Text;
using AxAutomate.Core;

namespace AxAutomate;

/// <summary>
/// The bounds written or checked (docs/axKit/workflow-graph.md WG-3.4): <c>AxAutomate --bounds [--check | --write]</c>,
/// which <c>tools/graph-check.ps1</c> runs. It plays nothing and takes no lock. It reads every script of
/// <c>tests/scripts</c>, the command inventory and the graph file, counts each goal (<see cref="GoalCounts.Of"/>) and
/// finds the nodes over the waypoint bound (<see cref="GraphBounds.Misses"/>), and writes the page
/// <c>docs/axKit/over-the-bounds.md</c> and the baseline <c>docs/axKit/goal-counts.json</c>, each with CRLF, UTF-8
/// without a byte order mark. Without <c>--write</c> the baseline keeps a count that grew and leaves out a new goal over
/// four (<see cref="GoalCounts.Held"/>); with it every count goes in (<see cref="GoalCounts.Taken"/>). With
/// <c>--check</c> nothing is written: the page and the baseline are compared with what a write would give, line ends
/// aside, and every goal that grew against the baseline is named (<see cref="GoalCounts.Grown"/>). The exit code is 0
/// when written or current, 1 otherwise.
/// </summary>
internal static class BoundsRun
{
    /// <summary>The baseline's path from the repository's root.</summary>
    public const string CountsFile = "docs/axKit/goal-counts.json";

    public static int Run(string repository, bool check, bool write, Action<string> say)
    {
        var inventoryPath = Full(repository, InventoryRun.File);
        var inventory = File.Exists(inventoryPath) ? CommandInventory.Read(File.ReadAllText(inventoryPath)) : [];
        var graphPath = Full(repository, GraphRun.File);
        var graph = File.Exists(graphPath) ? WorkflowGraph.Read(File.ReadAllText(graphPath)) : WorkflowGraph.Empty;
        if (graph is null)
        {
            say($"{GraphRun.File} is not a workflow graph of the format \"{WorkflowGraph.Format}\": put it right, then run tools\\graph-check.ps1 again.");
            return 1;
        }

        var countsPath = Full(repository, CountsFile);
        var baselineText = File.Exists(countsPath) ? File.ReadAllText(countsPath) : null;
        var baseline = baselineText is null ? [] : GoalCounts.Read(baselineText);
        if (baseline is null)
        {
            say($"{CountsFile} is not a baseline of the format \"{GoalCounts.Format}\": put it right, or take it back from git.");
            return 1;
        }

        var folder = Full(repository, GoalCounts.ScriptsFolder);
        var goals = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.txt", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(repository, file).Replace('\\', '/'))
                .Order(StringComparer.Ordinal)
                .SelectMany(path => GoalCounts.Of(path, File.ReadAllLines(Full(repository, path)), inventory))
                .ToList()
            : [];
        var misses = GraphBounds.Misses(graph);
        var page = GraphBounds.Page(goals, misses, graph);
        var counts = GoalCounts.Write(write ? GoalCounts.Taken(goals) : GoalCounts.Held(goals, baseline));
        var over = goals.Count(goal => goal.Actions > GoalCounts.Bound);
        var four = goals.Count(goal => goal.Actions == GoalCounts.Bound);
        var summary = $"{goals.Count} goals, {over} over four actions, {four} at four; {misses.Select(miss => miss.From.Id).Distinct().Count()} nodes over the waypoint bound";
        var pagePath = Full(repository, GraphBounds.PagePath);
        if (check)
        {
            var problems = new List<string>();
            if (!File.Exists(pagePath) || Lf(File.ReadAllText(pagePath)) != page)
            {
                problems.Add($"{GraphBounds.PagePath} is not what the scripts and the graph give.");
            }

            if (baselineText is null || Lf(baselineText) != counts)
            {
                problems.Add($"{CountsFile} is not what the scripts give.");
            }

            var grown = GoalCounts.Grown(goals, baseline);
            foreach (var line in problems)
            {
                say(line);
            }

            foreach (var line in grown)
            {
                say("Longer: " + line);
            }

            if (grown.Count > 0)
            {
                say("A goal may get shorter, never longer (docs/axKit/workflow-graph.md WG-3.4): shorten it, or at the maintainer's word run tools\\graph-check.ps1 -Write and commit the counts.");
            }
            else if (problems.Count > 0)
            {
                say("Run tools\\graph-check.ps1 (or tools\\pages.ps1) and commit what it writes.");
            }

            if (problems.Count > 0 || grown.Count > 0)
            {
                return 1;
            }

            say($"{GraphBounds.PagePath} and {CountsFile} are current: {summary}.");
            return 0;
        }

        File.WriteAllText(pagePath, page.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(false));
        File.WriteAllText(countsPath, counts.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(false));
        say($"Wrote {GraphBounds.PagePath} and {CountsFile}: {summary}.");
        if (!write)
        {
            foreach (var line in GoalCounts.Grown(goals, baseline))
            {
                say("Held, longer: " + line);
            }
        }

        return 0;
    }

    private static string Full(string repository, string path) =>
        Path.Combine(repository, path.Replace('/', Path.DirectorySeparatorChar));

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}

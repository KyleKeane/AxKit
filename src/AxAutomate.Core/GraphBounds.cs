using System.Text;

namespace AxAutomate.Core;

/// <summary>
/// A node of the map from which a waypoint of its window takes more than two actions (WG-3.1): the app, the node, the
/// waypoint, and the fewest actions from one to the other, null when no way was found.
/// </summary>
public sealed record WaypointMiss(string App, GraphNode From, GraphNode Waypoint, int? Actions);

/// <summary>
/// The bounds over the map and the goals (docs/axKit/workflow-graph.md WG-3), pure: the waypoint bound over the graph
/// file by breadth-first search (<see cref="Misses"/>), and the page of what is over the bounds,
/// <c>docs/axKit/over-the-bounds.md</c> (<see cref="Page"/>), with the goals' counts of <see cref="GoalCounts"/>. The
/// command line writes and checks the page (<c>tools/AxAutomate --bounds</c>, which <c>tools/graph-check.ps1</c> runs);
/// tested in <c>GoalCountsTests</c>.
/// </summary>
public static class GraphBounds
{
    /// <summary>The most actions from any node of a window to any of its waypoints (WG-3.1).</summary>
    public const int WaypointBound = 2;

    /// <summary>The page's path from the repository's root.</summary>
    public const string PagePath = "docs/axKit/over-the-bounds.md";

    /// <summary>
    /// For every app the graph covers, every node of its window from which one of the window's waypoints takes more
    /// than two actions, or is not reached at all: the fewest edges from the node to the waypoint, an edge being one
    /// action, through the window's own nodes alone (an edge that leaves the window, to another app or to
    /// <c>outside</c>, is not followed: the app's own key back would make every way one action). By app, node and
    /// waypoint, ordinal.
    /// </summary>
    public static IReadOnlyList<WaypointMiss> Misses(WorkflowGraphFile graph)
    {
        var misses = new List<WaypointMiss>();
        foreach (var app in graph.Apps.Select(app => app.App).Order(StringComparer.Ordinal))
        {
            var nodes = graph.Nodes.Where(node => node.App == app).OrderBy(node => node.Id, StringComparer.Ordinal).ToList();
            var byId = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            var waypoints = nodes.Where(node => node.Waypoint).ToList();
            foreach (var node in nodes)
            {
                var distances = Distances(node, byId);
                foreach (var waypoint in waypoints)
                {
                    int? actions = distances.TryGetValue(waypoint.Id, out var found) ? found : null;
                    if (actions is null or > WaypointBound)
                    {
                        misses.Add(new WaypointMiss(app, node, waypoint, actions));
                    }
                }
            }
        }

        return misses;
    }

    /// <summary>The fewest actions from a node to each node of its window it reaches, itself at none.</summary>
    private static Dictionary<string, int> Distances(GraphNode from, IReadOnlyDictionary<string, GraphNode> window)
    {
        var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [from.Id] = 0 };
        var queue = new Queue<GraphNode>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var edge in node.Edges)
            {
                if (!distances.ContainsKey(edge.To) && window.TryGetValue(edge.To, out var next))
                {
                    distances[edge.To] = distances[node.Id] + 1;
                    queue.Enqueue(next);
                }
            }
        }

        return distances;
    }

    /// <summary>
    /// The page (WG-3.4), every line ended by a line feed: a short header with the totals, then by app the goals over
    /// four actions with their count and their steps (the most first), those at exactly four, over the ideal three, and
    /// the nodes over the waypoint bound. An app with nothing over the bounds has no section.
    /// </summary>
    public static string Page(IReadOnlyList<CountedGoal> goals, IReadOnlyList<WaypointMiss> misses, WorkflowGraphFile graph)
    {
        var over = goals.Where(goal => goal.Actions > GoalCounts.Bound).ToList();
        var atFour = goals.Where(goal => goal.Actions == GoalCounts.Bound).ToList();
        var nodes = graph.Nodes.Count(node => node.Id != WorkflowGraph.Outside);
        var missed = misses.Select(miss => miss.From.Id).Distinct(StringComparer.Ordinal).Count();
        var lines = new List<string>
        {
            "# Over the bounds",
            string.Empty,
            "Written by `tools/graph-check.ps1` from the scripts of `tests/scripts`, the command inventory and",
            "`docs/axKit/workflow-graph.json`; never edit it by hand. The bounds are SPEC.md G7 and `docs/axKit/workflow-graph.md`",
            "WG-3: a goal takes four actions at most and ideally three, and every waypoint of a window is two actions or fewer",
            "from any of its places. A goal's actions are counted as WG-3.3 says: the app's own key is free, the steps that",
            "only prepare or look are not counted, and neither is the example's text a goal types before its first command.",
            "The counts are held in `docs/axKit/goal-counts.json`: a goal may get shorter, never longer (WG-3.4).",
            string.Empty,
            $"{goals.Count} goals counted: {over.Count} over four actions, {atFour.Count} at four, {goals.Count - over.Count - atFour.Count} within three.",
            $"The map covers {graph.Apps.Count} apps and {nodes} nodes: {missed} nodes with a waypoint more than two actions away ({misses.Count} in all, a node and a waypoint each).",
        };

        var apps = goals.Select(goal => goal.App).Concat(misses.Select(miss => miss.App)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        foreach (var app in apps)
        {
            var appOver = Most(over.Where(goal => goal.App == app));
            var appFour = Most(atFour.Where(goal => goal.App == app));
            var appMisses = misses.Where(miss => miss.App == app).ToList();
            if (appOver.Count == 0 && appFour.Count == 0 && appMisses.Count == 0)
            {
                continue;
            }

            lines.Add(string.Empty);
            lines.Add($"## {app}");
            AddGoals(lines, "Over four actions", appOver);
            AddGoals(lines, "At four actions, over the ideal three", appFour);
            if (appMisses.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("### Over the waypoint bound");
                lines.Add(string.Empty);
                foreach (var miss in appMisses)
                {
                    var actions = miss.Actions is { } count ? $"{count} actions" : "no way found";
                    lines.Add($"- From {Node(miss.From)} to {Node(miss.Waypoint)}: {actions}.");
                }
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    /// <summary>The goals with the most actions first, then in the order of their scripts.</summary>
    private static List<CountedGoal> Most(IEnumerable<CountedGoal> goals) =>
        [.. goals.Select((goal, index) => (goal, index)).OrderByDescending(entry => entry.goal.Actions).ThenBy(entry => entry.index).Select(entry => entry.goal)];

    private static void AddGoals(List<string> lines, string title, IReadOnlyList<CountedGoal> goals)
    {
        if (goals.Count == 0)
        {
            return;
        }

        lines.Add(string.Empty);
        lines.Add($"### {title}");
        lines.Add(string.Empty);
        foreach (var goal in goals)
        {
            var line = new StringBuilder($"- **{goal.Workflow}: {goal.Goal}**, {goal.Actions} actions (`{goal.Script}`): {string.Join("; ", goal.Steps)}.");
            if (goal.Example.Count > 0)
            {
                line.Append($" Not counted, the example: {string.Join("; ", goal.Example)}.");
            }

            lines.Add(line.ToString());
        }
    }

    /// <summary>A node in words: the place's words, what is over it when anything is, and its id.</summary>
    private static string Node(GraphNode node) =>
        node.Over == WorkflowGraph.None ? $"{node.Words} (`{node.Id}`)" : $"{node.Words} with the {node.Over} over it (`{node.Id}`)";
}

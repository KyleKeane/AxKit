using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// One action from a node and where it lands (docs/axKit/workflow-graph.md WG-2.1): the chord pressed, the command's
/// label when a command of the window runs by it (empty for a key of none), the node it lands on, the first kind of
/// event raised (an id of <c>EventKind</c>, empty when none was), and the words the window said, with <c>{temp}</c> and
/// <c>{home}</c> for paths. <paramref name="Free"/> marks an app's own key from <c>outside</c>, which no bound counts
/// (the maintainer's decision 2 of 2026-11-26).
/// </summary>
public sealed record GraphEdge(string Action, string Command, string To, string Kind, string Said, bool Free = false);

/// <summary>
/// One node (WG section 2): a place of a window, what is over it and the window's modes, with the words a user would
/// call the place, whether it is a waypoint, and its edges in the one order (WG-2.4).
/// </summary>
public sealed record GraphNode(string Id, string App, string Place, string Over, IReadOnlyList<string> Modes, bool Waypoint, string Words, IReadOnlyList<GraphEdge> Edges);

/// <summary>An app the file covers: its window's kind (docs/axKit/components.json) and the node its window opens on, its first waypoint.</summary>
public sealed record GraphApp(string App, string Type, string Start);

/// <summary>
/// The graph file (WG-2, the page's section 5): the apps it covers, the controls a crawl found on view that take the
/// focus and are no place (each "app: name (role)"; the places test fails while one is listed, WG-1.1), the nodes, and
/// the goals, which step 3 of the page fills.
/// </summary>
public sealed record WorkflowGraphFile(IReadOnlyList<GraphApp> Apps, IReadOnlyList<string> Unplaced, IReadOnlyList<GraphNode> Nodes);

/// <summary>
/// The workflow graph's file, <c>docs/axKit/workflow-graph.json</c> (docs/axKit/workflow-graph.md WG-2), pure: read,
/// written in its one order, an app's crawl merged in, and the node <c>outside</c> made from the apps' own keys. The
/// crawl that finds the nodes and edges on the hidden desktop is <see cref="GraphCrawl"/>; the command line writes the
/// file under <c>--inventory write</c> (<c>tools/AxAutomate/GraphRun.cs</c>). Tested in <c>WorkflowGraphTests</c>.
/// </summary>
public static class WorkflowGraph
{
    public const string Format = "axkit-workflow-graph 1";

    /// <summary>The desktop with no axKit window in front, a node of its own (WG section 2).</summary>
    public const string Outside = "outside";

    /// <summary>What nothing is over: the place alone has the keys.</summary>
    public const string None = "none";

    /// <summary>The file's sentence about itself, so that nobody writes it by hand.</summary>
    public const string About = "The workflow graph (docs/axKit/workflow-graph.md): every place of the windows of the apps listed, with what is over it, and every action from it with where it lands. Written by tools/AxAutomate --inventory write from the windows on the hidden desktop, never by hand; docs/axKit/map.md is written from it by tools/map.ps1.";

    private static readonly JsonSerializerOptions Plain = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>A node's id: the place, what is over it, and the modes when there are any, <c>axCalc.field|none</c>.</summary>
    public static string NodeId(string place, string over, IReadOnlyList<string> modes) =>
        modes.Count == 0 ? $"{place}|{over}" : $"{place}|{over}|{string.Join(",", modes)}";

    /// <summary>An empty graph: no app covered.</summary>
    public static WorkflowGraphFile Empty { get; } = new([], [], []);

    /// <summary>The file read; null when it is not a graph of this format.</summary>
    public static WorkflowGraphFile? Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || Text(root, "format") != Format
                || !root.TryGetProperty("apps", out var apps) || apps.ValueKind != JsonValueKind.Array
                || !root.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var readApps = apps.EnumerateArray().Select(app => new GraphApp(Text(app, "app"), Text(app, "type"), Text(app, "start"))).ToList();
            var unplaced = root.TryGetProperty("unplaced", out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Select(entry => entry.GetString() ?? string.Empty).ToList()
                : [];
            var readNodes = new List<GraphNode>();
            foreach (var node in nodes.EnumerateArray())
            {
                var edges = node.TryGetProperty("edges", out var found) && found.ValueKind == JsonValueKind.Array
                    ? found.EnumerateArray().Select(edge => new GraphEdge(Text(edge, "action"), Text(edge, "command"), Text(edge, "to"), Text(edge, "kind"), Text(edge, "said"),
                        edge.TryGetProperty("free", out var free) && free.ValueKind == JsonValueKind.True)).ToList()
                    : [];
                var modes = node.TryGetProperty("modes", out var modeList) && modeList.ValueKind == JsonValueKind.Array
                    ? modeList.EnumerateArray().Select(mode => mode.GetString() ?? string.Empty).ToList()
                    : [];
                readNodes.Add(new GraphNode(Text(node, "id"), Text(node, "app"), Text(node, "place"), Text(node, "over"), modes,
                    node.TryGetProperty("waypoint", out var waypoint) && waypoint.ValueKind == JsonValueKind.True, Text(node, "words"), edges));
            }

            return new WorkflowGraphFile(readApps, unplaced, readNodes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The file in the one order (WG-2.4): the apps by name, the unplaced controls in order, the node <c>outside</c>
    /// first and then the nodes by app and by id, a node's edges by chord; one edge a line, two spaces of indentation,
    /// line feeds, ending with one. The goals are step 3's and are written empty until then.
    /// </summary>
    public static string Write(WorkflowGraphFile graph)
    {
        var text = new StringBuilder();
        text.Append("{\n  \"format\": ").Append(Quoted(Format)).Append(",\n");
        text.Append("  \"about\": ").Append(Quoted(About)).Append(",\n");
        var apps = graph.Apps.OrderBy(app => app.App, StringComparer.Ordinal).ToList();
        text.Append("  \"apps\": [");
        for (var i = 0; i < apps.Count; i++)
        {
            var app = apps[i];
            text.Append(i == 0 ? "\n" : ",\n").Append("    {\"app\": ").Append(Quoted(app.App)).Append(", \"type\": ").Append(Quoted(app.Type))
                .Append(", \"start\": ").Append(Quoted(app.Start)).Append('}');
        }

        text.Append(apps.Count == 0 ? "],\n" : "\n  ],\n");
        var unplaced = graph.Unplaced.Order(StringComparer.Ordinal).ToList();
        text.Append("  \"unplaced\": [");
        text.Append(string.Join(", ", unplaced.Select(Quoted)));
        text.Append("],\n");
        var nodes = Sorted(graph.Nodes);
        text.Append("  \"nodes\": [");
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            text.Append(i == 0 ? "\n" : ",\n");
            text.Append("    {\"id\": ").Append(Quoted(node.Id)).Append(", \"app\": ").Append(Quoted(node.App))
                .Append(", \"place\": ").Append(Quoted(node.Place)).Append(", \"over\": ").Append(Quoted(node.Over))
                .Append(", \"modes\": [").Append(string.Join(", ", node.Modes.Select(Quoted))).Append(']')
                .Append(", \"waypoint\": ").Append(node.Waypoint ? "true" : "false")
                .Append(", \"words\": ").Append(Quoted(node.Words)).Append(",\n     \"edges\": [");
            var edges = node.Edges.OrderBy(edge => edge.Action, StringComparer.Ordinal).ThenBy(edge => edge.Command, StringComparer.Ordinal).ToList();
            for (var j = 0; j < edges.Count; j++)
            {
                var edge = edges[j];
                text.Append(j == 0 ? "\n" : ",\n").Append("       {\"action\": ").Append(Quoted(edge.Action));
                if (edge.Command.Length > 0)
                {
                    text.Append(", \"command\": ").Append(Quoted(edge.Command));
                }

                text.Append(", \"to\": ").Append(Quoted(edge.To)).Append(", \"kind\": ").Append(Quoted(edge.Kind)).Append(", \"said\": ").Append(Quoted(edge.Said));
                if (edge.Free)
                {
                    text.Append(", \"free\": true");
                }

                text.Append('}');
            }

            text.Append(edges.Count == 0 ? "]}" : "\n     ]}");
        }

        text.Append(nodes.Count == 0 ? "],\n" : "\n  ],\n");
        text.Append("  \"goals\": []\n}\n");
        return text.ToString();
    }

    /// <summary>The nodes in the one order: <c>outside</c> first, then by app and by id.</summary>
    public static IReadOnlyList<GraphNode> Sorted(IEnumerable<GraphNode> nodes) =>
        nodes.OrderBy(node => node.Id == Outside ? 0 : 1)
            .ThenBy(node => node.App, StringComparer.Ordinal)
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// The graph with the apps of a run's crawls in place of what it held of them (their apps, their nodes, their
    /// unplaced controls), the other apps kept as they were, and <c>outside</c> made again from every app the result
    /// covers (<see cref="OutsideNode"/>).
    /// </summary>
    public static WorkflowGraphFile Merge(WorkflowGraphFile graph, IReadOnlyList<GraphApp> apps, IReadOnlyList<GraphNode> nodes, IReadOnlyList<string> unplaced)
    {
        var crawled = apps.Select(app => app.App).ToHashSet(StringComparer.Ordinal);
        var keptApps = graph.Apps.Where(app => !crawled.Contains(app.App)).Concat(apps).ToList();
        var keptNodes = graph.Nodes.Where(node => node.Id != Outside && !crawled.Contains(node.App)).Concat(nodes.Where(node => node.Id != Outside)).ToList();
        var keptUnplaced = graph.Unplaced.Where(entry => !crawled.Contains(AppOf(entry))).Concat(unplaced).Distinct(StringComparer.Ordinal).ToList();
        keptNodes.Add(OutsideNode(keptApps));
        return new WorkflowGraphFile(keptApps, keptUnplaced, Sorted(keptNodes));
    }

    /// <summary>
    /// The node <c>outside</c> (WG-2.3): from the desktop, each covered app's own key (<see cref="AppLaunchKeys.Default"/>,
    /// Ctrl or Ctrl+Shift with a function key, AX-8.18) lands on the node its window opens on, and is free.
    /// </summary>
    public static GraphNode OutsideNode(IReadOnlyList<GraphApp> apps)
    {
        var edges = new List<GraphEdge>();
        foreach (var key in AppLaunchKeys.Default)
        {
            if (apps.FirstOrDefault(app => app.App == key.App.Name) is { Start.Length: > 0 } app)
            {
                edges.Add(new GraphEdge(key.Chord, "Open " + key.App.Name, app.Start, string.Empty, string.Empty, Free: true));
            }
        }

        return new GraphNode(Outside, string.Empty, string.Empty, None, [], false, "the desktop, no axKit window in front", edges);
    }

    /// <summary>The app an unplaced control's entry names, before its colon.</summary>
    public static string AppOf(string unplaced) => unplaced.IndexOf(':') is var colon and > 0 ? unplaced[..colon] : unplaced;

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static string Quoted(string value) => JsonSerializer.Serialize(value, Plain);
}

using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The workflow graph's file (docs/axKit/workflow-graph.md WG-2, <see cref="WorkflowGraph"/>): one order, read as it is
/// written, an app's crawl merged in with the others kept, the node outside made from the apps' own keys; and the
/// repository's file well formed and written as the writer writes it. That it is what the windows do today is the
/// inventory run's to write (<c>--inventory write</c>), not this test's.
/// </summary>
public sealed class WorkflowGraphTests
{
    private static GraphNode Node(string id, string app, bool waypoint, params GraphEdge[] edges)
    {
        var parts = id.Split('|');
        return new GraphNode(id, app, parts[0], parts[1], [], waypoint, "the " + parts[0], edges);
    }

    [Fact]
    public void A_graph_is_written_in_one_order_and_read_back_as_it_was()
    {
        var graph = new WorkflowGraphFile(
            [new GraphApp("axDown", "editor", "axDown.text|none"), new GraphApp("axCalc", "conversation", "axCalc.field|none")],
            [],
            [
                Node("axDown.text|none", "axDown", true, new GraphEdge("f1", "Command palette", "axDown.text|palette", "field-entered", string.Empty)),
                Node("axCalc.text|none", "axCalc", false),
                Node("axCalc.field|none", "axCalc", true,
                    new GraphEdge("tab", string.Empty, "axCalc.text|none", "text-entered", string.Empty),
                    new GraphEdge("ctrl+delete", "Clear the field", "axCalc.field|none", "said", "Empty")),
            ]);

        var text = WorkflowGraph.Write(graph);
        var read = WorkflowGraph.Read(text);

        Assert.NotNull(read);
        Assert.Equal(text, WorkflowGraph.Write(read));
        // The apps by name, the nodes by app and id, the edges by chord (WG-2.4).
        Assert.Equal(["axCalc", "axDown"], read.Apps.Select(app => app.App));
        Assert.Equal(["axCalc.field|none", "axCalc.text|none", "axDown.text|none"], read.Nodes.Select(node => node.Id));
        Assert.Equal(["ctrl+delete", "tab"], read.Nodes[0].Edges.Select(edge => edge.Action));
        Assert.Equal("Clear the field", read.Nodes[0].Edges[0].Command);
        Assert.Equal(string.Empty, read.Nodes[0].Edges[1].Command);
        Assert.EndsWith("\"goals\": []\n}\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_text_of_another_format_is_no_graph()
    {
        Assert.Null(WorkflowGraph.Read("{\"format\": \"axkit-window-commands 1\", \"apps\": []}"));
        Assert.Null(WorkflowGraph.Read("not json"));
        Assert.NotNull(WorkflowGraph.Read(WorkflowGraph.Write(WorkflowGraph.Empty)));
    }

    [Fact]
    public void A_crawl_replaces_its_apps_and_keeps_the_others_and_outside_is_made_again()
    {
        var before = WorkflowGraph.Merge(WorkflowGraph.Empty,
            [new GraphApp("axCalc", "conversation", "axCalc.field|none"), new GraphApp("axDown", "editor", "axDown.text|none")],
            [Node("axCalc.field|none", "axCalc", true), Node("axCalc.text|none", "axCalc", false), Node("axDown.text|none", "axDown", true)],
            ["axCalc: Old (Text)", "axDown: Kept (List)"]);

        var after = WorkflowGraph.Merge(before, [new GraphApp("axCalc", "conversation", "axCalc.field|none")], [Node("axCalc.field|none", "axCalc", true)], []);

        Assert.Equal([WorkflowGraph.Outside, "axCalc.field|none", "axDown.text|none"], after.Nodes.Select(node => node.Id));
        Assert.Equal(["axDown: Kept (List)"], after.Unplaced);
        var outside = after.Nodes[0];
        // Each covered app's own key, free, lands where its window opens (WG-2.3, the maintainer's decision 2).
        Assert.All(outside.Edges, edge => Assert.True(edge.Free));
        Assert.Contains(outside.Edges, edge => edge.To == "axCalc.field|none" && edge.Action == AxKit.Core.AppLaunchKeys.Default.First(key => key.App.Name == "axCalc").Chord);
        Assert.Contains(outside.Edges, edge => edge.To == "axDown.text|none");
        Assert.Equal(2, outside.Edges.Count);
    }

    [Fact]
    public void A_node_id_is_its_place_and_what_is_over_it()
    {
        Assert.Equal("axCalc.field|none", WorkflowGraph.NodeId("axCalc.field", "none", []));
        Assert.Equal("axClaude.field|none|stream", WorkflowGraph.NodeId("axClaude.field", "none", ["stream"]));
        Assert.Equal("axCalc", WorkflowGraph.AppOf("axCalc: Expression (Text)"));
    }

    /// <summary>
    /// The repository's file (WG-2.1): written as the writer writes it, every node of a covered app, every landing a
    /// node of the file, every covered app's first node in the file and a waypoint of it, and a way back from
    /// everything that stands over a place (a node of something over has an edge).
    /// </summary>
    [Fact]
    public void The_repository_file_is_well_formed()
    {
        var path = Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "workflow-graph.json");
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        var graph = WorkflowGraph.Read(text);

        Assert.NotNull(graph);
        Assert.Equal(WorkflowGraph.Write(graph), text);
        var ids = graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var covered = graph.Apps.Select(app => app.App).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(WorkflowGraph.Outside, ids);
        var wrong = new List<string>();
        foreach (var node in graph.Nodes.Where(node => node.Id != WorkflowGraph.Outside))
        {
            if (!covered.Contains(node.App))
            {
                wrong.Add($"{node.Id}: of an app the file does not list");
            }

            if (node.Over != WorkflowGraph.None && node.Edges.Count == 0)
            {
                wrong.Add($"{node.Id}: something over a place with no way back");
            }
        }

        foreach (var node in graph.Nodes)
        {
            wrong.AddRange(node.Edges.Where(edge => !ids.Contains(edge.To)).Select(edge => $"{node.Id}: {edge.Action} lands on {edge.To}, no node of the file"));
        }

        foreach (var app in graph.Apps)
        {
            if (!ids.Contains(app.Start))
            {
                wrong.Add($"{app.App}: the node it opens on, {app.Start}, is no node of the file");
            }

            if (!graph.Nodes.Any(node => node.App == app.App && node.Waypoint && node.Over == WorkflowGraph.None))
            {
                wrong.Add($"{app.App}: no waypoint");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }
}

using System.Diagnostics;
using System.Text.Json;
using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>What a crawl of one app's window found: its nodes, the node it opened on, the controls of no place, and what it could not do.</summary>
public sealed record CrawlResult(string App, string Start, IReadOnlyList<GraphNode> Nodes, IReadOnlyList<string> Unplaced, IReadOnlyList<string> Notes);

/// <summary>
/// The crawl of one window on the hidden desktop (docs/axKit/workflow-graph.md WG-2.2): from a window just started as an
/// app's first script starts it, every place on view is reached (the channel's <c>goto</c>) and at each, with nothing
/// over it, every chord is tried that a command of the window would run there now (its row's <c>now</c>) and every key
/// of the bundle's no row lists (the window's <c>places</c> names them); each landing is read from the channel (the
/// place, what is over it), with the first kind of event raised and the words said. Never tried: a command marked
/// <c>act</c> (it can send, delete or run) or any chord one holds, an app's own key (the launch keys, which are
/// <c>outside</c>'s edges), New window, the keys of the Context menu (<see cref="ContextMenuKeys"/>). A key goes through the channel's
/// <c>press</c>, which does not wait for what the key opens: a Windows dialog (Open, Save as) holds the window's thread
/// in its own loop, still answering the channel, so its landing reads <c>dialog</c>, and <c>dismiss</c> closes it as
/// its Cancel does, written as the Escape edge it is for a user. Where an action lands on something over a place, Escape is tried there once,
/// which is that node's edge. Between two tries the window is brought back: Escape while something is over it, the
/// place by <c>goto</c>, and a fresh start when neither does. A place that comes on view only after an action is
/// crawled in its turn. Content moves (the arrows, Home and End, the paragraph keys) are no edges (WG section 3).
/// </summary>
public static class GraphCrawl
{
    /// <summary>How long a window may stay quiet after a press before its landing is read.</summary>
    private const int QuietMs = 400;

    /// <summary>How long a landing is waited for at most.</summary>
    private const int SettleMs = 3000;

    /// <summary>How many fresh starts one crawl may make before it stops: a window that keeps needing one is reported, not crawled for ever.</summary>
    private const int MaxRestarts = 40;

    /// <summary>The longest words of an edge, so that a long text read out is not the map.</summary>
    private const int SaidLength = 200;

    /// <summary>
    /// The keys of the Context menu (the bundle's Ctrl+Period, Windows' Shift+F10 and Applications key): it is Windows'
    /// own popup menu, which holds the window's thread until it closes and never closes on the hidden desktop (axStart's
    /// field did not answer for 30 s on 2026-11-26), as menu mode needs the foreground (docs/axKit/automation.md,
    /// "What stays manual"). Its edges come from the scripts' walks (WG-2.2, step 4 of the page).
    /// </summary>
    private static readonly HashSet<string> ContextMenuKeys = new(["ctrl+period", "shift+f10", "apps"], StringComparer.Ordinal);

    /// <summary>
    /// Crawls the run's current window, the app's, started by <paramref name="startLine"/> (a script's <c>start</c>
    /// line, filled), which a fresh start repeats. <paramref name="temp"/> and the profile folder are written
    /// <c>{temp}</c> and <c>{home}</c> in the words.
    /// </summary>
    public static CrawlResult Crawl(Run run, string app, string startLine, string temp, Action<string> say)
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var notes = new List<string>();
        var nodes = new Dictionary<string, (string Place, string Over, List<GraphEdge> Edges)>(StringComparer.Ordinal);
        var unplaced = new SortedSet<string>(StringComparer.Ordinal);
        var words = new Dictionary<string, (string Words, bool Waypoint)>(StringComparer.Ordinal);
        var launchKeys = AppLaunchKeys.Default.Select(key => key.Chord).ToHashSet(StringComparer.Ordinal);
        var restarts = 0;
        var baseline = new Dictionary<string, string>(StringComparer.Ordinal);

        Settle(run);
        var (startPlace, startOver, _) = Landing(run);
        if (startPlace is null)
        {
            return new CrawlResult(app, string.Empty, [], [], [$"{app}: the window named no place as it opened; nothing crawled"]);
        }

        var start = WorkflowGraph.NodeId(startPlace, startOver, []);
        var bundle = ReadPlaces(run, words, unplaced, app);
        var queue = new List<string> { startPlace };
        queue.AddRange(words.Keys.Where(place => place != startPlace && Shown(run, place)).Order(StringComparer.Ordinal));
        for (var index = 0; index < queue.Count; index++)
        {
            var place = queue[index];
            if (!Reach(run, place, startLine, ref restarts, notes))
            {
                notes.Add($"{app}: {place} could not be reached; its edges are not in the file");
                continue;
            }

            ReadPlaces(run, words, unplaced, app);
            var node = WorkflowGraph.NodeId(place, WorkflowGraph.None, []);
            var edges = Node(nodes, node, place, WorkflowGraph.None);
            if (CommandListing.Read(run.Ask("commands data")) is not { } listing)
            {
                notes.Add($"{app}: {place} listed no commands");
                continue;
            }

            // The chords to try here, each once: a command's that acts now, then the bundle's no row lists. Never a chord
            // of a command marked act, wherever the window lists it; never an app's own key; never New window.
            var never = listing.Commands.Where(row => row.Mark == "act" || CommandNames.Plain(row.Label) == "New window")
                .SelectMany(row => row.Also.Prepend(row.Key).Append(row.Now)).Where(chord => chord.Length > 0).ToHashSet(StringComparer.Ordinal);
            var tries = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in listing.Commands.Where(row => row.Now.Length > 0 && row.Mark != "act"))
            {
                tries.TryAdd(row.Now, CommandNames.Plain(row.Label));
            }

            foreach (var chord in bundle)
            {
                tries.TryAdd(chord, string.Empty);
            }

            foreach (var (chord, command) in tries)
            {
                if (never.Contains(chord) || launchKeys.Contains(chord) || ContextMenuKeys.Contains(chord) || restarts > MaxRestarts)
                {
                    continue;
                }

                if (!Reach(run, place, startLine, ref restarts, notes))
                {
                    break;
                }

                // The window as the place first had it: a status bar that says otherwise (the field left in navigation
                // mode by Ctrl+Space, a line moved to) means an earlier try changed what the next key would do, and a
                // fresh start puts it back. A status that differs even so (a clock) is taken as the place's own.
                var status = Status(run);
                if (!baseline.TryGetValue(place, out var first))
                {
                    baseline[place] = status;
                }
                else if (status != first)
                {
                    if (!Restart(run, startLine, ref restarts) || !Reach(run, place, startLine, ref restarts, notes))
                    {
                        break;
                    }

                    baseline[place] = Status(run);
                }

                run.TakeEvents();
                run.Ask("events");
                var pressed = run.Ask("press " + chord);
                if (pressed.Contains("\"error\"", StringComparison.Ordinal))
                {
                    notes.Add($"{app}: {chord} at {place} could not be pressed: {pressed}");
                    continue;
                }

                var (said, kind) = Settle(run);
                var (landPlace, landOver, gone) = Landing(run);
                var to = gone || landPlace is null ? WorkflowGraph.Outside : WorkflowGraph.NodeId(landPlace, landOver, []);
                edges.Add(new GraphEdge(chord, command, to, kind, Clean(said, temp, profile)));
                if (gone || landPlace is null)
                {
                    // Hidden or ended (a hotkey window's Escape): a fresh start brings the window back for the next try.
                    Restart(run, startLine, ref restarts);
                    continue;
                }

                if (landOver != WorkflowGraph.None && !nodes.ContainsKey(to))
                {
                    // Escape once where the action landed: the over node's way back (WG-3.1).
                    var overEdges = Node(nodes, to, landPlace, landOver);
                    run.TakeEvents();
                    run.Ask("events");
                    run.Ask(landOver == "dialog" ? "dismiss" : "press escape");
                    var (backSaid, backKind) = Settle(run);
                    var (backPlace, backOver, backGone) = Landing(run);
                    overEdges.Add(new GraphEdge("escape", string.Empty, backGone || backPlace is null ? WorkflowGraph.Outside : WorkflowGraph.NodeId(backPlace, backOver, []),
                        backKind, Clean(backSaid, temp, profile)));
                    if (backGone || backPlace is null)
                    {
                        Restart(run, startLine, ref restarts);
                    }
                }
                else if (landOver == WorkflowGraph.None && !queue.Contains(landPlace))
                {
                    // A place that came on view by this action: crawled in its turn.
                    queue.Add(landPlace);
                }
            }
        }

        if (restarts > MaxRestarts)
        {
            notes.Add($"{app}: the window needed more than {MaxRestarts} fresh starts; the crawl stopped early");
        }

        var graphNodes = nodes.Select(entry => new GraphNode(entry.Key, app, entry.Value.Place, entry.Value.Over, [],
                entry.Value.Over == WorkflowGraph.None && (entry.Key == start || words.GetValueOrDefault(entry.Value.Place).Waypoint),
                words.TryGetValue(entry.Value.Place, out var found) ? found.Words : string.Empty, entry.Value.Edges))
            .ToList();
        say($"{app}: {graphNodes.Count} node(s), {graphNodes.Sum(node => node.Edges.Count)} edge(s), {restarts} fresh start(s)");
        return new CrawlResult(app, start, graphNodes, [.. unplaced], notes);
    }

    private static List<GraphEdge> Node(Dictionary<string, (string Place, string Over, List<GraphEdge> Edges)> nodes, string id, string place, string over)
    {
        if (!nodes.TryGetValue(id, out var node))
        {
            node = (place, over, []);
            nodes[id] = node;
        }

        return node.Edges;
    }

    /// <summary>The window's places into <paramref name="words"/>, its controls of no place into <paramref name="unplaced"/>, and the bundle's chords it names.</summary>
    private static IReadOnlyList<string> ReadPlaces(Run run, Dictionary<string, (string Words, bool Waypoint)> words, SortedSet<string> unplaced, string app)
    {
        try
        {
            using var document = JsonDocument.Parse(run.Ask("places"));
            var root = document.RootElement;
            if (root.TryGetProperty("places", out var places))
            {
                foreach (var place in places.EnumerateArray())
                {
                    words[place.GetProperty("id").GetString()!] = (place.GetProperty("words").GetString() ?? string.Empty, place.GetProperty("waypoint").GetBoolean());
                }
            }

            if (root.TryGetProperty("unplaced", out var controls))
            {
                foreach (var control in controls.EnumerateArray())
                {
                    unplaced.Add($"{app}: {control.GetString()}");
                }
            }

            return root.TryGetProperty("bundle", out var bundle) ? [.. bundle.EnumerateArray().Select(chord => chord.GetString() ?? string.Empty).Where(chord => chord.Length > 0)] : [];
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return [];
        }
    }

    /// <summary>Whether a place is on view now.</summary>
    private static bool Shown(Run run, string place)
    {
        try
        {
            using var document = JsonDocument.Parse(run.Ask("places"));
            return document.RootElement.GetProperty("places").EnumerateArray()
                .Any(entry => entry.GetProperty("id").GetString() == place && entry.GetProperty("shown").GetBoolean());
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>The status bar's texts, from the channel's state, as one text; empty when they cannot be read.</summary>
    private static string Status(Run run)
    {
        try
        {
            using var document = JsonDocument.Parse(run.Ask("state"));
            return document.RootElement.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Array
                ? string.Join(" | ", status.EnumerateArray().Select(text => text.GetString()))
                : string.Empty;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
        {
            return string.Empty;
        }
    }

    /// <summary>The place, what is over it, and whether the window is gone (ended or hidden), from the channel's state.</summary>
    private static (string? Place, string Over, bool Gone) Landing(Run run)
    {
        if (run.CurrentEnded)
        {
            return (null, WorkflowGraph.None, true);
        }

        try
        {
            using var document = JsonDocument.Parse(run.Ask("state"));
            var root = document.RootElement;
            if (root.TryGetProperty("error", out _))
            {
                return (null, WorkflowGraph.None, true);
            }

            var place = root.TryGetProperty("place", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            var over = root.TryGetProperty("over", out var o) && o.ValueKind == JsonValueKind.String ? o.GetString()! : WorkflowGraph.None;
            var visible = !root.TryGetProperty("visible", out var v) || v.ValueKind != JsonValueKind.False;
            return (place, over, !visible);
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
        {
            return (null, WorkflowGraph.None, true);
        }
    }

    /// <summary>
    /// Waits until the window has been quiet for a moment after a press, at most <see cref="SettleMs"/>: what it said
    /// meanwhile (its announcements, joined) and the first kind of event it raised.
    /// </summary>
    private static (string Said, string Kind) Settle(Run run)
    {
        var said = new List<string>();
        var clock = Stopwatch.StartNew();
        var lastNews = 0L;
        Thread.Sleep(150);
        while (clock.ElapsedMilliseconds < SettleMs && !run.CurrentEnded)
        {
            List<Event> events;
            try
            {
                events = run.TakeEvents();
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                break;
            }

            if (events.Count > 0)
            {
                lastNews = clock.ElapsedMilliseconds;
                said.AddRange(events.Where(e => e.Kind == "say").Select(e => e.Text.Trim()).Where(text => text.Length > 0));
            }
            else if (clock.ElapsedMilliseconds - lastNews >= QuietMs)
            {
                break;
            }

            Thread.Sleep(80);
        }

        var kind = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(run.CurrentEnded ? "{}" : run.Ask("events"));
            if (document.RootElement.TryGetProperty("events", out var kinds) && kinds.GetArrayLength() > 0)
            {
                kind = kinds[0].GetString() ?? string.Empty;
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
        {
            // No kind to say: the edge keeps its words.
        }

        return (string.Join(" ", said), kind);
    }

    /// <summary>
    /// The window brought back to a place with nothing over it: Escape while something is, then the place by its id; a
    /// fresh start when that does not do it. False when the place cannot be reached even so.
    /// </summary>
    private static bool Reach(Run run, string place, string startLine, ref int restarts, List<string> notes)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            for (var i = 0; i < 3; i++)
            {
                var (_, over, gone) = Landing(run);
                if (gone)
                {
                    break;
                }

                if (over == WorkflowGraph.None)
                {
                    break;
                }

                run.Ask(over == "dialog" ? "dismiss" : "keys escape");
                Settle(run);
            }

            var (now, nowOver, nowGone) = Landing(run);
            if (!nowGone && nowOver == WorkflowGraph.None)
            {
                if (now == place)
                {
                    return true;
                }

                if (!run.Ask("goto " + place).Contains("\"error\"", StringComparison.Ordinal))
                {
                    Settle(run);
                    var (moved, movedOver, _) = Landing(run);
                    if (moved == place && movedOver == WorkflowGraph.None)
                    {
                        return true;
                    }
                }
            }

            if (attempt == 0 && !Restart(run, startLine, ref restarts))
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>The window ended and started afresh by its start line; false past the crawl's limit of fresh starts.</summary>
    private static bool Restart(Run run, string startLine, ref int restarts)
    {
        if (++restarts > MaxRestarts)
        {
            return false;
        }

        run.EndCurrent();
        var (ok, _) = run.Step(startLine);
        if (ok)
        {
            Settle(run);
        }

        return ok;
    }

    /// <summary>The words as the file writes them: the run's folder and the profile as <c>{temp}</c> and <c>{home}</c>, redacted, cut at <see cref="SaidLength"/>.</summary>
    private static string Clean(string said, string temp, string profile)
    {
        var clean = Traces.Clean(said.ReplaceLineEndings(" "), temp, profile);
        return clean.Length <= SaidLength ? clean : clean[..SaidLength].TrimEnd() + "...";
    }
}

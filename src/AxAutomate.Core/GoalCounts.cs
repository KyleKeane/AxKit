using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// One goal's own actions (docs/axKit/workflow-graph.md WG-3.3): its workflow's label, its title, its script's path from
/// the repository's root, the count, the counted actions in words, one entry an action, and the steps passed over as
/// the example's text, in words.
/// </summary>
public sealed record CountedGoal(string Workflow, string Goal, string Script, int Actions, IReadOnlyList<string> Steps, IReadOnlyList<string> Example)
{
    /// <summary>The goal's folder under <c>tests/scripts</c>, its app ("axDown"; "mcp" for the AI tools' scripts).</summary>
    public string App => GoalCounts.AppOf(Script);
}

/// <summary>A goal as the committed baseline holds it (<c>docs/axKit/goal-counts.json</c>, WG-3.4): its workflow, title, script and count.</summary>
public sealed record GoalCount(string Workflow, string Goal, string Script, int Actions);

/// <summary>
/// The count of a goal's own actions (docs/axKit/workflow-graph.md WG-3.3, SPEC.md G7), pure, and the baseline of the
/// counts, <c>docs/axKit/goal-counts.json</c> (WG-3.4): read, written in its one order, what a write keeps of it, and
/// what the check names. Every <c># Goal:</c> section of a script is counted (a script without a goal line is one
/// goal), never a <c># Check:</c> section or the setup. The rule, in WG-3.3's words: a <c>keys</c> press is one action,
/// a <c>type</c> one, a <c>do</c> or a <c>menu</c> one when its command has a key and two when it has none (the palette:
/// F1, then its name typed with Enter), <c>close</c> one (Alt+F4), <c>template</c> one; <c>start</c>, <c>launch</c>,
/// <c>adopt</c>, <c>use</c> and <c>mcp call</c> are free (an app's own key, a window coming up, Claude's call); every
/// other step only prepares or looks, and a step after <c>iffound</c> answers what may come, so none of them counts.
/// Joined into one: a typed text and the Enter right after it (a typed name with its Enter), and a run of moves inside a
/// place (the arrows, Home, End, Page Up and Page Down, with Ctrl or Shift or neither) with the Enter right after it (a
/// choice in a list). The example: a goal whose first action is a <c>type</c> and in which a command comes after the
/// typing (a <c>do</c>, a <c>menu</c>, or a chord with Ctrl, Alt or a function key) types the example's text first;
/// those first steps, the typing and the typing keys between (Enter, Tab, Space, Backspace, Delete, letters and digits),
/// are not counted. The command line writes the page and the file (<c>tools/AxAutomate --bounds</c>, BoundsRun);
/// tested in <c>GoalCountsTests</c>.
/// </summary>
public static class GoalCounts
{
    public const string Format = "axkit-goal-counts 1";

    /// <summary>The most actions a goal may take (G7); the ideal is one fewer.</summary>
    public const int Bound = 4;

    /// <summary>The file's sentence about itself, so that nobody writes it by hand.</summary>
    public const string About = "The count of each goal's own actions (docs/axKit/workflow-graph.md WG-3.3), the baseline the check holds the scripts to: a goal may get shorter, never longer, and one within four actions never goes over them (WG-3.4). Written by tools/graph-check.ps1, which holds a count that grew; -Write takes it in, at the maintainer's word.";

    /// <summary>The folder of the scripts counted, from the repository's root.</summary>
    public const string ScriptsFolder = "tests/scripts";

    private static readonly JsonSerializerOptions Plain = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>The most characters of a typed text a step's words quote.</summary>
    private const int Quoted = 30;

    /// <summary>The keys that move inside a place: the user's own content, a line, an item, an answer.</summary>
    private static readonly HashSet<string> MoveKeys = new(StringComparer.Ordinal) { "up", "down", "left", "right", "home", "end", "pageup", "pagedown" };

    /// <summary>The keys that type, besides the letters and the digits.</summary>
    private static readonly HashSet<string> TypingKeys = new(StringComparer.Ordinal) { "enter", "tab", "space", "backspace", "delete" };

    /// <summary>The words that cost nothing: an app's own key, a window coming up or taken, Claude's call.</summary>
    private static readonly HashSet<string> FreeWords = new(StringComparer.Ordinal) { "start", "launch", "adopt", "use", "mcp" };

    /// <summary>One step of a goal as the count sees it: what it is, its cost, its words, and for a press its chord.</summary>
    private sealed record Act(ActKind Kind, int Cost, string Words, string Chord = "");

    private enum ActKind { Type, Press, Move, Typing, Command }

    /// <summary>
    /// The goals of one script counted (<paramref name="path"/> from the repository's root, with forward slashes): its
    /// goal sections in order, the checks left out. The inventory gives a <c>do</c> and a <c>menu</c> their key.
    /// </summary>
    public static IReadOnlyList<CountedGoal> Of(string path, IReadOnlyList<string> lines, IReadOnlyList<InventoryApp> inventory)
    {
        var workflow = ScriptReader.Parse(lines, System.IO.Path.GetFileNameWithoutExtension(path), path);
        var apps = ScriptApps.ByLine(lines);
        var goals = new List<CountedGoal>();
        foreach (var goal in workflow.Goals.Where(goal => !goal.Check))
        {
            var acts = new List<Act>();
            for (var number = goal.FirstLine; number <= goal.LastLine && number <= lines.Count; number++)
            {
                acts.AddRange(Acts(lines[number - 1], apps[number - 1], inventory));
            }

            var example = ExampleLength(acts);
            var counted = Joined(acts.Skip(example).ToList());
            goals.Add(new CountedGoal(workflow.Title, goal.Title, path, counted.Sum(act => act.Cost),
                [.. counted.Select(act => act.Words)], [.. acts.Take(example).Select(act => act.Words)]));
        }

        return goals;
    }

    /// <summary>What a script's line comes to: no act for a step that is free, prepares or looks, one act a press of a <c>keys</c> step.</summary>
    private static IEnumerable<Act> Acts(string line, string? app, IReadOnlyList<InventoryApp> inventory)
    {
        var text = line.Trim();
        if (text.Length == 0 || text.StartsWith('#') || text.StartsWith("iffound ", StringComparison.Ordinal) || !ScriptReader.IsInput(text))
        {
            yield break;
        }

        var space = text.IndexOf(' ');
        var word = space < 0 ? text : text[..space];
        var rest = space < 0 ? string.Empty : text[(space + 1)..].Trim();
        if (FreeWords.Contains(word))
        {
            yield break;
        }

        switch (word)
        {
            case "keys":
                foreach (var press in rest.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    yield return Press(press, app, inventory);
                }

                break;
            case "type":
                yield return new Act(ActKind.Type, 1, $"Type \"{Cut(rest)}\"");
                break;
            case "do":
                CommandInventory.Resolve(inventory, app, rest, out var row);
                yield return Command(rest, row);
                break;
            case "menu":
                yield return Command(rest, CommandInventory.OfMenuText(inventory, app, rest), menu: true);
                break;
            case "close":
                yield return new Act(ActKind.Command, 1, $"Close the window ({Reference.Keys("alt+f4")})");
                break;
            default:
                yield return new Act(ActKind.Command, 1, Cut(text));
                break;
        }
    }

    /// <summary>
    /// A command chosen by name: one action when its row has a key (a chord, or a key text such as "K in the
    /// conversation"), two through the palette when it has none (F1, then its name typed with Enter). A <c>do</c> whose name the inventory does not find is taken to have its key, since
    /// the step presses one; a <c>menu</c> step whose item has no row is taken to go through the palette.
    /// </summary>
    private static Act Command(string name, ListedCommand? row, bool menu = false)
    {
        var key = row is null ? string.Empty
            : row.Key.Length > 0 ? (row.KeyText.Length > 0 ? row.KeyText : Reference.Keys(row.Key))
            : row.Also.Count > 0 ? Reference.Keys(row.Also[0])
            : row.KeyText;
        if (key.Length > 0)
        {
            return new Act(ActKind.Command, 1, $"{name} ({key})");
        }

        return row is null && !menu ? new Act(ActKind.Command, 1, name) : new Act(ActKind.Command, 2, $"{name} (F1, its name and Enter: 2)");
    }

    private static Act Press(string press, string? app, IReadOnlyList<InventoryApp> inventory)
    {
        var words = Reference.Keys(press);
        var parsed = KeyChord.Parse(press);
        var parts = press.ToLowerInvariant().Split('+');
        var key = parts[^1];
        var ctrl = parts.Contains("ctrl");
        var alt = parts.Contains("alt");
        if (!alt && MoveKeys.Contains(key))
        {
            return new Act(ActKind.Move, 1, words, press);
        }

        if (!ctrl && !alt && (TypingKeys.Contains(key) || (key.Length == 1 && char.IsLetterOrDigit(key[0]))))
        {
            return new Act(ActKind.Typing, 1, words, press);
        }

        // The command a chord runs, when one row of the app holds it: "Ctrl+F (Find)".
        if (CommandInventory.OfChord(inventory, app, press) is { } row)
        {
            words += $" ({row.Label})";
        }

        return new Act(KeyChord.IsCommandChord(press) || parsed is null ? ActKind.Command : ActKind.Press, 1, words, press);
    }

    /// <summary>
    /// How many first acts are the example's text: when the first is a <c>type</c>, the run of typing and typing keys
    /// from the start, if a command comes after it; else none.
    /// </summary>
    private static int ExampleLength(IReadOnlyList<Act> acts)
    {
        if (acts.Count == 0 || acts[0].Kind != ActKind.Type)
        {
            return 0;
        }

        var length = 0;
        while (length < acts.Count && acts[length].Kind is ActKind.Type or ActKind.Typing)
        {
            length++;
        }

        return acts.Skip(length).Any(act => act.Kind == ActKind.Command) ? length : 0;
    }

    /// <summary>The acts with a typed text and its Enter joined, and a run of moves with the Enter after it.</summary>
    private static List<Act> Joined(IReadOnlyList<Act> acts)
    {
        var joined = new List<Act>();
        for (var i = 0; i < acts.Count; i++)
        {
            var act = acts[i];
            if (act.Kind == ActKind.Move)
            {
                var words = new List<string> { act.Words };
                while (i + 1 < acts.Count && acts[i + 1].Kind == ActKind.Move)
                {
                    words.Add(acts[++i].Words);
                }

                act = new Act(ActKind.Move, 1, words.Count == 1 ? act.Words : string.Join(", ", words) + " (one move)");
            }

            if (act.Kind is ActKind.Type or ActKind.Move && i + 1 < acts.Count && IsEnter(acts[i + 1]))
            {
                act = act with { Words = act.Words + (act.Kind == ActKind.Type ? " and Enter" : ", Enter") };
                i++;
            }

            joined.Add(act);
        }

        return joined;
    }

    private static bool IsEnter(Act act) => act.Kind == ActKind.Typing && string.Equals(act.Chord, "enter", StringComparison.OrdinalIgnoreCase);

    private static string Cut(string text) => text.Length <= Quoted ? text : text[..Quoted] + "...";

    /// <summary>The folder of a script's path under <see cref="ScriptsFolder"/>: its app.</summary>
    public static string AppOf(string script)
    {
        var rest = script.StartsWith(ScriptsFolder + "/", StringComparison.Ordinal) ? script[(ScriptsFolder.Length + 1)..] : script;
        var slash = rest.IndexOf('/');
        return slash < 0 ? string.Empty : rest[..slash];
    }

    /// <summary>A goal's key in the baseline: its workflow's label and its title, as the workflow record knows a goal.</summary>
    private static string Key(string workflow, string goal) => workflow + "\n" + goal;

    /// <summary>
    /// What a write without <c>-Write</c> puts in the baseline: every goal counted, but a goal whose count grew keeps the
    /// count the baseline had, and a goal the baseline lacks is left out while it is over four, so that the check goes
    /// on naming both until the maintainer takes them in (WG-3.4). A goal no script has any more is dropped.
    /// </summary>
    public static IReadOnlyList<GoalCount> Held(IReadOnlyList<CountedGoal> counted, IReadOnlyList<GoalCount> baseline)
    {
        var before = ByKey(baseline);
        var held = new List<GoalCount>();
        foreach (var goal in counted)
        {
            if (before.TryGetValue(Key(goal.Workflow, goal.Goal), out var was))
            {
                held.Add(new GoalCount(goal.Workflow, goal.Goal, goal.Script, Math.Min(goal.Actions, was.Actions)));
            }
            else if (goal.Actions <= Bound)
            {
                held.Add(Entry(goal));
            }
        }

        return held;
    }

    /// <summary>Every goal as counted, for the baseline written with <c>-Write</c>.</summary>
    public static IReadOnlyList<GoalCount> Taken(IReadOnlyList<CountedGoal> counted) => [.. counted.Select(Entry)];

    /// <summary>
    /// What the check names against the baseline (WG-3.4), one line a goal: a goal within four that is over four now, a
    /// goal whose count grew, and a goal the baseline lacks that is over four.
    /// </summary>
    public static IReadOnlyList<string> Grown(IReadOnlyList<CountedGoal> counted, IReadOnlyList<GoalCount> baseline)
    {
        var before = ByKey(baseline);
        var lines = new List<string>();
        foreach (var goal in counted)
        {
            var name = $"{goal.App}, {goal.Workflow}, {goal.Goal} ({goal.Script})";
            if (!before.TryGetValue(Key(goal.Workflow, goal.Goal), out var was))
            {
                if (goal.Actions > Bound)
                {
                    lines.Add($"{name}: a new goal of {goal.Actions} actions, over four.");
                }
            }
            else if (goal.Actions > Bound && was.Actions <= Bound)
            {
                lines.Add($"{name}: {goal.Actions} actions, over four; it took {was.Actions}.");
            }
            else if (goal.Actions > was.Actions)
            {
                lines.Add($"{name}: {goal.Actions} actions; it took {was.Actions}.");
            }
        }

        return lines;
    }

    private static Dictionary<string, GoalCount> ByKey(IReadOnlyList<GoalCount> baseline)
    {
        var byKey = new Dictionary<string, GoalCount>(StringComparer.Ordinal);
        foreach (var goal in baseline)
        {
            byKey.TryAdd(Key(goal.Workflow, goal.Goal), goal);
        }

        return byKey;
    }

    private static GoalCount Entry(CountedGoal goal) => new(goal.Workflow, goal.Goal, goal.Script, goal.Actions);

    /// <summary>The baseline read; null when it is not a file of this format.</summary>
    public static IReadOnlyList<GoalCount>? Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || Text(root, "format") != Format
                || !root.TryGetProperty("goals", out var goals) || goals.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return
            [
                .. goals.EnumerateArray().Select(goal => new GoalCount(Text(goal, "workflow"), Text(goal, "goal"), Text(goal, "script"),
                    goal.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Number ? actions.GetInt32() : 0)),
            ];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The baseline in its one order: by script, ordinal, and a script's goals in its order; one goal a line, two spaces
    /// of indentation, line feeds, ending with one.
    /// </summary>
    public static string Write(IReadOnlyList<GoalCount> goals)
    {
        var text = new StringBuilder();
        text.Append("{\n  \"format\": ").Append(Quote(Format)).Append(",\n");
        text.Append("  \"about\": ").Append(Quote(About)).Append(",\n");
        text.Append("  \"goals\": [");
        var ordered = goals.Select((goal, index) => (goal, index)).OrderBy(entry => entry.goal.Script, StringComparer.Ordinal).ThenBy(entry => entry.index).Select(entry => entry.goal).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var goal = ordered[i];
            text.Append(i == 0 ? "\n" : ",\n").Append("    {\"workflow\": ").Append(Quote(goal.Workflow)).Append(", \"goal\": ").Append(Quote(goal.Goal))
                .Append(", \"script\": ").Append(Quote(goal.Script)).Append(", \"actions\": ").Append(goal.Actions).Append('}');
        }

        text.Append(ordered.Count == 0 ? "]\n}\n" : "\n  ]\n}\n");
        return text.ToString();
    }

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static string Quote(string value) => JsonSerializer.Serialize(value, Plain);
}
